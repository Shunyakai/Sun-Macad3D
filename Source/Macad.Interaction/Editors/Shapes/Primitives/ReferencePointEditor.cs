using System;
using Macad.Common;
using Macad.Core.Shapes;
using Macad.Interaction.Panels;

namespace Macad.Interaction.Editors.Shapes;

public sealed class ReferencePointEditor : Editor<ReferencePoint>
{
    protected override void OnStart()
    {
        CreatePanel<ReferencePointPropertyPanel>(Entity, PropertyPanelSortingKey.Shapes);
    }

    [AutoRegister]
    internal static void Register()
    {
        RegisterEditor<ReferencePointEditor>();
    }
}
