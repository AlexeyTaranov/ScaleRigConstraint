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
    }

    [Serializable]
    public struct ScalableRigConstraintJobData : IAnimationJobData
    {
        [SyncSceneToStream] public WeightedTransformArray ReadData;
        public WeightedTransformArray WriteData;

        public bool IsValid() => true;

        public void SetDefaultValues()
        {
        }
    }
}
