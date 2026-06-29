using Macad.Common;
using Macad.Core.Shapes;
using Macad.Interaction.Panels;

namespace Macad.Interaction.Editors.Shapes;

public partial class RegularPolygonPropertyPanel : PropertyPanel
{
    public RegularPolygon RegularPolygon { get; private set; }

    public override void Initialize(BaseObject instance)
    {
        RegularPolygon = instance as RegularPolygon;
        InitializeComponent();
    }

    public override void Cleanup()
    {
    }
}
