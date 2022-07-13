using Rhino;
using Rhino.PlugIns;
using System;
using RhinoWindows.Controls;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace RhDockableWebView2
{
  ///<summary>
  /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
  /// class. DO NOT create instances of this class yourself. It is the
  /// responsibility of Rhino to create an instance of this class.</para>
  /// <para>To complete plug-in information, please also see all PlugInDescription
  /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
  /// "Show All Files" to see it in the "Solution Explorer" window).</para>
  ///</summary>
  public class RhDockableWebView2Plugin : Rhino.PlugIns.PlugIn
  {
    private WV2DockBar WV2DockBar;

    public RhDockableWebView2Plugin()
    {
      Instance = this;
    }

    public static RhDockableWebView2Plugin Instance { get; private set; }

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
      CreateDockBar();
      return base.OnLoad(ref errorMessage);
    }

    private void CreateDockBar()
    {
      var createOptions = new DockBarCreateOptions
      {
        DockLocation = DockBarDockLocation.Right,
        Visible = true,
        DockStyle = DockBarDockStyle.Any,
        FloatPoint = new System.Drawing.Point(100, 100)
      };

      WV2DockBar = new WV2DockBar();
      WV2DockBar.Create(createOptions);
    }
  }

  public class WV2DockBar : DockBar
  {
    public static Guid BarId => new Guid("{c520731e-376a-4d82-975a-403664fca2fc}");

    public static DockPage DockPage;

    private bool SelectionExpired = false;

    private bool ExistingSchemaLogExpired = false;

    private RhinoRevitMappings RevitMappings;

    private SpeckleDisplayConduit Display;

    public WV2DockBar() : base(RhDockableWebView2Plugin.Instance, BarId, "WebView2")
    {
      if (DockPage == null)
      {
        DockPage = new DockPage(this);
      }

      // TODO: Generalize for other BIM software mappings, and pass to the ui
      // This would require another abstraction layer in the ui & schema assignments, but I'll leave it as a next step
      RevitMappings = new RhinoRevitMappings();
      Display = new SpeckleDisplayConduit();
      Display.Enabled = true;
      SetContentControl(new WpfHost(DockPage, null));
      RegisterRhinoEvents();
    }

    private void RegisterRhinoEvents()
    {
      RhinoDoc.ActiveDocumentChanged += RhinoDoc_ActiveDocumentChanged;
      RhinoDoc.SelectObjects += (sender, e) => SelectionExpired = true;
      RhinoDoc.DeselectObjects += (sender, e) => SelectionExpired = true;
      RhinoDoc.DeselectAllObjects += (sender, e) => SelectionExpired = true;

      RhinoApp.Idle += RhinoApp_Idle;
    }

    public void SendToBrowser(string eventName, string eventInfo = "")
    {
      var script = string.Format("window.Interop.$emit('{0}', '{1}')", eventName, eventInfo);
      try
      {
        DockPage.myWebView.ExecuteScriptAsync(script);
      }
      catch (Exception e)
      {
        // TODO: report any browser errors
      }
    }

    public void ReceiveFromBrowser(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
      var json = e.WebMessageAsJson;
      var message = JsonSerializer.Deserialize<JsonElement>(json);
      var action = message.GetProperty("action").GetString();
      switch (action)
      {
        case "set-schema":
          var setSchemaAction = JsonSerializer.Deserialize<SetSchemaAction>(json);
          foreach (var objId in setSchemaAction.objectIds)
          {
            var obj = RhinoDoc.ActiveDoc.Objects.FindId(new Guid(objId));
            setSchemaAction.schema["objectId"] = objId; // NOTE: storing the object id inside so it's easier to pass back to ui for (later) hovering/selecting
            obj.Attributes.SetUserString("schema", JsonSerializer.Serialize(setSchemaAction.schema));
          }
          ExistingSchemaLogExpired = true;
          break;
        case "clear-schema-all":
          var existingObjects = RhinoDoc.ActiveDoc.Objects.FindByUserString("schema", "*", false);
          foreach (var obj in existingObjects)
          {
            obj.Attributes.DeleteUserString("schema");
          }
          ExistingSchemaLogExpired = true;
          break;
        case "set-hover":
          var hoverAction = JsonSerializer.Deserialize<SetHoverAction>(json);
          Display.ObjectIds = hoverAction.objectIds;
          RhinoDoc.ActiveDoc?.Views.Redraw();
          break;
        case "set-select":
          var selectAction = JsonSerializer.Deserialize<SetHoverAction>(json);
          RhinoDoc.ActiveDoc.Objects.UnselectAll();
          RhinoDoc.ActiveDoc.Objects.Select(selectAction.objectIds.Select(id => new Guid(id)));
          RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ZoomExtentsSelected();
          break;
        default:
          break;
      }
    }

    private void RhinoApp_Idle(object sender, EventArgs e)
    {
      if (SelectionExpired)
      {
        SelectionExpired = false;
        ExistingSchemaLogExpired = true;
        SendToBrowser("object-selection", GetSelectionInfo());
      }

      if (ExistingSchemaLogExpired)
      {
        ExistingSchemaLogExpired = false;
        SendToBrowser("object-schemas", GetExistingElements());
      }
    }

    private void RhinoDoc_DeselectObjects(object sender, Rhino.DocObjects.RhinoObjectSelectionEventArgs e)
    {
      SelectionExpired = true;
    }

    private void RhinoDoc_SelectObjects(object sender, Rhino.DocObjects.RhinoObjectSelectionEventArgs e)
    {
      SelectionExpired = true;
    }

    private void RhinoDoc_ActiveDocumentChanged(object sender, DocumentEventArgs e)
    {
      SelectionExpired = true;
      // TODO: Parse new doc for existing stuff
    }

    public string GetSelectionInfo()
    {
      RevitMappings = new RhinoRevitMappings(); // NOTE: hack for refreshing shit during live debugging.

      var selection = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).ToList();

      var viableSchemas = RevitMappings.GetSelectionSchemas(selection);
      var objIds = selection.ToList().Select(obj => obj.Id.ToString()).ToList();
      var existingSchemas = selection.ToList().Select(obj =>
      {
        var schema = obj.Attributes.GetUserString("schema");
        if (schema!=null)
        {
          return new
          {
            id = obj.Id.ToString(),
            schema = JsonSerializer.Deserialize<Dictionary<string, object>>(schema)
          };
        }
        return null;
      }).Where(o => o!=null);

      var selectionInfo = new
      {
        schemas = viableSchemas,
        objIds = objIds,
        existingSchemas = existingSchemas
      };

      var serialized = JsonSerializer.Serialize<object>(selectionInfo);
      return serialized;
    }

    public string GetExistingElements()
    {
      var existingObjects =
        RhinoDoc.ActiveDoc.Objects.FindByUserString("schema", "*", false)
        .Select(obj => JsonSerializer.Deserialize<Dictionary<string, object>>(obj.Attributes.GetUserString("schema")));

      return JsonSerializer.Serialize(existingObjects);
    }

  }

  public class SetSchemaAction
  {
    public string objectId { get; set; }
    public string action { get; set; }
    public List<string> objectIds { get; set; }
    public Dictionary<string, object> schema { get; set; }
  }

  public class SetHoverAction
  {
    public string action { get; set; }
    public List<string> objectIds { get; set; }
  }


}