using System;
using Macad.Common;
using Macad.Common.Serialization;
using Macad.Occt;

namespace Macad.Core.Shapes;

[SerializeType]
public sealed class Torus : Shape
{
    #region Construction Properties

    [SerializeMember]
    public double Radius1
    {
        get
        {
            return _Radius1;
        }
        set
        {
            if (_Radius1 != value)
            {
                SaveUndo();
                _Radius1 = value > 0.0 ? value : 0.001;
                Invalidate();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public double Radius2
    {
        get
        {
            return _Radius2;
        }
        set
        {
            if (_Radius2 != value)
            {
                SaveUndo();
                _Radius2 = value > 0.0 ? value : 0.001;
                Invalidate();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public double SegmentAngle
    {
        get { return _SegmentAngle; }
        set
        {
            if (_SegmentAngle != value)
            {
                SaveUndo();
                _SegmentAngle = value.Clamp(0, 360);
                Invalidate();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Members

    private double _Radius1;
    private double _Radius2;
    private double _SegmentAngle;

    #endregion

    #region Initialization
                
    public override ShapeType ShapeType
    {
        get { return ShapeType.Solid; }
    }

    //--------------------------------------------------------------------------------------------------

    public static Torus Create(double radius1, double radius2)
    {
        return new Torus()
        {
            _Radius1 = radius1,
            _Radius2 = radius2
        };
    }

    //--------------------------------------------------------------------------------------------------

    public Torus()
    {
        _Radius1 = 2.0;
        _Radius2 = 0.5;
        _SegmentAngle = 360.0;
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Make

    protected override bool MakeInternal(MakeFlags flags)
    {
        var r1 = Radius1 > 0.0 ? Radius1 : 0.001;
        var r2 = Radius2 > 0.0 ? Radius2 : 0.001;
        
        var makeTorus = SegmentAngle is <= 0 or >= 360
                               ? new BRepPrimAPI_MakeTorus(r1, r2) 
                               : new BRepPrimAPI_MakeTorus(r1, r2, SegmentAngle.Clamp(0.001, 360.0).ToRad());

        BRep = makeTorus.Solid();
        return base.MakeInternal(flags);
    }

    #endregion
}
