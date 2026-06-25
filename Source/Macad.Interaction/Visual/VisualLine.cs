using System.Linq;
using Macad.Common;
using Macad.Core;
using Macad.Core.Auxiliary;
using Macad.Core.Topology;
using Macad.Occt;
using Macad.Occt.Extensions;

namespace Macad.Interaction.Visual;

public sealed class VisualLine : VisualObject
{
    public override AIS_InteractiveObject AisObject
    {
        get
        {
            _EnsureAisObject();
            return _AisObject;
        }
    }

    //--------------------------------------------------------------------------------------------------

    public override bool IsSelectable
    {
        get { return _IsSelectable(); }
    }

    //--------------------------------------------------------------------------------------------------

    AIS_Line _AisObject;
    readonly DatumLine _DatumLine;
    Geom_Point _PStart;
    Geom_Point _PEnd;

    //--------------------------------------------------------------------------------------------------

    #region c'tors

    static VisualLine()
    {
        Layer.PresentationChanged += _OnPresentationChanged;
        Layer.InteractivityChanged += _OnInteractivityChanged;
        VisualObjectManager.IsolatedEntitiesChanged += _VisualObjectManager_IsolatedEntitiesChanged;
    }

    //--------------------------------------------------------------------------------------------------

    public static VisualLine Create(WorkspaceController workspaceController, InteractiveEntity entity)
    {
        if (entity is DatumLine datumLine)
        {
            return new VisualLine(workspaceController, datumLine);
        }

        return null;
    }

    //--------------------------------------------------------------------------------------------------

    public VisualLine(WorkspaceController workspaceController, DatumLine entity) 
        : base(workspaceController, entity)
    {
        _DatumLine = entity;
        Update();
    }

    //--------------------------------------------------------------------------------------------------

    public static SelectionSignature SelectionSignature => new (AIS_KindOfInteractive.Datum, 6);

    //--------------------------------------------------------------------------------------------------

    [AutoRegister]
    internal static void Register()
    {
        VisualObjectManager.Register<DatumLine>(Create);
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Layer

    static void _VisualObjectManager_IsolatedEntitiesChanged(VisualObjectManager visualObjectManager)
    {
        foreach (var visualLine in visualObjectManager.GetAll().OfType<VisualLine>())
        {
            visualLine._UpdateInteractivityStatus();
        }
    }

    //--------------------------------------------------------------------------------------------------

    static void _OnInteractivityChanged(Layer layer)
    {
        var workspaceController = InteractiveContext.Current?.WorkspaceController;
        if (workspaceController == null)
            return;

        foreach (var visualLine in workspaceController.VisualObjects.Where(entity => entity.Layer == layer).OfType<VisualLine>())
        {
            visualLine._UpdateInteractivityStatus();
        }
    }

    //--------------------------------------------------------------------------------------------------

    static void _OnPresentationChanged(Layer layer)
    {
        var workspaceController = InteractiveContext.Current?.WorkspaceController;
        if (workspaceController == null)
            return;

        foreach (var visualLine in workspaceController.VisualObjects.Where(entity => entity.Layer == layer).OfType<VisualLine>())
        {
            var aisShape = visualLine._AisObject;
            if (aisShape == null)
                continue;

            aisShape.SetColor((layer?.Color ?? Colors.Auxillary).ToQuantityColor());
            workspaceController.AisContext.RecomputePrsOnly(aisShape, false, true);
        }
        workspaceController.Invalidate();
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    public override void Remove()
    {
        if (_AisObject != null)
        {
            AisContext.Erase(_AisObject, false);
            _AisObject = null;
        }
    }

    //--------------------------------------------------------------------------------------------------

    public override void Update()
    {
        if (_AisObject == null)
        {
            _EnsureAisObject();
        }
        else
        {
            _UpdatePresentation();
            AisContext.Redisplay(_AisObject, false);
        }
    }

    //--------------------------------------------------------------------------------------------------

    void _UpdatePresentation()
    {
        if (_AisObject == null)
            return;

        var cs = _DatumLine.GetCoordinateSystem();
        var dir = cs.Direction;
        var pStart = _DatumLine.Position;
        var pEnd = pStart.Translated(dir.ToVec(_DatumLine.Length));

        _PStart = new Geom_CartesianPoint(pStart);
        _PEnd = new Geom_CartesianPoint(pEnd);

        _AisObject.SetPoints(_PStart, _PEnd);
        _AisObject.SetColor((_DatumLine.Layer?.Color ?? Colors.Auxillary).ToQuantityColor());
        _AisObject.SetWidth(3.0);
    }

    //--------------------------------------------------------------------------------------------------

    void _EnsureAisObject()
    {
        if (_AisObject != null)
            return;

        if (_DatumLine == null) 
            return;

        var cs = _DatumLine.GetCoordinateSystem();
        var dir = cs.Direction;
        var pStart = _DatumLine.Position;
        var pEnd = pStart.Translated(dir.ToVec(_DatumLine.Length));

        _PStart = new Geom_CartesianPoint(pStart);
        _PEnd = new Geom_CartesianPoint(pEnd);

        _AisObject = new AIS_Line(_PStart, _PEnd);

        _UpdatePresentation();

        _AisObject.SetOwner(new AISX_Guid(_DatumLine.Guid));

        _UpdateInteractivityStatus();
    }

    //--------------------------------------------------------------------------------------------------

    bool _IsSelectable()
    {
        if (_AisObject == null)
            return false;

        var layer = Entity?.Layer;
        if (layer == null)
            return false;

        if (!layer.IsVisible
            || layer.IsLocked
            || CoreContext.Current.Layers.IsolateActiveLayer && CoreContext.Current.Layers.ActiveLayer != layer)
        {
            return false;
        }
        return true;
    }

    //--------------------------------------------------------------------------------------------------

    void _UpdateInteractivityStatus()
    {
        if (_AisObject == null)
            return;

        var layer = Entity?.Layer;
        if (layer == null)
            return;

        bool isVisible = layer.IsVisible;
        if (WorkspaceController.VisualObjects.EntityIsolationEnabled)
        {
            isVisible &= WorkspaceController.VisualObjects.GetIsolatedEntities().Contains(Entity);
        }

        if (isVisible)
        {
            if (AisContext.IsDisplayed(_AisObject))
            {
                AisContext.Update(_AisObject, false);
            }
            else
            {
                AisContext.Display(_AisObject, false);
            }

            if (WorkspaceController.Selection.SelectedEntities.Contains(Entity) && !AisContext.IsSelected(_AisObject))
            {
                AisContext.AddOrRemoveSelected(_AisObject, false);
            }
        }
        else
        {
            if (AisContext.IsDisplayed(_AisObject))
            {
                AisContext.Erase(_AisObject, false);
            }
        }

        RaiseAisObjectChanged();
    }

    //--------------------------------------------------------------------------------------------------
}
