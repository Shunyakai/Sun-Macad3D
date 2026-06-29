using System.Linq;
using Macad.Core;
using Macad.Core.Auxiliary;
using Macad.Occt;
using Macad.Presentation;

using static Macad.Interaction.CommandHelper;

namespace Macad.Interaction;

public static class AuxiliaryCommands
{
    #region Datum Plane

    public static ActionCommand CreateDatumPlane { get; } = new(
        () =>
        {
            var plane = DatumPlane.Create();
            plane.Position = InteractiveContext.Current.Workspace.WorkingPlane.Location;
            plane.Rotation = InteractiveContext.Current.Workspace.WorkingPlane.Rotation();
            InteractiveContext.Current?.Document.Add(plane);
            InteractiveContext.Current?.UndoHandler.Commit();

            Selection.SelectEntity(plane);
            Invalidate();
        },
        CanStartTool)
    {
        Header = () => "Datum Plane",
        Description = () => "Creates a new datum plane which can be used as a reference. A datum plan can be papered with an image.",
        Icon = () => "Auxiliary-DatumPlane",
        HelpTopic = "322f5cc2-0fc7-43f9-bb80-5e87cb3e3651"
    };

    //--------------------------------------------------------------------------------------------------
    public static ActionCommand SetWorkingPlaneToDatumPlane { get; } = new (
        () =>
        {
            var plane = Selection.SelectedEntities.OfType<DatumPlane>().First();
            InteractiveContext.Current.Workspace.WorkingPlane = new Pln(plane.GetCoordinateSystem());
            Invalidate();
        },
        () => Selection.SelectedEntities.OfType<DatumPlane>().Count() == 1)
    {
        Header = () => "Set as Working Plane",
        Description = () => "Sets the working plane to the selected datum plane.",
        Icon = () => "WorkingPlane-Set"
    };

    //--------------------------------------------------------------------------------------------------

    #endregion

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand CreateDatumLine { get; } = new(
        () =>
        {
            var line = DatumLine.Create();
            line.Position = InteractiveContext.Current.Workspace.WorkingPlane.Location;
            line.Rotation = InteractiveContext.Current.Workspace.WorkingPlane.Rotation();
            InteractiveContext.Current?.Document.Add(line);
            InteractiveContext.Current?.UndoHandler.Commit();

            Selection.SelectEntity(line);
            Invalidate();
        },
        CanStartTool)
    {
        Header = () => "Datum Line",
        Description = () => "Creates a new datum line which can be used as a reference.",
        Icon = () => "Auxiliary-DatumLine",
    };

    //--------------------------------------------------------------------------------------------------
    #region Annotation Label

    public static ActionCommand CreateAnnotationLabel { get; } = new(
        () =>
        {
            StartTool(new Tools.AnnotationLabelTool());
        },
        CanStartTool)
    {
        Header = () => "Annotation",
        Description = () => "Creates an annotation label pointing to an object or point.",
        Icon = () => "Tool-EdgesSelection",
    };

    #endregion

    //--------------------------------------------------------------------------------------------------

    #region Macros

    public static ActionCommand ToggleRecordMacro { get; } = new(
        () =>
        {
            if (Macros.MacroManager.Instance.IsRecording)
            {
                Macros.MacroManager.Instance.StopRecording();
                Messages.Info("Macro recording stopped.");
            }
            else
            {
                if (Macros.MacroManager.Instance.StartRecording())
                {
                    Messages.Info($"Macro recording started: {Macros.MacroManager.Instance.CurrentMacroPath}");
                }
            }
        },
        () => true)
    {
        Header = () => Macros.MacroManager.Instance.IsRecording ? "Stop Recording" : "Record Macro",
        Description = () => "Starts or stops recording actions to a macro file.",
        Icon = () => "Macro-Record"
    };

    public static ActionCommand RunMacro { get; } = new(
        () =>
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Macro files (*.csx)|*.csx|All files (*.*)|*.*",
                InitialDirectory = Macros.MacroManager.GetMacrosDirectory()
            };
            if (dlg.ShowDialog() == true)
            {
                Macros.MacroManager.Instance.RunMacro(dlg.FileName);
            }
        },
        () => !Macros.MacroManager.Instance.IsRecording)
    {
        Header = () => "Run Macro",
        Description = () => "Executes a macro script from a file.",
        Icon = () => "Macro-Run"
    };

    #endregion
}