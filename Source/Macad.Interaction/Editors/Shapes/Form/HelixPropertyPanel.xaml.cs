using System;
using System.Diagnostics;
using System.Windows.Input;
using Macad.Common;
using Macad.Core.Shapes;
using Macad.Interaction.Panels;
using Macad.Presentation;

namespace Macad.Interaction.Editors.Shapes;

public partial class HelixPropertyPanel : PropertyPanel
{
    public Helix Helix { get; private set; }

    //--------------------------------------------------------------------------------------------------

    public ICommand SwitchHandednessCommand { get; private set; }

    void ExecuteSwitchHandedness(string param)
    {
        if (Enum.TryParse<Helix.HelixHandedness>(param, out var handedness))
        {
            Helix.Handedness = handedness;
            CommitChange();
        }
    }

    //--------------------------------------------------------------------------------------------------

    public override void Initialize(BaseObject instance)
    {
        Helix = instance as Helix;
        Debug.Assert(Helix != null);

        SwitchHandednessCommand = new RelayCommand<string>(ExecuteSwitchHandedness);

        InitializeComponent();
    }

    //--------------------------------------------------------------------------------------------------

    public override void Cleanup()
    {
    }

    //--------------------------------------------------------------------------------------------------
}
