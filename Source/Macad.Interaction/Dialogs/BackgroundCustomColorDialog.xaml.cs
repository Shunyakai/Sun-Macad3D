using System.Windows;
using System.Windows.Media;
using Macad.Core;
using Macad.Presentation;

namespace Macad.Interaction.Dialogs;

public partial class BackgroundCustomColorDialog : Dialog
{
    public Color SelectedColor { get; set; }

    public BackgroundCustomColorDialog()
    {
        var view = InteractiveContext.Current?.ViewportController?.V3dView;
        if (view != null)
        {
            var occtColor = view.BackgroundColor();
            if (occtColor != null)
            {
                var macadColor = occtColor.ToColor();
                SelectedColor = macadColor.ToWpfColor();
            }
        }
        InitializeComponent();
        ColorPickerControl.RecentUsedColors = InteractiveContext.Current?.RecentUsedColors;
    }

    public static bool Execute(Window ownerWindow, out Color chosenColor)
    {
        var dlg = new BackgroundCustomColorDialog
        {
            Owner = ownerWindow
        };
        if (dlg.ShowDialog())
        {
            chosenColor = dlg.SelectedColor;
            return true;
        }
        chosenColor = System.Windows.Media.Colors.Transparent;
        return false;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
