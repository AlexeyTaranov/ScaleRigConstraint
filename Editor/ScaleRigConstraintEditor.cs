using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Assertions;

namespace ScaleRigConstraintAnimation
{
    [CustomEditor(typeof(ScaleRigConstraint))]
    public class ScaleRigConstraintEditor : Editor
    {
        private const float MinOffset = 0.1f;

        private SerializedProperty weightProperty;
        private SerializedProperty bonesProperty;
        private ScaleRigConstraint rigConstraint;

        private Func<bool> isCompleteModification;

        private struct TransformValue
        {
            public Vector3 position;
            public Vector3 scale;
            public Quaternion rotation;
        }

        public void OnEnable()
        {
            weightProperty = serializedObject.FindProperty("m_Weight");
            rigConstraint = (ScaleRigConstraint)serializedObject.targetObject;
            var data = serializedObject.FindProperty("m_Data");
            bonesProperty = data.FindPropertyRelative(nameof(ScaleRigConstraint.data.bones));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(weightProperty);
            EditorGUILayout.PropertyField(bonesProperty);
            DrawButtonsAndExecuteModifications();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawButtonsAndExecuteModifications()
        {
            if (Application.isPlaying)
            {
                GUILayout.Label("To apply modify data need stop play mode");
                return;
            }

            if (isCompleteModification?.Invoke() == true)
            {
                isCompleteModification = null;
            }

            if (isCompleteModification == null)
            {
                if (GUILayout.Button("Start Modify"))
                {
                    isCompleteModification = GenerateNewConstraintData();
                }

                if (GUILayout.Button("Preview"))
                {
                    isCompleteModification = ShowPreview();
                }
            }
        }

        Dictionary<Transform, TransformValue> GetDefaultTransformValues()
        {
            var animator = rigConstraint.transform.GetComponentInParent<Animator>();
            Assert.IsNotNull(animator,
                $"[{nameof(ScaleRigConstraint)}] Can't find animator in parent for constraint: {rigConstraint.name}");
            if (animator == null)
            {
                return null;
            }

            var defaultPositions = new Dictionary<Transform, TransformValue>();
            var bones = animator.GetComponentsInChildren<Transform>();
            foreach (var bone in bones)
            {
                defaultPositions.Add(bone, new TransformValue
                {
                    position = bone.localPosition,
                    scale = bone.localScale,
                    rotation = bone.localRotation
                });
            }

            return defaultPositions;
        }

        private Func<bool> GenerateNewConstraintData()
        {
            var defaultTransforms = GetDefaultTransformValues();
            if (defaultTransforms != null)
            {
                return DrawUI;
            }

            return () => true;

            bool DrawUI()
            {
                DrawLabelWithModifiedObjectsCount(defaultTransforms);
                if (GUILayout.Button("Apply"))
                {
                    var modifiedTransforms = defaultTransforms.Where(pair => IsUpdatedTransform(pair.Value, pair.Key))
                        .Select(pair => pair.Key).ToArray();
                    ApplyNewConstraintData(modifiedTransforms);
                    RestoreLocalTranslations(ref defaultTransforms);
                    serializedObject.ApplyModifiedProperties();
                    return true;
                }

                if (GUILayout.Button("Cancel"))
                {
                    RestoreLocalTranslations(ref defaultTransforms);
                    return true;
                }

                return false;
            }
        }

        private Func<bool> ShowPreview()
        {
            var defaultTransforms = GetDefaultTransformValues();
            if (defaultTransforms == null)
            {
                return () => true;
            }

            ApplyPreview();
            return DrawUI;

            bool DrawUI()
            {
                if (GUILayout.Button("Cancel"))
                {
                    RestoreLocalTranslations(ref defaultTransforms);
                    return true;
                }

                return false;
            }
        }

        void DrawLabelWithModifiedObjectsCount(in Dictionary<Transform, TransformValue> defaultValues)
        {
            var modifiedObjects = GetModifiedObjects(defaultValues);
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

        IEnumerable<Transform> GetModifiedObjects(in Dictionary<Transform, TransformValue> defaultValues)
        {
            return defaultValues.Where(pair => IsUpdatedTransform(pair.Value, pair.Key)).Select((pair => pair.Key));
        }

        bool IsUpdatedTransform(TransformValue defaultValue, Transform transform)
        {
            bool isChangedValues = Vector3.Distance(defaultValue.position, transform.localPosition) > MinOffset ||
                                   Vector3.Distance(defaultValue.scale, transform.localScale) > MinOffset;
            return isChangedValues;
        }

        private void RestoreLocalTranslations(ref Dictionary<Transform, TransformValue> defaultValues)
        {
            foreach (var tuple in defaultValues)
            {
                var inputT = tuple.Key;
                inputT.localPosition = tuple.Value.position;
                inputT.localScale = tuple.Value.scale;
                inputT.localRotation = tuple.Value.rotation;
            }
        }

        void ApplyNewConstraintData(Transform[] modifiedObjects)
        {
            var bones = new WeightedTransformArray();
            var max = Mathf.Min(8, modifiedObjects.Length);
            var custom = new ScalePosition[max];
            for (int i = 0; i < max; i++)
            {
                custom[i] = new ScalePosition(modifiedObjects[i].localScale, modifiedObjects[i].localPosition);
                bones.Add(new WeightedTransform(modifiedObjects[i], 1));
            }

            rigConstraint.data.bones = bones;
            rigConstraint.data.scaleData = custom;
        }

        void ApplyPreview()
        {
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
