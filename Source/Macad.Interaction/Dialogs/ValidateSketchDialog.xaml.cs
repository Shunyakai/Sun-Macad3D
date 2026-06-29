using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Macad.Common;
using Macad.Core;
using Macad.Core.Shapes;
using Macad.Occt;
using Macad.Interaction.Editors.Shapes;
using Macad.Presentation;

namespace Macad.Interaction.Dialogs;

public enum SketchErrorType
{
    OpenVertex,
    DegenerateEdge,
    DuplicateEdge
}

public class SketchValidationError
{
    public SketchErrorType ErrorType { get; set; }
    public string Description { get; set; }
    public List<int> PointIndices { get; set; } = new();
    public List<int> SegmentIndices { get; set; } = new();
}

public partial class ValidateSketchDialog : Dialog
{
    public static void Execute(Window ownerWindow, SketchEditorTool sketchEditorTool)
    {
        var dlg = new ValidateSketchDialog(sketchEditorTool)
        {
            Owner = ownerWindow
        };
        dlg.ShowDialog();
    }

    //--------------------------------------------------------------------------------------------------

    public double Tolerance
    {
        get { return _Tolerance; }
        set
        {
            if (value.Equals(_Tolerance)) return;
            _Tolerance = value;
            RaisePropertyChanged();
        }
    }

    double _Tolerance = 0.01;

    //--------------------------------------------------------------------------------------------------

    public ObservableCollection<SketchValidationError> Issues { get; } = new();

    //--------------------------------------------------------------------------------------------------

    readonly SketchEditorTool _SketchEditorTool;
    bool _IsUpdatingSelection;

    //--------------------------------------------------------------------------------------------------

    public ValidateSketchDialog(SketchEditorTool sketchEditorTool)
    {
        _SketchEditorTool = sketchEditorTool;
        InitializeComponent();
        DetectIssues();
    }

    //--------------------------------------------------------------------------------------------------

    void OnDetectClick(object sender, RoutedEventArgs e)
    {
        DetectIssues();
    }

    //--------------------------------------------------------------------------------------------------

    void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    //--------------------------------------------------------------------------------------------------

    void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_IsUpdatingSelection)
            return;

        _IsUpdatingSelection = true;
        try
        {
            var selectedIssues = issuesListView.SelectedItems.Cast<SketchValidationError>().ToList();
            var pointIndices = selectedIssues.SelectMany(x => x.PointIndices).Distinct().ToList();
            var segmentIndices = selectedIssues.SelectMany(x => x.SegmentIndices).Distinct().ToList();
            _SketchEditorTool.Select(pointIndices, segmentIndices);
        }
        finally
        {
            _IsUpdatingSelection = false;
        }
    }

    //--------------------------------------------------------------------------------------------------

    void OnFixSelectedClick(object sender, RoutedEventArgs e)
    {
        var selectedIssues = issuesListView.SelectedItems.Cast<SketchValidationError>().ToList();
        if (selectedIssues.Count == 0)
        {
            TaskDialog.ShowMessage(this, "No selection", "Please select one or more issues to fix.", "No Issues Selected", TaskDialogCommonButtons.OK, TaskDialogIcon.Information);
            return;
        }

        FixIssues(selectedIssues);
    }

    //--------------------------------------------------------------------------------------------------

    void OnFixAllClick(object sender, RoutedEventArgs e)
    {
        if (Issues.Count == 0)
            return;

        FixIssues(Issues.ToList());
    }

    //--------------------------------------------------------------------------------------------------

    void FixIssues(List<SketchValidationError> issuesToFix)
    {
        var sketch = _SketchEditorTool.Sketch;
        bool modified = false;

        _SketchEditorTool.Select(null, null);

        // Group open vertices and others to perform them in sequence
        var openVertexIssues = issuesToFix.Where(i => i.ErrorType == SketchErrorType.OpenVertex).ToList();
        var otherIssues = issuesToFix.Where(i => i.ErrorType != SketchErrorType.OpenVertex).ToList();

        // 1. Fix degenerate/duplicate edges first
        foreach (var issue in otherIssues)
        {
            if (issue.ErrorType == SketchErrorType.DegenerateEdge)
            {
                int segId = issue.SegmentIndices[0];
                if (sketch.Segments.TryGetValue(segId, out var segment))
                {
                    sketch.DeleteSegment(segment);
                    modified = true;
                }
            }
            else if (issue.ErrorType == SketchErrorType.DuplicateEdge)
            {
                // Delete the second segment in duplicate pair
                int segId = issue.SegmentIndices[1];
                if (sketch.Segments.TryGetValue(segId, out var segment))
                {
                    sketch.DeleteSegment(segment);
                    modified = true;
                }
            }
        }

        // 2. Fix open vertices
        // Keep track of point remapping to handle chained point merges
        var pointMap = new Dictionary<int, int>();
        int FindTarget(int p)
        {
            while (pointMap.TryGetValue(p, out int target))
            {
                p = target;
            }
            return p;
        }

        foreach (var issue in openVertexIssues)
        {
            int p1 = FindTarget(issue.PointIndices[0]);
            int p2 = FindTarget(issue.PointIndices[1]);

            if (p1 != p2 && sketch.Points.ContainsKey(p1) && sketch.Points.ContainsKey(p2))
            {
                sketch.MergePoints(p2, p1);
                pointMap[p2] = p1;
                modified = true;
            }
        }

        if (modified)
        {
            sketch.Invalidate();
            InteractiveContext.Current?.UndoHandler?.Commit();
            DetectIssues();
        }
    }

    //--------------------------------------------------------------------------------------------------

    public void DetectIssues()
    {
        Issues.Clear();

        var sketch = _SketchEditorTool.Sketch;
        var pts = sketch.Points;
        var segments = sketch.Segments;

        // 1. Detect Degenerate Edges
        foreach (var kvp in segments)
        {
            var segment = kvp.Value;
            var segIdx = kvp.Key;
            
            bool isDegenerate = false;
            string reason = "";
            
            Geom2d_Curve curve = null;
            try
            {
                curve = segment.MakeCurve(pts);
            }
            catch
            {
                isDegenerate = true;
                reason = "cannot generate curve geometry";
            }

            if (curve == null)
            {
                isDegenerate = true;
                reason = "missing or invalid curve definition";
            }
            else if (segment is SketchSegmentLine line)
            {
                double len = line.Length(pts);
                if (len < Tolerance)
                {
                    isDegenerate = true;
                    reason = $"length ({len:N4} mm) is less than tolerance";
                }
            }
            else if (segment is SketchSegmentCircle circle)
            {
                double r = circle.Radius(pts);
                if (r < Tolerance)
                {
                    isDegenerate = true;
                    reason = $"radius ({r:N4} mm) is less than tolerance";
                }
            }
            else if (segment is SketchSegmentArc arc)
            {
                double r = arc.Radius(pts);
                if (r < Tolerance)
                {
                    isDegenerate = true;
                    reason = $"radius ({r:N4} mm) is less than tolerance";
                }
                else
                {
                    var start = pts[arc.StartPoint];
                    var end = pts[arc.EndPoint];
                    var rim = pts[arc.RimPoint];
                    if (start.Distance(end) < Tolerance || end.Distance(rim) < Tolerance || start.Distance(rim) < Tolerance)
                    {
                        isDegenerate = true;
                        reason = "key points are too close to each other";
                    }
                }
            }
            else if (segment.Points != null && segment.Points.Length > 1)
            {
                var firstPnt = pts[segment.Points[0]];
                bool allClose = true;
                for (int i = 1; i < segment.Points.Length; i++)
                {
                    if (firstPnt.Distance(pts[segment.Points[i]]) >= Tolerance)
                    {
                        allClose = false;
                        break;
                    }
                }
                if (allClose)
                {
                    isDegenerate = true;
                    reason = "all control points are coincident";
                }
            }

            if (isDegenerate)
            {
                Issues.Add(new SketchValidationError
                {
                    ErrorType = SketchErrorType.DegenerateEdge,
                    Description = $"Segment {segIdx} ({segment.GetType().Name.Replace("SketchSegment", "")}): {reason}",
                    SegmentIndices = new List<int> { segIdx }
                });
            }
        }

        // 2. Detect Duplicate/Overlapping Edges
        var segmentList = segments.ToList();
        for (int i = 0; i < segmentList.Count; i++)
        {
            var kvp1 = segmentList[i];
            for (int j = i + 1; j < segmentList.Count; j++)
            {
                var kvp2 = segmentList[j];
                
                if (AreSegmentsOverlapping(sketch, kvp1.Value, kvp2.Value, Tolerance))
                {
                    Issues.Add(new SketchValidationError
                    {
                        ErrorType = SketchErrorType.DuplicateEdge,
                        Description = $"Overlapping segments: {kvp1.Key} and {kvp2.Key} are duplicate.",
                        SegmentIndices = new List<int> { kvp1.Key, kvp2.Key }
                    });
                }
            }
        }

        // 3. Detect Open Vertices (Missing Coincidences)
        var endpoints = new HashSet<int>();
        foreach (var segment in segments.Values)
        {
            if (!segment.IsPeriodic)
            {
                if (segment.StartPoint != -1) endpoints.Add(segment.StartPoint);
                if (segment.EndPoint != -1) endpoints.Add(segment.EndPoint);
            }
        }

        var endpointList = endpoints.ToList();
        for (int i = 0; i < endpointList.Count; i++)
        {
            int p1 = endpointList[i];
            Pnt2d pt1 = pts[p1];
            for (int j = i + 1; j < endpointList.Count; j++)
            {
                int p2 = endpointList[j];
                Pnt2d pt2 = pts[p2];

                double dist = pt1.Distance(pt2);
                if (dist < Tolerance)
                {
                    Issues.Add(new SketchValidationError
                    {
                        ErrorType = SketchErrorType.OpenVertex,
                        Description = $"Open vertices: Points {p1} and {p2} are within {dist:N4} mm.",
                        PointIndices = new List<int> { p1, p2 }
                    });
                }
            }
        }

        RaisePropertyChanged(nameof(Issues));
    }

    //--------------------------------------------------------------------------------------------------

    public static bool AreSegmentsOverlapping(Sketch sketch, SketchSegment seg1, SketchSegment seg2, double tolerance)
    {
        if (seg1.GetType() != seg2.GetType())
            return false;

        var pts = sketch.Points;

        if (seg1 is SketchSegmentLine l1 && seg2 is SketchSegmentLine l2)
        {
            if (!pts.ContainsKey(l1.StartPoint) || !pts.ContainsKey(l1.EndPoint) ||
                !pts.ContainsKey(l2.StartPoint) || !pts.ContainsKey(l2.EndPoint))
                return false;

            var s1 = pts[l1.StartPoint];
            var e1 = pts[l1.EndPoint];
            var s2 = pts[l2.StartPoint];
            var e2 = pts[l2.EndPoint];
            return (s1.Distance(s2) < tolerance && e1.Distance(e2) < tolerance)
                || (s1.Distance(e2) < tolerance && e1.Distance(s2) < tolerance);
        }
        if (seg1 is SketchSegmentCircle c1 && seg2 is SketchSegmentCircle c2)
        {
            if (!pts.ContainsKey(c1.CenterPoint) || !pts.ContainsKey(c1.RimPoint) ||
                !pts.ContainsKey(c2.CenterPoint) || !pts.ContainsKey(c2.RimPoint))
                return false;

            var ctr1 = pts[c1.CenterPoint];
            var rim1 = pts[c1.RimPoint];
            var ctr2 = pts[c2.CenterPoint];
            var rim2 = pts[c2.RimPoint];
            return ctr1.Distance(ctr2) < tolerance && Math.Abs(c1.Radius(pts) - c2.Radius(pts)) < tolerance;
        }
        if (seg1 is SketchSegmentArc a1 && seg2 is SketchSegmentArc a2)
        {
            if (!pts.ContainsKey(a1.StartPoint) || !pts.ContainsKey(a1.EndPoint) || !pts.ContainsKey(a1.RimPoint) ||
                !pts.ContainsKey(a2.StartPoint) || !pts.ContainsKey(a2.EndPoint) || !pts.ContainsKey(a2.RimPoint))
                return false;

            var s1 = pts[a1.StartPoint];
            var e1 = pts[a1.EndPoint];
            var r1 = pts[a1.RimPoint];
            var s2 = pts[a2.StartPoint];
            var e2 = pts[a2.EndPoint];
            var r2 = pts[a2.RimPoint];
            return ((s1.Distance(s2) < tolerance && e1.Distance(e2) < tolerance) || (s1.Distance(e2) < tolerance && e1.Distance(s2) < tolerance))
                && r1.Distance(r2) < tolerance;
        }
        
        if (seg1.Points.Length == seg2.Points.Length)
        {
            bool allClose = true;
            for (int k = 0; k < seg1.Points.Length; k++)
            {
                if (!pts.ContainsKey(seg1.Points[k]) || !pts.ContainsKey(seg2.Points[k]))
                    return false;

                if (pts[seg1.Points[k]].Distance(pts[seg2.Points[k]]) >= tolerance)
                {
                    allClose = false;
                    break;
                }
            }
            if (allClose) return true;

            if (seg1 is SketchSegmentBezier || seg1 is SketchSegmentEllipticalArc)
            {
                bool allCloseRev = true;
                int n = seg1.Points.Length;
                for (int k = 0; k < n; k++)
                {
                    if (!pts.ContainsKey(seg1.Points[k]) || !pts.ContainsKey(seg2.Points[n - 1 - k]))
                        return false;

                    if (pts[seg1.Points[k]].Distance(pts[seg2.Points[n - 1 - k]]) >= tolerance)
                    {
                        allCloseRev = false;
                        break;
                    }
                }
                if (allCloseRev) return true;
            }
        }

        return false;
    }
}
