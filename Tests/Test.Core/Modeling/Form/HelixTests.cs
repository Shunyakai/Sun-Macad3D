using System.IO;
using Macad.Test.Utils;
using Macad.Common;
using Macad.Core;
using Macad.Core.Shapes;
using Macad.Core.Topology;
using Macad.Occt;
using NUnit.Framework;

namespace Macad.Test.Core.Modeling.Form;

[TestFixture]
public class HelixTests
{
    [Test]
    public void SimpleHelix()
    {
        var sketch = new Macad.Core.Shapes.Sketch();
        var body = Body.Create(sketch);
        TestSketchGenerator.FillSketch(sketch, TestSketchGenerator.SketchType.Circle);

        var helix = Helix.Create(body);
        Assert.IsNotNull(helix);

        helix.Pitch = 10.0;
        helix.Height = 50.0;
        helix.Radius = 10.0;
        helix.TaperAngle = 0.0;
        helix.Handedness = Helix.HelixHandedness.Right;

        AssertHelper.IsMade(helix);
        Assert.IsNotNull(helix.GetBRep());
        Assert.AreEqual(ShapeType.Solid, helix.ShapeType);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void HandednessAndTaper()
    {
        var sketch = new Macad.Core.Shapes.Sketch();
        var body = Body.Create(sketch);
        TestSketchGenerator.FillSketch(sketch, TestSketchGenerator.SketchType.Circle);

        var helix = Helix.Create(body);
        Assert.IsNotNull(helix);

        // Test Left handedness and tapered angle
        helix.Pitch = 15.0;
        helix.Height = 45.0;
        helix.Radius = 12.0;
        helix.TaperAngle = 10.0;
        helix.Handedness = Helix.HelixHandedness.Left;

        AssertHelper.IsMade(helix);
        Assert.IsNotNull(helix.GetBRep());
    }
}
