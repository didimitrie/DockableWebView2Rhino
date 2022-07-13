using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhDockableWebView2
{
  public class RhinoRevitMappings
  {
    #region schema definitions
    // TODO: These (or some of these) can be initialized and populated from a revit project info commit.
    public Schema Floor { get; set; } = new Schema
    {
      Name = "Floor",
      Description = "Creates Revit floors from planar horizontal surfaces",
      Params = new List<object>
        {
          new MultiselectParam { Name = "Family", Values = new List<string> {"Foo", "Bar", "Baz" } },
          new MultiselectParam { Name = "Type", Values = new List<string> {"A", "B", "C", "D" } },
          new CheckboxParam { Name = "Structural", Value = true, Description = "Defines this element as load bearing." },
          new StringParam { Name = "Comments" }
        }
    };

    public Schema Wall { get; set; } = new Schema
    {
      Name = "Wall",
      Description = "Creates Revit walls from planar extrusions.",
      Params = new List<object>
        {
          new MultiselectParam { Name = "Family", Values = new List<string> {"Foo", "Bar", "Baz" } },
          new MultiselectParam { Name = "Type", Values = new List<string> {"A", "B", "C", "D" } },
          new DoubleParam { Name = "bottom offset", Value = 0 },
          new DoubleParam { Name = "top offset", Value = 0 },
          new CheckboxParam { Name = "Structural", Value = true, Description = "Does this wall make the building stand up? In Revit, ofc." },
          new StringParam { Name = "Comments" }
        }
    };

    public Schema DirectShape { get; set; } = new Schema
    {
      Name = "DirectShape",
      Params = new List<object>
        {
          new MultiselectParam { Name = "Type", Values = new List<string> {"Floor", "Wall", "Roof", "Column" } }, // Competitors seems to have this functionality
          new CheckboxParam { Name = "Smooth Import", Description = "Elements will look better, but will take longer to create." },
          new StringParam { Name = "Comments" }
        }
    };

    public Schema Column = new Schema
    {
      Name = "Column",
      Params = new List<object>
        {
          new MultiselectParam { Name = "Family Instance", Values = new List<string> {"C 123", "C 10x10", "C 20x20", "L 20x40" } },
          new CheckboxParam { Name = "Structural", Value = true, Description = "Defines this element as load bearing." },
          new StringParam { Name = "Comments" }
        }
    };

    public Schema Beam = new Schema
    {
      Name = "Beam",
      Params = new List<object>
        {
          new MultiselectParam { Name = "Type", Values = new List<string> {"W 123", "W 10x10", "FOO 20x20", "STEEL 20x40" } },
          new DoubleParam { Name = "bottom offset", Value = 0 },
          new DoubleParam { Name = "top offset", Value = 0 },
          new StringParam { Name = "Comments" }
        }
    };


    public Schema Gridline = new Schema
    {
      Name = "Gridline"
    };

    public Schema IncompatibleSelection = new Schema
    {
      Name = "Incompatible Selection",
      Description = "Current selection objects cannot be assigned one single schema."
    };

    #endregion


    public RhinoRevitMappings()
    {
    }

    public List<Schema> GetSelectionSchemas(IEnumerable<RhinoObject> selection)
    {
      var result = new List<Schema>();
      var first = true;
      foreach(var obj in selection)
      {
        var schemas = GetObjectSchemas(obj);
        if(first)
        {
          result = schemas;
          first = false;
          continue;
        }
        result = result.Intersect(schemas).ToList();
      }
      if (result.Count == 0) return new List<Schema> { IncompatibleSelection };
      return result.ToList();
    }

    public List<Schema> GetObjectSchemas(RhinoObject obj)
    {
      var cats = new List<Schema>();

      switch (obj.Geometry)
      {
        case Mesh _m:
          cats.Add(DirectShape);
          break;

        case Brep b:
          if (b.IsSurface) cats.Add(DirectShape); // TODO: Wall by face, totally faking it right now
          else cats.Add(DirectShape);
          break;

        case Extrusion e:
          if (e.ProfileCount > 1) break;
          var crv = e.Profile3d(0, 0);
          if (!(crv.IsLinear() || crv.IsArc())) break;
          if (crv.PointAtStart.Z == crv.PointAtEnd.Z) cats.Add(Wall);
          break;

        case Curve c:
          if (c.IsLinear()) cats.Add(Beam);
          if (c.IsLinear() && c.PointAtEnd.Z == c.PointAtStart.Z) cats.Add(Gridline);
          if (c.IsLinear() && c.PointAtEnd.X == c.PointAtStart.X && c.PointAtEnd.Y == c.PointAtStart.Y) cats.Add(Column);
          if (c.IsArc() && !c.IsCircle() && c.PointAtEnd.Z == c.PointAtStart.Z) cats.Add(Gridline);
          break;
      }

      return cats;
    }
  }

  public class Schema
  {
    public string Name { get; set; }
    public string Description { get; set; }

    public List<object> Params { get; set; } // NOTE: because FML, see: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-polymorphism

    // NOTE: used for hash sets
    public override int GetHashCode()
    {
      return Name.GetHashCode();
    }

    // NOTE: used(?) for linq intersection
    public override bool Equals(object obj)
    {
      if (obj is Schema s) return s.Name == Name;
      return false;
    }
  }

  public class SchemaParam
  {
    public string Name { get; set; }
    public string Description { get; set; }
    
    public string Type { get; set; }

    public SchemaParam()
    {
      Type = GetType().Name;
    }
  }

  public class StringParam : SchemaParam
  {
    public string Value { get; set; }
  }

  public class DoubleParam : SchemaParam
  {
    public double Value { get; set; } = 0;
  }

  public class MultiselectParam : SchemaParam
  {
    public List<string> Values { get; set; } 
    public string SelectedValue { get; set; }
  }

  public class CheckboxParam : SchemaParam
  {
    public bool Value { get; set; } = true;
  }


}
