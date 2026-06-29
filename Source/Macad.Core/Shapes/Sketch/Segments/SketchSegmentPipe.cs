using System;
using System.Collections.Generic;
using Macad.Common.Serialization;
using Macad.Occt;

namespace Macad.Core.Shapes;

[SerializeType]
public class SketchSegmentPipe : SketchSegment
{
    [SerializeMember]
    public double Radius { get; set; }

    public SketchSegmentPipe()
    {
        Points = new int[2];
    }

    public SketchSegmentPipe(int p1, int p2, double radius)
    {
        Points = new int[] { p1, p2 };
        Radius = radius;
    }

    public override int StartPoint
    {
        get { return Points[0]; }
    }

    public override int EndPoint
    {
        get { return Points[1]; }
    }

    public override SketchSegment Clone()
    {
        return base.Clone(new SketchSegmentPipe(Points[0], Points[1], Radius));
    }

    public override Geom2d_Curve MakeCurve(Dictionary<int, Pnt2d> points)
    {
        try
        {
            var p1 = points[Points[0]];
            var p2 = points[Points[1]];
            if (p1.Distance(p2) <= 0)
                return null;

            var line = new gce_MakeLin2d(p1, p2).Value();
            if (line == null)
                return null;

            return new Geom2d_TrimmedCurve(new Geom2d_Line(line), ElCLib.Parameter(line, p1), ElCLib.Parameter(line, p2));
        }
        catch (Exception)
        {
            return null;
        }
    }
}
