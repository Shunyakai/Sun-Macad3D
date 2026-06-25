using System;
using System.Windows.Input;
using Macad.Common;
using Macad.Core.Shapes;
using Macad.Interaction.Panels;
using Macad.Occt;

namespace Macad.Interaction.Editors.Shapes;

public sealed class TorusEditor : Editor<Torus>
{
    BoxScaleLiveAction _ScaleAction;
    LabelHudElement[] _HudElements = new LabelHudElement[2];
    double _StartRadius1;
    double _StartRadius2;
    Pnt _StartPosition;
    bool _IsScaling;

    //--------------------------------------------------------------------------------------------------

    protected override void OnStart()
    {
        CreatePanel<TorusPropertyPanel>(Entity, PropertyPanelSortingKey.Shapes);
    }
                    
    //--------------------------------------------------------------------------------------------------

    protected override void OnToolsStart()
    {
        Shape.ShapeChanged += _Shape_ShapeChanged;
        _ShowAction();
    }

    //--------------------------------------------------------------------------------------------------

    protected override void OnToolsStop()
    {
        _HudElements.Fill(null);
        _ScaleAction = null;
        Shape.ShapeChanged -= _Shape_ShapeChanged;              
    }

    //--------------------------------------------------------------------------------------------------

    void _Shape_ShapeChanged(Shape shape)
    {
        if (shape == Entity)
        {
            _UpdateActions();
        }
    }

    //--------------------------------------------------------------------------------------------------

    #region Scale Action

    void _ShowAction()
    {
        if (Entity?.Body == null)
        {
            StopAllActions();
            _ScaleAction = null;
            return;
        }

        if (_ScaleAction == null)
        {
            _ScaleAction = new BoxScaleLiveAction(false, Entity.Body);
            _ScaleAction.Preview += _ScaleAction_Preview;
            _ScaleAction.Finished += _ScaleAction_Finished;
            StartAction(_ScaleAction);
        }

        _UpdateActions();
    }

    //--------------------------------------------------------------------------------------------------

    void _UpdateActions()
    {
        if (_ScaleAction == null)
            return;

        if (!_IsScaling)
        {
            _StartRadius1 = Entity.Radius1;
            _StartRadius2 = Entity.Radius2;
            _StartPosition = Entity.Body.Position;
        }

        double maxR = Entity.Radius1 + Entity.Radius2;
        Bnd_Box box = new Bnd_Box(new Pnt(-maxR, -maxR, -Entity.Radius2), 
                                  new Pnt( maxR,  maxR,  Entity.Radius2));
        _ScaleAction.Box = box;
        _ScaleAction.Transformation = Entity.Body.GetTransformation();
    }

    //--------------------------------------------------------------------------------------------------

    void _ScaleAction_Preview(BoxScaleLiveAction sender, BoxScaleLiveAction.EventArgs args)
    {
        _IsScaling = true;
        SetHintMessage("__Scale torus__ using gizmo, press `k:Ctrl` to round to grid stepping, press `k:Shift` to scale relative to center.");

        double newRadius1 = 0;
        double newRadius2 = 0;
        Pnt newPosition = _StartPosition;

        bool center = args.MouseEventData.ModifierKeys.HasFlag(ModifierKeys.Shift);

        double r1Delta = args.DeltaSum * 0.5 * Math.Max(args.Direction.X.Abs(), args.Direction.Y.Abs());
        if (r1Delta != 0)
        {
            if (center)
                r1Delta *= 2.0;

            newRadius1 = _StartRadius1 + r1Delta;
            if (args.MouseEventData.ModifierKeys.HasFlag(ModifierKeys.Control))
            {
                newRadius1 = Maths.RoundToNearest(newRadius1, WorkspaceController.Workspace.GridStep);
            }

            if (newRadius1 <= 0.001)
                return;

            r1Delta = newRadius1 - _StartRadius1;
            if (r1Delta == 0)
                return;
        }

        double r2Delta = args.DeltaSum * 0.5 * args.Direction.Z.Abs();
        if (r2Delta != 0)
        {
            if (center)
                r2Delta *= 2.0;

            newRadius2 = _StartRadius2 + r2Delta;
            if (args.MouseEventData.ModifierKeys.HasFlag(ModifierKeys.Control))
            {
                newRadius2 = Maths.RoundToNearest(newRadius2, WorkspaceController.Workspace.GridStep);
            }

            if (newRadius2 <= 0.001)
                return;

            r2Delta = newRadius2 - _StartRadius2;
            if (r2Delta == 0)
                return;
        }

        if (newRadius1 != 0)
        {
            Entity.Radius1 = newRadius1;
            Vec offset = new Vec(Math.Sign(args.Direction.X), Math.Sign(args.Direction.Y), 0);
            if (offset != Vec.Zero && !center)
            {
                newPosition.Translate(offset.Scaled(r1Delta)
                                            .Transformed(new Trsf(Entity.Body.Rotation)));
            }
            if (_HudElements[0] == null)
            {
                _HudElements[0] = new LabelHudElement();
                Add(_HudElements[0]);
            }
            _HudElements[0]?.SetValue($"Major Radius: {Entity.Radius1.ToInvariantString("F2")} mm");
        }

        if (newRadius2 != 0)
        {
            Entity.Radius2 = newRadius2;
            if (_HudElements[1] == null)
            {
                _HudElements[1] = new LabelHudElement();
                Add(_HudElements[1]);
            }
            _HudElements[1]?.SetValue($"Minor Radius: {Entity.Radius2.ToInvariantString("F2")} mm");
        }

        Entity.Body.Position = newPosition;
        _UpdateActions();
    }

    //--------------------------------------------------------------------------------------------------

    void _ScaleAction_Finished(BoxScaleLiveAction sender, BoxScaleLiveAction.EventArgs args)
    {
        _IsScaling = false;
        if (!args.DeltaSum.IsEqual(0.0, double.Epsilon))
        {
            CommitChanges();
        }
        _HudElements.ForEach(Remove);
        _HudElements.Fill(null);
        RemoveHintMessage();
        _UpdateActions();
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    [AutoRegister]
    internal static void Register()
    {
        RegisterEditor<TorusEditor>();
    }
}
