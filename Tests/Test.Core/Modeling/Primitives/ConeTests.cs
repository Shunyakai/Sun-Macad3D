using System;
using System.IO;
using Macad.Common;
using Macad.Core.Shapes;
using Macad.Core.Topology;
using Macad.Test.Utils;
using NUnit.Framework;

namespace Macad.Test.Core.Modeling.Primitives;

[TestFixture]
public class ConeTests
{
    [Test]
    public void Simple()
    {
        var shape = new Cone()
        {
            Radius1 = 10,
            Radius2 = 0,
            Height = 20
        };

        Assert.IsTrue(shape.Make(Shape.MakeFlags.None));
        Assert.IsNotNull(shape.GetBRep());
    }

    [Test]
    public void Truncated()
    {
        var shape = new Cone()
        {
            Radius1 = 10,
            Radius2 = 5,
            Height = 20
        };

        Assert.IsTrue(shape.Make(Shape.MakeFlags.None));
        Assert.IsNotNull(shape.GetBRep());
    }

    [Test]
    public void SegmentAngle()
    {
        var shape = new Cone()
        {
            Radius1 = 10,
            Radius2 = 5,
            Height = 20,
            SegmentAngle = 180
        };

        Assert.IsTrue(shape.Make(Shape.MakeFlags.None));
        Assert.IsNotNull(shape.GetBRep());
    }

    [Test]
    public void NegativeHeight()
    {
        var shape = new Cone()
        {
            Radius1 = 10,
            Radius2 = 0,
            Height = -20
        };

        Assert.IsTrue(shape.Make(Shape.MakeFlags.None));
        Assert.IsNotNull(shape.GetBRep());
    }
}
