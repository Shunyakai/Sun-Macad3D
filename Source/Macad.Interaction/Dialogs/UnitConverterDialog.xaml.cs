using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Macad.Presentation;

namespace Macad.Interaction.Dialogs;

public partial class UnitConverterDialog : Dialog, INotifyPropertyChanged
{
    public static void Execute(Window ownerWindow)
    {
        UnitConverterDialog dlg = new()
        {
            Owner = ownerWindow
        };
        dlg.ShowDialog();
    }

    //--------------------------------------------------------------------------------------------------

    public new event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        if (name != nameof(OutputText))
        {
            _Recalculate();
        }
    }

    //--------------------------------------------------------------------------------------------------

    public List<string> Categories { get; } = new() { "Length", "Angle", "Area", "Volume" };

    private string _SelectedCategory = "Length";
    public string SelectedCategory
    {
        get => _SelectedCategory;
        set
        {
            _SelectedCategory = value;
            OnPropertyChanged(nameof(SelectedCategory));
            _UpdateAvailableUnits();
        }
    }

    private List<string> _AvailableUnits;
    public List<string> AvailableUnits
    {
        get => _AvailableUnits;
        set { _AvailableUnits = value; OnPropertyChanged(nameof(AvailableUnits)); }
    }

    private string _FromUnit;
    public string FromUnit
    {
        get => _FromUnit;
        set { _FromUnit = value; OnPropertyChanged(nameof(FromUnit)); }
    }

    private string _ToUnit;
    public string ToUnit
    {
        get => _ToUnit;
        set { _ToUnit = value; OnPropertyChanged(nameof(ToUnit)); }
    }

    private string _InputText = "1.0";
    public string InputText
    {
        get => _InputText;
        set { _InputText = value; OnPropertyChanged(nameof(InputText)); }
    }

    private string _OutputText = "1.0";
    public string OutputText
    {
        get => _OutputText;
        private set { _OutputText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OutputText))); }
    }

    //--------------------------------------------------------------------------------------------------

    public UnitConverterDialog()
    {
        InitializeComponent();
        _UpdateAvailableUnits();
    }

    private void _UpdateAvailableUnits()
    {
        switch (SelectedCategory)
        {
            case "Length":
                AvailableUnits = new List<string> { "mm", "cm", "dm", "m", "inch", "foot", "yard" };
                FromUnit = "mm";
                ToUnit = "inch";
                break;
            case "Angle":
                AvailableUnits = new List<string> { "degree", "radian", "gradian" };
                FromUnit = "degree";
                ToUnit = "radian";
                break;
            case "Area":
                AvailableUnits = new List<string> { "mm²", "cm²", "m²", "in²", "ft²" };
                FromUnit = "mm²";
                ToUnit = "cm²";
                break;
            case "Volume":
                AvailableUnits = new List<string> { "mm³", "cm³", "m³", "in³", "ft³", "liter" };
                FromUnit = "mm³";
                ToUnit = "liter";
                break;
        }
    }

    private void SwapUnits_Click(object sender, RoutedEventArgs e)
    {
        var temp = FromUnit;
        FromUnit = ToUnit;
        ToUnit = temp;
    }

    private void _Recalculate()
    {
        if (!double.TryParse(InputText, out double inputValue))
        {
            OutputText = "Invalid Input";
            return;
        }

        try
        {
            double baseValue = _ConvertToBase(inputValue, SelectedCategory, FromUnit);
            double targetValue = _ConvertFromBase(baseValue, SelectedCategory, ToUnit);
            OutputText = targetValue.ToString("F6").TrimEnd('0').TrimEnd('.', ',');
        }
        catch
        {
            OutputText = "Error";
        }
    }

    private double _ConvertToBase(double value, string category, string unit)
    {
        if (category == "Length")
        {
            return unit switch
            {
                "mm" => value,
                "cm" => value * 10.0,
                "dm" => value * 100.0,
                "m" => value * 1000.0,
                "inch" => value * 25.4,
                "foot" => value * 304.8,
                "yard" => value * 914.4,
                _ => value
            };
        }
        if (category == "Angle")
        {
            return unit switch
            {
                "degree" => value,
                "radian" => value * (180.0 / Math.PI),
                "gradian" => value * 0.9,
                _ => value
            };
        }
        if (category == "Area")
        {
            return unit switch
            {
                "mm²" => value,
                "cm²" => value * 100.0,
                "m²" => value * 1000000.0,
                "in²" => value * 645.16,
                "ft²" => value * 92903.04,
                _ => value
            };
        }
        if (category == "Volume")
        {
            return unit switch
            {
                "mm³" => value,
                "cm³" => value * 1000.0,
                "m³" => value * 1000000000.0,
                "in³" => value * 16387.064,
                "ft³" => value * 28316846.592,
                "liter" => value * 1000000.0,
                _ => value
            };
        }
        return value;
    }

    private double _ConvertFromBase(double baseValue, string category, string unit)
    {
        if (category == "Length")
        {
            return unit switch
            {
                "mm" => baseValue,
                "cm" => baseValue / 10.0,
                "dm" => baseValue / 100.0,
                "m" => baseValue / 1000.0,
                "inch" => baseValue / 25.4,
                "foot" => baseValue / 304.8,
                "yard" => baseValue / 914.4,
                _ => baseValue
            };
        }
        if (category == "Angle")
        {
            return unit switch
            {
                "degree" => baseValue,
                "radian" => baseValue * (Math.PI / 180.0),
                "gradian" => baseValue / 0.9,
                _ => baseValue
            };
        }
        if (category == "Area")
        {
            return unit switch
            {
                "mm²" => baseValue,
                "cm²" => baseValue / 100.0,
                "m²" => baseValue / 1000000.0,
                "in²" => baseValue / 645.16,
                "ft²" => baseValue / 92903.04,
                _ => baseValue
            };
        }
        if (category == "Volume")
        {
            return unit switch
            {
                "mm³" => baseValue,
                "cm³" => baseValue / 1000.0,
                "m³" => baseValue / 1000000000.0,
                "in³" => baseValue / 16387.064,
                "ft³" => baseValue / 28316846.592,
                "liter" => baseValue / 1000000.0,
                _ => baseValue
            };
        }
        return baseValue;
    }
}
