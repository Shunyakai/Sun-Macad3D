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

public class MeasureDistanceTool : Tool
{
    SelectSubshapeAction _FirstAction;
    SelectSubshapeAction _SecondAction;
    TopoDS_Shape _Shape1;
    TopoDS_Shape _Shape2;
    AIS_Shape _AisShape1;
    AIS_Shape _AisShape2;
    AIS_Shape _AisConnectionShape;
    ValueHudElement _ValueHud;

    //--------------------------------------------------------------------------------------------------

    protected override bool OnStart()
    {
        _ValueHud = new ValueHudElement
        {
            Label = "Measure",
            Units = ValueUnits.Length
        };
        Add(_ValueHud);

        _FirstAction = new SelectSubshapeAction(SubshapeTypes.Vertex | SubshapeTypes.Edge | SubshapeTypes.Face);
        if (!StartAction(_FirstAction))
            return false;

        _FirstAction.Finished += _FirstAction_Finished;
        _FirstAction.Preview += _FirstAction_Preview;

        SetHintMessage("__Select first geometric element__ (vertex, edge, face) to measure.");
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
        if (_AisConnectionShape != null)
        {
            WorkspaceController.AisContext.Remove(_AisConnectionShape, false);
            _AisConnectionShape = null;
        }
    }

    //--------------------------------------------------------------------------------------------------

    void _FirstAction_Preview(SelectSubshapeAction sender, SelectSubshapeAction.EventArgs args)
    {
        if (args.SelectedSubshape != null)
        {
            _UpdateFirstPreview(args.SelectedSubshape);
        }
    }

    //--------------------------------------------------------------------------------------------------

    void _UpdateFirstPreview(TopoDS_Shape shape)
    {
        double val = 0;
        string label = "Measure";
        ValueUnits units = ValueUnits.Length;

        if (shape.ShapeType() == TopAbs_ShapeEnum.EDGE)
        {
            GProp_GProps massProps = new GProp_GProps();
            BRepGProp.LinearProperties(shape, massProps);
            val = massProps.Mass();
            label = "Length";
        }
        else if (shape.ShapeType() == TopAbs_ShapeEnum.FACE)
        {
            GProp_GProps massProps = new GProp_GProps();
            BRepGProp.SurfaceProperties(shape, massProps);
            val = massProps.Mass();
            label = "Area";
            units = ValueUnits.None;
        }
        else if (shape.ShapeType() == TopAbs_ShapeEnum.VERTEX)
        {
            var pnt = shape.ToVertex().Pnt();
            label = $"Pnt ({pnt.X:F1}, {pnt.Y:F1}, {pnt.Z:F1})";
            units = ValueUnits.None;
        }

        _ValueHud.Label = label;
        _ValueHud.Units = units;
        _ValueHud.Value = val;
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

            _UpdateFirstPreview(_Shape1);
            StopAction(_FirstAction);

            // Log properties of first selection
            if (_Shape1.ShapeType() == TopAbs_ShapeEnum.EDGE)
            {
                GProp_GProps massProps = new GProp_GProps();
                BRepGProp.LinearProperties(_Shape1, massProps);
                Messages.Info($"Selected Edge Length: {massProps.Mass():F4} mm");
            }
            else if (_Shape1.ShapeType() == TopAbs_ShapeEnum.FACE)
            {
                GProp_GProps massProps = new GProp_GProps();
                BRepGProp.SurfaceProperties(_Shape1, massProps);
                Messages.Info($"Selected Face Area: {massProps.Mass():F4} mm²");
            }
            else if (_Shape1.ShapeType() == TopAbs_ShapeEnum.VERTEX)
            {
                var pnt = _Shape1.ToVertex().Pnt();
                Messages.Info($"Selected Vertex Coordinates: X={pnt.X:F4}, Y={pnt.Y:F4}, Z={pnt.Z:F4}");
            }

            _SecondAction = new SelectSubshapeAction(SubshapeTypes.Vertex | SubshapeTypes.Edge | SubshapeTypes.Face);
            if (StartAction(_SecondAction))
            {
                _SecondAction.Finished += _SecondAction_Finished;
                _SecondAction.Preview += _SecondAction_Preview;
                SetHintMessage("__Select second geometric element__ to measure distance, or press `k:Esc` to finish.");
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
        try
        {
            var extrema = new BRepExtrema_DistShapeShape(_Shape1, shape2);
            if (extrema.Perform() && extrema.IsDone())
            {
                double dist = extrema.Value();
                _ValueHud.Label = "Distance";
                _ValueHud.Units = ValueUnits.Length;
                _ValueHud.Value = dist;
            }
        }
        catch
        {
            // Ignore preview exceptions to avoid crash/lag
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

            try
            {
                var extrema = new BRepExtrema_DistShapeShape(_Shape1, _Shape2);
                if (extrema.Perform() && extrema.IsDone())
                {
                    double dist = extrema.Value();
                    _ValueHud.Label = "Distance";
                    _ValueHud.Units = ValueUnits.Length;
                    _ValueHud.Value = dist;

                    string msg = $"Distance: {dist:F4} mm";
                    Messages.Info(msg);
                    SetHintMessage($"__Distance: {dist:F4} mm__. Select another element, or press `k:Esc` to finish.");

                    if (extrema.NbSolution() > 0)
                    {
                        Pnt p1 = extrema.PointOnShape1(1);
                        Pnt p2 = extrema.PointOnShape2(1);

                        if (_AisConnectionShape != null)
                        {
                            WorkspaceController.AisContext.Remove(_AisConnectionShape, false);
                        }
                        var connectionEdge = new BRepBuilderAPI_MakeEdge(p1, p2).Edge();
                        _AisConnectionShape = new AIS_Shape(connectionEdge);
                        _AisConnectionShape.SetColor(new Color(1.0f, 0.0f, 0.0f).ToQuantityColor());
                        _AisConnectionShape.SetWidth(2.5f);
                        _AisConnectionShape.SetZLayer(-2);
                        WorkspaceController.AisContext.Display(_AisConnectionShape, false);
                    }
                }
                else
                {
                    Messages.Error("Could not compute distance.");
                }
            }
            catch (Exception e)
            {
                Messages.Exception("Error calculating distance.", e);
            }
        }

        action.Reset();
        WorkspaceController.Invalidate();
    }
}
