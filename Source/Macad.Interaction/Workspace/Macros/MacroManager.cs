using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Input;
using Macad.Presentation;
using Macad.Core;

namespace Macad.Interaction.Macros;

public class MacroManager
{
    public static MacroManager Instance { get; } = new MacroManager();

    public bool IsRecording { get; private set; }
    public string CurrentMacroPath { get; private set; }

    private StreamWriter _writer;
    private readonly Dictionary<ICommand, string> _commandNames = new();

    private MacroManager()
    {
        _InitCommandNames();
        CommandEventHub.CommandExecuted += OnCommandExecuted;
    }

    private void _InitCommandNames()
    {
        var classes = new[]
        {
            typeof(WorkspaceCommands),
            typeof(ModelCommands),
            typeof(ToolboxCommands),
            typeof(SketchCommands),
            typeof(AuxiliaryCommands)
        };

        foreach (var type in classes)
        {
            foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (typeof(ICommand).IsAssignableFrom(prop.PropertyType))
                {
                    try
                    {
                        if (prop.GetValue(null) is ICommand cmd)
                        {
                            _commandNames[cmd] = $"{type.Name}.{prop.Name}";
                        }
                    }
                    catch
                    {
                        // Ignore properties that fail to resolve
                    }
                }
            }
        }
    }

    public static string GetMacrosDirectory()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "Macad", "Macros");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }

    public bool StartRecording(string filePath = null)
    {
        if (IsRecording)
            return false;

        if (string.IsNullOrEmpty(filePath))
        {
            var dir = GetMacrosDirectory();
            var index = 1;
            while (File.Exists(filePath = Path.Combine(dir, $"macro_{index}.csx")))
            {
                index++;
            }
        }

        try
        {
            _writer = new StreamWriter(filePath, false);
            _writer.WriteLine("using System;");
            _writer.WriteLine("using Macad.Core;");
            _writer.WriteLine("using Macad.Core.Shapes;");
            _writer.WriteLine("using Macad.Core.Topology;");
            _writer.WriteLine("using Macad.Interaction;");
            _writer.WriteLine();

            CurrentMacroPath = filePath;
            IsRecording = true;
            return true;
        }
        catch (Exception ex)
        {
            Messages.Error($"Failed to start macro recording: {ex.Message}");
            return false;
        }
    }

    public void StopRecording()
    {
        if (!IsRecording)
            return;

        try
        {
            _writer?.Dispose();
            _writer = null;
        }
        finally
        {
            IsRecording = false;
            CurrentMacroPath = null;
        }
    }

    private void OnCommandExecuted(ICommand command, object parameter)
    {
        if (!IsRecording || _writer == null)
            return;

        var code = GetCommandCode(command, parameter);
        if (!string.IsNullOrEmpty(code))
        {
            try
            {
                _writer.WriteLine(code);
                _writer.Flush();
            }
            catch
            {
                // Silence stream write errors
            }
        }
    }

    public string GetCommandCode(ICommand command, object parameter)
    {
        if (_commandNames.TryGetValue(command, out var name))
        {
            var paramStr = "null";
            if (parameter != null)
            {
                if (parameter is string s)
                    paramStr = $"\"{s}\"";
                else if (parameter is bool b)
                    paramStr = b ? "true" : "false";
                else if (parameter.GetType().IsEnum)
                    paramStr = $"{parameter.GetType().FullName.Replace('+', '.')}.{parameter}";
                else if (parameter is double d)
                    paramStr = d.ToString(System.Globalization.CultureInfo.InvariantCulture);
                else if (parameter is int i)
                    paramStr = i.ToString();
                else
                    paramStr = "null";
            }
            return $"{name}.Execute({paramStr});";
        }
        return null;
    }

    public bool RunMacro(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Messages.Error($"Macro file does not exist: {filePath}");
            return false;
        }

        try
        {
            var script = ScriptInstance.LoadScriptFromFile(filePath, new InteractiveScriptContext(), forceReload: true);
            if (script != null)
            {
                return script.Run();
            }
            return false;
        }
        catch (Exception ex)
        {
            Messages.Error($"Failed to run macro {Path.GetFileName(filePath)}: {ex.Message}");
            return false;
        }
    }
}
