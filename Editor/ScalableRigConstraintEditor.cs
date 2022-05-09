using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations.Rigging;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace ScalableRig
{
    [CustomEditor(typeof(ScalableRigConstraint))]
    public class ScalableRigConstraintEditor : Editor
    {
        private State _state = State.Default;

        private SerializedProperty _weightProperty;
        private ReorderableList _reorderableList;
        private ScalableRigConstraint _constraint;

        private Dictionary<Transform, (Vector3 pos, Vector3 scale, Quaternion rot)> _localDefaultPositions =
            new Dictionary<Transform, (Vector3 pos, Vector3 scale, Quaternion rot)>();

        private enum State
        {
            Default,
            ModifySkeletonToApply,
            Preview
        }

        public void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += DeleteModifiedSettingsAndSetDefaultState;
            _weightProperty = serializedObject.FindProperty("m_Weight");
            _constraint = (ScalableRigConstraint)serializedObject.targetObject;
            var data = serializedObject.FindProperty("m_Data");
            var readData = data.FindPropertyRelative(nameof(ScalableRigConstraint.data.Bones));
            var readFieldInfo = _constraint.data.GetType().GetField(nameof(ScalableRigConstraint.data.Bones));
            var range = readFieldInfo.GetCustomAttribute<RangeAttribute>();
            _reorderableList = WeightedTransformHelper.CreateReorderableList(readData, ref _constraint.data.Bones, range);
            _reorderableList.displayAdd = false;
            _reorderableList.displayRemove = false;
            _reorderableList.draggable = false;
        }

        public void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= DeleteModifiedSettingsAndSetDefaultState;
        }

        private void DeleteModifiedSettingsAndSetDefaultState()
        {
            _state = State.Default;
            RestoreLocalTranslations();
            _localDefaultPositions.Clear();
        }

        private void RestoreLocalTranslations()
        {
            foreach (var tuple in _localDefaultPositions)
            {
                var inputT = tuple.Key;
                var (pos, scale, rot) = tuple.Value;
                inputT.localPosition = pos;
                inputT.localScale = scale;
                inputT.localRotation = rot;
            }
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

            var constraint = target as ScalableRigConstraint;
            switch (_state)
            {
                case State.Default:
                    if (GUILayout.Button("Start Modify"))
                    {
                        SaveDefaultRigParams();
                        _state = State.ModifySkeletonToApply;
                    }

                    if (GUILayout.Button("Preview"))
                    {
                        SaveDefaultRigParams();
                        ApplyPreview();
                        _state = State.Preview;
                    }

                    break;
                case State.ModifySkeletonToApply:
                    DrawLabelWithModifiedObjectsCount();
                    if (GUILayout.Button("Apply"))
                    {
                        var offsets = GetOffsetsFromDefaultPositions();
                        RestoreLocalTranslations();
                        GenerateNewGosWithOffsetsAndApplyToConstraint(offsets);
                        _state = State.Default;
                    }

                    break;
                case State.Preview:
                    if (GUILayout.Button("Stop Preview"))
                    {
                        RestoreLocalTranslations();
                        _state = State.Default;
                    }

                    break;
            }

            List<(Transform target, Vector3 pos, Vector3 scale)> GetOffsetsFromDefaultPositions()
            {
                var modifiedLocalTranslations = new List<(Transform target, Vector3 pos, Vector3 scale)>();
                foreach (var tuple in _localDefaultPositions)
                {
                    var inputT = tuple.Key;
                    var (pos, scale, rot) = tuple.Value;
                    const float minOffset = 0.1f;
                    bool isChangedValues = Vector3.Distance(pos, inputT.localPosition) > minOffset ||
                                           Vector3.Distance(scale, inputT.localScale) > minOffset;
                    if (isChangedValues)
                    {
                        modifiedLocalTranslations.Add((inputT, pos, inputT.localScale));
                    }
                }

                return modifiedLocalTranslations;
            }

            void DrawLabelWithModifiedObjectsCount()
            {
                var changed = GetModifiedObjectsCount();
                var max = WeightedTransformArray.k_MaxLength;
                var textLabel = $"Changed objects: {changed}, max: {max}.";
                if (changed > max)
                {
                    textLabel += " Will be saved only first 8 changes.";
                    var errorStyle = new GUIStyle()
                        { fontStyle = FontStyle.Bold, normal = new GUIStyleState() { textColor = Color.red } };
                    GUILayout.Label(textLabel, errorStyle);
                }
                else
                {
                    GUILayout.Label(textLabel);
                }
            }

            int GetModifiedObjectsCount()
            {
                int count = 0;
                foreach (var tuple in _localDefaultPositions)
                {
                    var inputT = tuple.Key;
                    var (pos, scale, rot) = tuple.Value;
                    const float minOffset = 0.1f;
                    bool isChangedValues = Vector3.Distance(pos, inputT.localPosition) > minOffset ||
                                           Vector3.Distance(scale, inputT.localScale) > minOffset;
                    if (isChangedValues)
                    {
                        count++;
                    }
                }

                return count;
            }

            void GenerateNewGosWithOffsetsAndApplyToConstraint(
                List<(Transform target, Vector3 pos, Vector3 scale)> localOffsets)
            {
                var bones = new WeightedTransformArray();
                var max = Mathf.Min(8, localOffsets.Count);
                var custom = new ScalePosition[max];
                for (int i = 0; i < max; i++)
                {
                    var (transform, localPos, localScale) = localOffsets[i];
                    custom[i] = new ScalePosition(localScale, localPos);
                    bones.Add(new WeightedTransform(transform, 1));
                }

                constraint.data.Bones = bones;
                constraint.data.ScaleData = custom;
            }

            void ApplyPreview()
            {
                var bones = constraint.data.Bones;
                var customData = constraint.data.ScaleData;
                for (int i = 0; i < bones.Count; i++)
                {
                    var bone = constraint.data.Bones[i];
                    bone.transform.localPosition = customData[i].Position;
                    bone.transform.localScale = customData[i].Scale;
                }
            }

            void SaveDefaultRigParams()
            {
                _localDefaultPositions.Clear();
                var animator = constraint.transform.GetComponentInParent<Animator>();
                if (animator == null)
                {
                    Debug.LogError(
                        $"[{nameof(ScalableRigConstraint)}] Can't find animator in parent for constraint: {constraint.name}",
                        constraint);
                    return;
                }

                var bones = animator.GetComponentsInChildren<Transform>();
                foreach (var bone in bones)
                {
                    _localDefaultPositions.Add(bone, (bone.localPosition, bone.localScale, bone.localRotation));
                }
            }
        }
    }
}
