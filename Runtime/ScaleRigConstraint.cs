using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace ScaleRigConstraintAnimation
{
    public class ScaleRigConstraint : RigConstraint<
        ScaleRigConstraintJob,
        ScaleRigConstraintJobData,
        ScaleRigConstraintBinder>
    {
        internal static string WeightPropertyName => nameof(m_Weight);
    }

    [Serializable]
    public struct ScaleRigConstraintJobData : IAnimationJobData
    {
        [SerializeField] private TransformDataSerialize[] scaleData;

        public IReadOnlyList<TransformDataSerialize> ScaleData => scaleData;

        public bool IsValid()
        {
            return scaleData.Any(t => t.Transform == null) == false;
        }

        public void SetDefaultValues()
        {
        }

        public void SetScaleData(IEnumerable<TransformDataSerialize> localScaleData)
        {
            scaleData = localScaleData.ToArray();
        }
    }

    [Serializable]
    public class TransformDataSerialize
    {
        [SerializeField] private Vector3 scale;
        [SerializeField] private Vector3 position;
        [SerializeField] private Transform transform;

        public Transform Transform => transform;

        public TransformDataSerialize(Transform transform, Vector3 scale, Vector3 position)
        {
            this.scale = scale;
            this.position = position;
            this.transform = transform;
        }

        public TransformDataSerialize(Transform transform)
        {
            scale = transform.localScale;
            position = transform.localPosition;
            this.transform = transform;
        }

        public TransformData ToTransformData()
        {
            return new TransformData(scale, position);
        }
    }

    public struct TransformData
    {
        public Vector3 Position { get; }
        public Vector3 Scale { get; }

        public TransformData(Vector3 scale, Vector3 position)
        {
            Scale = scale;
            Position = position;
        }
    }
}