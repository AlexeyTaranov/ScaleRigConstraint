using System.Collections.Generic;
using ScalableRig;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScalableRigConstraint))]
public class ScalableRigConstraintEditor : Editor
{
    private State _state = State.Default;

    private Dictionary<Transform, (Vector3 pos, Vector3 scale, Quaternion rot)> _localDefaultPositions =
        new Dictionary<Transform, (Vector3 pos, Vector3 scale, Quaternion rot)>();

    private enum State
    {
        Default,
        ModifySkeletonToApply,
        Preview
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
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
                if (GUILayout.Button("Apply"))
                {
                    var offsets = GetOffsetsFromDefaultPositions();
                    RestoreLocalTranslations();
                    GenerateNewGosWithOffsetsAndApplyToConstraints(offsets);
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

        void RestoreLocalTranslations()
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

        void GenerateNewGosWithOffsetsAndApplyToConstraints(IEnumerable<(Transform target, Vector3 pos, Vector3 scale)> localOffsets)
        {
            var constraintData = new List<TransferTransform>();
            for (int i = constraint.transform.childCount - 1; i >= 0; i--)
            {
                var child = constraint.transform.GetChild(i);
                DestroyImmediate(child.gameObject);
            }
            foreach (var (transform, localPos, localScale) in localOffsets)
            {
                var newParent = new GameObject($"{transform.name}_Parent");
                newParent.transform.position = transform.position;
                newParent.transform.SetParent(constraint.transform, true);

                var newTarget = new GameObject($"{transform.name}_Target");
                newTarget.transform.position = transform.position;
                newTarget.transform.SetParent(newParent.transform);
                newTarget.transform.localScale = localScale;
                newTarget.transform.localPosition = localPos;
                constraintData.Add(new TransferTransform()
                {
                    Read = newTarget.transform,
                    Write = transform
                });
            }
            constraint.data.TransferData = constraintData.ToArray();
        }

        void ApplyPreview()
        {
            foreach (var transferTransform in constraint.data.TransferData)
            {
                var read = transferTransform.Read;
                var write = transferTransform.Write;
                write.localPosition = read.localPosition;
                write.localScale = read.localScale;
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
