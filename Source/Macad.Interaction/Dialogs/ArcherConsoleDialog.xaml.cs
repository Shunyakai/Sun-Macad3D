using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Macad.Core;
using Macad.Core.Topology;
using Macad.Presentation;

namespace Macad.Interaction.Dialogs;

public partial class ArcherConsoleDialog : Dialog
{
    private static ArcherConsoleDialog _Instance;

    public static void Execute(Window ownerWindow)
    {
        if (_Instance != null && _Instance.IsLoaded)
        {
            _Instance.Activate();
            return;
        }

        _Instance = new ArcherConsoleDialog
        {
            Owner = ownerWindow
        };
        _Instance.Show();
    }

    //--------------------------------------------------------------------------------------------------

    public ArcherConsoleDialog()
    {
        InitializeComponent();
        _AppendText("BRL-CAD Archer Console Emulation for Macad3D\nType 'help' to see list of drawing and list commands.\n\n");
        InputBox.Focus();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            string command = InputBox.Text;
            InputBox.Clear();
            if (!string.IsNullOrWhiteSpace(command))
            {
                _ExecuteCommand(command.Trim());
            }
            e.Handled = true;
        }
    }

    //--------------------------------------------------------------------------------------------------

    private void _AppendText(string text)
    {
        ConsoleLog.AppendText(text);
        ConsoleLog.ScrollToEnd();
    }

    private void _ExecuteCommand(string fullCommand)
    {
        _AppendText($"archer> {fullCommand}\n");

        string[] parts = fullCommand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string cmd = parts[0].ToLower();

        switch (cmd)
        {
            case "help":
                _AppendText("Available commands:\n" +
                            "  draw <name>      - Displays the object in the viewport\n" +
                            "  draw -a          - Displays all objects in the viewport\n" +
                            "  erase <name>     - Hides the object from the viewport\n" +
                            "  Z or clear       - Clears all objects from the viewport\n" +
                            "  ls or tops       - Lists top-level objects in the database\n" +
                            "  kill <name>      - Deletes the object from the database\n\n");
                break;

            case "ls":
            case "tops":
                var bodies = InteractiveContext.Current?.Document?.OfType<Body>() ?? Enumerable.Empty<Body>();
                if (!bodies.Any())
                {
                    _AppendText("No objects in current database.\n\n");
                }
                else
                {
                    _AppendText("Objects list:\n");
                    foreach (var body in bodies)
                    {
                        _AppendText($"  {body.Name}\n");
                    }
                    _AppendText("\n");
                }
                break;

            case "z":
            case "clear":
                var allBodies = InteractiveContext.Current?.Document?.OfType<Body>() ?? Enumerable.Empty<Body>();
                foreach (var b in allBodies)
                {
                    b.IsVisible = false;
                }
                InteractiveContext.Current?.WorkspaceController?.Invalidate();
                _AppendText("Graphics viewport cleared.\n\n");
                break;

            case "draw":
                if (parts.Length < 2)
                {
                    _AppendText("Usage: draw <name> OR draw -a\n\n");
                    break;
                }
                string drawTarget = parts[1];
                var bodiesToDraw = InteractiveContext.Current?.Document?.OfType<Body>() ?? Enumerable.Empty<Body>();
                if (drawTarget.ToLower() == "-a")
                {
                    foreach (var b in bodiesToDraw)
                    {
                        b.IsVisible = true;
                    }
                    InteractiveContext.Current?.WorkspaceController?.Invalidate();
                    _AppendText("Displaying all objects in viewport.\n\n");
                }
                else
                {
                    var b = bodiesToDraw.FirstOrDefault(x => x.Name.Equals(drawTarget, StringComparison.OrdinalIgnoreCase));
                    if (b != null)
                    {
                        b.IsVisible = true;
                        InteractiveContext.Current?.WorkspaceController?.Invalidate();
                        _AppendText($"Displayed: {b.Name}\n\n");
                    }
                    else
                    {
                        _AppendText($"Object '{drawTarget}' not found.\n\n");
                    }
                }
                break;

            case "erase":
                if (parts.Length < 2)
                {
                    _AppendText("Usage: erase <name>\n\n");
                    break;
                }
                string eraseTarget = parts[1];
                var bodiesToErase = InteractiveContext.Current?.Document?.OfType<Body>() ?? Enumerable.Empty<Body>();
                var eraseBody = bodiesToErase.FirstOrDefault(x => x.Name.Equals(eraseTarget, StringComparison.OrdinalIgnoreCase));
                if (eraseBody != null)
                {
                    eraseBody.IsVisible = false;
                    InteractiveContext.Current?.WorkspaceController?.Invalidate();
                    _AppendText($"Erased: {eraseBody.Name}\n\n");
                }
                else
                {
                    _AppendText($"Object '{eraseTarget}' not found.\n\n");
                }
                break;

            case "kill":
                if (parts.Length < 2)
                {
                    _AppendText("Usage: kill <name>\n\n");
                    break;
                }
                string killTarget = parts[1];
                var bodiesToKill = InteractiveContext.Current?.Document?.OfType<Body>() ?? Enumerable.Empty<Body>();
                var killBody = bodiesToKill.FirstOrDefault(x => x.Name.Equals(killTarget, StringComparison.OrdinalIgnoreCase));
                if (killBody != null)
                {
                    InteractiveContext.Current.Document.Remove(killBody);
                    InteractiveContext.Current?.WorkspaceController?.Invalidate();
                    _AppendText($"Deleted: {killBody.Name}\n\n");
                }
                else
                {
                    _AppendText($"Object '{killTarget}' not found.\n\n");
                }
                break;

            default:
                _AppendText($"Command '{cmd}' not recognized. Type 'help' for instructions.\n\n");
                break;
        }
    }
}
