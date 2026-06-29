using System.Diagnostics;
using System.Windows;
using Macad.Common;
using Macad.Core.Auxiliary;
using Macad.Interaction.Panels;

namespace Macad.Interaction.Editors.Auxiliary;

public partial class AnnotationLabelPropertyPanel : PropertyPanel
{
    public AnnotationLabel AnnotationLabel
    {
        get { return _AnnotationLabel; }
        set
        {
            if (_AnnotationLabel != value)
            {
                _AnnotationLabel = value;
                RaisePropertyChanged();
            }
        }
    }

    AnnotationLabel _AnnotationLabel;

    public override void Initialize(BaseObject instance)
    {
        AnnotationLabel = instance as AnnotationLabel;
        Debug.Assert(AnnotationLabel != null);

        if (Application.Current != null)
            InitializeComponent();
    }

    public override void Cleanup()
    {
        AnnotationLabel = null;
    }
}
