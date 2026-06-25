using System;
using System.Runtime.CompilerServices;
using Macad.Common.Serialization;
using Macad.Core.Topology;
using Macad.Occt;

namespace Macad.Core.Auxiliary;

[SerializeType]
public class DatumLine : InteractiveEntity, ITransformable
{
    #region Properties

    [SerializeMember]
    public Pnt Position
    {
        get
        {
            return _Position;
        }
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

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public Quaternion Rotation
    {
        get
        {
            return _Rotation;
        }
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

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public double Length
    {
        get { return _Length; }
        set
        {
            if (_Length != value)
            {
                SaveUndo();
                _Length = Math.Max(0.01, value);
                RaisePropertyChanged();
                RaiseVisualChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Members

    Pnt _Position = Pnt.Origin;
    Quaternion _Rotation = Quaternion.Identity;
    double _Length;

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Initialization

    public static DatumLine Create()
    {
        var datumLine = new DatumLine()
        {
            Name = CoreContext.Current.Document?.AddNextNameSuffix(nameof(DatumLine)) ?? nameof(DatumLine),
            Layer = CoreContext.Current.Layers?.ActiveLayer,
            Document = CoreContext.Current.Document
        };
        datumLine.RaiseVisualChanged();
        return datumLine;
    }

    //--------------------------------------------------------------------------------------------------

    public DatumLine()
    {
        _Length = 100;
    }

    //--------------------------------------------------------------------------------------------------

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

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region ITransformable
        
    public Ax3 GetCoordinateSystem()
    {
        return Rotation.ToAx3(Position);
    }

    //--------------------------------------------------------------------------------------------------

    #endregion
}
