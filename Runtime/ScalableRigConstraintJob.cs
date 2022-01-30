using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace ScalableRig
{
    public struct ScalableRigConstraintJob : IWeightedAnimationJob
    {
        public void ProcessAnimation(AnimationStream stream)
        {
            throw new System.NotImplementedException();
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
            throw new System.NotImplementedException();
        }

        public FloatProperty jobWeight { get; set; }
    }

    public class
        ScalableRigConstraintBinder : AnimationJobBinder<ScalableRigConstraintJob, ScalableRigConstraintJobData>
    {
        public override ScalableRigConstraintJob Create(Animator animator, ref ScalableRigConstraintJobData data,
            Component component)
        {
            throw new System.NotImplementedException();
        }

        public override void Destroy(ScalableRigConstraintJob job)
        {
            throw new System.NotImplementedException();
        }
    }
}