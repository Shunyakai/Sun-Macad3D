using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Macad.Common;
using Macad.Core.Topology;
using Macad.Core.Components;
using Macad.Occt;
using Macad.Presentation;

namespace Macad.Window;

/// <summary>
/// Interaction logic for AppearanceDialog.xaml
/// </summary>
public partial class AppearanceDialog : Dialog
{
    public class ColorItem
    {
        public string Name { get; }
        public Color Color { get; }

        public ColorItem(string name, Color color)
        {
            Name = name;
            Color = color;
        }

        public override string ToString() => Name;
    }

    public class MaterialItem
    {
        public string Name { get; }
        public Graphic3d_NameOfMaterial Value { get; }

        public MaterialItem(string name, Graphic3d_NameOfMaterial value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString() => Name;
    }

    private readonly Body _Body;
    private readonly int _FaceIndex = -1; // -1 means editing entire body
    private readonly VisualStyle _VisualStyle;
    private readonly List<ColorItem> _Colors;
    private readonly List<MaterialItem> _Materials;

    public AppearanceDialog(Body body) : this(body, -1)
    {
    }

    public AppearanceDialog(Body body, int faceIndex)
    {
        _Body = body;
        _FaceIndex = faceIndex;
        _VisualStyle = VisualStyle.Create(_Body);

        _Colors = new List<ColorItem>
        {
            new ColorItem("Default Gray", new Color(0.75f, 0.75f, 0.75f)),
            new ColorItem("Red", new Color(1.0f, 0.0f, 0.0f)),
            new ColorItem("Orange", new Color(1.0f, 0.5f, 0.0f)),
            new ColorItem("Yellow", new Color(1.0f, 1.0f, 0.0f)),
            new ColorItem("Green", new Color(0.0f, 0.8f, 0.0f)),
            new ColorItem("Cyan", new Color(0.0f, 0.8f, 0.8f)),
            new ColorItem("Blue", new Color(0.0f, 0.0f, 1.0f)),
            new ColorItem("Purple", new Color(0.6f, 0.0f, 0.6f)),
            new ColorItem("White", new Color(1.0f, 1.0f, 1.0f)),
            new ColorItem("Dark Gray", new Color(0.3f, 0.3f, 0.3f)),
            new ColorItem("Black", new Color(0.0f, 0.0f, 0.0f))
        };

        _Materials = new List<MaterialItem>
        {
            new MaterialItem("Default", Graphic3d_NameOfMaterial.DEFAULT),
            new MaterialItem("Brass", Graphic3d_NameOfMaterial.Brass),
            new MaterialItem("Bronze", Graphic3d_NameOfMaterial.Bronze),
            new MaterialItem("Copper", Graphic3d_NameOfMaterial.Copper),
            new MaterialItem("Gold", Graphic3d_NameOfMaterial.Gold),
            new MaterialItem("Silver", Graphic3d_NameOfMaterial.Silver),
            new MaterialItem("Steel", Graphic3d_NameOfMaterial.Steel),
            new MaterialItem("Stone", Graphic3d_NameOfMaterial.Stone),
            new MaterialItem("Obsidian", Graphic3d_NameOfMaterial.Obsidian),
            new MaterialItem("Jade", Graphic3d_NameOfMaterial.Jade),
            new MaterialItem("Glass", Graphic3d_NameOfMaterial.Glass),
            new MaterialItem("Diamond", Graphic3d_NameOfMaterial.Diamond)
        };

        InitializeComponent();

        TargetLabel.Text = _FaceIndex == -1 
            ? $"Editing Appearance of '{_Body.Name}'" 
            : $"Editing Appearance of Face #{_FaceIndex} on '{_Body.Name}'";

        foreach (var c in _Colors)
        {
            ColorCombo.Items.Add(c);
        }

        foreach (var m in _Materials)
        {
            MaterialCombo.Items.Add(m);
        }

        _LoadCurrentValues();
    }

    private void _LoadCurrentValues()
    {
        Color currentColor;
        float currentTransparency;
        Graphic3d_NameOfMaterial currentMaterial;

        if (_FaceIndex == -1)
        {
            currentColor = _VisualStyle.Color;
            currentTransparency = _VisualStyle.Transparency;
            currentMaterial = _VisualStyle.Material;
        }
        else
        {
            var faceApp = _VisualStyle.FaceAppearances?.FirstOrDefault(f => f.FaceIndex == _FaceIndex);
            if (faceApp != null)
            {
                currentColor = faceApp.Color;
                currentTransparency = faceApp.Transparency;
                currentMaterial = faceApp.Material;
            }
            else
            {
                currentColor = _Colors[0].Color;
                currentTransparency = 0.0f;
                currentMaterial = Graphic3d_NameOfMaterial.DEFAULT;
            }
        }

        // Set Color
        var matchedColor = _Colors.FirstOrDefault(c => 
            Math.Abs(c.Color.Red - currentColor.Red) < 0.05 && 
            Math.Abs(c.Color.Green - currentColor.Green) < 0.05 && 
            Math.Abs(c.Color.Blue - currentColor.Blue) < 0.05);

        ColorCombo.SelectedItem = matchedColor ?? _Colors[0];

        // Set Transparency
        TransparencySlider.Value = (int)(currentTransparency * 100);
        TransparencyValueText.Text = $"{(int)TransparencySlider.Value}%";

        // Set Material
        var matchedMaterial = _Materials.FirstOrDefault(m => m.Value == currentMaterial);
        MaterialCombo.SelectedItem = matchedMaterial ?? _Materials[0];
    }

    private void ColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Preview or nothing
    }

    private void TransparencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TransparencyValueText != null)
        {
            TransparencyValueText.Text = $"{(int)TransparencySlider.Value}%";
        }
    }

    private void ResetBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_FaceIndex == -1)
        {
            _VisualStyle.Color = new Color(0.75f, 0.75f, 0.75f);
            _VisualStyle.Transparency = 0.0f;
            _VisualStyle.Material = Graphic3d_NameOfMaterial.DEFAULT;
        }
        else
        {
            if (_VisualStyle.FaceAppearances != null)
            {
                var faceApp = _VisualStyle.FaceAppearances.FirstOrDefault(f => f.FaceIndex == _FaceIndex);
                if (faceApp != null)
                {
                    var updatedList = _VisualStyle.FaceAppearances.ToList();
                    updatedList.Remove(faceApp);
                    _VisualStyle.FaceAppearances = updatedList;
                }
            }
        }

        DialogResult = true;
        Close();
    }

    private void OkBtn_Click(object sender, RoutedEventArgs e)
    {
        Color selectedColor = (ColorCombo.SelectedItem as ColorItem)?.Color ?? _Colors[0].Color;
        float selectedTransparency = (float)(TransparencySlider.Value / 100.0);
        Graphic3d_NameOfMaterial selectedMaterial = (MaterialCombo.SelectedItem as MaterialItem)?.Value ?? Graphic3d_NameOfMaterial.DEFAULT;

        if (_FaceIndex == -1)
        {
            _VisualStyle.Color = selectedColor;
            _VisualStyle.Transparency = selectedTransparency;
            _VisualStyle.Material = selectedMaterial;
        }
        else
        {
            var updatedList = _VisualStyle.FaceAppearances?.ToList() ?? new List<FaceAppearance>();
            var faceApp = updatedList.FirstOrDefault(f => f.FaceIndex == _FaceIndex);
            if (faceApp != null)
            {
                faceApp.Color = selectedColor;
                faceApp.Transparency = selectedTransparency;
                faceApp.Material = selectedMaterial;
            }
            else
            {
                updatedList.Add(new FaceAppearance(_FaceIndex, selectedColor, selectedTransparency, selectedMaterial));
            }
            _VisualStyle.FaceAppearances = updatedList;
        }

        DialogResult = true;
        Close();
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
