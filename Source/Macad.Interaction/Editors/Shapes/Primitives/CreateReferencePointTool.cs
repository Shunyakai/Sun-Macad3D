using System;
using System.Windows.Input;
using Macad.Common;
using Macad.Core;
using Macad.Core.Shapes;
using Macad.Core.Topology;
using Macad.Interaction.Visual;
using Macad.Occt;
using Macad.Presentation;

namespace Macad.Interaction.Editors.Shapes;

public class CreateReferencePointTool : Tool
{
    Coord2DHudElement _Coord2DHudElement;

    protected override bool OnStart()
    {
        WorkspaceController.Selection.SelectEntity(null);

        var pointAction = new PointAction();
        if (!StartAction(pointAction))
            return false;
        pointAction.Preview += _PointAction_Preview;
        pointAction.Finished += _PointAction_Finished;

        SetHintMessage("__Select position__ for the reference point.");
        _Coord2DHudElement = new Coord2DHudElement();
        Add(_Coord2DHudElement);
        SetCursor(Cursors.SetPoint);

        return true;
    }

    void _PointAction_Preview(PointAction sender, PointAction.EventArgs args)
    {
        _Coord2DHudElement?.SetValues(args.PointOnPlane.X, args.PointOnPlane.Y);
    }

    void _PointAction_Finished(PointAction action, PointAction.EventArgs args)
    {
        Pnt position = args.Point.Rounded();
        StopAction(action);

        var newShape = ReferencePoint.Create();
        var body = Body.Create(newShape);
        body.Rotation = WorkspaceController.Workspace.GetWorkingPlaneRotation();
        body.Position = position;

        InteractiveContext.Current.Document.Add(body);
        CommitChanges();

        Stop();
        WorkspaceController.Selection.SelectEntity(body);
        WorkspaceController.Invalidate();
    }
}
