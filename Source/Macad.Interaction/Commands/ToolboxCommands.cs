using System.Linq;
using System.Windows;
using System.Windows.Data;
using Macad.Interaction.Dialogs;
using Macad.Interaction.Editors.Toolkits;
using Microsoft.Win32;
using Macad.Core;
using Macad.Presentation;
using Macad.Common;
using Macad.Core.Drawing;
using Macad.Core.Shapes;
using Macad.Core.Topology;

using static Macad.Interaction.CommandHelper;

namespace Macad.Interaction;

public static class ToolboxCommands
{
    public static ActionCommand RunScriptFrom { get; } = new(
        () =>
        {
            PrewarmScriptCompiler.Execute();

            var dlg = new OpenFileDialog()
            {
                Title = "Open Script...",
                CheckFileExists = true,
                Filter = "C# script file|*.csx",
                DefaultExt = "csx",
            };
            var result = dlg.ShowDialog(Application.Current.MainWindow);
            if (result ?? false)
            {
                RunScriptCommand.Execute(dlg.FileName);
            }
        },
        () => true
    )
    {
        Header = () => "Run Script",
        Title = () => "Run Script From File",
        Description = () => "Select and execute a script file (.csx).",
        Icon = () => "Tool-RunScript"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand<string> RunScriptCommand { get; } = new(
        (param) =>
        {
            if (param.IsNullOrEmpty())
                return;

            ScriptInstance script;
            bool success = false;
            using (new WaitCursor())
            {
                script = ScriptInstance.LoadScriptFromFile(param, InteractiveScriptContext.Default);
                success = script?.Run() ?? false;
            }
            if (!success)
            {
                ErrorDialogs.CannotRunScript(param);
                return;
            }

            InteractiveContext.Current.AddToScriptMruList(script.Path);
            Invalidate();
        },
        (param) => true
    )
    {
        Header = (param) => "Run Script",
        Title = (param) =>"Run Script From File",
        Description = (param) =>"Select and execute a script file (.csx).",
        Icon = (param) => "Tool-RunScript"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ScriptCreate { get; } = new(
        () =>
        {
            var box = Box.Create(10, 10, 10);
            var body = Body.Create(box);
            InteractiveContext.Current.Document.Add(body);
            Commit();
            Invalidate();
            Messages.Info("Created a box body via automation.");
        },
        () => InteractiveContext.Current?.Document != null
    )
    {
        Header = () => "Create",
        Title = () => "Create Solid",
        Description = () => "Create a solid body using automation.",
        Icon = () => "Tool-RunScript"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ScriptUpdate { get; } = new(
        () =>
        {
            var body = Selection?.SelectedEntities?.FirstOrDefault() as Body;
            if (body == null)
            {
                Messages.Error("Please select a body to update.");
                return;
            }
            Shape shape = body.Shape;
            Box box = null;
            while (shape != null)
            {
                if (shape is Box b)
                {
                    box = b;
                    break;
                }
                shape = shape.Predecessor as Shape;
            }
            if (box != null)
            {
                box.DimensionX += 5;
                box.DimensionY += 5;
                box.DimensionZ += 5;
                box.RaiseShapeChanged();
                Commit();
                Invalidate();
                Messages.Info("Selected Box shape updated successfully.");
            }
            else
            {
                Messages.Warning("The selected body does not contain a Box shape to update.");
            }
        },
        () => Selection?.SelectedEntities?.Any(entity => entity is Body) ?? false
    )
    {
        Header = () => "Update",
        Title = () => "Update Solid",
        Description = () => "Update/modify the selected solid using automation.",
        Icon = () => "Tool-RunScript"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand AutomationScripting { get; } = new(
        () => ScriptCreate.Execute(),
        () => ScriptCreate.CanExecute()
    )
    {
        Header = () => "Automation & Scripting",
        Title = () => "Automation & Scripting",
        Description = () => "Create or update solid bodies using automation.",
        Icon = () => "Tool-RunScript"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand FileExchange { get; } = new(
        () => ExchangeCommands.ImportFileToModel.Execute(),
        () => ExchangeCommands.ImportFileToModel.CanExecute()
    )
    {
        Header = () => "File & Data Exchange",
        Title = () => "File & Data Exchange",
        Description = () => "Import and export data or manage model files.",
        Icon = () => "App-Import"
    };

    public static ActionCommand PrewarmScriptCompiler { get; } = new(
        () => ScriptUtils.WarmupScriptCompiler(),
        () => true
    )
    {
        Header = () => "Prewarm Compiler",
        Title = () => "Prewarm Compiler",
        Description = () => "Prewarm the script compiler to speed up execution.",
        Icon = () => "Tool-RunScript"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ExportViewHlr { get; } = new(
        () =>
        {
            ExportViewportHlr.Execute(Application.Current.MainWindow, InteractiveContext.Current?.ViewportController?.Viewport);
        },
        () => InteractiveContext.Current?.ViewportController?.Viewport != null)
    {
        Header = () => "Create From View",
        Title = () => "Create Drawing From View",
        Description = () => "Create a 2D line drawing from the current view and save it to a vector file.",
        Icon = () => "View-ExportDrawing",
        HelpTopic = "0c834add-faf4-48f0-a8c3-e6dce411774c"
    };

    //--------------------------------------------------------------------------------------------------
        
    public static ActionCommand CreateSliceContour { get; } = new(
        () =>
        {
            if (CurrentTool is SliceContourEditTool)
            {
                CurrentTool.Stop();
            }
            else
            {
                var body = Selection?.SelectedEntities?.First() as Body;
                if (body == null)
                    return;

                var tool = new SliceContourEditTool(body);
                StartTool(tool);
            }
        },
        () => CanExecuteOnSingleSolid() || CurrentTool is SliceContourEditTool)
    {
        Header = () => "Slice Contour",
        Title = () => "Create Slice Contour",
        Description = () => "Create and export the contours of a number of slices from the selected body.",
        Icon = () => "Make-SliceContour",
        HelpTopic = "0c834add-faf4-48f0-a8c3-e6dce411774c",
        IsCheckedBinding = BindingHelper.Create(InteractiveContext.Current, $"{nameof(EditorState)}.{nameof(EditorState.ActiveTool)}", BindingMode.TwoWay,
                                                EqualityToBoolConverter.Instance, nameof(SliceContourEditTool))
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand CreateEtchingMask { get; } = new(
        () =>
        {
            if (CurrentTool is EtchingMaskEditTool)
            {
                CurrentTool.Stop();
            }
            else
            {
                var body = Selection?.SelectedEntities?.First() as Body;
                if (body == null)
                    return;

                var tool = new EtchingMaskEditTool(body);
                StartTool(tool);
            }
        },
        () => CanExecuteOnSingleSolid() || CurrentTool is EtchingMaskEditTool)
    {
        Header = () => "Etching Mask",
        Title = () => "Create Etching Mask",
        Description = () => "Create and export the mask from the selected body for etching.",
        Icon = () => "Make-EtchingMask",
        HelpTopic = "0c834add-faf4-48f0-a8c3-e6dce411774c",
        IsCheckedBinding = BindingHelper.Create(InteractiveContext.Current, $"{nameof(EditorState)}.{nameof(EditorState.ActiveTool)}", BindingMode.TwoWay,
                                                EqualityToBoolConverter.Instance, nameof(EtchingMaskEditTool))
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ExportPipeDrawing { get; } = new(
        () =>
        {
            var body = Selection.SelectedEntities.First() as Body;
            if (body == null || PipeDrawing.FindPipeModifier(body) == null)
            {
                return; // That shouldn't ever happen
            }

            if (!ExportDialog.Execute<IDrawingExporter>(out string fileName, out var exporter))
                return; // Cancelled

            if (!ExchangerSettings.Execute<IDrawingExporter>(exporter))
                return;

            Drawing drawing = new()
            {
                Name = "Pipe"
            };
            drawing.Add(PipeDrawing.Create(body));

            if (!exporter.DoExport(fileName, drawing))
            {
                ErrorDialogs.CannotExport(fileName);
            }
        },
        () => CanExecuteOnSingleSolid() 
              && PipeDrawing.FindPipeModifier(Selection.SelectedEntities.First() as Body) != null)
    {
        Header = () => "Pipe Drawing",
        Title = () => "Create Pipe Drawing",
        Description = () => "Create and export a drawing with dimensions for bending pipes.",
        Icon = () => "Make-PipeDrawing",
        HelpTopic = "69425fd0-ff1a-4dc3-9014-12860684e057"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ConvertToEditableSketch { get; } = new(
        () =>
        {
            var bodies = Selection.SelectedEntities
                                  .OfType<Body>()
                                  .Where(body => body.Shape.ShapeType == ShapeType.Sketch)
                                  .ToArray();

            if (bodies.Length == 0)
                return;

            bool success;
            using (new WaitCursor())
            {
                success = Core.Toolkits.ConvertToEditableSketch.CollapseShapeStack(bodies);
            }
            if (!success)
            {
                ErrorDialogs.CommandExecutionFailed("The selected shape cannot be converted to a editable sketch.");
                return;
            }

            Commit();
        },
        CanExecuteOnMultiSketch)
    {
        Header = () => "Convert to Sketch",
        Title = () => "Convert to editable Sketch",
        Description = () => "Convert the current shape into an editable sketch and replace the shape stack of the selected body with it.",
        Icon = () => "Tool-ConvertToSketch",
    };

    //--------------------------------------------------------------------------------------------------
        
    public static ActionCommand ConvertToSolid { get; } = new(
        () =>
        {
            var bodies = Selection.SelectedEntities
                                  .OfType<Body>()
                                  .Where(body => body.Shape.ShapeType is ShapeType.Solid or ShapeType.Mesh)
                                  .ToArray();

            if (bodies.Length == 0)
                return;

            bool success;
            using (new WaitCursor())
            {
                success = Core.Toolkits.ConvertToSolid.CollapseShapeStack(bodies);
            }
            if (!success)
            {
                ErrorDialogs.CommandExecutionFailed("The selected shape cannot be converted to a solid.");
                return;
            }

            Commit();
        },
        () => CanExecuteOnSingle(entity => (entity as Body)?.Shape?.ShapeType is ShapeType.Solid or ShapeType.Mesh))
    {
        Header = () => "Convert to Solid",
        Title = () => "Convert to Solid",
        Description = () => "Convert the current shape into a single solid shape and replace the whole shape stack of the selected body with it.",
        Icon = () => "Tool-ConvertToSolid",
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand MeasureDistance { get; } = new(
        () =>
        {
            var wsCtrl = InteractiveContext.Current?.WorkspaceController;
            if (wsCtrl != null)
            {
                wsCtrl.StartTool(new MeasureDistanceTool());
                Invalidate();
            }
        },
        CanExecuteOnWorkspace)
    {
        Header = () => "Measure",
        Title = () => "Measure Distance & Properties",
        Description = () => "Measure the properties of a subshape (vertex coordinate, edge length, face area) or shortest distance between two selected elements.",
        Icon = () => "Tool-MeasureDistance",
        IsCheckedBinding = BindingHelper.Create(InteractiveContext.Current, $"{nameof(EditorState)}.{nameof(EditorState.ActiveTool)}", BindingMode.OneWay, 
                                                EqualityToBoolConverter.Instance, nameof(MeasureDistanceTool))
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand MeasureAngle { get; } = new(
        () =>
        {
            var wsCtrl = InteractiveContext.Current?.WorkspaceController;
            if (wsCtrl != null)
            {
                wsCtrl.StartTool(new MeasureAngleTool());
                Invalidate();
            }
        },
        CanExecuteOnWorkspace)
    {
        Header = () => "Angle",
        Title = () => "Measure Angle",
        Description = () => "Measure the angle between two planar faces or linear edges.",
        Icon = () => "Tool-MeasureAngle",
        IsCheckedBinding = BindingHelper.Create(InteractiveContext.Current, $"{nameof(EditorState)}.{nameof(EditorState.ActiveTool)}", BindingMode.OneWay, 
                                                EqualityToBoolConverter.Instance, nameof(MeasureAngleTool))
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand MeasureMassProperties { get; } = new(
        () =>
        {
            var wsCtrl = InteractiveContext.Current?.WorkspaceController;
            if (wsCtrl != null)
            {
                wsCtrl.StartTool(new MeasureMassPropertiesTool());
                Invalidate();
            }
        },
        CanExecuteOnWorkspace)
    {
        Header = () => "Mass Props",
        Title = () => "Calculate Mass Properties",
        Description = () => "Calculate the mass properties (surface area, volume, mass, center of gravity, moments of inertia) of a selected solid body.",
        Icon = () => "Tool-MeasureMassProperties",
        IsCheckedBinding = BindingHelper.Create(InteractiveContext.Current, $"{nameof(EditorState)}.{nameof(EditorState.ActiveTool)}", BindingMode.OneWay, 
                                                EqualityToBoolConverter.Instance, nameof(MeasureMassPropertiesTool))
    };

    //--------------------------------------------------------------------------------------------------

}