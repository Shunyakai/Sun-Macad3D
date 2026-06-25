using System;
using Macad.Common;
using Macad.Common.Serialization;
using Macad.Occt;

namespace Macad.Core.Shapes;

[SerializeType]
public sealed class Cone : Shape
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
                _Radius1 = value >= 0.0 ? value : 0.0;
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
                _Radius2 = value >= 0.0 ? value : 0.0;
                Invalidate();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public double Height
    {
        get
        {
            return _Height;
        }
        set
        {
            if (_Height != value)
            {
                SaveUndo();
                _Height = value;
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
    private double _Height;
    private double _SegmentAngle;

    #endregion

    #region Initialization
                
    public override ShapeType ShapeType
    {
        get { return ShapeType.Solid; }
    }

    //--------------------------------------------------------------------------------------------------

    public static Cone Create(double radius1, double radius2, double height)
    {
        return new Cone()
        {
            _Radius1 = radius1,
            _Radius2 = radius2,
            _Height = height
        };
    }

    //--------------------------------------------------------------------------------------------------

    public Cone()
    {
        _Radius1 = 1.0;
        _Radius2 = 0.0;
        _Height = 1.0;
        _SegmentAngle = 360.0;
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Make

    protected override bool MakeInternal(MakeFlags flags)
    {
        var r1 = Math.Max(Radius1, 0.0);
        var r2 = Math.Max(Radius2, 0.0);
        if (r1 == 0.0 && r2 == 0.0)
        {
            r1 = 0.001;
        }
        var height = Math.Max(Height.Abs(), 0.001);
        
        var makeCone = SegmentAngle is <= 0 or >= 360
                               ? new BRepPrimAPI_MakeCone(r1, r2, height) 
                               : new BRepPrimAPI_MakeCone(r1, r2, height, SegmentAngle.Clamp(0.001, 360.0).ToRad());

        TopoDS_Shape brep = makeCone.Solid();
        if (_Height < 0)
        {
            brep = brep.Moved(new TopLoc_Location(new Trsf(Pnt.Origin, new Pnt(0, 0, _Height))));
        }

        BRep = brep;
        return base.MakeInternal(flags);
    }

    #endregion
}
