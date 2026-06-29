using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using Macad.Presentation;

namespace Macad.Interaction.Dialogs;

public partial class PrintPreviewDialog : Dialog
{
    private BitmapSource _BitmapSource;

    public static void Execute(Window ownerWindow)
    {
        var vpController = InteractiveContext.Current?.ViewportController;
        if (vpController == null)
        {
            MessageBox.Show(ownerWindow, "No active viewport found.", "Print Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Bitmap bmp = vpController.RenderToBitmap(1920, 1080);
        if (bmp == null)
        {
            MessageBox.Show(ownerWindow, "Failed to capture viewport visual.", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        BitmapSource bmpSource = CreateBitmapSource(bmp);

        PrintPreviewDialog dlg = new(bmpSource)
        {
            Owner = ownerWindow
        };
        dlg.ShowDialog();
    }

    //--------------------------------------------------------------------------------------------------

    public PrintPreviewDialog(BitmapSource bmpSource)
    {
        _BitmapSource = bmpSource;
        InitializeComponent();
        _BuildDocument();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        PrintDialog printDlg = new();
        if (printDlg.ShowDialog() == true)
        {
            DocViewer.Print();
        }
    }

    //--------------------------------------------------------------------------------------------------

    private void _BuildDocument()
    {
        double pageWidth = 1122.5; 
        double pageHeight = 793.7;

        FixedDocument doc = new();
        PageContent pageContent = new();
        FixedPage fixedPage = new()
        {
            Width = pageWidth,
            Height = pageHeight,
            Background = System.Windows.Media.Brushes.White
        };

        double margin = 40;
        double maxWidth = pageWidth - (margin * 2);
        double maxHeight = pageHeight - (margin * 2);

        double ratio = _BitmapSource.Width / (double)_BitmapSource.Height;
        double imgWidth = maxWidth;
        double imgHeight = maxWidth / ratio;

        if (imgHeight > maxHeight)
        {
            imgHeight = maxHeight;
            imgWidth = maxHeight * ratio;
        }

        System.Windows.Controls.Image image = new()
        {
            Source = _BitmapSource,
            Width = imgWidth,
            Height = imgHeight,
            Stretch = Stretch.Uniform
        };

        FixedPage.SetLeft(image, (pageWidth - imgWidth) / 2);
        FixedPage.SetTop(image, (pageHeight - imgHeight) / 2);

        fixedPage.Children.Add(image);
        ((IAddChild)pageContent).AddChild(fixedPage);
        doc.Pages.Add(pageContent);

        DocViewer.Document = doc;
    }

    //--------------------------------------------------------------------------------------------------
    #region Win32 GDI+ Interop

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private static BitmapSource CreateBitmapSource(Bitmap bitmap)
    {
        IntPtr hBitmap = bitmap.GetHbitmap();
        try
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }

    #endregion
}
