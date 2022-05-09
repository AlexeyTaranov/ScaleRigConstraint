using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace ScalableRig
{
    public class ScalableRigConstraint : RigConstraint<
        ScalableRigConstraintJob,
        ScalableRigConstraintJobData,
        ScalableRigConstraintBinder>
    {
        internal static string WeightPropertyName => nameof(m_Weight);
    }

    [Serializable]
    public struct ScalableRigConstraintJobData : IAnimationJobData
    {
        public ScalePosition[] ScaleData;
        [SyncSceneToStream, Range(0, 1)] public WeightedTransformArray Bones;

        public bool IsValid() => true;

        public void SetDefaultValues()
        {
        }
    }

    [Serializable]
    public struct ScalePosition
    {
        [SerializeField] private Vector3 _scale;
        [SerializeField] private Vector3 _position;

        public Vector3 Scale => _scale;
        public Vector3 Position => _position;


        public ScalePosition(Vector3 scale, Vector3 position)
        {
            _scale = scale;
            _position = position;
        }
    }
}