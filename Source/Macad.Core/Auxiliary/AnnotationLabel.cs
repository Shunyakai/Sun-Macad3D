using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Macad.Common.Serialization;
using Macad.Core.Topology;
using Macad.Core.Shapes;
using Macad.Occt;

namespace Macad.Core.Auxiliary;

[SerializeType]
public class AnnotationLabel : InteractiveEntity, ITransformable
{
    #region Properties

    [SerializeMember]
    public Pnt Position
    {
        get => _Position;
        set
        {
            if (!_Position.IsEqual(value, double.Epsilon))
            {
                SaveUndo();
                _Position = value;
                RaisePropertyChanged();
                RaiseVisualChanged();
            }
        }
    }

    [SerializeMember]
    public Quaternion Rotation
    {
        get => _Rotation;
        set
        {
            if (!_Rotation.IsEqual(value))
            {
                SaveUndo();
                _Rotation = value;
                RaisePropertyChanged();
                RaiseVisualChanged();
            }
        }
    }

    [SerializeMember]
    public string Text
    {
        get => _Text;
        set
        {
            if (_Text != value)
            {
                SaveUndo();
                _Text = value;
                RaisePropertyChanged();
                RaiseVisualChanged();
            }
        }
    }

    [SerializeMember]
    public Pnt LeaderTarget
    {
        get => _LeaderTarget;
        set
        {
            if (!_LeaderTarget.IsEqual(value, double.Epsilon))
            {
                SaveUndo();
                _LeaderTarget = value;
                RaisePropertyChanged();
                RaiseVisualChanged();
            }
        }
    }

    [SerializeMember]
    public bool HasLeader
    {
        get => _HasLeader;
        set
        {
            if (_HasLeader != value)
            {
                SaveUndo();
                _HasLeader = value;
                RaisePropertyChanged();
                RaiseVisualChanged();
            }
        }
    }

    public Shape Shape => null;

    #endregion

    #region Initialization

    Pnt _Position = Pnt.Origin;
    Quaternion _Rotation = Quaternion.Identity;
    Pnt _LeaderTarget = Pnt.Origin;
    string _Text = "Label";
    bool _HasLeader = true;

    public static AnnotationLabel Create()
    {
        var label = new AnnotationLabel()
        {
            Name = CoreContext.Current.Document?.AddNextNameSuffix(nameof(AnnotationLabel)) ?? nameof(AnnotationLabel),
            Layer = CoreContext.Current.Layers?.ActiveLayer,
            Document = CoreContext.Current.Document
        };
        label.RaiseVisualChanged();
        return label;
    }

    public AnnotationLabel()
    {
        _Text = "Label";
    }

    #endregion

    #region Entity

    protected override void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        base.RaisePropertyChanged(propertyName);
        if (!IsDeserializing)
        {
            CoreContext.Current?.Document?.MarkAsUnsaved();
        }
    }

    #endregion

    #region ITransformable

    public Ax3 GetCoordinateSystem()
    {
        return Rotation.ToAx3(Position);
    }

    #endregion
}
