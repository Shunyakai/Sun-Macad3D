using System;
using System.Collections.Generic;
using System.Windows.Input;
using Macad.Common;
using Macad.Core;
using Macad.Core.Topology;
using Macad.Core.Components;
using Macad.Occt;
using Macad.Presentation;

namespace Macad.Interaction;

public class AppearancePerFaceTool : Tool
{
    public static Func<Body, int, bool> ShowAppearanceDialogCallback { get; set; }

    SelectSubshapeAction _SelectAction;
    Body _TargetBody;
    VisualStyle _VisualStyle;

    public AppearancePerFaceTool(Body body)
    {
        _TargetBody = body;
        _VisualStyle = VisualStyle.Create(_TargetBody);
    }

    protected override bool OnStart()
    {
        _SelectAction = new SelectSubshapeAction(SubshapeTypes.Face);
        if (!StartAction(_SelectAction))
            return false;

        _SelectAction.Finished += _SelectAction_Finished;

        SetHintMessage("__Select a face__ to customize its appearance, or press `k:Esc` to finish.");
        SetCursor(Cursors.SelectShape);

        return true;
    }

    private void _SelectAction_Finished(SelectSubshapeAction action, SelectSubshapeAction.EventArgs args)
    {
        if (args.SelectedSubshape != null && args.SelectedEntity == _TargetBody)
        {
            var selectedFace = args.SelectedSubshape;
            var brep = _TargetBody.GetTransformedBRep(true);
            if (brep != null)
            {
                int faceIndex = -1;
                int currentIndex = 0;
                var explorer = new TopExp_Explorer(brep, TopAbs_ShapeEnum.FACE, TopAbs_ShapeEnum.SHAPE);
                while (explorer.More())
                {
                    if (explorer.Current().IsSame(selectedFace))
                    {
                        faceIndex = currentIndex;
                        break;
                    }
                    explorer.Next();
                    currentIndex++;
                }

                if (faceIndex != -1 && ShowAppearanceDialogCallback != null)
                {
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (ShowAppearanceDialogCallback(_TargetBody, faceIndex))
                        {
                            _TargetBody.RaiseVisualChanged();
                            WorkspaceController.Invalidate();
                        }
                    });
                }
            }
        }

        action.Reset();
        WorkspaceController.Invalidate();
    }
}
