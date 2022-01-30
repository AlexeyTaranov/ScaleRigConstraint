using UnityEngine.Animations.Rigging;

namespace ScalableRig
{
    public class ScalableRigConstraint : RigConstraint<ScalableRigConstraintJob, ScalableRigConstraintJobData,
        ScalableRigConstraintBinder>
    {
    }

    [System.Serializable]
    public struct ScalableRigConstraintJobData : IAnimationJobData
    {
        public bool IsValid()
        {
            throw new System.NotImplementedException();
        }

        public void SetDefaultValues()
        {
            throw new System.NotImplementedException();
        }
    }
}