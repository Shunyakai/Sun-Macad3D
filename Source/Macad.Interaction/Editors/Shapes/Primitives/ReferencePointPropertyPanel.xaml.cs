using Macad.Common;
using Macad.Core.Shapes;
using Macad.Interaction.Panels;

namespace Macad.Interaction.Editors.Shapes;

public partial class ReferencePointPropertyPanel : PropertyPanel
{
    public ReferencePoint ReferencePoint { get; private set; }

    public override void Initialize(BaseObject instance)
    {
        ReferencePoint = instance as ReferencePoint;
        InitializeComponent();
    }

    public override void Cleanup()
    {
    }
}
