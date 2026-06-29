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
    public double Radius
    {
        get
        {
            return _Radius;
        }
        set
        {
            if (_Radius != value)
            {
                SaveUndo();
                _Radius = value > 0.0 ? value : 0.001;
                Invalidate();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public double RadiusTop
    {
        get
        {
            return _RadiusTop;
        }
        set
        {
            if (_RadiusTop != value)
            {
                SaveUndo();
                _RadiusTop = value >= 0.0 ? value : 0.0;
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

    private double _Radius;
    private double _RadiusTop;
    private double _Height;
    private double _SegmentAngle;

    #endregion

    #region Initialization
                
    public override ShapeType ShapeType
    {
        get { return ShapeType.Solid; }
    }

    //--------------------------------------------------------------------------------------------------

    public static Cone Create(double radius, double radiusTop, double height)
    {
        return new Cone()
        {
            _Radius = radius,
            _RadiusTop = radiusTop,
            _Height = height
        };
    }

    //--------------------------------------------------------------------------------------------------

    public Cone()
    {
        _Radius = 1.0;
        _RadiusTop = 0.0;
        _Height = 1.0;
        _SegmentAngle = 360.0;
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Make

    protected override bool MakeInternal(MakeFlags flags)
    {
        var radius = Radius > 0.0 ? Radius : 0.001;
        var radiusTop = RadiusTop >= 0.0 ? RadiusTop : 0.0;
        var height = Math.Max(Height.Abs(), 0.001);
        
        var makeCone = SegmentAngle is <= 0 or >= 360
                           ? new BRepPrimAPI_MakeCone(radius, radiusTop, height) 
                           : new BRepPrimAPI_MakeCone(radius, radiusTop, height, SegmentAngle.Clamp(0.001, 360.0).ToRad());

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
