using System.Windows;
using Macad.Core;
using Macad.Occt;
using Macad.Presentation;

namespace Macad.Interaction.Dialogs;

public partial class CenterViewDialog : Dialog
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public CenterViewDialog()
    {
        var view = InteractiveContext.Current?.ViewportController?.V3dView;
        if (view != null)
        {
            var center = view.Camera().Center();
            X = center.X;
            Y = center.Y;
            Z = center.Z;
        }
        InitializeComponent();
    }

    public static bool Execute(Window ownerWindow)
    {
        var dlg = new CenterViewDialog
        {
            Owner = ownerWindow
        };
        if (dlg.ShowDialog())
        {
            var view = InteractiveContext.Current?.ViewportController?.V3dView;
            if (view != null)
            {
                view.Camera().SetCenter(new Pnt(dlg.X, dlg.Y, dlg.Z));
                InteractiveContext.Current?.WorkspaceController?.Invalidate();
            }
            return true;
        }
        return false;
    }

    private void Center_Click(object sender, RoutedEventArgs e)
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
