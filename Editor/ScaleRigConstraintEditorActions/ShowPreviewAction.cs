using UnityEngine;

namespace ScaleRigConstraintAnimation.ScaleRigConstraintEditorActions
{
    public class ShowPreviewAction : BaseScaleRigConstraintEditorAction
    {
        public ShowPreviewAction(ScaleRigConstraintEditor scaleRigEditor) : base(scaleRigEditor)
        {
            ApplyPreview();
        }

        public override bool IsCompleteModification()
        {
            if (GUILayout.Button("Cancel"))
            {
                return true;
            }

            return false;
        }

        private void ApplyPreview()
        {
            var rigConstraint = ScaleRigEditor.rigConstraint;
            var customData = rigConstraint.data.ScaleData;
            foreach (var data in customData)
            {
                var transform = data.Transform;
                var transformData = data.ToTransformData();
                transform.localPosition = transformData.Position;
                transform.localScale = transformData.Scale;
            }
        }
    }
}