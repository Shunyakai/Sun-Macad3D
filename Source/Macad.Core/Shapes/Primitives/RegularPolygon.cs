using System;
using Macad.Common;
using Macad.Common.Serialization;
using Macad.Occt;

namespace Macad.Core.Shapes;

[SerializeType]
public sealed class RegularPolygon : Shape
{
    #region Construction Properties

    [SerializeMember]
    public double Radius
    {
        get { return _Radius; }
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
    private double _Radius = 10.0;

    [SerializeMember]
    public int Sides
    {
        get { return _Sides; }
        set
        {
            if (_Sides != value)
            {
                SaveUndo();
                _Sides = value >= 3 ? value : 3;
                Invalidate();
                RaisePropertyChanged();
            }
        }
    }
    private int _Sides = 6;

    #endregion

    #region Initialization

    public override ShapeType ShapeType => ShapeType.Sketch;

    public static RegularPolygon Create(double radius, int sides)
    {
        return new RegularPolygon
        {
            _Radius = radius,
            _Sides = sides
        };
    }

    #endregion

    #region Make

    protected override bool MakeInternal(MakeFlags flags)
    {
        try
        {
            int sides = Sides >= 3 ? Sides : 3;
            double radius = Radius > 0.0 ? Radius : 0.001;

            Pnt[] vertices = new Pnt[sides + 1];
            double angleStep = (2.0 * Math.PI) / sides;

            for (int i = 0; i < sides; i++)
            {
                double angle = i * angleStep;
                vertices[i] = new Pnt(radius * Math.Cos(angle), radius * Math.Sin(angle), 0.0);
            }
            vertices[sides] = vertices[0]; // Close wire

            BRepBuilderAPI_MakePolygon makePolygon = new BRepBuilderAPI_MakePolygon();
            foreach (var vertex in vertices)
            {
                makePolygon.Add(vertex);
            }

            if (makePolygon.IsDone())
            {
                BRep = makePolygon.Wire();
                return base.MakeInternal(flags);
            }
        }
        catch (Exception)
        {
            // Handle exceptions
        }
        return false;
    }

    #endregion
}
