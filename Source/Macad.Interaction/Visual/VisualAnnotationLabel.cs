using System;
using System.Linq;
using Macad.Core;
using Macad.Core.Auxiliary;
using Macad.Core.Topology;
using Macad.Occt;
using Macad.Occt.Extensions;
using Macad.Common;


namespace Macad.Interaction.Visual;

public class VisualAnnotationLabel : VisualObject
{
    readonly AnnotationLabel _AnnotationLabel;
    AIS_TextLabel _AisTextLabel;
    AIS_Line _AisLine;

    public VisualAnnotationLabel(WorkspaceController workspaceController, AnnotationLabel entity) 
        : base(workspaceController, entity)
    {
        _AnnotationLabel = entity;
        Update();
    }

    public override AIS_InteractiveObject AisObject => _AisTextLabel;

    public override bool IsSelectable
    {
        get => true;
    }

    public static VisualAnnotationLabel Create(WorkspaceController workspaceController, InteractiveEntity entity)
    {
        if (entity is AnnotationLabel label)
        {
            return new VisualAnnotationLabel(workspaceController, label);
        }
        return null;
    }

    [AutoRegister]
    internal static void Register()
    {
        VisualObjectManager.Register<AnnotationLabel>(Create);
    }

    public override void Remove()
    {
        if (_AisTextLabel != null)
        {
            AisContext.Erase(_AisTextLabel, false);
            _AisTextLabel = null;
        }
        if (_AisLine != null)
        {
            AisContext.Erase(_AisLine, false);
            _AisLine = null;
        }
    }

    public override void Update()
    {
        _EnsureAisObjects();
        _UpdatePresentation();
    }

    void _EnsureAisObjects()
    {
        if (_AisTextLabel == null)
        {
            _AisTextLabel = new AIS_TextLabel();
            _AisTextLabel.SetOwner(new AISX_Guid(_AnnotationLabel.Guid));
            _AisTextLabel.SetDisplayMode(1);
            AisContext.Display(_AisTextLabel, false);
        }

        if (_AnnotationLabel.HasLeader)
        {
            if (_AisLine == null)
            {
                var startPt = new Geom_CartesianPoint(_AnnotationLabel.Position);
                var endPt = new Geom_CartesianPoint(_AnnotationLabel.LeaderTarget);
                _AisLine = new AIS_Line(startPt, endPt);
                _AisLine.SetOwner(new AISX_Guid(_AnnotationLabel.Guid));
                AisContext.Display(_AisLine, false);
            }
        }
        else
        {
            if (_AisLine != null)
            {
                AisContext.Erase(_AisLine, false);
                _AisLine = null;
            }
        }
    }

    void _UpdatePresentation()
    {
        if (_AisTextLabel != null)
        {
            _AisTextLabel.SetPosition(_AnnotationLabel.Position);
            _AisTextLabel.SetText(new TCollection_ExtendedString(_AnnotationLabel.Text));
            _AisTextLabel.SetColor(new Color(1.0f, 1.0f, 0.0f).ToQuantityColor()); // Yellow
            AisContext.Redisplay(_AisTextLabel, false);
        }

        if (_AisLine != null)
        {
            var startPt = new Geom_CartesianPoint(_AnnotationLabel.Position);
            var endPt = new Geom_CartesianPoint(_AnnotationLabel.LeaderTarget);
            _AisLine.SetPoints(startPt, endPt);
            _AisLine.SetColor(new Color(1.0f, 1.0f, 0.0f).ToQuantityColor());
            AisContext.Redisplay(_AisLine, false);
        }
    }
}
