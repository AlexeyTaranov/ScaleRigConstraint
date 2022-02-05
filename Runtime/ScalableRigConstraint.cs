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
        private void OnValidate()
        {
            if (m_Data.Data.Length > 8)
            {
                Debug.LogError($"[{nameof(ScalableRigConstraint)}] Support blend only maximum 8 bones");
                var arrayWithLimit = new TransferTransformOffsetLocalScale[8];
                Array.Copy(m_Data.Data, arrayWithLimit, 8);
                m_Data.Data = arrayWithLimit;
            }
        }
    }

    [Serializable]
    public struct ScalableRigConstraintJobData : IAnimationJobData
    {
        public TransferTransformOffsetLocalScale[] Data;

        public bool IsValid() => true;

        public void SetDefaultValues()
        {
        }
    }

    [Serializable]
    public struct TransferTransformOffsetLocalScale
    {
        public Transform ToWrite;
        public Transform FromRead;
    }
}