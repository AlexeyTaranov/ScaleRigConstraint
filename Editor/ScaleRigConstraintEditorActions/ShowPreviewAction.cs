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
            var bones = rigConstraint.data.bones;
            var customData = rigConstraint.data.scaleData;
            for (int i = 0; i < bones.Count; i++)
            {
                var bone = rigConstraint.data.bones[i];
                bone.transform.localPosition = customData[i].Position;
                bone.transform.localScale = customData[i].Scale;
            }
        }
    }
}
