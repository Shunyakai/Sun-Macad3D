using System;
using System.Windows.Input;
using Macad.Common;
using Macad.Core;
using Macad.Interaction.Visual;
using Macad.Occt;
using System.Windows;

namespace Macad.Interaction.Tools;

public class ZoomBoxTool : Tool
{
    Point _StartPoint;
    bool _Dragging;
    AIS_RubberBand _AisRubberBand;

    protected override bool OnStart()
    {
        SetHintMessage("Drag a rectangular box to zoom in.");
        SetCursor(System.Windows.Input.Cursors.Cross);
        return true;
    }

    protected override void OnStop()
    {
        _CleanupRubberband();
        base.OnStop();
    }

    public override bool OnMouseDown(MouseEventData data)
    {
        if (data.ModifierKeys == ModifierKeys.None)
        {
            _StartPoint = data.ScreenPoint;
            _Dragging = true;

            var aisContext = WorkspaceController.AisContext;
            _AisRubberBand = new AIS_RubberBand(
                new Color(0.0f, 0.0f, 1.0f).ToQuantityColor(), 
                Aspect_TypeOfLine.DASH, 
                new Color(0.0f, 0.0f, 1.0f).ToQuantityColor(), 
                0.9, 2, true);
            _AisRubberBand.SetLocalTransformation(Trsf.Identity);

            // Set small initial size to prevent native OpenCascade initialization crashes
            int x = (int)_StartPoint.X;
            int y = (int)_StartPoint.Y;
            var height = data.ViewportController.ScreenSize.Height;
            _AisRubberBand.SetRectangle(x, height - y, x + 1, height - (y + 1));

            aisContext.Display(_AisRubberBand, false);
            _AisRubberBand.ViewAffinity().SetVisible(false);
            aisContext.SetViewAffinity(_AisRubberBand, data.ViewportController.V3dView, true);

            _UpdateRubberband(data.ScreenPoint, data.ViewportController);
            return true;
        }
        return false;
    }

    public override bool OnMouseMove(MouseEventData data)
    {
        if (_Dragging && _AisRubberBand != null)
        {
            _UpdateRubberband(data.ScreenPoint, data.ViewportController);
            return true;
        }
        return false;
    }

    public override bool OnMouseUp(MouseEventData data)
    {
        if (_Dragging)
        {
            _Dragging = false;
            _CleanupRubberband();

            Point endPoint = data.ScreenPoint;
            double dx = Math.Abs(endPoint.X - _StartPoint.X);
            double dy = Math.Abs(endPoint.Y - _StartPoint.Y);

            if (dx > 5 && dy > 5)
            {
                int x1 = (int)Math.Min(_StartPoint.X, endPoint.X);
                int y1 = (int)Math.Min(_StartPoint.Y, endPoint.Y);
                int x2 = (int)Math.Max(_StartPoint.X, endPoint.X);
                int y2 = (int)Math.Max(_StartPoint.Y, endPoint.Y);

                data.ViewportController.V3dView.WindowFit(x1, y1, x2, y2);
                WorkspaceController.Invalidate(true);
            }

            Stop();
            return true;
        }
        return false;
    }

    void _UpdateRubberband(Point currentPoint, ViewportController vpController)
    {
        if (_AisRubberBand == null)
            return;

        var screenSize = vpController.ScreenSize;
        int left = (int)Math.Max(0, Math.Min(_StartPoint.X, currentPoint.X));
        int right = (int)Math.Min(screenSize.Width, Math.Max(_StartPoint.X, currentPoint.X));
        int top = (int)Math.Max(0, Math.Min(_StartPoint.Y, currentPoint.Y));
        int bottom = (int)Math.Min(screenSize.Height, Math.Max(_StartPoint.Y, currentPoint.Y));

        // Enforce a minimum size of 1x1 to prevent native OpenCascade crashes
        if (right <= left)
            right = left + 1;
        if (bottom <= top)
            bottom = top + 1;

        // AIS_RubberBand expects coordinates where bottom is 0 and top is height (bottom-up)
        _AisRubberBand.SetRectangle(left, screenSize.Height - bottom, right, screenSize.Height - top);
        WorkspaceController.AisContext.Redisplay(_AisRubberBand, false);
        WorkspaceController.Invalidate(true);
    }

    void _CleanupRubberband()
    {
        if (_AisRubberBand != null)
        {
            WorkspaceController.AisContext.Remove(_AisRubberBand, false);
            _AisRubberBand = null;
        }
    }
}

