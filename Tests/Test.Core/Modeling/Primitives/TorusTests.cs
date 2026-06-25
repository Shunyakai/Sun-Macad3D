using System.IO;
using Macad.Common;
using Macad.Core.Shapes;
using Macad.Core.Topology;
using Macad.Test.Utils;
using NUnit.Framework;

namespace Macad.Test.Core.Modeling.Primitives;

[TestFixture]
public class TorusTests
{
    [Test]
    public void Simple()
    {
        var shape = new Torus()
        {
            Radius1 = 20,
            Radius2 = 5
        };

        Assert.IsTrue(shape.Make(Shape.MakeFlags.None));
        Assert.IsNotNull(shape.GetBRep());
    }

    [Test]
    public void SegmentAngle()
    {
        var shape = new Torus()
        {
            Radius1 = 20,
            Radius2 = 5,
            SegmentAngle = 180
        };

        Assert.IsTrue(shape.Make(Shape.MakeFlags.None));
        Assert.IsNotNull(shape.GetBRep());
    }
}
