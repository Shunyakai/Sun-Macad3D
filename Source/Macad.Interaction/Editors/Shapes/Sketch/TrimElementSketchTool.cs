using System.Linq;
using Macad.Core.Shapes;
using Macad.Occt;

namespace Macad.Interaction.Editors.Shapes;

public class TrimElementSketchTool : SketchTool
{
    PointOnSketchElementAction _ToolAction;
    const string _Message = "__Select__ a segment to trim at its intersection points.";

    protected override bool OnStart()
    {
        SketchEditorTool.Elements.ConstraintsVisible = false;
        SketchEditorTool.Select([], []);

        _ToolAction = new PointOnSketchElementAction(SketchEditorTool);
        if (!StartAction(_ToolAction))
            return false;
        _ToolAction.Preview += _ToolAction_Preview;
        _ToolAction.Finished += _ToolAction_Finished;

        SetHintMessage(_Message);
        SetCursor(Cursors.Minus);
        return true;
    }

    void _ToolAction_Preview(PointOnSketchElementAction sender, PointOnSketchElementAction.PreviewEventArgs args)
    {
        if (args.ElementType != Sketch.ElementType.Segment)
        {
            args.Cancel = true;
            return;
        }

        var element = SketchEditorTool.Elements.SegmentElements.FirstOrDefault(el => el.Segment == args.Segment);
        if (element?.AisObject is { } aisObject)
        {
            args.MouseEventData.Return.AdditionalHighlights.Add(new MouseEventData.Element(aisObject));
        }
    }

    void _ToolAction_Finished(PointOnSketchElementAction sender, PointOnSketchElementAction.EventArgs args)
    {
        if (args.ElementType == Sketch.ElementType.Segment && args.Segment != null)
        {
            Sketch.SaveUndo(Sketch.ElementType.All);
            if (SketchUtils.TrimSegment(Sketch, args.Segment, args.Parameter))
            {
                CommitChanges();
            }
        }

        _ToolAction.Reset();
        SetHintMessage(_Message);
    }
}
