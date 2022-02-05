using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace ScalableRig
{
    public struct ScalableRigConstraintJob : IWeightedAnimationJob
    {
        public ReadOnlyTransformHandle[] ReadScale;
        public ReadWriteTransformHandle[] WriteScale;
        public FloatProperty jobWeight { get; set; }

        public void ProcessAnimation(AnimationStream stream)
        {
            for (int i = 0; i < ReadScale.Length; i++)
            {
                var read = ReadScale[i];
                var write = WriteScale[i];
                write.SetLocalScale(stream, read.GetLocalScale(stream));
            }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }
    }

    public class ScalableRigConstraintBinder :
        AnimationJobBinder<ScalableRigConstraintJob, ScalableRigConstraintJobData>
    {
        public override ScalableRigConstraintJob Create(Animator animator, ref ScalableRigConstraintJobData data,
            Component component)
        {
            var arrayLength = data.Data.Length;
            var readScales = new ReadOnlyTransformHandle[arrayLength];
            var writeScales = new ReadWriteTransformHandle[arrayLength];
            for (int i = 0; i < data.Data.Length; i++)
            {
                var pair = data.Data[i];
                readScales[i] = ReadOnlyTransformHandle.Bind(animator, pair.FromRead);
                writeScales[i] = ReadWriteTransformHandle.Bind(animator, pair.ToWrite);
            }

            var job = new ScalableRigConstraintJob
            {
                ReadScale = readScales,
                WriteScale = writeScales,
                jobWeight = FloatProperty.Bind(animator, component, "m_Weight"),
            };
            return job;
        }

        public override void Destroy(ScalableRigConstraintJob job)
        {
        }
    }
}