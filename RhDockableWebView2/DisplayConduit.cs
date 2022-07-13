using Rhino.Display;
using Rhino;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace RhDockableWebView2
{
  public class SpeckleDisplayConduit : Rhino.Display.DisplayConduit
  {
    public List<string> ObjectIds { get; set; } = new List<string>();

    public Color Color { get; set; } = Color.RoyalBlue;

    protected override void DrawOverlay(DrawEventArgs e)
    {
      base.DrawOverlay(e);
      if (!Enabled) return;

      //e.Display.ZBiasMode = ZBiasMode.TowardsCamera;

      foreach(var id in ObjectIds)
      {
        if(id==null) continue;
        var obj = Rhino.RhinoDoc.ActiveDoc.Objects.FindId(new Guid(id));
        switch(obj.ObjectType)
        {
          case ObjectType.Curve:
            e.Display.DrawCurve((Curve)obj.Geometry, Color);
            break;
          case ObjectType.Mesh:
            DisplayMaterial mMaterial = new DisplayMaterial(Color, 0.5);
            e.Display.DrawMeshShaded(obj.Geometry as Mesh, mMaterial);
            break;
          case ObjectType.Extrusion:
            DisplayMaterial eMaterial = new DisplayMaterial(Color, 0.5);
            e.Display.DrawBrepShaded(((Extrusion)obj.Geometry).ToBrep(), eMaterial);
            break;
          case ObjectType.Brep:
            DisplayMaterial bMaterial = new DisplayMaterial(Color, 0.5);
            e.Display.DrawBrepShaded((Brep)obj.Geometry, bMaterial);
            break;
        }
      }

    }
  }
}
