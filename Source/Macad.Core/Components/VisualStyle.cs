using System.Diagnostics;
using System.Collections.Generic;
using Macad.Core.Topology;
using Macad.Common;
using Macad.Common.Serialization;
using Macad.Occt;

namespace Macad.Core.Components;

public class VisualStyle : Component
{
    #region Properties

    [SerializeMember]
    public Color Color
    {
        get { return _Color; }
        set
        {
            if (_Color != value)
            {
                SaveUndo();
                _Color = value;
                _RaiseVisualStyleChanged();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public float Transparency
    {
        get { return _Transparency; }
        set
        {
            if (_Transparency != value)
            {
                SaveUndo();
                _Transparency = value;
                _RaiseVisualStyleChanged();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public Graphic3d_NameOfMaterial Material
    {
        get { return _Material; }
        set
        {
            if (_Material != value)
            {
                SaveUndo();
                _Material = value;
                _RaiseVisualStyleChanged();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public List<FaceAppearance> FaceAppearances
    {
        get { return _FaceAppearances; }
        set
        {
            SaveUndo();
            _FaceAppearances = value;
            _RaiseVisualStyleChanged();
            RaisePropertyChanged();
        }
    }

    //--------------------------------------------------------------------------------------------------

    public Body Body
    {
        get { return Owner as Body; }
    }

    //--------------------------------------------------------------------------------------------------

    public override IDecorable Owner
    {
        get
        {
            return base.Owner;

        }
        set
        {
            if (value == Owner) return;
            Body?.RaiseVisualChanged();
            base.Owner = value;
            if (value != null)
            {
                Debug.Assert(Body != null);
                Body?.RaiseVisualChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Member

    Color _Color;
    float _Transparency;
    Graphic3d_NameOfMaterial _Material;
    List<FaceAppearance> _FaceAppearances;

    //--------------------------------------------------------------------------------------------------

    #endregion

    public VisualStyle() 
    {
        _Color = Colors.Default;
        _Transparency = 0;
        _Material = Graphic3d_NameOfMaterial.DEFAULT;
        _FaceAppearances = new List<FaceAppearance>();
    }

    //--------------------------------------------------------------------------------------------------

    protected override void OwnerChanged(IDecorable oldOwner, IDecorable newOwner)
    {
        base.OwnerChanged(oldOwner, newOwner);

        if (newOwner != null)
        {
        }
        else
        {
//            InvalidateVisual();
        }
    }

    //--------------------------------------------------------------------------------------------------

    public static VisualStyle Create(Body ownerBody)
    {
        var component = ownerBody.FindComponent<VisualStyle>(true);
        if ((component != null) && (component.Owner == ownerBody))
            return component;

        component = new VisualStyle();
        ownerBody.AddComponent(component);


        return component;
    }

    //--------------------------------------------------------------------------------------------------

    #region Events

    public delegate void VisualStyleChangedEventHandler(Body body, VisualStyle visualStyle);

    public event VisualStyleChangedEventHandler VisualStyleChanged;

    void _RaiseVisualStyleChanged()
    {
        VisualStyleChanged?.Invoke(Owner as Body, this);
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

}

[SerializeType]
public class FaceAppearance
{
    [SerializeMember]
    public int FaceIndex { get; set; }

    [SerializeMember]
    public Color Color { get; set; }

    [SerializeMember]
    public float Transparency { get; set; }

    [SerializeMember]
    public Graphic3d_NameOfMaterial Material { get; set; }

    public FaceAppearance()
    {
        Color = Colors.Default;
        Transparency = 0.0f;
        Material = Graphic3d_NameOfMaterial.DEFAULT;
    }

    public FaceAppearance(int faceIndex, Color color, float transparency, Graphic3d_NameOfMaterial material)
    {
        FaceIndex = faceIndex;
        Color = color;
        Transparency = transparency;
        Material = material;
    }
}