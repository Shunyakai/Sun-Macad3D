using Macad.Core.Shapes;
using Macad.Interaction.Panels;
using Macad.Presentation;

namespace Macad.Interaction.Editors.Shapes;

public class HelixEditor : Editor<Helix>
{
    protected override void OnStart()
    {
        var panel = CreatePanel<HelixPropertyPanel>(Entity, PropertyPanelSortingKey.Shapes);

        if (Entity.Predecessor is Sketch sketch)
        {
            CreatePanel<SketchPropertyPanel>(sketch, panel);
        }
    }

    //--------------------------------------------------------------------------------------------------

    public override (IActionCommand, object) GetStartEditingCommand()
    {
        if (Entity.Predecessor is Sketch sketch)
        {
            return (SketchCommands.StartSketchEditor, sketch);
        }
        return base.GetStartEditingCommand();
    }

    //--------------------------------------------------------------------------------------------------

    [AutoRegister]
    internal static void Register()
    {
        RegisterEditor<HelixEditor>();
    }

    //--------------------------------------------------------------------------------------------------
}
