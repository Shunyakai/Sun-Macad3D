using Macad.Common;
using Macad.Core.Shapes;
using Macad.Interaction.Panels;

namespace Macad.Interaction.Editors.Shapes;

public partial class ConePropertyPanel : PropertyPanel
{
    public Cone Cone { get; private set; }

    //--------------------------------------------------------------------------------------------------

    public override void Initialize(BaseObject instance)
    {
        Cone = instance as Cone;
        InitializeComponent();
    }

    //--------------------------------------------------------------------------------------------------

    public override void Cleanup()
    {
    }

    //--------------------------------------------------------------------------------------------------
}
