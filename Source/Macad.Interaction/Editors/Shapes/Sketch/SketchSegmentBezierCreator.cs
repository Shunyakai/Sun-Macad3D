using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Macad.Core.Shapes;
using Macad.Occt;
using Macad.Presentation;

namespace Macad.Interaction.Editors.Shapes;

public sealed class SketchSegmentBezierCreator : SketchSegmentCreator
{
    SketchPointAction _PointAction;
    SketchSegmentBezier _Segment;
    SketchEditorSegmentElement _Element;
    Coord2DHudElement _Coord2DHudElement;
    readonly Dictionary<int, Pnt2d> _Points = new();
    readonly List<int> _MergePointIndices = new();

    //--------------------------------------------------------------------------------------------------

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

        SetHintMessage("__Select start point__ for bézier curve.");

        return true;
    }

    //--------------------------------------------------------------------------------------------------

    protected override void Cleanup()
    {
        _Element?.Remove();
        base.Cleanup();
    }

    //--------------------------------------------------------------------------------------------------

    void _PointAction_Preview(SketchPointAction sender, SketchPointAction.EventArgs args)
    {
        int currentIndex = _MergePointIndices.Count;

        if (currentIndex > 0)
        {
            _Points[currentIndex] = args.Point;

            if (_Segment == null && currentIndex == 1)
            {
                _Segment = new SketchSegmentBezier([0, 1]);
                _Element = new SketchEditorSegmentElement(SketchEditorTool, -1, _Segment, SketchEditorTool.Transform, SketchEditorTool.Sketch.Plane)
                {
                    IsCreating = true
                };
            }

            if (_Element != null)
            {
                if (currentIndex > 1 && _Segment.Points.Length <= currentIndex)
                {
                    int[] indices = new int[currentIndex + 1];
                    for (int j = 0; j <= currentIndex; j++)
                        indices[j] = j;
                    _Segment = new SketchSegmentBezier(indices);
                    _Element.Remove();
                    _Element = new SketchEditorSegmentElement(SketchEditorTool, -1, _Segment, SketchEditorTool.Transform, SketchEditorTool.Sketch.Plane)
                    {
                        IsCreating = true
                    };
                }

                _Element.OnPointsChanged(_Points, null);
            }
        }

        _Coord2DHudElement.SetValues(args.PointOnWorkingPlane.X, args.PointOnWorkingPlane.Y);
    }

    //--------------------------------------------------------------------------------------------------

    void _PointAction_Finished(SketchPointAction sender, SketchPointAction.EventArgs args)
    {
        int currentIndex = _MergePointIndices.Count;

        if (currentIndex > 0)
        {
            if (_Points[currentIndex - 1].Distance(args.Point) < 0.001)
            {
                _PointAction.Reset();
                return;
            }
        }

        _Points[currentIndex] = args.Point;
        _MergePointIndices.Add(args.MergeCandidateIndex);

        SetHintMessage("Select next point for bézier curve. Press __Enter__ to finish.");

        _PointAction.Reset();
        SketchEditorTool.WorkspaceController.Invalidate();
    }

    //--------------------------------------------------------------------------------------------------

    public override bool OnKeyPressed(Key key, ModifierKeys modifierKeys)
    {
        if (key == Key.Enter || key == Key.Return)
        {
            _Finish();
            return true;
        }
        return base.OnKeyPressed(key, modifierKeys);
    }

    //--------------------------------------------------------------------------------------------------

    void _Finish()
    {
        if (_MergePointIndices.Count < 2)
        {
            Stop();
            return;
        }

        int currentIndex = _MergePointIndices.Count;
        if (_Points.ContainsKey(currentIndex))
        {
            _Points.Remove(currentIndex);
        }

        int[] indices = new int[_MergePointIndices.Count];
        for (int j = 0; j < _MergePointIndices.Count; j++)
            indices[j] = j;

        var finalSegment = new SketchSegmentBezier(indices);

        var segmentList = new List<SketchSegment> { finalSegment };
        SketchEditorTool.FinishSegmentCreation(_Points, _MergePointIndices.ToArray(), segmentList, null, -1);
    }
}
