using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Macad.Common;
using Macad.Core;
using Macad.Core.Shapes;
using Macad.Core.Topology;
using Macad.Interaction;
using Macad.Interaction.Dialogs;
using Macad.Presentation;
using System.Windows.Media.Imaging;

namespace Macad.Window;

public static class AppCommands
{
    static AppCommands()
    {
        Macad.Interaction.AppearancePerFaceTool.ShowAppearanceDialogCallback = (body, faceIndex) =>
        {
            var dialog = new AppearanceDialog(body, faceIndex)
            {
                Owner = MainWindow.Current
            };
            return dialog.ShowDialog() == true;
        };
    }

    public static ActionCommand ExitApplication { get; } = new(
        () => { Application.Current.MainWindow.Close(); })
    {
        Header = () => "Exit Program"
    };

    //--------------------------------------------------------------------------------------------------

    internal static RelayCommand InitApplication { get; } = new(
        () =>
        {
            Messages.Info("Welcome to Macad|3D.");

            DocumentCommands.CreateNewModel.Execute();

            // Check for update
            if (!AppContext.IsInSandbox && VersionCheck.IsAutoCheckEnabled)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(VersionCheck.BeginCheckForUpdate, DispatcherPriority.ApplicationIdle);
            }
        });

    //--------------------------------------------------------------------------------------------------

    internal static RelayCommand RunStartupCommands { get; } = new(
        () =>
        {
            var cmdArgs = AppContext.CommandLine;

            // Check for command line option to load project
            if (cmdArgs.HasPathToOpen
                && DocumentCommands.OpenFile.CanExecute(cmdArgs.PathToOpen))
            {
                // Load models immediately, import other files deferred.
                // Importers can open dialogs, which is not allowed before main window is shown
                if (PathUtils.GetExtensionWithoutPoint(cmdArgs.PathToOpen).Equals(Model.FileExtension))
                {
                    DocumentCommands.OpenFile.Execute(cmdArgs.PathToOpen);
                }
                else
                {
                    Dispatcher.CurrentDispatcher.InvokeAsync(() => DocumentCommands.OpenFile.Execute(cmdArgs.PathToOpen), DispatcherPriority.Loaded);
                }
            }

            // Check for command line option to run script
            if (cmdArgs.HasScriptToRun)
            {
                ToolboxCommands.RunScriptCommand.Execute(cmdArgs.ScriptToRun);
            }
        });

    //--------------------------------------------------------------------------------------------------

    public static RelayCommand<CancelEventArgs> PrepareWindowClose { get; } = new(
        (e) =>
        {
            if (DocumentCommands.SaveAll.CanExecute())
            {
                var result = Dialogs.AskForSavingChanges();
                switch (result)
                {
                    case TaskDialogResults.Cancel:
                        e.Cancel = true;
                        return;

                    case TaskDialogResults.Yes:
                        DocumentCommands.SaveAll.Execute();
                        e.Cancel = DocumentCommands.SaveAll.CanExecute();
                        break;

                    case TaskDialogResults.No:
                        break;
                }
            }
        });

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand RestartApplication { get; } = new(
        () =>
        {
            MainWindow.Current.Closed += _OnWindowClosed;
            MainWindow.Current.Close();
            MainWindow.Current.Closed -= _OnWindowClosed;

            void _OnWindowClosed(object source, EventArgs eventArgs)
            {
                if (AppContext.IsInSandbox)
                {
                    return;
                }

                var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
                if (exePath == null)
                    return;

                string docPath = CoreContext.Current.Document.FilePath;
                if (!File.Exists(docPath))
                {
                    docPath = "";
                }
                
                Process.Start(new ProcessStartInfo(exePath)
                {
                    UseShellExecute = true, 
                    ArgumentList = { docPath, "-nowelcome" }
                });
            }
        })
    {
        Header = () => "Restart Application",
        Description = () => "Restarts the application.",
    };

    //--------------------------------------------------------------------------------------------------
    
    public static ActionCommand ShowAboutDialog { get; } = new(
        () =>
        {
            new AboutDialog
            {
                Owner = MainWindow.Current
            }.ShowDialog();
        })
    {
        Header = () => "About Macad|3D...",
        Description = () => "Shows version and license information.",
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ShowUnitsConverter { get; } = new(
        () =>
        {
            new UnitsConverterDialog
            {
                Owner = MainWindow.Current
            }.ShowDialog();
        })
    {
        Header = () => "Units",
        Title = () => "Units Converter",
        Description = () => "Convert values between different physical/engineering units.",
        Icon = () => "Tool-UnitsConverter",
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ShowAppearance { get; } = new(
        () =>
        {
            var selectedBody = InteractiveContext.Current?.WorkspaceController?.Selection?.SelectedEntities?.OfType<Body>()?.FirstOrDefault();
            if (selectedBody != null)
            {
                new AppearanceDialog(selectedBody)
                {
                    Owner = MainWindow.Current
                }.ShowDialog();
                InteractiveContext.Current?.WorkspaceController?.Invalidate();
            }
        },
        () => InteractiveContext.Current?.WorkspaceController?.Selection?.SelectedEntities?.OfType<Body>()?.Count() == 1)
    {
        Header = () => "Appearance",
        Title = () => "Object Appearance",
        Description = () => "Set color, transparency, and predefined materials for the selected object.",
        Icon = () => "Tool-Appearance",
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand StartAppearancePerFace { get; } = new(
        () =>
        {
            var selectedBody = InteractiveContext.Current?.WorkspaceController?.Selection?.SelectedEntities?.OfType<Body>()?.FirstOrDefault();
            var wsCtrl = InteractiveContext.Current?.WorkspaceController;
            if (selectedBody != null && wsCtrl != null)
            {
                wsCtrl.StartTool(new AppearancePerFaceTool(selectedBody));
            }
        },
        () => InteractiveContext.Current?.WorkspaceController?.Selection?.SelectedEntities?.OfType<Body>()?.Count() == 1)
    {
        Header = () => "Face Appearance",
        Title = () => "Face Appearance",
        Description = () => "Select individual faces in the viewport to assign custom color, transparency, or materials.",
        Icon = () => "Tool-AppearancePerFace",
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ShowLayerEditor { get; } = new(
        () => MainWindow.Current?.Docking.ActivateToolAnchorable("Layers"))
    {
        Header = () => "Layer Editor",
        Description = () => "Opens the layer editor.",
        Icon = () => "Layer-Editor"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ShowShapeInspector { get; } = new(
        () => { MainWindow.Current?.Docking.ActivateToolAnchorable("ShapeInspector"); })
    {
        Header = () => "Shape Inspector",
        Description = () => "Opens the shape inspector.",
        Icon = () => "Tool-ShapeInspect"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ResetWindowLayout { get; } = new(
        () => { MainWindow.Current?.Docking.LoadWindowLayout("Default"); })
    {
        Header = () => "Reset Window Layout",
        Description = () => "Resets the window layout to the default layout.",
        Icon = () => "App-RestoreLayout"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand<string> ShowHelpTopic { get; } = new(
        (topicId) =>
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var url = $"https://macad3d.net/userguide/go/?version={version.Major}.{version.Minor}&guid={topicId}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        })
    {
        Header = (topicId) => "Show User Guide",
        Description = (topicId) => "Open and browse the Macad|3D User Guide.",
        Icon = (topicId) => "App-UserGuide"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand<object> ShowHelp { get; } = new(
        (obj) =>
        {
            string topicId = null;
            if (obj is string s)
            {
                topicId = s;
            }
            else
            {
                DependencyObject source = obj as DependencyObject ?? MainWindow.Current;
                DependencyObject currentObj = FocusManager.GetFocusedElement(source) as DependencyObject;
                while (topicId == null && currentObj != null)
                {
                    topicId = Help.GetTopicId(currentObj);
                    currentObj = LogicalTreeHelper.GetParent(currentObj);
                }
            }

            ShowHelpTopic.Execute(topicId ?? "");
        })
    {
        Header = (topicId) => "Show User Guide",
        Description = (topicId) => "Open and browse the Macad|3D User Guide.",
        Icon = (topicId) => "App-UserGuide"
    };
    
    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ShowShortcutCheatSheet { get; } = new(
        () => 
        {
            ShortcutCheatSheet.Execute(MainWindow.Current);
        })
    {
        Header = () => "Keyboard Shortcuts",
        Description = () => "Shows a cheatsheet with all keyboard shortcuts currently defined.",
        Icon = () => "App-ShortcutHelp"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ShowPreferencesDialog { get; } = new(
        () =>
        {
            PreferencesDialog.Execute(MainWindow.Current, RestartApplication); 
        })
    {
        Header = () => "Edit Preferences",
        Description = () => "Opens the dialog for editing preferences.",
        Icon = () => "App-Preferences"
    };

    //--------------------------------------------------------------------------------------------------

    private static DispatcherTimer _TurntableTimer;

    public static ActionCommand ToggleTurntable { get; } = new(
        () =>
        {
            if (_TurntableTimer == null)
            {
                _TurntableTimer = new DispatcherTimer(DispatcherPriority.Render)
                {
                    Interval = TimeSpan.FromMilliseconds(30)
                };
                _TurntableTimer.Tick += (s, e) =>
                {
                    var viewportController = InteractiveContext.Current?.ViewportController;
                    if (viewportController != null)
                    {
                        viewportController.Rotate(1.0, 0, 0);
                    }
                };
            }

            if (_TurntableTimer.IsEnabled)
            {
                _TurntableTimer.Stop();
                Messages.Info("Turntable view stopped.");
            }
            else
            {
                _TurntableTimer.Start();
                Messages.Info("Turntable view started.");
            }
        })
    {
        Header = () => "View Turntable",
        Description = () => "Continuously rotates the viewport camera to showcase the 3D models.",
        Icon = () => "View-Rotate"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand SaveImage { get; } = new(
        () =>
        {
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|All Files (*.*)|*.*",
                FileName = "Snapshot",
                DefaultExt = ".png"
            };
            if (dlg.ShowDialog(Application.Current.MainWindow) == true)
            {
                try
                {
                    var viewportController = InteractiveContext.Current?.ViewportController;
                    if (viewportController != null)
                    {
                        using var bitmap = viewportController.RenderToBitmap(1920, 1080);
                        if (bitmap != null)
                        {
                            bitmap.Save(dlg.FileName);
                            Messages.Info($"Screenshot saved successfully to {dlg.FileName}");
                        }
                        else
                        {
                            Messages.Error("Failed to render viewport to bitmap.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Messages.Error($"Failed to save image: {ex.Message}");
                }
            }
        })
    {
        Header = () => "Save Image",
        Description = () => "Saves the current 3D viewport rendering as an image file.",
        Icon = () => "App-SaveWorkspace"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand LoadImage { get; } = new(
        () =>
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                CheckPathExists = true,
                Filter = Macad.Interaction.Visual.ImageCache.FileDialogFilter,
                FilterIndex = 1
            };
            var result = dlg.ShowDialog(Application.Current.MainWindow) ?? false;
            if (!result)
                return;

            var pixmap = Macad.Interaction.Visual.ImageCache.LoadCachedImage(dlg.FileName, true);
            if (pixmap == null)
            {
                Macad.Interaction.Dialogs.ErrorDialogs.CannotLoadImage(dlg.FileName);
                return;
            }

            var plane = Macad.Core.Auxiliary.DatumPlane.Create();
            plane.Position = InteractiveContext.Current.Workspace.WorkingPlane.Location;
            plane.Rotation = InteractiveContext.Current.Workspace.WorkingPlane.Rotation();
            plane.ImageFilePath = dlg.FileName;

            // Try get dimensions from image
            double imageSizeX = 100;
            double imageSizeY = 100;
            try
            {
                using var stream = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                var bitmapFrame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                if (bitmapFrame.DpiX != 0 || bitmapFrame.DpiY != 0)
                {
                    imageSizeX = bitmapFrame.PixelWidth / bitmapFrame.DpiX * 25.4;
                    imageSizeY = bitmapFrame.PixelHeight / bitmapFrame.DpiY * 25.4;
                }
                else
                {
                    var imageAspect = (double)pixmap.Width() / (double)pixmap.Height();
                    imageSizeY = imageSizeX / imageAspect;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            plane.KeepAspectRatio = false;
            plane.SizeX = imageSizeX;
            plane.SizeY = imageSizeY;
            plane.KeepAspectRatio = true;

            InteractiveContext.Current?.Document.Add(plane);
            InteractiveContext.Current?.UndoHandler.Commit();

            InteractiveContext.Current?.WorkspaceController?.Selection?.SelectEntity(plane);
            InteractiveContext.Current?.WorkspaceController?.Invalidate();
        })
    {
        Header = () => "Load Image...",
        Description = () => "Loads an image and maps it to a new datum reference plane.",
        Icon = () => "Auxiliary-DatumPlane"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ExportDependencyGraph { get; } = new(
        () =>
        {
            var body = InteractiveContext.Current?.WorkspaceController?.Selection?.SelectedEntities?.OfType<Body>()?.FirstOrDefault();
            if (body == null)
            {
                Messages.Error("Please select a Body in the viewport to export its shape dependencies.");
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Graphviz DOT File (*.dot)|*.dot", FileName = $"{body.Name}_Dependencies" };
            if (dlg.ShowDialog(Application.Current.MainWindow) == true)
            {
                try
                {
                    using var sw = new StreamWriter(dlg.FileName);
                    sw.WriteLine("digraph G {");
                    sw.WriteLine("  rankdir=RL;");
                    sw.WriteLine($"  node [shape=box, style=filled, color=lightgray];");

                    var shape = body.Shape;
                    while (shape != null)
                    {
                        sw.WriteLine($"  \"{shape.Name}\" [label=\"{shape.Name}\\n({shape.GetType().Name})\"];");
                        if (shape.Predecessor is Shape pred)
                        {
                            sw.WriteLine($"  \"{shape.Name}\" -> \"{pred.Name}\";");
                        }
                        shape = shape.Predecessor as Shape;
                    }
                    sw.WriteLine("}");
                    Messages.Info($"Exported dependency graph to {dlg.FileName}");
                }
                catch (Exception ex)
                {
                    Messages.Error($"Failed to export dependency graph: {ex.Message}");
                }
            }
        },
        () => InteractiveContext.Current?.WorkspaceController?.Selection?.SelectedEntities?.OfType<Body>()?.Count() == 1)
    {
        Header = () => "Export Dependency Graph",
        Description = () => "Exports the predecessor shape history of the selected body to a Graphviz DOT file.",
        Icon = () => "App-Export"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ShowAddonManager { get; } = new(
        () =>
        {
            new GeneralToolsWindow(0)
            {
                Owner = Application.Current.MainWindow
            }.ShowDialog();
        })
    {
        Header = () => "Addon Manager",
        Description = () => "Opens the Addon Manager to install or uninstall plugins.",
        Icon = () => "App-RestoreLayout"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ShowAnnotations { get; } = new(
        () =>
        {
            new GeneralToolsWindow(1)
            {
                Owner = Application.Current.MainWindow
            }.ShowDialog();
        })
    {
        Header = () => "Annotation Label",
        Description = () => "Create, edit, and view annotations and notes in the document.",
        Icon = () => "Tool-Text"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ShowSelectionClarifier { get; } = new(
        () =>
        {
            new GeneralToolsWindow(2)
            {
                Owner = Application.Current.MainWindow
            }.ShowDialog();
        })
    {
        Header = () => "Clarify Selection",
        Description = () => "Filters and clarifies the current elements selection in the viewport.",
        Icon = () => "View-SelectionMode"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ShowTextDocument { get; } = new(
        () =>
        {
            new GeneralToolsWindow(3)
            {
                Owner = Application.Current.MainWindow
            }.ShowDialog();
        })
    {
        Header = () => "Text Document",
        Description = () => "Opens a simple text document editor for project design notes.",
        Icon = () => "Tool-Text"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ShowSceneInspector { get; } = new(
        () =>
        {
            new GeneralToolsWindow(4)
            {
                Owner = Application.Current.MainWindow
            }.ShowDialog();
        })
    {
        Header = () => "Scene Inspector",
        Description = () => "Inspects all elements and shapes in the document hierarchy tree.",
        Icon = () => "Tool-ShapeInspect"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ShowDependencyGraph { get; } = new(
        () =>
        {
            new GeneralToolsWindow(5)
            {
                Owner = Application.Current.MainWindow
            }.ShowDialog();
        })
    {
        Header = () => "Dependency Graph",
        Description = () => "Visualizes the predecessor shape dependency chain of the selected body.",
        Icon = () => "App-RestoreLayout"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ShowDocumentUtility { get; } = new(
        () =>
        {
            new GeneralToolsWindow(6)
            {
                Owner = Application.Current.MainWindow
            }.ShowDialog();
        })
    {
        Header = () => "Document Utility",
        Description = () => "Displays document statistics and allows running database garbage collection.",
        Icon = () => "App-RestoreLayout"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand ShowEditParameters { get; } = new(
        () =>
        {
            new GeneralToolsWindow(7)
            {
                Owner = Application.Current.MainWindow
            }.ShowDialog();
        })
    {
        Header = () => "Edit Parameters",
        Description = () => "Interactive parameter editor for Selected Parametric Primitives (Box, Cylinder, Sphere).",
        Icon = () => "Tool-Align"
    };

    public static ActionCommand ShowCustomizeDialog { get; } = new(
        () =>
        {
            Macad.Interaction.Dialogs.CustomizeDialog.Execute(MainWindow.Current); 
        })
    {
        Header = () => "Customize Settings",
        Description = () => "Opens the dialog for remapping shortcuts and running macros.",
        Icon = () => "App-Preferences"
    };

    //--------------------------------------------------------------------------------------------------

    public static ActionCommand CustomizationUi { get; } = new(
        () => ShowPreferencesDialog.Execute(),
        () => true)
    {
        Header = () => "Customization & UI",
        Description = () => "Configure application preferences and UI settings.",
        Icon = () => "App-Preferences"
    };

}