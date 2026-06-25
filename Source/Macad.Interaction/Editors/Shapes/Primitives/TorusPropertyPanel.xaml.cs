using Macad.Common;
using Macad.Core.Shapes;
using Macad.Interaction.Panels;

namespace Macad.Interaction.Editors.Shapes;

public partial class TorusPropertyPanel : PropertyPanel
{
    public Torus Torus { get; private set; }

    //--------------------------------------------------------------------------------------------------

    public override void Initialize(BaseObject instance)
    {
        Torus = instance as Torus;
        InitializeComponent();
    }

    //--------------------------------------------------------------------------------------------------

    public override void Cleanup()
    {
    }

    //--------------------------------------------------------------------------------------------------
}
