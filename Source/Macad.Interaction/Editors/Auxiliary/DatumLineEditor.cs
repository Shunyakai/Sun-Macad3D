using Macad.Core.Auxiliary;
using Macad.Interaction.Panels;

namespace Macad.Interaction.Editors.Auxiliary;

public sealed class DatumLineEditor : Editor<DatumLine>
{
    protected override void OnStart()
    {
        CreatePanel<DatumLinePropertyPanel>(Entity, PropertyPanelSortingKey.Body);
    }
        
    //--------------------------------------------------------------------------------------------------

    [AutoRegister]
    internal static void Register()
    {
        RegisterEditor<DatumLineEditor>();
    }
}
