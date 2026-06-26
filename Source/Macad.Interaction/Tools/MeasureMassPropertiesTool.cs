using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Macad.Common;
using Macad.Core;
using Macad.Core.Shapes;
using Macad.Core.Topology;
using Macad.Occt;
using Macad.Presentation;

namespace Macad.Interaction;

public class MeasureMassPropertiesTool : Tool
{
    SelectEntityAction<Body> _SelectAction;
    AIS_Shape _AisShape;

    //--------------------------------------------------------------------------------------------------

    protected override bool OnStart()
    {
        _SelectAction = new SelectEntityAction<Body>(this);
        if (!StartAction(_SelectAction))
            return false;

        _SelectAction.Finished += _SelectAction_Finished;

        SetHintMessage("__Select a solid body__ to calculate its mass properties.");
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
        if (_AisShape != null)
        {
            WorkspaceController.AisContext.Remove(_AisShape, false);
            _AisShape = null;
        }
    }

    //--------------------------------------------------------------------------------------------------

    void _SelectAction_Finished(SelectEntityAction<Body> action, SelectEntityAction<Body>.EventArgs args)
    {
        if (args.SelectedEntity != null)
        {
            var body = args.SelectedEntity;
            var shape = body.GetTransformedBRep();
            if (shape == null)
            {
                Messages.Error("Could not retrieve geometric representation for selected body.");
                action.Reset();
                return;
            }

            _ClearVisuals();

            // Highlight the selected body
            _AisShape = new AIS_Shape(shape);
            _AisShape.SetColor(new Color(0.0f, 0.8f, 0.0f).ToQuantityColor());
            _AisShape.SetWidth(3);
            _AisShape.SetZLayer(-2);
            WorkspaceController.AisContext.Display(_AisShape, false);

            try
            {
                // Calculate properties
                var volProps = new GProp_GProps();
                BRepGProp.VolumeProperties(shape, volProps);
                double volume = volProps.Mass();
                Pnt centerOfMass = volProps.CentreOfMass();
                Mat matrix = volProps.MatrixOfInertia();

                var surfProps = new GProp_GProps();
                BRepGProp.SurfaceProperties(shape, surfProps);
                double area = surfProps.Mass();

                double mass = volume * 0.001; // assuming standard density is 1.0 g/cm³ (0.001 g/mm³)

                string infoText = 
                    $"Surface Area: {area:F4} mm²\n" +
                    $"Volume: {volume:F4} mm³\n" +
                    $"Mass (Density: 1.0 g/cm³): {mass:F4} g\n\n" +
                    $"Center of Mass:\n" +
                    $"  X: {centerOfMass.X:F4} mm\n" +
                    $"  Y: {centerOfMass.Y:F4} mm\n" +
                    $"  Z: {centerOfMass.Z:F4} mm\n\n" +
                    $"Moments of Inertia:\n" +
                    $"  Row 1: [ {matrix.Row(1).X:E4}, {matrix.Row(1).Y:E4}, {matrix.Row(1).Z:E4} ]\n" +
                    $"  Row 2: [ {matrix.Row(2).X:E4}, {matrix.Row(2).Y:E4}, {matrix.Row(2).Z:E4} ]\n" +
                    $"  Row 3: [ {matrix.Row(3).X:E4}, {matrix.Row(3).Y:E4}, {matrix.Row(3).Z:E4} ]";

                // Print to log
                string logText = $"Mass Properties of '{body.Name}':\n" + infoText;
                Messages.Info(logText);

                // Show to user
                TaskDialog.ShowMessage(
                    Application.Current.MainWindow,
                    $"Mass Properties - {body.Name}",
                    infoText,
                    "Mass Properties",
                    TaskDialogCommonButtons.OK,
                    TaskDialogIcon.Information
                );
            }
            catch (Exception e)
            {
                Messages.Exception("Error calculating mass properties.", e);
            }
        }

        action.Reset();
        WorkspaceController.Invalidate();
    }
}
