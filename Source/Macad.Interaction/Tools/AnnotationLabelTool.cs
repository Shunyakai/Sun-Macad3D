using System;
using System.Windows.Input;
using Macad.Common;
using Macad.Core;
using Macad.Core.Auxiliary;
using Macad.Core.Topology;
using Macad.Interaction.Visual;
using Macad.Occt;

namespace Macad.Interaction.Tools;

public class AnnotationLabelTool : Tool
{
    enum Phase
    {
        TargetPoint,
        TextPoint
    }

    Phase _CurrentPhase;
    Pnt _TargetPoint;
    Pnt _TextPoint;
    AnnotationLabel _PreviewLabel;
    VisualAnnotationLabel _VisualLabel;

    protected override bool OnStart()
    {
        _CurrentPhase = Phase.TargetPoint;
        var pointAction = new PointAction();
        if (!StartAction(pointAction))
            return false;
        pointAction.Preview += _TargetAction_Preview;
        pointAction.Finished += _TargetAction_Finished;

        SetHintMessage("__Select label anchor point on object.__");
        SetCursor(Cursors.SetPoint);
        return true;
    }

    protected override void Cleanup()
    {
        if (_VisualLabel != null)
        {
            WorkspaceController.VisualObjects.Remove(_VisualLabel.Entity);
            _VisualLabel = null;
        }
        base.Cleanup();
    }

    void _TargetAction_Preview(PointAction action, PointAction.EventArgs args)
    {
    }

    void _TargetAction_Finished(PointAction action, PointAction.EventArgs args)
    {
        _TargetPoint = args.Point;

        StopAction(action);
        var newAction = new PointAction();
        newAction.Preview += _TextAction_Preview;
        newAction.Finished += _TextAction_Finished;
        if (!StartAction(newAction))
            return;

        _CurrentPhase = Phase.TextPoint;
        SetHintMessage("__Select text placement point.__");

        // Create a preview entity and visual object
        _PreviewLabel = new AnnotationLabel()
        {
            Position = _TargetPoint,
            LeaderTarget = _TargetPoint,
            Text = "Label",
            HasLeader = true
        };
        _VisualLabel = WorkspaceController.VisualObjects.Add(_PreviewLabel) as VisualAnnotationLabel;
    }

    void _TextAction_Preview(PointAction action, PointAction.EventArgs args)
    {
        _TextPoint = args.Point;
        if (_PreviewLabel != null)
        {
            _PreviewLabel.Position = _TextPoint;
            _PreviewLabel.LeaderTarget = _TargetPoint;
            _VisualLabel?.Update();
        }
    }

    void _TextAction_Finished(PointAction action, PointAction.EventArgs args)
    {
        _TextPoint = args.Point;
        StopAction(action);

        // Save preview visual, create final entity
        var label = AnnotationLabel.Create();
        label.Position = _TextPoint;
        label.LeaderTarget = _TargetPoint;
        label.Text = "Label Text";

        // Remove preview visual
        if (_VisualLabel != null)
        {
            WorkspaceController.VisualObjects.Remove(_VisualLabel.Entity);
            _VisualLabel = null;
        }

        // Add final visual object
        InteractiveContext.Current.Document.Add(label);
        WorkspaceController.VisualObjects.Add(label);
        WorkspaceController.Selection.SelectEntity(label);

        // Finish tool
        Stop();
        InteractiveContext.Current.UndoHandler.Commit();
        WorkspaceController.Invalidate();
    }
}
