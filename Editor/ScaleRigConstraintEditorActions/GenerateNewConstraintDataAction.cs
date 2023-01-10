using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScaleRigConstraintAnimation.ScaleRigConstraintEditorActions
{
    public class GenerateNewConstraintDataAction : BaseScaleRigConstraintEditorAction
    {
        public GenerateNewConstraintDataAction(ScaleRigConstraintEditor scaleRigEditor) : base(scaleRigEditor)
        {
        }

        public override bool IsCompleteModification()
        {
            DrawLabelWithModifiedObjectsCount();
            if (GUILayout.Button("Apply"))
            {
                var modifiedTransforms = DefaultTransformValues
                    .Where(pair => pair.Value.IsHaveLocalTransformOffset(pair.Key))
                    .Select(pair => pair.Key).ToArray();
                ApplyNewConstraintData(modifiedTransforms);
                RestoreDefaultTransformsWithoutModifications();
                ScaleRigEditor.serializedObject.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(ScaleRigEditor.serializedObject.targetObject);
                return true;
            }

            if (GUILayout.Button("Cancel"))
            {
                return true;
            }

            return false;
        }

        private void DrawLabelWithModifiedObjectsCount()
        {
            var modifiedObjects = DefaultTransformValues.Where(pair => pair.Value.IsHaveLocalTransformOffset(pair.Key))
                .Select(pair => pair.Key);
            var modifiedObjectsCount = modifiedObjects.Count();
            var textLabel = $"Changed objects: {modifiedObjectsCount}";
            GUILayout.Label(textLabel);
            foreach (var modifiedObject in modifiedObjects)
            {
                GUILayout.Label($"Modified object: {modifiedObject.name}");
            }
        }

        private void ApplyNewConstraintData(Transform[] modifiedObjects)
        {
            var localScaleData =
                modifiedObjects.Select(t => new TransformDataSerialize(t, t.localScale, t.localPosition));
            var rigConstraint = ScaleRigEditor.rigConstraint;
            rigConstraint.data.SetScaleData(localScaleData);
        }
    }
}