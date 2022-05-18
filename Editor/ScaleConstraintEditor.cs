using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations.Rigging;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Assertions;

namespace ScaleConstraintAnimation
{
    [CustomEditor(typeof(ScaleConstraint))]
    public class ScaleConstraintEditor : Editor
    {
        private const float MinOffset = 0.1f;

        private SerializedProperty weightProperty;
        private ReorderableList reorderableList;
        private ScaleConstraint constraint;

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
            constraint = (ScaleConstraint)serializedObject.targetObject;
            var data = serializedObject.FindProperty("m_Data");
            var readData = data.FindPropertyRelative(nameof(ScaleConstraint.data.bones));
            var readFieldInfo = constraint.data.GetType().GetField(nameof(ScaleConstraint.data.bones));
            var range = readFieldInfo.GetCustomAttribute<RangeAttribute>();
            reorderableList = WeightedTransformHelper.CreateReorderableList(readData, ref constraint.data.bones, range);
            reorderableList.displayAdd = false;
            reorderableList.displayRemove = false;
            reorderableList.draggable = false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(weightProperty);
            reorderableList.DoLayoutList();
            reorderableList.list = constraint.data.bones;
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
            var animator = constraint.transform.GetComponentInParent<Animator>();
            Assert.IsNotNull(animator,
                $"[{nameof(ScaleConstraint)}] Can't find animator in parent for constraint: {constraint.name}");
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
            return defaultTransforms != null ? DrawUI : () => true;

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

            constraint.data.bones = bones;
            constraint.data.scaleData = custom;
        }

        void ApplyPreview()
        {
            var bones = constraint.data.bones;
            var customData = constraint.data.scaleData;
            for (int i = 0; i < bones.Count; i++)
            {
                var bone = constraint.data.bones[i];
                bone.transform.localPosition = customData[i].Position;
                bone.transform.localScale = customData[i].Scale;
            }
        }
    }
}
