using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Macad.Presentation;

namespace Macad.Window;

/// <summary>
/// Interaction logic for UnitsConverterDialog.xaml
/// </summary>
public partial class UnitsConverterDialog : Dialog
{
    public class Unit
    {
        public string Name { get; }
        public string Symbol { get; }
        public double Factor { get; } // Multiplier to get to base unit

        public Unit(string name, string symbol, double factor)
        {
            Name = name;
            Symbol = symbol;
            Factor = factor;
        }

        public override string ToString() => $"{Name} ({Symbol})";
    }

    public class Category
    {
        public string Name { get; }
        public List<Unit> Units { get; }

        public Category(string name, List<Unit> units)
        {
            Name = name;
            Units = units;
        }

        public override string ToString() => Name;
    }

    private readonly List<Category> _Categories;
    private bool _IsUpdating;

    public UnitsConverterDialog()
    {
        _Categories = new List<Category>
        {
            new Category("Length", new List<Unit>
            {
                new Unit("Millimeter", "mm", 1.0),
                new Unit("Centimeter", "cm", 10.0),
                new Unit("Meter", "m", 1000.0),
                new Unit("Inch", "in", 25.4),
                new Unit("Foot", "ft", 304.8),
                new Unit("Yard", "yd", 914.4)
            }),
            new Category("Area", new List<Unit>
            {
                new Unit("Square Millimeter", "mm²", 1.0),
                new Unit("Square Centimeter", "cm²", 100.0),
                new Unit("Square Meter", "m²", 1000000.0),
                new Unit("Square Inch", "in²", 645.16),
                new Unit("Square Foot", "ft²", 92903.04)
            }),
            new Category("Volume", new List<Unit>
            {
                new Unit("Cubic Millimeter", "mm³", 1.0),
                new Unit("Cubic Centimeter", "cm³", 1000.0),
                new Unit("Cubic Meter", "m³", 1000000000.0),
                new Unit("Milliliter", "ml", 1000.0),
                new Unit("Liter", "l", 1000000.0),
                new Unit("Cubic Inch", "in³", 16387.064),
                new Unit("Cubic Foot", "ft³", 28316846.592)
            }),
            new Category("Angle", new List<Unit>
            {
                new Unit("Degree", "°", 1.0),
                new Unit("Radian", "rad", 180.0 / Math.PI),
                new Unit("Gradian", "grad", 0.9)
            }),
            new Category("Mass", new List<Unit>
            {
                new Unit("Gram", "g", 1.0),
                new Unit("Kilogram", "kg", 1000.0),
                new Unit("Ounce", "oz", 28.349523125),
                new Unit("Pound", "lb", 453.59237)
            }),
            new Category("Density", new List<Unit>
            {
                new Unit("Gram per Cubic Centimeter", "g/cm³", 1.0),
                new Unit("Kilogram per Cubic Meter", "kg/m³", 0.001),
                new Unit("Pound per Cubic Inch", "lb/in³", 27.67990471)
            })
        };

        InitializeComponent();

        InputBox.Text = "1.0";
        
        foreach (var category in _Categories)
        {
            CategoryCombo.Items.Add(category);
        }
        CategoryCombo.SelectedIndex = 0;
    }

    private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryCombo.SelectedItem is not Category category)
            return;

        _IsUpdating = true;

        FromUnitCombo.Items.Clear();
        ToUnitCombo.Items.Clear();

        foreach (var unit in category.Units)
        {
            FromUnitCombo.Items.Add(unit);
            ToUnitCombo.Items.Add(unit);
        }

        FromUnitCombo.SelectedIndex = 0;
        ToUnitCombo.SelectedIndex = Math.Min(1, category.Units.Count - 1);

        _IsUpdating = false;
        PerformConversion();
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        PerformConversion();
    }

    private void FromUnitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PerformConversion();
    }

    private void ToUnitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PerformConversion();
    }

    private void SwapBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_IsUpdating) return;

        int fromIndex = FromUnitCombo.SelectedIndex;
        int toIndex = ToUnitCombo.SelectedIndex;

        if (fromIndex >= 0 && toIndex >= 0)
        {
            _IsUpdating = true;
            FromUnitCombo.SelectedIndex = toIndex;
            ToUnitCombo.SelectedIndex = fromIndex;
            _IsUpdating = false;
            PerformConversion();
        }
    }

    private void CopyBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(ResultBox.Text))
        {
            try
            {
                Clipboard.SetText(ResultBox.Text);
            }
            catch (Exception)
            {
                // Ignore clipboard issues
            }
        }
    }

    private void PerformConversion()
    {
        if (_IsUpdating) return;

        if (FromUnitCombo.SelectedItem is not Unit fromUnit || ToUnitCombo.SelectedItem is not Unit toUnit)
            return;

        string inputStr = InputBox.Text.Trim();
        if (double.TryParse(inputStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ||
            double.TryParse(inputStr, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
        {
            // Convert to base unit first
            double baseValue = value * fromUnit.Factor;
            // Convert from base unit to target unit
            double targetValue = baseValue / toUnit.Factor;

            ResultBox.Text = targetValue.ToString("F6", CultureInfo.InvariantCulture);
        }
        else
        {
            ResultBox.Text = "Invalid Input";
        }
    }
}
