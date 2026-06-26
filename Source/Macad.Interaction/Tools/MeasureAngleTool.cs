using System;
using System.Collections.Generic;
using System.Windows.Input;
using Macad.Common;
using Macad.Core;
using Macad.Core.Shapes;
using Macad.Core.Topology;
using Macad.Interaction.Visual;
using Macad.Occt;
using Macad.Presentation;

namespace Macad.Interaction;

public class MeasureAngleTool : Tool
{
    SelectSubshapeAction _FirstAction;
    SelectSubshapeAction _SecondAction;
    TopoDS_Shape _Shape1;
    TopoDS_Shape _Shape2;
    AIS_Shape _AisShape1;
    AIS_Shape _AisShape2;
    ValueHudElement _ValueHud;

    //--------------------------------------------------------------------------------------------------

    protected override bool OnStart()
    {
        _ValueHud = new ValueHudElement
        {
            Label = "Angle",
            Units = ValueUnits.Degree
        };
        Add(_ValueHud);

        var filter = new OrSelectionFilter(
            new FaceSelectionFilter(FaceSelectionFilter.FaceType.Plane),
            new EdgeSelectionFilter(EdgeSelectionFilter.EdgeType.Line)
        );

        _FirstAction = new SelectSubshapeAction(SubshapeTypes.Face | SubshapeTypes.Edge, null, filter);
        if (!StartAction(_FirstAction))
            return false;

        _FirstAction.Finished += _FirstAction_Finished;

        SetHintMessage("__Select first planar face or linear edge__ to measure angle.");
        SetCursor(Cursors.SelectShape);

        return true;
    }

    //--------------------------------------------------------------------------------------------------

    protected override void Cleanup()
    {
        _ClearVisuals();
        base.Cleanup();
    }

    //--------------------------------------------------------------------------------------------------

    void _ClearVisuals()
    {
        if (_AisShape1 != null)
        {
            WorkspaceController.AisContext.Remove(_AisShape1, false);
            _AisShape1 = null;
        }
        if (_AisShape2 != null)
        {
            WorkspaceController.AisContext.Remove(_AisShape2, false);
            _AisShape2 = null;
        }
    }

    //--------------------------------------------------------------------------------------------------

    Dir? _GetDirection(TopoDS_Shape shape)
    {
        try
        {
            if (shape.ShapeType() == TopAbs_ShapeEnum.FACE)
            {
                var faceAdaptor = new BRepAdaptor_Surface(shape.ToFace());
                if (faceAdaptor.GetSurfaceType() == GeomAbs_SurfaceType.Plane)
                {
                    var dir = faceAdaptor.Plane().Axis.Direction;
                    if (shape.Orientation() == TopAbs_Orientation.REVERSED)
                        dir.Reverse();
                    return dir;
                }
            }
            else if (shape.ShapeType() == TopAbs_ShapeEnum.EDGE)
            {
                var edgeAdaptor = new BRepAdaptor_Curve(shape.ToEdge());
                if (edgeAdaptor.GetCurveType() == GeomAbs_CurveType.Line)
                {
                    return edgeAdaptor.Line().Direction();
                }
            }
        }
        catch (Exception e)
        {
            Messages.Exception("Error extracting direction for angle calculation.", e);
        }
        return null;
    }

    //--------------------------------------------------------------------------------------------------

    void _FirstAction_Finished(SelectSubshapeAction action, SelectSubshapeAction.EventArgs args)
    {
        if (args.SelectedSubshape != null)
        {
            _Shape1 = args.SelectedSubshape;
            _ClearVisuals();

            _AisShape1 = new AIS_Shape(_Shape1);
            _AisShape1.SetColor(new Color(0.0f, 0.8f, 0.0f).ToQuantityColor());
            _AisShape1.SetWidth(3);
            var pointAspect1 = _AisShape1.Attributes().PointAspect();
            if (pointAspect1 != null)
                pointAspect1.SetScale(3);
            _AisShape1.SetZLayer(-2);
            WorkspaceController.AisContext.Display(_AisShape1, false);

            StopAction(_FirstAction);

            var filter = new OrSelectionFilter(
                new FaceSelectionFilter(FaceSelectionFilter.FaceType.Plane),
                new EdgeSelectionFilter(EdgeSelectionFilter.EdgeType.Line)
            );
            _SecondAction = new SelectSubshapeAction(SubshapeTypes.Face | SubshapeTypes.Edge, null, filter);
            if (StartAction(_SecondAction))
            {
                _SecondAction.Finished += _SecondAction_Finished;
                _SecondAction.Preview += _SecondAction_Preview;
                SetHintMessage("__Select second planar face or linear edge__ to measure angle, or press `k:Esc` to finish.");
            }
        }
        else
        {
            action.Reset();
        }
        WorkspaceController.Invalidate();
    }

    //--------------------------------------------------------------------------------------------------

    void _SecondAction_Preview(SelectSubshapeAction sender, SelectSubshapeAction.EventArgs args)
    {
        if (args.SelectedSubshape != null)
        {
            _UpdateSecondPreview(args.SelectedSubshape);
        }
    }

    //--------------------------------------------------------------------------------------------------

    void _UpdateSecondPreview(TopoDS_Shape shape2)
    {
        var dir1Opt = _GetDirection(_Shape1);
        var dir2Opt = _GetDirection(shape2);
        if (dir1Opt.HasValue && dir2Opt.HasValue)
        {
            double angleDeg = _ComputeAngle(dir1Opt.Value, dir2Opt.Value, _Shape1.ShapeType(), shape2.ShapeType());
            _ValueHud.Label = "Angle";
            _ValueHud.Units = ValueUnits.Degree;
            _ValueHud.Value = angleDeg;
        }
    }

    //--------------------------------------------------------------------------------------------------

    double _ComputeAngle(Dir dir1, Dir dir2, TopAbs_ShapeEnum type1, TopAbs_ShapeEnum type2)
    {
        double angleRad = dir1.Angle(dir2);
        if ((type1 == TopAbs_ShapeEnum.FACE && type2 == TopAbs_ShapeEnum.EDGE) ||
            (type1 == TopAbs_ShapeEnum.EDGE && type2 == TopAbs_ShapeEnum.FACE))
        {
            // Angle between line and plane is 90 - angle between line and normal
            return Math.Abs(90.0 - angleRad.ToDeg());
        }
        else
        {
            return angleRad.ToDeg();
        }
    }

    //--------------------------------------------------------------------------------------------------

    void _SecondAction_Finished(SelectSubshapeAction action, SelectSubshapeAction.EventArgs args)
    {
        if (args.SelectedSubshape != null)
        {
            _Shape2 = args.SelectedSubshape;

            if (_AisShape2 != null)
            {
                WorkspaceController.AisContext.Remove(_AisShape2, false);
            }
            _AisShape2 = new AIS_Shape(_Shape2);
            _AisShape2.SetColor(new Color(0.0f, 0.8f, 0.0f).ToQuantityColor());
            _AisShape2.SetWidth(3);
            var pointAspect2 = _AisShape2.Attributes().PointAspect();
            if (pointAspect2 != null)
                pointAspect2.SetScale(3);
            _AisShape2.SetZLayer(-2);
            WorkspaceController.AisContext.Display(_AisShape2, false);

            var dir1Opt = _GetDirection(_Shape1);
            var dir2Opt = _GetDirection(_Shape2);
            if (dir1Opt.HasValue && dir2Opt.HasValue)
            {
                double angleDeg = _ComputeAngle(dir1Opt.Value, dir2Opt.Value, _Shape1.ShapeType(), _Shape2.ShapeType());
                _ValueHud.Label = "Angle";
                _ValueHud.Units = ValueUnits.Degree;
                _ValueHud.Value = angleDeg;

                string msg = $"Angle: {angleDeg:F4}° (Supplementary: {180.0 - angleDeg:F4}°)";
                Messages.Info(msg);
                SetHintMessage($"__Angle: {angleDeg:F4}°__. Select another element, or press `k:Esc` to finish.");
            }
            else
            {
                Messages.Error("Could not compute angle.");
            }
        }

        action.Reset();
        WorkspaceController.Invalidate();
    }
}
