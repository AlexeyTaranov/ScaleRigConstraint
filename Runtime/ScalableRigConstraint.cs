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
        public TransferTransform[] TransferData;

        public bool IsValid() => true;

        public void SetDefaultValues()
        {
        }
    }

    [Serializable]
    public struct TransferTransform
    {
        public Transform Read;
        public Transform Write;
    }
}
