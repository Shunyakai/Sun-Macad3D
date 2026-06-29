using System.Collections.Generic;
using Macad.Core.Shapes;
using Macad.Occt;
using Macad.Presentation;

namespace Macad.Interaction.Editors.Shapes;

public sealed class SketchSegmentPipeCreator : SketchSegmentCreator
{
    SketchPointAction _PointAction;
    SketchSegmentPipe _Segment;
    Coord2DHudElement _Coord2DHudElement;
    readonly Dictionary<int, Pnt2d> _Points = new(2);
    readonly int[] _MergePointIndices = new int[2];
    int _Step = 0;

    protected override bool OnStart()
    {
        base.OnStart();

        _PointAction = new SketchPointAction(SketchEditorTool);
        if (!StartAction(_PointAction))
            return false;

        _PointAction.Preview += _PointAction_Preview;
        _PointAction.Finished += _PointAction_Finished;

        _Coord2DHudElement = new Coord2DHudElement();
        Add(_Coord2DHudElement);

        SetHintMessage("__Select start point__ for pipe profile.");

        return true;
    }

    void _PointAction_Preview(SketchPointAction sender, SketchPointAction.EventArgs args)
    {
        _Coord2DHudElement.SetValues(args.PointOnWorkingPlane.X, args.PointOnWorkingPlane.Y);
    }

    void _PointAction_Finished(SketchPointAction sender, SketchPointAction.EventArgs args)
    {
        if (_Step == 0)
        {
            _Points.Add(0, args.Point);
            _MergePointIndices[0] = args.MergeCandidateIndex;
            _Points.Add(1, args.Point);

            _Step = 1;
            SetHintMessage("__Select end point__ for pipe profile.");
            _PointAction.Reset();
        }
        else if (_Step == 1)
        {
            if (_Points[0].Distance(args.Point) < 0.001)
            {
                _PointAction.Reset();
                return;
            }

            _Points[1] = args.Point;
            _MergePointIndices[1] = args.MergeCandidateIndex;

            _Segment = new SketchSegmentPipe(0, 1, 5.0);

            SketchEditorTool.FinishSegmentCreation(_Points, _MergePointIndices, [_Segment], null, _MergePointIndices[1] >= 0 ? -1 : 1);
        }
    }
}
