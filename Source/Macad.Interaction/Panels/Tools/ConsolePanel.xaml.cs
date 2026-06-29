using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Macad.Presentation;

namespace Macad.Interaction.Panels;

public class ConsoleItem
{
    public string Text { get; set; }
    public Brush ColorBrush { get; set; }
}

public partial class ConsolePanel : UserControl
{
    public ObservableCollection<ConsoleItem> History { get; } = new();
    private ScriptState<object> _scriptState;

    public ConsolePanel()
    {
        InitializeComponent();
        DataContext = this;

        History.Add(new ConsoleItem 
        { 
            Text = "C# Interactive Console. Type C# code and press Enter to evaluate.", 
            ColorBrush = Brushes.Gray 
        });

        CommandEventHub.CommandExecuted += OnCommandExecuted;
    }

    private void OnCommandExecuted(ICommand command, object parameter)
    {
        var code = Macros.MacroManager.Instance.GetCommandCode(command, parameter);
        if (!string.IsNullOrEmpty(code))
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                History.Add(new ConsoleItem 
                { 
                    Text = $"[GUI] {code}", 
                    ColorBrush = Brushes.Orange 
                });
                historyScroll.ScrollToEnd();
            });
        }
    }

    private async void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        var input = inputBox.Text;
        if (string.IsNullOrWhiteSpace(input))
            return;

        inputBox.Text = string.Empty;
        History.Add(new ConsoleItem { Text = $"> {input}", ColorBrush = Brushes.LightGray });
        historyScroll.ScrollToEnd();

        try
        {
            if (_scriptState == null)
            {
                var options = ScriptOptions.Default
                    .WithReferences(new[]
                    {
                        typeof(object).Assembly,
                        typeof(System.Linq.Enumerable).Assembly,
                        typeof(Macad.Common.Color).Assembly,
                        typeof(Macad.Core.Topology.Model).Assembly,
                        typeof(Macad.Presentation.RelayCommand).Assembly,
                        typeof(Macad.Interaction.WorkspaceController).Assembly,
                        typeof(Macad.Occt.Pnt).Assembly,
                        typeof(Macad.Occt.Extensions.AISX_Guid).Assembly
                    })
                    .WithImports(new[]
                    {
                        "System",
                        "System.Linq",
                        "Macad.Common",
                        "Macad.Core",
                        "Macad.Core.Shapes",
                        "Macad.Core.Topology",
                        "Macad.Interaction",
                        "Macad.Occt"
                    });

                _scriptState = await CSharpScript.RunAsync(
                    input, 
                    options, 
                    globals: new InteractiveScriptContext()
                );
            }
            else
            {
                _scriptState = await _scriptState.ContinueWithAsync(input);
            }

            if (_scriptState.ReturnValue != null)
            {
                History.Add(new ConsoleItem { Text = _scriptState.ReturnValue.ToString(), ColorBrush = Brushes.Cyan });
            }
        }
        catch (Exception ex)
        {
            History.Add(new ConsoleItem { Text = ex.Message, ColorBrush = Brushes.Red });
        }
        historyScroll.ScrollToEnd();
    }
}
