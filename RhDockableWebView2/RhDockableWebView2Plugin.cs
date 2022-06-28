using Rhino;
using Rhino.PlugIns;
using System;
using RhinoWindows.Controls;

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

  internal class WV2DockBar : DockBar
  {
    public static Guid BarId => new Guid("{c520731e-376a-4d82-975a-403664fca2fc}");
    public static DockPage DockPage;

    public WV2DockBar() : base(RhDockableWebView2Plugin.Instance, BarId, "WebView2")
    {
      if(DockPage == null)
      {
        DockPage = new DockPage();
      }

      SetContentControl(new WpfHost(DockPage, null));
      RegisterEvents();
    }

    public void SendEvent()
    {
      DockPage.myWebView.ExecuteScriptAsync("console.log('test')");
    }

    private void RegisterEvents()
    {
      RhinoDoc.ActiveDocumentChanged += RhinoDoc_ActiveDocumentChanged;
      RhinoDoc.SelectObjects += RhinoDoc_SelectObjects;
      RhinoDoc.DeselectObjects += RhinoDoc_DeselectObjects;
    }

    private void RhinoDoc_DeselectObjects(object sender, Rhino.DocObjects.RhinoObjectSelectionEventArgs e)
    {
      var cp = e;
    }

    private void RhinoDoc_SelectObjects(object sender, Rhino.DocObjects.RhinoObjectSelectionEventArgs e)
    {
      var cp = e;
    }

    private void RhinoDoc_ActiveDocumentChanged(object sender, DocumentEventArgs e)
    {
      var cp = e;
    }
  }


}