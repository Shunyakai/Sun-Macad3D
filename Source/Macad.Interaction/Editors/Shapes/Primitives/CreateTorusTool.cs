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

public class CreateTorusTool : Tool
{
    enum Phase
    {
        PivotPoint,
        Radius1,
        Radius2
    }

    //--------------------------------------------------------------------------------------------------

    Phase _CurrentPhase;
    Pln _Plane;
    Pnt _PivotPoint;
    Pnt2d _PointPlane1;
    Pnt2d _PointPlane2;
    Pnt2d _PointPlane3;
    double _Radius1;
    double _Radius2;

    Torus _PreviewShape;
    VisualObject _VisualShape;
    bool _IsTemporaryVisual;

    Coord2DHudElement _Coord2DHudElement;
    ValueHudElement _ValueHudElement;

    //--------------------------------------------------------------------------------------------------

    protected override bool OnStart()
    {
        WorkspaceController.Selection.SelectEntity(null);

        var pointAction = new PointAction();
        if (!StartAction(pointAction))
            return false;
        pointAction.Preview += _PivotAction_Preview;
        pointAction.Finished += _PivotAction_Finished;

        _CurrentPhase = Phase.PivotPoint;
        SetHintMessage("__Select center point.__");
        _Coord2DHudElement = new Coord2DHudElement();
        Add(_Coord2DHudElement);
        SetCursor(Cursors.SetPoint);
        return true;
    }
        
    //--------------------------------------------------------------------------------------------------

    protected override void Cleanup()
    {
        if (_VisualShape != null)
        {
            WorkspaceController.VisualObjects.Remove(_VisualShape.Entity);
            _VisualShape.Remove();
            _VisualShape = null;
        }
        base.Cleanup();
    }

    //--------------------------------------------------------------------------------------------------

    void _PivotAction_Preview(PointAction sender, PointAction.EventArgs args)
    {
        _Coord2DHudElement?.SetValues(args.PointOnPlane.X, args.PointOnPlane.Y);
    }

    //--------------------------------------------------------------------------------------------------

    void _PivotAction_Finished(PointAction action, PointAction.EventArgs args)
    {
        _Plane = WorkspaceController.Workspace.WorkingPlane;
        _PointPlane1 = args.PointOnPlane;
        _PivotPoint = args.Point.Rounded();

        StopAction(action);
        var pointAction = new PointAction();
        if (!StartAction(pointAction))
            return;
        pointAction.Preview += _Radius1Action_Preview;
        pointAction.Finished += _Radius1Action_Finished;

        _CurrentPhase = Phase.Radius1;
        SetHintMessage("__Select major radius__, press `k:Ctrl` to round to grid stepping.");

        if (_ValueHudElement == null)
        {
            _ValueHudElement = new ValueHudElement
            {
                Label = "Major Radius:",
                Units = ValueUnits.Length
            };
            _ValueHudElement.ValueEntered += _ValueEntered;
            Add(_ValueHudElement);
        }

        SetCursor(Cursors.SetRadius);
    }

    //--------------------------------------------------------------------------------------------------

    void _Radius1Action_Preview(PointAction sender, PointAction.EventArgs args)
    {
        _PointPlane2 = args.PointOnPlane;

        if (_PointPlane1.IsEqual(_PointPlane2, Double.Epsilon))
            return;

        _Radius1 = new Vec2d(_PointPlane1, _PointPlane2).Magnitude().Round();
        if (args.MouseEventData.ModifierKeys.HasFlag(ModifierKeys.Control))
        {
            _Radius1 = Maths.RoundToNearest(_Radius1, WorkspaceController.Workspace.GridStep);
        }

        if (Math.Abs(_Radius1) <= 0)
        {
            _Radius1 = 0.001;
        }

        _PointPlane2 = _PointPlane1.Translated(new Vec2d(_PointPlane1, _PointPlane2).Normalized().Scaled(_Radius1));
        args.MarkerPosition = ElSLib.Value(_PointPlane2.X, _PointPlane2.Y, _Plane).Rounded();

        _EnsurePreviewShape();
        _PreviewShape.Radius1 = _Radius1;
        if(_IsTemporaryVisual)
            _VisualShape?.Update();

        _ValueHudElement?.SetValue(_Radius1);
        _Coord2DHudElement?.SetValues(args.PointOnPlane.X, args.PointOnPlane.Y);
    }

    //--------------------------------------------------------------------------------------------------

    void _Radius1Action_Finished(PointAction action, PointAction.EventArgs args)
    {
        StopAction(action);
        var pointAction = new PointAction();
        if (!StartAction(pointAction))
            return;
        pointAction.Preview += _Radius2Action_Preview;
        pointAction.Finished += _Radius2Action_Finished;

        _CurrentPhase = Phase.Radius2;
        SetHintMessage("__Select minor radius__, press `k:Ctrl` to round to grid stepping.");

        if (_ValueHudElement != null)
        {
            _ValueHudElement.Label = "Minor Radius:";
            _ValueHudElement.Value = 0;
        }

        Remove(_Coord2DHudElement);
        SetCursor(Cursors.SetRadius);

        _EnsurePreviewShape();
    }

    //--------------------------------------------------------------------------------------------------

    void _Radius2Action_Preview(PointAction sender, PointAction.EventArgs args)
    {
        _PointPlane3 = args.PointOnPlane;

        _Radius2 = new Vec2d(_PointPlane2, _PointPlane3).Magnitude().Round();
        if (args.MouseEventData.ModifierKeys.HasFlag(ModifierKeys.Control))
        {
            _Radius2 = Maths.RoundToNearest(_Radius2, WorkspaceController.Workspace.GridStep);
        }

        if (Math.Abs(_Radius2) <= 0)
        {
            _Radius2 = 0.001;
        }

        args.MarkerPosition = ElSLib.Value(_PointPlane3.X, _PointPlane3.Y, _Plane).Rounded();

        _EnsurePreviewShape();
        _PreviewShape.Radius2 = _Radius2;
        if(_IsTemporaryVisual)
            _VisualShape?.Update();

        _ValueHudElement?.SetValue(_Radius2);
    }

    //--------------------------------------------------------------------------------------------------

    void _Radius2Action_Finished(PointAction action, PointAction.EventArgs args)
    {
        InteractiveContext.Current.Document.Add(_PreviewShape.Body);
        if (!_IsTemporaryVisual)
        {
            _VisualShape.IsSelectable = true;
            _VisualShape = null; // Prevent removing
        }
        CommitChanges();

        Stop();

        WorkspaceController.Selection.SelectEntity(_PreviewShape.Body);
        WorkspaceController.Invalidate();
    }

    //--------------------------------------------------------------------------------------------------

    void _ValueEntered(ValueHudElement hudElement, double newValue)
    {
        if (_CurrentPhase == Phase.Radius1)
        {
            _Radius1 = Math.Abs(newValue) >= 0.001 ? newValue : 0.001;
            _EnsurePreviewShape();
            _PreviewShape.Radius1 = _Radius1;
            _Radius1Action_Finished(null, null);
        }
        else if (_CurrentPhase == Phase.Radius2)
        {
            _Radius2 = Math.Abs(newValue) >= 0.001 ? newValue : 0.001;
            _EnsurePreviewShape();
            _PreviewShape.Radius2 = _Radius2;
            _Radius2Action_Finished(null, null);
        }
    }

    //--------------------------------------------------------------------------------------------------

    void _EnsurePreviewShape()
    {
        if (_PreviewShape != null)
            return;

        // Create solid
        _PreviewShape = new Torus()
        {
            Radius1 = _Radius1,
            Radius2 = 0.01
        };
        var body = Body.Create(_PreviewShape);
        _PreviewShape.Body.Rotation = WorkspaceController.Workspace.GetWorkingPlaneRotation();
        _PreviewShape.Body.Position = _PivotPoint;
        if (body.Layer.IsVisible)
        {
            _VisualShape = WorkspaceController.VisualObjects.Get(body, true);
            _IsTemporaryVisual = false;
        }
        else
        {
            _VisualShape = new VisualShape(WorkspaceController, body, VisualShape.Options.Ghosting);
            _IsTemporaryVisual = true;
        }
        _VisualShape.IsSelectable = false;
    }
}
