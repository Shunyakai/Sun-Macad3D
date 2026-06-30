using Macad.Common;
using Macad.Core;
using Macad.Core.Auxiliary;
using Macad.Core.Geom;
using Macad.Core.Shapes;
using Macad.Core.Topology;
using Macad.Interaction.Visual;
using Macad.Occt;
using Macad.Interaction;

namespace Macad.Interaction.Editors.Shapes;

public class AttachSketchTool : Tool
{
    readonly Body _SketchBody;
    Trihedron _DefaultPlanes;
    Pln _Plane = Pln.XOY;
    Pln _SavedWorkingPlane;

    //--------------------------------------------------------------------------------------------------

    public AttachSketchTool(Body sketchBody)
    {
        _SketchBody = sketchBody;
    }

    //--------------------------------------------------------------------------------------------------

    protected override bool OnStart()
    {
        if (_SketchBody == null)
            return false;

        InteractiveContext.Current.WorkspaceController.Selection.SelectEntity(null);
        _SavedWorkingPlane = WorkspaceController.Workspace.WorkingPlane;

        _DefaultPlanes = new(WorkspaceController, _SavedWorkingPlane.Position, Trihedron.Components.Planes);
        Add(_DefaultPlanes);

        var selectionFilter = new FaceSelectionFilter(FaceSelectionFilter.FaceType.Plane)
            .Or(new SignatureSelectionFilter(VisualPlane.SelectionSignature));
        var toolAction = new SelectSubshapeAction(SubshapeTypes.Face, null, selectionFilter);
        if (!StartAction(toolAction))
            return false;

        toolAction.Finished += _ToolAction_Finished;
        toolAction.Preview += _ToolActionPreview;

        SetHintMessage("__Select face or plane__ to attach the sketch to.");
        SetCursor(Cursors.SelectFace);
        return true;
    }

    //--------------------------------------------------------------------------------------------------

    protected override void Cleanup()
    {
        if (WorkspaceController.Workspace.WorkingPlane != _SavedWorkingPlane)
        {
            WorkspaceController.Workspace.WorkingPlane = _SavedWorkingPlane;
        }

        base.Cleanup();
    }

    //--------------------------------------------------------------------------------------------------

    bool _GetPlaneFromAction(SelectSubshapeAction.EventArgs args)
    {
        if (args.SelectedEntity is DatumPlane datumPlane)
        {
            _Plane = new Pln(datumPlane.GetCoordinateSystem());
            return true;
        }
        else if (args.SelectedSubshapeType == SubshapeTypes.Face)
        {
            var face = args.SelectedSubshape.ToFace();
            var brepAdaptor = new BRepAdaptor_Surface(face, true);
            if (brepAdaptor.GetSurfaceType() != GeomAbs_SurfaceType.Plane)
            {
                SetHintMessage("Selected face is not a plane type surface.");
            }
            else
            {
                FaceAlgo.GetCenteredPlaneFromFace(face, out _Plane);
                return true;
            }
        }
        else if (args.SelectedAisObject != null)
        {
            switch (_DefaultPlanes.GetComponent(args.SelectedAisObject))
            {
                case Trihedron.Components.PlaneXY:
                    _Plane = _SavedWorkingPlane;
                    break;
                case Trihedron.Components.PlaneZX:
                    _Plane = new Pln(new Ax3(_SavedWorkingPlane.Location, _SavedWorkingPlane.YAxis.Direction.Reversed(), _SavedWorkingPlane.XAxis.Direction));
                    break;
                case Trihedron.Components.PlaneYZ:
                    _Plane = new Pln(new Ax3(_SavedWorkingPlane.Location, _SavedWorkingPlane.XAxis.Direction, _SavedWorkingPlane.YAxis.Direction));
                    break;
                default:
                    return false;
            }

            bool flip = !args.MouseEventData.PickAxis.IsOpposite(_Plane.Axis, Maths.HalfPI);
            if (flip)
            {
                _Plane = new Pln(new Ax3(_Plane.Location, _Plane.Axis.Direction.Reversed(), _Plane.XAxis.Direction.Reversed()));
            }

            return true;
        }

        return false;
    }

    //--------------------------------------------------------------------------------------------------

    void _ToolActionPreview(SelectSubshapeAction action, SelectSubshapeAction.EventArgs args)
    {
        WorkspaceController.Workspace.WorkingPlane = _GetPlaneFromAction(args) ? _Plane : _SavedWorkingPlane;
    }

    //--------------------------------------------------------------------------------------------------

    void _ToolAction_Finished(SelectSubshapeAction action, SelectSubshapeAction.EventArgs args)
    {
        if (_GetPlaneFromAction(args))
        {
            StopAction(action);
            Stop();
            _AttachSketch();
        }
        else
        {
            action.Reset();
        }

        WorkspaceController.Invalidate();
    }

    //--------------------------------------------------------------------------------------------------

    void _AttachSketch()
    {
        _SketchBody.Position = _Plane.Location;
        _SketchBody.Rotation = _Plane.Rotation();
        InteractiveContext.Current?.UndoHandler.Commit();
        InteractiveContext.Current?.WorkspaceController.Invalidate();
    }

    //--------------------------------------------------------------------------------------------------
}
