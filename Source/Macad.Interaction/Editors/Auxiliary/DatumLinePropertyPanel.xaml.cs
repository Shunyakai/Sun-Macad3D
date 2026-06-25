using System.Diagnostics;
using System.Windows;
using Macad.Common;
using Macad.Core;
using Macad.Core.Auxiliary;
using Macad.Interaction.Panels;

namespace Macad.Interaction.Editors.Auxiliary;

/// <summary>
/// Interaction logic for DatumLinePropertyPanel.xaml
/// </summary>
public partial class DatumLinePropertyPanel : PropertyPanel
{
    public DatumLine DatumLine
    {
        get { return _DatumLine; }
        set
        {
            if (_DatumLine != value)
            {
                _DatumLine = value;
                RaisePropertyChanged();
            }
        }
    }

    DatumLine _DatumLine;

    //--------------------------------------------------------------------------------------------------

    public override void Initialize(BaseObject instance)
    {
        DatumLine = instance as DatumLine;
        Debug.Assert(DatumLine != null);

        if(Application.Current != null)
            InitializeComponent();
    }

    //--------------------------------------------------------------------------------------------------

    public override void Cleanup()
    {
        DatumLine = null;
    }
}
