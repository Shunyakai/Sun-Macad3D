using Macad.Core.Auxiliary;
using Macad.Interaction.Panels;

namespace Macad.Interaction.Editors.Auxiliary;

public sealed class AnnotationLabelEditor : Editor<AnnotationLabel>
{
    protected override void OnStart()
    {
        CreatePanel<AnnotationLabelPropertyPanel>(Entity, PropertyPanelSortingKey.Body);
    }

    [AutoRegister]
    internal static void Register()
    {
        RegisterEditor<AnnotationLabelEditor>();
    }
}
