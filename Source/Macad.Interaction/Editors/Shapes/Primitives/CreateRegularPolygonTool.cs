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

public class CreateRegularPolygonTool : Tool
{
    enum Phase
    {
        CenterPoint,
        Radius
    }

    Phase _CurrentPhase;
    Pnt _CenterPosition;
    double _Radius;
    int _Sides = 6;

    RegularPolygon _PreviewShape;
    VisualObject _VisualShape;
    bool _IsTemporaryVisual;

    Coord2DHudElement _Coord2DHudElement;
    ValueHudElement _ValueHudElement;

    protected override bool OnStart()
    {
        WorkspaceController.Selection.SelectEntity(null);

        var pointAction = new PointAction();
        if (!StartAction(pointAction))
            return false;
        pointAction.Preview += _CenterAction_Preview;
        pointAction.Finished += _CenterAction_Finished;

        _CurrentPhase = Phase.CenterPoint;
        SetHintMessage("__Select center point__ of the regular polygon.");
        _Coord2DHudElement = new Coord2DHudElement();
        Add(_Coord2DHudElement);
        SetCursor(Cursors.SetPoint);

        return true;
    }

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

    void _CenterAction_Preview(PointAction sender, PointAction.EventArgs args)
    {
        _Coord2DHudElement?.SetValues(args.PointOnPlane.X, args.PointOnPlane.Y);
    }

    void _CenterAction_Finished(PointAction action, PointAction.EventArgs args)
    {
        _CenterPosition = args.Point.Rounded();
        StopAction(action);

        var axis = new Ax1(_CenterPosition, WorkspaceController.Workspace.WorkingPlane.Axis.Direction);
        var axisValueAction = new AxisValueAction(axis);
        if (!StartAction(axisValueAction))
            return;
        axisValueAction.Preview += _RadiusAction_Preview;
        axisValueAction.Finished += _RadiusAction_Finished;

        _CurrentPhase = Phase.Radius;
        SetHintMessage("__Select radius__, press `k:Ctrl` to round to grid stepping.");

        Remove(_Coord2DHudElement);
        if (_ValueHudElement == null)
        {
            _ValueHudElement = new ValueHudElement
            {
                Label = "Radius:",
                Units = ValueUnits.Length
            };
            _ValueHudElement.ValueEntered += _ValueEntered;
            Add(_ValueHudElement);
        }
        SetCursor(Cursors.SetRadius);
    }

    void _RadiusAction_Preview(AxisValueAction action, AxisValueAction.EventArgs args)
    {
        _Radius = args.Distance.Round();
        if (_Radius < 0.001)
            _Radius = 0.001;

        if (args.MouseEventData.ModifierKeys.HasFlag(ModifierKeys.Control))
        {
            _Radius = Maths.RoundToNearest(_Radius, WorkspaceController.Workspace.GridStep);
        }

        _EnsurePreviewShape();
        _PreviewShape.Radius = _Radius;
        if (_IsTemporaryVisual)
            _VisualShape?.Update();

        _ValueHudElement?.SetValue(_Radius);
    }

    void _RadiusAction_Finished(AxisValueAction action, AxisValueAction.EventArgs args)
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

    void _ValueEntered(ValueHudElement hudElement, double newValue)
    {
        if (_CurrentPhase == Phase.Radius)
        {
            _Radius = Math.Abs(newValue) >= 0.001 ? newValue : 0.001;
            _EnsurePreviewShape();
            _PreviewShape.Radius = _Radius;
            _RadiusAction_Finished(null, null);
        }
    }

    void _EnsurePreviewShape()
    {
        if (_PreviewShape != null)
            return;

        _PreviewShape = new RegularPolygon
        {
            Radius = _Radius,
            Sides = _Sides
        };
        var body = Body.Create(_PreviewShape);
        _PreviewShape.Body.Rotation = WorkspaceController.Workspace.GetWorkingPlaneRotation();
        _PreviewShape.Body.Position = _CenterPosition;
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
