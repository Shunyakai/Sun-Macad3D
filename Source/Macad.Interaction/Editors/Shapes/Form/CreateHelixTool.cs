using System.Diagnostics;
using System.Linq;
using Macad.Core;
using Macad.Core.Shapes;
using Macad.Core.Topology;

namespace Macad.Interaction.Editors.Shapes;

public class CreateHelixTool : Tool
{
    readonly Body _TargetBody;
    readonly Shape _TargetShape;

    //--------------------------------------------------------------------------------------------------

    public CreateHelixTool(Body targetBody)
    {
        _TargetBody = targetBody;
        _TargetShape = _TargetBody?.Shape;
        Debug.Assert(_TargetShape != null);
    }

    //--------------------------------------------------------------------------------------------------

    protected override bool OnStart()
    {
        if (_TargetShape.ShapeType == ShapeType.Sketch)
        {
            var modifierShape = Helix.Create(_TargetBody);
            if (modifierShape != null)
            {
                CommitChanges();
            }

            WorkspaceController.Invalidate();
            Stop();
            return false;
        }

        Stop();
        return false;
    }
}
