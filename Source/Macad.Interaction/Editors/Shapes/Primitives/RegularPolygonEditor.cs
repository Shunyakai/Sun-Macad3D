using System;
using Macad.Common;
using Macad.Core.Shapes;
using Macad.Interaction.Panels;

namespace Macad.Interaction.Editors.Shapes;

public sealed class RegularPolygonEditor : Editor<RegularPolygon>
{
    protected override void OnStart()
    {
        CreatePanel<RegularPolygonPropertyPanel>(Entity, PropertyPanelSortingKey.Shapes);
    }

    [AutoRegister]
    internal static void Register()
    {
        RegisterEditor<RegularPolygonEditor>();
    }
}
