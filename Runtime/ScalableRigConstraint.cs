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
        [SyncSceneToStream,Range(0, 1)] public WeightedTransformArray ReadData;
        public WeightedTransformArray WriteData;

        public bool IsValid() => true;

        public void SetDefaultValues()
        {
        }
    }
}
