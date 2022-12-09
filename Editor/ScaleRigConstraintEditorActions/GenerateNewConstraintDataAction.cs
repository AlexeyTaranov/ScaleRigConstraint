using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;

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
            var max = WeightedTransformArray.k_MaxLength;
            var textLabel = $"Changed objects: {modifiedObjectsCount}, max: {max}.";
            if (modifiedObjectsCount > max)
            {
                textLabel += " Will be saved only first 8 changes.";
                var errorStyle = new GUIStyle()
                    { fontStyle = FontStyle.Bold, normal = new GUIStyleState() { textColor = Color.red } };
                GUILayout.Label(textLabel, errorStyle);
            }
            else
            {
                GUILayout.Label(textLabel);
                foreach (var modifiedObject in modifiedObjects)
                {
                    GUILayout.Label($"Modified object: {modifiedObject.name}");
                }
            }
        }

        private void ApplyNewConstraintData(Transform[] modifiedObjects)
        {
            var max = Mathf.Min(WeightedTransformArray.k_MaxLength, modifiedObjects.Length);
            var localScaleData = modifiedObjects.Select(t => (new ScalePosition(t.localScale, t.localPosition), t))
                .Take(max)
                .ToArray();
            var rigConstraint = ScaleRigEditor.rigConstraint;
            rigConstraint.data.SetScaleData(localScaleData);
        }
    }
}
