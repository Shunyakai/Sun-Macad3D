using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Macad.Core;
using Macad.Core.Auxiliary;
using Macad.Core.Shapes;
using Macad.Core.Topology;
using Macad.Interaction;
using Macad.Presentation;

namespace Macad.Window;

public partial class GeneralToolsWindow : Dialog
{
    // Memory-based lists
    private static readonly List<KeyValuePair<string, string>> _Annotations = new()
    {
        new KeyValuePair<string, string>("Design Note #1", "Ensure wall thickness is at least 3.0mm for 3D printing structural integrity."),
        new KeyValuePair<string, string>("Material Spec", "Default material configured to Brass for cosmetic parts; Steel for shafts.")
    };

    private static string _ProjectNotes = "Macad3D Project Notes:\n- Phase 1: Draw sketch profiles.\n- Phase 2: Extrude solid bodies.\n- Phase 3: Apply fillets/chamfers and export as STEP.";

    private readonly List<MockAddon> _Addons = new()
    {
        new MockAddon("SolveSpace Solver Plugin", "Assembly constraint solver integration based on SolveSpace.", "v1.2.0", true),
        new MockAddon("Finite Element Analysis (FEA)", "Warping, stress, and structural loading simulator.", "v0.9.5", false),
        new MockAddon("KiCad PCB Link", "Import PCB layouts and render them as 3D reference planes.", "v2.1.0", false),
        new MockAddon("Standard Parts Library", "Fasteners, nuts, screws, and bearings catalog.", "v1.1.2", false),
        new MockAddon("LuxCoreRender Engine", "Raytracing renderer plugin for high-quality product snapshots.", "v1.0.0", false)
    };

    // Constructor
    public GeneralToolsWindow(int activeTab = 0)
    {
        InitializeComponent();
        ToolsTabControl.SelectedIndex = activeTab;

        // Initialize tabs
        InitAddonManager();
        InitAnnotations();
        InitSelectionClarifier();
        InitTextDocument();
        InitSceneInspector();
        InitDependencyGraph();
        InitDocumentUtility();
        InitParametersEditor();
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    #region Addon Manager Tab

    private void InitAddonManager()
    {
        AddonsListPanel.Children.Clear();
        foreach (var addon in _Addons)
        {
            var card = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 85, 85)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(12)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var infoPanel = new StackPanel();
            infoPanel.Children.Add(new TextBlock { Text = $"{addon.Name} ({addon.Version})", FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(0, 0, 0, 4) });
            infoPanel.Children.Add(new TextBlock { Text = addon.Description, TextWrapping = TextWrapping.Wrap, Foreground = System.Windows.Media.Brushes.LightGray, FontSize = 11 });

            var installBtn = new Button
            {
                Content = addon.Installed ? "Uninstall" : "Install",
                Style = (Style)FindResource("Macad.Styles.Button.DialogFooter"),
                Width = 90,
                Height = 26,
                VerticalAlignment = VerticalAlignment.Center
            };
            installBtn.Click += (s, e) =>
            {
                addon.Installed = !addon.Installed;
                InitAddonManager();
            };

            Grid.SetColumn(infoPanel, 0);
            Grid.SetColumn(installBtn, 1);
            grid.Children.Add(infoPanel);
            grid.Children.Add(installBtn);
            card.Child = grid;

            AddonsListPanel.Children.Add(card);
        }
    }

    private class MockAddon
    {
        public string Name { get; }
        public string Description { get; }
        public string Version { get; }
        public bool Installed { get; set; }

        public MockAddon(string name, string desc, string ver, bool installed)
        {
            Name = name;
            Description = desc;
            Version = ver;
            Installed = installed;
        }
    }

    #endregion

    #region Annotations Tab

    private void InitAnnotations()
    {
        AnnotationsList.Items.Clear();
        foreach (var annot in _Annotations)
        {
            AnnotationsList.Items.Add(annot);
        }
    }

    private void AnnotationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AnnotationsList.SelectedItem is KeyValuePair<string, string> selected)
        {
            AnnotationTitleText.Text = selected.Key;
            AnnotationContentText.Text = selected.Value;
        }
    }

    private void NewAnnotation_Click(object sender, RoutedEventArgs e)
    {
        AnnotationTitleText.Text = "New Annotation";
        AnnotationContentText.Text = "";
        AnnotationsList.SelectedIndex = -1;
    }

    private void SaveAnnotation_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AnnotationTitleText.Text)) return;

        var newPair = new KeyValuePair<string, string>(AnnotationTitleText.Text, AnnotationContentText.Text);
        if (AnnotationsList.SelectedIndex >= 0)
        {
            _Annotations[AnnotationsList.SelectedIndex] = newPair;
        }
        else
        {
            _Annotations.Add(newPair);
        }
        InitAnnotations();
    }

    private void DeleteAnnotation_Click(object sender, RoutedEventArgs e)
    {
        if (AnnotationsList.SelectedIndex >= 0)
        {
            _Annotations.RemoveAt(AnnotationsList.SelectedIndex);
            AnnotationTitleText.Text = "";
            AnnotationContentText.Text = "";
            InitAnnotations();
        }
    }

    #endregion

    #region Selection Clarifier Tab

    private void InitSelectionClarifier()
    {
        SelectedEntitiesList.Items.Clear();
        var selected = InteractiveContext.Current?.WorkspaceController?.Selection?.SelectedEntities;
        if (selected != null)
        {
            foreach (var ent in selected)
            {
                SelectedEntitiesList.Items.Add($"{ent.Name} ({ent.GetType().Name})");
            }
        }
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        var wsCtrl = InteractiveContext.Current?.WorkspaceController;
        if (wsCtrl == null) return;

        if (FilterSolidsChk == null || FilterFacesChk == null || FilterEdgesChk == null || FilterVerticesChk == null)
            return;

        // Apply filters to selection context
        SubshapeTypes types = SubshapeTypes.None;
        if (FilterSolidsChk.IsChecked == true) types |= SubshapeTypes.All; // Allow shapes
        if (FilterFacesChk.IsChecked == true) types |= SubshapeTypes.Face;
        if (FilterEdgesChk.IsChecked == true) types |= SubshapeTypes.Edge;
        if (FilterVerticesChk.IsChecked == true) types |= SubshapeTypes.Vertex;

        wsCtrl.Selection?.ActiveContext?.SetSubshapeSelection(types);
        wsCtrl.Selection?.Invalidate();
    }

    private void SelectedEntitiesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var wsCtrl = InteractiveContext.Current?.WorkspaceController;
        if (wsCtrl == null || SelectedEntitiesList.SelectedIndex < 0) return;

        var selected = wsCtrl.Selection?.SelectedEntities;
        if (selected != null && SelectedEntitiesList.SelectedIndex < selected.Count)
        {
            var target = selected[SelectedEntitiesList.SelectedIndex];
            wsCtrl.Selection.SelectEntity(target);
            wsCtrl.Invalidate();
        }
    }

    #endregion

    #region Text Document Tab

    private void InitTextDocument()
    {
        ProjectNotesBox.Text = _ProjectNotes;
    }

    private void LoadNotes_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Text Files|*.txt|All Files|*.*" };
        if (dlg.ShowDialog() == true)
        {
            ProjectNotesBox.Text = File.ReadAllText(dlg.FileName);
            _ProjectNotes = ProjectNotesBox.Text;
        }
    }

    private void SaveNotes_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Text Files|*.txt|All Files|*.*", FileName = "DesignNotes" };
        if (dlg.ShowDialog() == true)
        {
            File.WriteAllText(dlg.FileName, ProjectNotesBox.Text);
            _ProjectNotes = ProjectNotesBox.Text;
        }
    }

    private void ClearNotes_Click(object sender, RoutedEventArgs e)
    {
        ProjectNotesBox.Text = "";
        _ProjectNotes = "";
    }

    #endregion

    #region Scene Inspector Tab

    private void InitSceneInspector()
    {
        SceneTreeView.Items.Clear();
        var doc = InteractiveContext.Current?.Document;
        if (doc == null) return;

        var root = new InspectorNode { Name = doc.Name ?? "Document Root", Details = $"{doc.Count()} entities" };
        foreach (var entity in doc)
        {
            var node = new InspectorNode
            {
                Name = entity.Name,
                Details = $"[{entity.GetType().Name}] on Layer '{entity.Layer?.Name ?? "Default"}'"
            };
            if (entity is Body body && body.Shape != null)
            {
                node.Children.Add(new InspectorNode
                {
                    Name = $"Active Shape: {body.Shape.Name}",
                    Details = $"[{body.Shape.GetType().Name}]"
                });
            }
            root.Children.Add(node);
        }
        SceneTreeView.Items.Add(root);
    }

    private class InspectorNode
    {
        public string Name { get; set; }
        public string Details { get; set; }
        public List<InspectorNode> Children { get; } = new();
    }

    #endregion

    #region Dependency Graph Tab

    private void InitDependencyGraph()
    {
        DependencyGraphList.Items.Clear();
        var body = InteractiveContext.Current?.WorkspaceController?.Selection?.SelectedEntities?.OfType<Body>()?.FirstOrDefault();
        if (body == null)
        {
            DependencyGraphList.Items.Add("Please select a Body in the viewport to view its shape dependencies.");
            return;
        }

        DependencyGraphList.Items.Add($"Body: {body.Name}");
        var shape = body.Shape;
        int step = 1;
        while (shape != null)
        {
            DependencyGraphList.Items.Add($"  Step {step++}: {shape.Name} ({shape.GetType().Name})");
            shape = shape.Predecessor as Shape;
        }
    }

    private void ExportGraph_Click(object sender, RoutedEventArgs e)
    {
        var body = InteractiveContext.Current?.WorkspaceController?.Selection?.SelectedEntities?.OfType<Body>()?.FirstOrDefault();
        if (body == null) return;

        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Graphviz DOT File (*.dot)|*.dot", FileName = $"{body.Name}_Dependencies" };
        if (dlg.ShowDialog() == true)
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
    }

    #endregion

    #region Document Utility Tab

    private void InitDocumentUtility()
    {
        var doc = InteractiveContext.Current?.Document;
        if (doc == null) return;

        int bodyCount = doc.Count(e => e is Body);
        int planeCount = doc.Count(e => e is DatumPlane);

        DocStatsBlock.Text = $"File Path: {doc.FilePath ?? "Unsaved Document"}\n" +
                             $"Total Entities: {doc.Count()}\n" +
                             $"  Bodies: {bodyCount}\n" +
                             $"  Reference Planes: {planeCount}\n" +
                             $"Layers: {doc.Layers?.Count() ?? 0}\n" +
                             $"Undo Stack Operations: {InteractiveContext.Current?.UndoHandler?.UndoStack?.Count ?? 0u}";
    }

    private void PurgeDoc_Click(object sender, RoutedEventArgs e)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Messages.Info("Garbage collection completed. Memory purged successfully.");
        InitDocumentUtility();
    }

    #endregion

    #region Edit Parameters Tab

    private Shape _ActiveParamShape;
    private readonly List<Tuple<string, double, TextBox>> _ParamBoxes = new();

    private void InitParametersEditor()
    {
        ParametersEditorPanel.Children.Clear();
        _ParamBoxes.Clear();
        _ActiveParamShape = null;

        var body = InteractiveContext.Current?.WorkspaceController?.Selection?.SelectedEntities?.OfType<Body>()?.FirstOrDefault();
        if (body == null || body.Shape == null)
        {
            ParamHeaderLabel.Text = "No Body Selected";
            ParametersEditorPanel.Children.Add(new TextBlock { Text = "Please select a parametric primitive (e.g. Box, Cylinder) to edit its dimensions.", Foreground = System.Windows.Media.Brushes.LightGray, Margin = new Thickness(0, 10, 0, 0) });
            return;
        }

        _ActiveParamShape = body.Shape;
        ParamHeaderLabel.Text = $"Edit Parameters: {body.Name} ({_ActiveParamShape.GetType().Name})";

        if (_ActiveParamShape is Box box)
        {
            AddParamField("Dimension X", box.DimensionX);
            AddParamField("Dimension Y", box.DimensionY);
            AddParamField("Dimension Z", box.DimensionZ);
        }
        else if (_ActiveParamShape is Cylinder cylinder)
        {
            AddParamField("Radius", cylinder.Radius);
            AddParamField("Height", cylinder.Height);
        }
        else if (_ActiveParamShape is Sphere sphere)
        {
            AddParamField("Radius", sphere.Radius);
        }
        else
        {
            ParametersEditorPanel.Children.Add(new TextBlock { Text = "Parameters not available or editable for this shape type.", Foreground = System.Windows.Media.Brushes.LightGray, Margin = new Thickness(0, 10, 0, 0) });
        }
    }

    private void AddParamField(string name, double val)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var label = new Label { Content = name, Foreground = System.Windows.Media.Brushes.White, VerticalAlignment = VerticalAlignment.Center };
        var text = new TextBox { Text = val.ToString("F3"), Height = 25, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)), Foreground = System.Windows.Media.Brushes.White, BorderBrush = System.Windows.Media.Brushes.Gray, Padding = new Thickness(3) };

        Grid.SetColumn(label, 0);
        Grid.SetColumn(text, 1);
        grid.Children.Add(label);
        grid.Children.Add(text);

        ParametersEditorPanel.Children.Add(grid);
        _ParamBoxes.Add(new Tuple<string, double, TextBox>(name, val, text));
    }

    private void ApplyParams_Click(object sender, RoutedEventArgs e)
    {
        if (_ActiveParamShape == null) return;

        var body = InteractiveContext.Current?.WorkspaceController?.Selection?.SelectedEntities?.OfType<Body>()?.FirstOrDefault();
        if (body == null) return;

        try
        {

            if (_ActiveParamShape is Box box)
            {
                box.DimensionX = double.Parse(_ParamBoxes[0].Item3.Text);
                box.DimensionY = double.Parse(_ParamBoxes[1].Item3.Text);
                box.DimensionZ = double.Parse(_ParamBoxes[2].Item3.Text);
            }
            else if (_ActiveParamShape is Cylinder cylinder)
            {
                cylinder.Radius = double.Parse(_ParamBoxes[0].Item3.Text);
                cylinder.Height = double.Parse(_ParamBoxes[1].Item3.Text);
            }
            else if (_ActiveParamShape is Sphere sphere)
            {
                sphere.Radius = double.Parse(_ParamBoxes[0].Item3.Text);
            }

            InteractiveContext.Current?.UndoHandler?.Commit();
            body.RaiseVisualChanged();
            InteractiveContext.Current?.WorkspaceController?.Invalidate();
            Messages.Info("Parameters applied successfully.");
        }
        catch (Exception ex)
        {
            Messages.Error($"Failed to parse or apply parameters: {ex.Message}");
        }
    }

    #endregion
}
