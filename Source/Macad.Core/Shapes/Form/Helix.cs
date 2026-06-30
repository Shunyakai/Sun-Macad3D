using System;
using System.Diagnostics;
using Macad.Core.Topology;
using Macad.Common;
using Macad.Common.Serialization;
using Macad.Occt;

namespace Macad.Core.Shapes;

[SerializeType]
public sealed class Helix : ModifierBase
{
    #region Enums

    [SerializeType]
    public enum HelixHandedness
    {
        Right,
        Left
    }

    #endregion

    #region Properties

    [SerializeMember]
    public double Pitch
    {
        get { return _Pitch; }
        set
        {
            if (_Pitch != value)
            {
                SaveUndo();
                _Pitch = Math.Max(0.001, value);
                Invalidate();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public double Height
    {
        get { return _Height; }
        set
        {
            if (_Height != value)
            {
                SaveUndo();
                _Height = Math.Max(0.001, value);
                Invalidate();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public double Radius
    {
        get { return _Radius; }
        set
        {
            if (_Radius != value)
            {
                SaveUndo();
                _Radius = Math.Max(0.0, value);
                Invalidate();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public double TaperAngle
    {
        get { return _TaperAngle; }
        set
        {
            if (_TaperAngle != value)
            {
                SaveUndo();
                _TaperAngle = value.Clamp(-89.9, 89.9);
                Invalidate();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public HelixHandedness Handedness
    {
        get { return _Handedness; }
        set
        {
            if (_Handedness != value)
            {
                SaveUndo();
                _Handedness = value;
                Invalidate();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    public override ShapeType ShapeType
    {
        get { return IsSkipped ? GetOperand(0)?.GetShapeType() ?? ShapeType.Unknown : ShapeType.Solid; }
    }

    #endregion

    #region Members

    double _Pitch;
    double _Height;
    double _Radius;
    double _TaperAngle;
    HelixHandedness _Handedness;

    #endregion

    #region Create

    public Helix()
    {
        _Pitch = 10.0;
        _Height = 50.0;
        _Radius = 10.0;
        _TaperAngle = 0.0;
        _Handedness = HelixHandedness.Right;
    }

    //--------------------------------------------------------------------------------------------------

    public static Helix Create(Body body)
    {
        Debug.Assert(body != null);

        var modifierShape = new Helix();
        body.AddShape(modifierShape);

        return modifierShape;
    }

    #endregion

    #region Make

    protected override bool MakeInternal(MakeFlags flags)
    {
        if (Operands.Count != 1)
        {
            Messages.Error("Helix needs exactly one source shape profile.");
            HasErrors = true;
            return false;
        }

        var faceShape = GetOperand2DFaces(0, null);
        if (faceShape == null)
            return false;

        if (faceShape.Faces().Count == 0)
        {
            Messages.Error("The sketch does not contain any valid contours.");
            return false;
        }

        var spineWire = _CreateHelixSpine();
        if (spineWire == null)
        {
            Messages.Error("The helix path cannot be computed.");
            return false;
        }

        var maker = new BRepOffsetAPI_MakePipe(spineWire, faceShape);
        if (!maker.IsDone())
        {
            Messages.Error("The helix sweep cannot be computed.");
            return false;
        }

        BRep = maker.Shape();

        var analyzer = new BRepCheck_Analyzer(BRep);
        if (!analyzer.IsValid())
        {
            Messages.Warning("The resulting solid is not valid. Check input shape and parameters.");
        }

        return base.MakeInternal(flags);
    }

    //--------------------------------------------------------------------------------------------------

    TopoDS_Wire _CreateHelixSpine()
    {
        double radius = _Radius;
        double pitch = _Pitch;
        double height = _Height;

        if (radius < 0 || pitch <= 0 || height <= 0)
            return null;

        double turns = height / pitch;
        double totalAngle = 2 * Math.PI * turns;
        int pointsPerTurn = 64;
        int totalPoints = Math.Max(64, (int)(pointsPerTurn * turns));

        TColgp_Array1OfPnt poles = new(1, totalPoints + 1);
        double angleRad = _TaperAngle * Math.PI / 180.0;
        double tanAngle = Math.Tan(angleRad);

        for (int i = 0; i <= totalPoints; i++)
        {
            double fraction = (double)i / totalPoints;
            double z = height * fraction;
            double r = radius + z * tanAngle;
            if (r < 0) r = 0;

            double angle = totalAngle * fraction;
            if (_Handedness == HelixHandedness.Left)
            {
                angle = -angle;
            }

            double x = r * Math.Cos(angle);
            double y = r * Math.Sin(angle);

            poles.SetValue(i + 1, new Pnt(x, y, z));
        }

        var interpolator = new GeomAPI_PointsToBSpline(poles);
        if (!interpolator.IsDone())
            return null;

        var curve = interpolator.Curve();
        if (curve == null)
            return null;

        var edgeMaker = new BRepBuilderAPI_MakeEdge(curve);
        if (!edgeMaker.IsDone())
            return null;

        var wireMaker = new BRepBuilderAPI_MakeWire(edgeMaker.Edge());
        if (!wireMaker.IsDone())
            return null;

        return wireMaker.Wire();
    }

    #endregion

    #region Overrides

    public override void InvalidateTransformation()
    {
        base.InvalidateTransformation();
        Invalidate();
    }

    #endregion
}
