using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace RhDockableWebView2
{
  public class RhDockableWebView2Command : Command
  {
    public RhDockableWebView2Command()
    {
      Instance = this;
    }

    public static RhDockableWebView2Command Instance { get; private set; }

    public override string EnglishName => "RhDockableWebView2Command";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      RhinoWindows.Controls.DockBar.Show(WV2DockBar.BarId, false);
      return Result.Success;
    }
  }
}
