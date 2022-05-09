using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations.Rigging;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace ScaleConstraintAnimation
{
    [CustomEditor(typeof(ScaleConstraint))]
    public class ScaleConstraintEditor : Editor
    {
        const float MinOffset = 0.1f;
        private SerializedProperty _weightProperty;
        private ReorderableList _reorderableList;
        private ScaleConstraint _constraint;

        private Func<bool> _isCompleteModification;

        private struct TransformValue
        {
            public Vector3 Position;
            public Vector3 Scale;
            public Quaternion Rotation;
        }

        public void OnEnable()
        {
            _weightProperty = serializedObject.FindProperty("m_Weight");
            _constraint = (ScaleConstraint)serializedObject.targetObject;
            var data = serializedObject.FindProperty("m_Data");
            var readData = data.FindPropertyRelative(nameof(ScaleConstraint.data.Bones));
            var readFieldInfo = _constraint.data.GetType().GetField(nameof(ScaleConstraint.data.Bones));
            var range = readFieldInfo.GetCustomAttribute<RangeAttribute>();
            _reorderableList =
                WeightedTransformHelper.CreateReorderableList(readData, ref _constraint.data.Bones, range);
            _reorderableList.displayAdd = false;
            _reorderableList.displayRemove = false;
            _reorderableList.draggable = false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_weightProperty);
            _reorderableList.DoLayoutList();
            _reorderableList.list = _constraint.data.Bones;
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

            if (_isCompleteModification?.Invoke() == true)
            {
                _isCompleteModification = null;
            }

            if (_isCompleteModification == null)
            {
                if (GUILayout.Button("Start Modify"))
                {
                    _isCompleteModification = GenerateNewConstraintData();
                }

                if (GUILayout.Button("Preview"))
                {
                    _isCompleteModification = ShowPreview();
                }
            }
        }

        Dictionary<Transform, TransformValue> GetDefaultTransformValues()
        {
            var defaultPositions = new Dictionary<Transform, TransformValue>();
            var animator = _constraint.transform.GetComponentInParent<Animator>();
            if (animator == null)
            {
                Debug.LogError(
                    $"[{nameof(ScaleConstraint)}] Can't find animator in parent for constraint: {_constraint.name}",
                    _constraint);
                return defaultPositions;
            }

            var bones = animator.GetComponentsInChildren<Transform>();
            foreach (var bone in bones)
            {
                defaultPositions.Add(bone, new TransformValue
                {
                    Position = bone.localPosition,
                    Scale = bone.localScale,
                    Rotation = bone.localRotation
                });
            }

            return defaultPositions;
        }

        private Func<bool> GenerateNewConstraintData()
        {
            var defaultTransforms = GetDefaultTransformValues();
            return DrawUI;

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
            bool isChangedValues = Vector3.Distance(defaultValue.Position, transform.localPosition) > MinOffset ||
                                   Vector3.Distance(defaultValue.Scale, transform.localScale) > MinOffset;
            return isChangedValues;
        }

        private void RestoreLocalTranslations(ref Dictionary<Transform, TransformValue> defaultValues)
        {
            foreach (var tuple in defaultValues)
            {
                var inputT = tuple.Key;
                inputT.localPosition = tuple.Value.Position;
                inputT.localScale = tuple.Value.Scale;
                inputT.localRotation = tuple.Value.Rotation;
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

            _constraint.data.Bones = bones;
            _constraint.data.ScaleData = custom;
        }

        void ApplyPreview()
        {
            var bones = _constraint.data.Bones;
            var customData = _constraint.data.ScaleData;
            for (int i = 0; i < bones.Count; i++)
            {
                var bone = _constraint.data.Bones[i];
                bone.transform.localPosition = customData[i].Position;
                bone.transform.localScale = customData[i].Scale;
            }
        }
    }
}