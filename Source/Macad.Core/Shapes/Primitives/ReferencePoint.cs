using System;
using Macad.Common.Serialization;
using Macad.Occt;

namespace Macad.Core.Shapes;

[SerializeType]
public sealed class ReferencePoint : Shape
{
    public override ShapeType ShapeType => ShapeType.Sketch;

    public static ReferencePoint Create()
    {
        return new ReferencePoint();
    }

    protected override bool MakeInternal(MakeFlags flags)
    {
        var makeVertex = new BRepBuilderAPI_MakeVertex(new Pnt());
        if (makeVertex.IsDone())
        {
            BRep = makeVertex.Vertex();
            return base.MakeInternal(flags);
        }
        return false;
    }
}
