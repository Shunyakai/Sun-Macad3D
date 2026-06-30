using System;
using System.Collections.Generic;
using Macad.Core.Shapes;
using Macad.Occt;
using Macad.Presentation;

namespace Macad.Interaction.Editors.Shapes;

public sealed class SketchSegmentPolygonCreator : SketchSegmentCreator
{
    SketchPointAction _PointAction;
    SketchSegmentLine[] _Segments;
    SketchEditorSegmentElement[] _Elements;
    Coord2DHudElement _Coord2DHudElement;
    MultiValueHudElement _ValueHudElement;
    readonly Dictionary<int, Pnt2d> _Points = new();
    int[] _MergePointIndices;
    Pnt2d _CenterPoint;
    int _SidesCount = 6; // Default is regular hexagon

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
        SetHintMessage("__Select center point__ of the regular polygon.");

        return true;
    }

    //--------------------------------------------------------------------------------------------------

    protected override void Cleanup()
    {
        if (_Elements != null)
        {
            foreach (var element in _Elements)
                element.Remove();
        }
        base.Cleanup();
    }

    //--------------------------------------------------------------------------------------------------

    void _RecreatePolygonElements(int newSidesCount)
    {
        if (newSidesCount < 3) newSidesCount = 3;
        if (newSidesCount > 100) newSidesCount = 100;

        if (_Segments != null && _SidesCount == newSidesCount)
            return;

        if (_Elements != null)
        {
            foreach (var element in _Elements)
                element.Remove();
        }

        _SidesCount = newSidesCount;
        _Points.Clear();
        _MergePointIndices = new int[_SidesCount];
        for (int i = 0; i < _SidesCount; i++)
        {
            _Points[i] = _CenterPoint;
            _MergePointIndices[i] = -1;
        }

        _Segments = new SketchSegmentLine[_SidesCount];
        for (int i = 0; i < _SidesCount; i++)
        {
            _Segments[i] = new SketchSegmentLine(i, (i + 1) % _SidesCount);
        }

        _Elements = new SketchEditorSegmentElement[_SidesCount];
        for (int i = 0; i < _SidesCount; i++)
        {
            _Elements[i] = new SketchEditorSegmentElement(SketchEditorTool, -1, _Segments[i], SketchEditorTool.Transform, SketchEditorTool.Sketch.Plane)
            {
                IsCreating = true
            };
        }
    }

    //--------------------------------------------------------------------------------------------------

    void _PointAction_Preview(SketchPointAction sender, SketchPointAction.EventArgs args)
    {
        if (_Segments != null)
        {
            _UpdatePolygonPoints(args.Point);
            foreach (var component in _Elements)
            {
                component.OnPointsChanged(_Points, null);
            }

            if (_ValueHudElement == null)
            {
                _ValueHudElement = new MultiValueHudElement
                {
                    Label1 = "Sides:",
                    Units1 = ValueUnits.None,
                    Label2 = "Radius:",
                    Units2 = ValueUnits.Length
                };
                _ValueHudElement.MultiValueEntered += _ValueHudElement_MultiValueEntered;
                Add(_ValueHudElement);
            }
            _ValueHudElement.SetValue1(_SidesCount);
            _ValueHudElement.SetValue2(_CenterPoint.Distance(args.Point));
        }

        _Coord2DHudElement.SetValues(args.PointOnWorkingPlane.X, args.PointOnWorkingPlane.Y);
    }

    //--------------------------------------------------------------------------------------------------

    void _PointAction_Finished(SketchPointAction sender, SketchPointAction.EventArgs args)
    {
        if (_Segments == null)
        {
            _CenterPoint = args.Point;
            _RecreatePolygonElements(_SidesCount);
            _MergePointIndices[0] = args.MergeCandidateIndex; // First vertex center or alignment merge

            SetHintMessage("__Select vertex point__ to define radius and orientation.");
            _PointAction.Reset();
        }
        else
        {
            _SetSecondPoint(args.Point, args.MergeCandidateIndex);
        }
    }

    //--------------------------------------------------------------------------------------------------

    void _SetSecondPoint(Pnt2d point, int mergeCandidateIndex)
    {
        double radius = _CenterPoint.Distance(point);
        if (radius < 0.001)
        {
            _PointAction.Reset();
            return;
        }

        _PointAction.Stop();

        _UpdatePolygonPoints(point);
        _MergePointIndices[0] = mergeCandidateIndex; // Mark the selected orientation vertex merge index

        SketchEditorTool.FinishSegmentCreation(_Points, _MergePointIndices, _Segments, null);
    }

    //--------------------------------------------------------------------------------------------------

    void _UpdatePolygonPoints(Pnt2d second)
    {
        double radius = _CenterPoint.Distance(second);
        double angle = Math.Atan2(second.Y - _CenterPoint.Y, second.X - _CenterPoint.X);
        for (int i = 0; i < _SidesCount; i++)
        {
            double vertexAngle = angle + (i * 2.0 * Math.PI / _SidesCount);
            _Points[i] = new Pnt2d(_CenterPoint.X + radius * Math.Cos(vertexAngle),
                                   _CenterPoint.Y + radius * Math.Sin(vertexAngle));
        }
    }

    //--------------------------------------------------------------------------------------------------

    void _ValueHudElement_MultiValueEntered(MultiValueHudElement hudelement, double newvalue1, double newvalue2)
    {
        int newSides = (int)Math.Round(newvalue1);
        if (newSides < 3) newSides = 3;
        if (newSides > 100) newSides = 100;

        _RecreatePolygonElements(newSides);

        if (newvalue2 <= 0)
            return;

        Vec2d vec = new(_CenterPoint, _Points[0]);
        if (vec.Magnitude() == 0)
        {
            vec = new Vec2d(1, 0);
        }
        
        _UpdatePolygonPoints(_CenterPoint.Translated(vec.Normalized().Scaled(newvalue2)));
        _SetSecondPoint(_Points[0], -1);
    }
}
