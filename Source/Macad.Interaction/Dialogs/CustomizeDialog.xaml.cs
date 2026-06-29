using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Macad.Presentation;

namespace Macad.Interaction.Dialogs;

public class CommandInfo : INotifyPropertyChanged
{
    public string Name { get; set; }
    public IActionCommand Command { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }

    private string _currentShortcut;
    public string CurrentShortcut
    {
        get => _currentShortcut;
        set
        {
            if (_currentShortcut != value)
            {
                _currentShortcut = value;
                RaisePropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public partial class CustomizeDialog : Dialog
{
    public ObservableCollection<CommandInfo> Commands { get; } = new();
    public ObservableCollection<string> MacroList { get; } = new();

    private Key _assignedKey = Key.None;
    private ModifierKeys _assignedModifiers = ModifierKeys.None;

    public CustomizeDialog()
    {
        _CreateCommandList();
        _LoadMacroList();

        InitializeComponent();
    }

    private void _CreateCommandList()
    {
        var commandMap = typeof(ShortcutHandler)
            .GetField("_NameToCommand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.GetValue(null) as Dictionary<string, IActionCommand>;

        if (commandMap != null)
        {
            foreach (var kvp in commandMap)
            {
                var title = kvp.Value.GetTitle(null) ?? kvp.Value.GetHeader(null) ?? kvp.Key;
                var currentShortcut = InteractiveContext.Current.ShortcutHandler.GetShortcutForCommand(kvp.Value, null) ?? "None";
                Commands.Add(new CommandInfo
                {
                    Name = kvp.Key,
                    Command = kvp.Value,
                    Title = title,
                    Description = kvp.Value.GetDescription(null) ?? "",
                    CurrentShortcut = currentShortcut
                });
            }
        }
    }

    private void _LoadMacroList()
    {
        MacroList.Clear();
        var dir = Macros.MacroManager.GetMacrosDirectory();
        if (Directory.Exists(dir))
        {
            foreach (var file in Directory.GetFiles(dir, "*.csx"))
            {
                MacroList.Add(Path.GetFileName(file));
            }
        }
    }

    private void OnCommandSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        assignTextBox.Text = string.Empty;
        _assignedKey = Key.None;
        _assignedModifiers = ModifierKeys.None;
    }

    private void OnAssignTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key;
        if (key == Key.System)
        {
            key = e.SystemKey;
        }

        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        _assignedKey = key;
        _assignedModifiers = Keyboard.Modifiers;

        var keyStrings = new List<string>();
        if (_assignedModifiers.HasFlag(ModifierKeys.Control))
            keyStrings.Add("Ctrl");
        if (_assignedModifiers.HasFlag(ModifierKeys.Alt))
            keyStrings.Add("Alt");
        if (_assignedModifiers.HasFlag(ModifierKeys.Shift))
            keyStrings.Add("Shift");
        keyStrings.Add(_assignedKey.ToString());

        assignTextBox.Text = string.Join("+", keyStrings);
    }

    private void OnAssignClick(object sender, RoutedEventArgs e)
    {
        var selected = commandListView.SelectedItem as CommandInfo;
        if (selected == null || _assignedKey == Key.None)
            return;

        InteractiveContext.Current.ShortcutHandler.UpdateShortcut("Workspace", selected.Command, null, _assignedKey, _assignedModifiers);
        
        var shortcut = new Shortcut(_assignedKey, _assignedModifiers, selected.Command);
        selected.CurrentShortcut = shortcut.GetKeyString("+");
    }

    private void OnRunMacroClick(object sender, RoutedEventArgs e)
    {
        var selected = macroListView.SelectedItem as string;
        if (selected != null)
        {
            var path = Path.Combine(Macros.MacroManager.GetMacrosDirectory(), selected);
            Macros.MacroManager.Instance.RunMacro(path);
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        InteractiveContext.Current.ShortcutHandler.SaveCustomShortcuts();
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    public static bool Execute(Window ownerWindow)
    {
        CustomizeDialog dlg = new()
        {
            Owner = ownerWindow
        };
        return dlg.ShowDialog();
    }
}
