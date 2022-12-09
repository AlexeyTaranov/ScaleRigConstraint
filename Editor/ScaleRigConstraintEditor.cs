using System.Collections.Generic;
using ScaleRigConstraintAnimation.ScaleRigConstraintEditorActions;
using UnityEditor;
using UnityEngine;

namespace ScaleRigConstraintAnimation
{
    public abstract class BaseScaleRigConstraintEditorAction
    {
        protected readonly ScaleRigConstraintEditor ScaleRigEditor;
        protected readonly Dictionary<Transform, TransformValue> DefaultTransformValues;

        protected BaseScaleRigConstraintEditorAction(ScaleRigConstraintEditor scaleRigEditor)
        {
            ScaleRigEditor = scaleRigEditor;
            DefaultTransformValues = scaleRigEditor.GetCurrentTransformValues();
        }

        public abstract bool IsCompleteModification();

        public void RestoreDefaultTransformsWithoutModifications()
        {
            foreach (var pair in DefaultTransformValues)
            {
                pair.Value.ApplyLocalValues(pair.Key);
            }
        }
    }

    public struct TransformValue
    {
        private const float MinOffset = 0.1f;

        private Vector3 position;
        private Vector3 scale;
        private Quaternion rotation;

        public static TransformValue GetLocalTransformValue(Transform transform)
        {
            return new TransformValue()
            {
                position = transform.localPosition,
                rotation = transform.localRotation,
                scale = transform.localScale
            };
        }

        public void ApplyLocalValues(Transform transform)
        {
            transform.localPosition = position;
            transform.localScale = scale;
            transform.localRotation = rotation;
        }

        public bool IsHaveLocalTransformOffset(Transform transform)
        {
            bool isChangedValues = Vector3.Distance(position, transform.localPosition) > MinOffset ||
                                   Vector3.Distance(scale, transform.localScale) > MinOffset;
            return isChangedValues;
        }
    }

    [CustomEditor(typeof(ScaleRigConstraint))]
    public class ScaleRigConstraintEditor : Editor
    {
        private SerializedProperty weightProperty;
        private SerializedProperty bonesProperty;

        private Animator animator;

        private BaseScaleRigConstraintEditorAction currentUpdateRigAction;

        public ScaleRigConstraint rigConstraint;

        public void OnEnable()
        {
            weightProperty = serializedObject.FindProperty("m_Weight");
            rigConstraint = (ScaleRigConstraint)serializedObject.targetObject;
            var data = serializedObject.FindProperty("m_Data");
            bonesProperty = data.FindPropertyRelative(nameof(ScaleRigConstraint.data.bones));
            animator = rigConstraint.gameObject.GetComponentInParent<Animator>();
        }

        public void OnDisable()
        {
            currentUpdateRigAction?.RestoreDefaultTransformsWithoutModifications();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(weightProperty);
            EditorGUILayout.PropertyField(bonesProperty);
            DrawButtonsAndExecuteModifications();
            serializedObject.ApplyModifiedProperties();
        }


        private void DrawButtonsAndExecuteModifications()
        {
            if (Application.isPlaying)
            {
                GUILayout.Label("To apply modify data need stop play mode");
                return;
            }

            if (animator == null)
            {
                animator = rigConstraint.gameObject.GetComponentInParent<Animator>();
            }

            if (animator == null)
            {
                GUILayout.Label("Can't find animator!");
            }

            var isUpdatedAction = currentUpdateRigAction?.IsCompleteModification() == true;

            if (isUpdatedAction)
            {
                currentUpdateRigAction.RestoreDefaultTransformsWithoutModifications();
                currentUpdateRigAction = null;
            }

            if (currentUpdateRigAction == null)
            {
                if (GUILayout.Button("Start Modify"))
                {
                    currentUpdateRigAction = new GenerateNewConstraintDataAction(this);
                }

                if (GUILayout.Button("Preview"))
                {
                    currentUpdateRigAction = new ShowPreviewAction(this);
                }
            }
        }

        public Dictionary<Transform, TransformValue> GetCurrentTransformValues()
        {
            var defaultPositions = new Dictionary<Transform, TransformValue>();
            var bones = animator.GetComponentsInChildren<Transform>();
            foreach (var bone in bones)
            {
                defaultPositions.Add(bone, TransformValue.GetLocalTransformValue(bone));
            }

            return defaultPositions;
        }
    }
}
