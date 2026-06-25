using System;
using System.Windows.Input;
using Macad.Common;
using Macad.Core.Shapes;
using Macad.Interaction.Panels;
using Macad.Occt;

namespace Macad.Interaction.Editors.Shapes;

public sealed class ConeEditor : Editor<Cone>
{
    BoxScaleLiveAction _ScaleAction;
    LabelHudElement[] _HudElements = new LabelHudElement[2];
    bool? _HeightDirection;
    double _StartHeight;
    double _StartRadius1;
    Pnt _StartPosition;
    bool _IsScaling;

    //--------------------------------------------------------------------------------------------------

    protected override void OnStart()
    {
        CreatePanel<ConePropertyPanel>(Entity, PropertyPanelSortingKey.Shapes);
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
            _StartHeight = Entity.Height;
            _StartRadius1 = Entity.Radius1;
            _StartPosition = Entity.Body.Position;
        }

        double maxR = Math.Max(Entity.Radius1, Entity.Radius2);
        Bnd_Box box = new Bnd_Box(new Pnt(-maxR, -maxR, 0.0), 
                                  new Pnt( maxR,  maxR, Entity.Height));
        _ScaleAction.Box = box;
        _ScaleAction.Transformation = Entity.Body.GetTransformation();
    }

    //--------------------------------------------------------------------------------------------------

    void _ScaleAction_Preview(BoxScaleLiveAction sender, BoxScaleLiveAction.EventArgs args)
    {
        _IsScaling = true;
        SetHintMessage("__Scale cone__ using gizmo, press `k:Ctrl` to round to grid stepping, press `k:Shift` to scale relative to center.");

        double newHeight = 0;
        double newRadius1 = 0;
        Pnt newPosition = _StartPosition;

        bool center = args.MouseEventData.ModifierKeys.HasFlag(ModifierKeys.Shift);

        double radius1Delta = args.DeltaSum * 0.5 * Math.Max(args.Direction.X.Abs(), args.Direction.Y.Abs());
        if (radius1Delta != 0)
        {
            if (center)
                radius1Delta *= 2.0;

            newRadius1 = _StartRadius1 + radius1Delta;
            if (args.MouseEventData.ModifierKeys.HasFlag(ModifierKeys.Control))
            {
                newRadius1 = Maths.RoundToNearest(newRadius1, WorkspaceController.Workspace.GridStep);
            }

            if (newRadius1 < 0)
                return;

            radius1Delta = newRadius1 - _StartRadius1;
            if (radius1Delta == 0)
                return;
        }

        double heightDelta = args.DeltaSum * args.Direction.Z;
        if (heightDelta != 0)
        {
            _HeightDirection ??= _StartHeight > 0;

            if (args.Direction.Z < 0 == _HeightDirection.Value)
                heightDelta *= -1;

            if (center)
                heightDelta *= 2.0;

            newHeight = _StartHeight + heightDelta;
            if (args.MouseEventData.ModifierKeys.HasFlag(ModifierKeys.Control))
            {
                newHeight = Maths.RoundToNearest(newHeight, WorkspaceController.Workspace.GridStep);
            }

            heightDelta = newHeight - _StartHeight;
            if (heightDelta == 0)
                return;
        }

        if (newHeight != 0)
        {
            if (center)
            {
                newPosition.Translate(Dir.DZ.ToVec(heightDelta * -0.5)
                                         .Transformed(new Trsf(Entity.Body.Rotation)));
            } 
            else if (args.Direction.Z < 0 == _HeightDirection.Value)
            {
                newPosition.Translate(Dir.DZ.ToVec(-heightDelta)
                                         .Transformed(new Trsf(Entity.Body.Rotation)));
            }
            Entity.Height = newHeight;

            if (_HudElements[1] == null)
            {
                _HudElements[1] = new LabelHudElement();
                Add(_HudElements[1]);
            }
            _HudElements[1]?.SetValue($"Height: {Entity.Height.ToInvariantString("F2")} mm");
        }

        if (newRadius1 != 0)
        {
            Entity.Radius1 = newRadius1;

            Vec offset = new Vec(Math.Sign(args.Direction.X), Math.Sign(args.Direction.Y), 0);
            if (offset != Vec.Zero && !center)
            {
                newPosition.Translate(offset.Scaled(radius1Delta)
                                            .Transformed(new Trsf(Entity.Body.Rotation)));
            }
            if (_HudElements[0] == null)
            {
                _HudElements[0] = new LabelHudElement();
                Add(_HudElements[0]);
            }
            _HudElements[0]?.SetValue($"Radius 1:  {Entity.Radius1.ToInvariantString("F2")} mm");
        }

        Entity.Body.Position = newPosition;
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
        _HeightDirection = null;
    }

    //--------------------------------------------------------------------------------------------------
        
    #endregion

    [AutoRegister]
    internal static void Register()
    {
        RegisterEditor<ConeEditor>();
    }
}
