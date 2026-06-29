using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Macad.Presentation;
using Macad.Common.Serialization;
using Macad.Core;

namespace Macad.Interaction;

[SerializeType]
public class ShortcutConfig
{
    [SerializeMember]
    public string CommandName { get; set; }
    [SerializeMember]
    public string Parameter { get; set; }
    [SerializeMember]
    public string Key { get; set; }
    [SerializeMember]
    public string ModifierKeys { get; set; }
    [SerializeMember]
    public string Scope { get; set; }
}

public sealed class ShortcutHandler
{
    //--------------------------------------------------------------------------------------------------

    readonly Dictionary<string, List<Shortcut>> _ShortcutScopes = new();
    private static readonly Dictionary<string, IActionCommand> _NameToCommand = new();
    private static readonly Dictionary<IActionCommand, string> _CommandToName = new();

    //--------------------------------------------------------------------------------------------------

    static ShortcutHandler()
    {
        RegisterCommandType(typeof(WorkspaceCommands));
        RegisterCommandType(typeof(ModelCommands));
        RegisterCommandType(typeof(ToolboxCommands));
        RegisterCommandType(typeof(SketchCommands));
        RegisterCommandType(typeof(AuxiliaryCommands));
        RegisterCommandType(typeof(DocumentCommands));
    }

    public ShortcutHandler()
    {
        // Register to action
        IActionCommand.GetShortcutDefaultHandler = GetShortcutForCommand;
    }

    public static void RegisterCommandType(Type type)
    {
        foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            if (typeof(IActionCommand).IsAssignableFrom(prop.PropertyType))
            {
                try
                {
                    if (prop.GetValue(null) is IActionCommand cmd)
                    {
                        var fullName = $"{type.Name}.{prop.Name}";
                        _NameToCommand[fullName] = cmd;
                        _CommandToName[cmd] = fullName;
                    }
                }
                catch { }
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    public IEnumerable<string> GetScopes()
    {
        return _ShortcutScopes.Keys;
    }

    //--------------------------------------------------------------------------------------------------

    public void AddShortcut(string scope, Shortcut shortcut)
    {
        if (!_ShortcutScopes.TryGetValue(scope, out var shortcutList))
        {
            shortcutList = new List<Shortcut>();
            _ShortcutScopes.Add(scope, shortcutList);
        }

        shortcutList.Add(shortcut);
    }

    //--------------------------------------------------------------------------------------------------

    public void AddShortcuts(string scope, IEnumerable<Shortcut> shortcut)
    {
        if (!_ShortcutScopes.TryGetValue(scope, out var shortcutList))
        {
            shortcutList = new List<Shortcut>();
            _ShortcutScopes.Add(scope, shortcutList);
        }

        shortcutList.AddRange(shortcut);
    }

    //--------------------------------------------------------------------------------------------------
    
    public bool KeyPressed(string scope, Key key, ModifierKeys modifierKeys)
    {
        if (!_ShortcutScopes.TryGetValue(scope, out var shortcuts))
            return false;

        var shortcut = shortcuts.Find(s => s.Key == key && s.ModifierKeys == modifierKeys);
        if (shortcut?.Command == null || !shortcut.Command.CanExecute(shortcut.Parameter))
            return false;

        shortcut.Command.Execute(shortcut.Parameter);
        return true;
    }

    //--------------------------------------------------------------------------------------------------

    public IEnumerable<Shortcut> GetShortcutsForScope(string scope)
    {
        if (_ShortcutScopes.TryGetValue(scope, out var list))
            return list;

        return Array.Empty<Shortcut>();
    }

    //--------------------------------------------------------------------------------------------------
    
    public string GetShortcutForCommand(IActionCommand command, object param)
    {
        var shortcut = _ShortcutScopes.Values
                                 .Select(list => list.FirstOrDefault(s => s.Command == command 
                                                                          && (s.Parameter?.Equals(param) ?? param == null)))
                                 .FirstOrDefault(s => s != null);
        if(shortcut == null)
            return null;

        return shortcut.GetKeyString();
    }

    //--------------------------------------------------------------------------------------------------

    public void UpdateShortcut(string scope, IActionCommand command, object parameter, Key key, ModifierKeys modifierKeys)
    {
        if (_ShortcutScopes.TryGetValue(scope, out var list))
        {
            list.RemoveAll(s => s.Command == command && (s.Parameter?.Equals(parameter) ?? parameter == null));
        }

        if (key != Key.None)
        {
            AddShortcut(scope, new Shortcut(key, modifierKeys, command, parameter));
        }
    }

    public void SaveCustomShortcuts()
    {
        var configs = new List<ShortcutConfig>();
        foreach (var scopeKvp in _ShortcutScopes)
        {
            var scope = scopeKvp.Key;
            foreach (var shortcut in scopeKvp.Value)
            {
                if (_CommandToName.TryGetValue(shortcut.Command, out var name))
                {
                    var paramStr = shortcut.Parameter == null ? "" : shortcut.Parameter.ToString();
                    configs.Add(new ShortcutConfig
                    {
                        Scope = scope,
                        CommandName = name,
                        Parameter = paramStr,
                        Key = shortcut.Key.ToString(),
                        ModifierKeys = shortcut.ModifierKeys.ToString()
                    });
                }
            }
        }
        CoreContext.Current?.SaveLocalSettings("CustomShortcuts", configs);
    }

    public void LoadCustomShortcuts()
    {
        var configs = CoreContext.Current?.LoadLocalSettings<List<ShortcutConfig>>("CustomShortcuts");
        if (configs == null || configs.Count == 0)
            return;

        _ShortcutScopes.Clear();
        foreach (var config in configs)
        {
            if (_NameToCommand.TryGetValue(config.CommandName, out var cmd))
            {
                if (Enum.TryParse<Key>(config.Key, out var key) && Enum.TryParse<ModifierKeys>(config.ModifierKeys, out var mod))
                {
                    object param = null;
                    if (!string.IsNullOrEmpty(config.Parameter))
                    {
                        if (bool.TryParse(config.Parameter, out var b))
                            param = b;
                        else
                            param = config.Parameter;
                    }
                    AddShortcut(config.Scope, new Shortcut(key, mod, cmd, param));
                }
            }
        }
    }
}