using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace ScalableRig
{
    public struct ScalableRigConstraintJob : IWeightedAnimationJob
    {
        public NativeArray<ReadOnlyTransformHandle> Read;
        public NativeArray<ReadWriteTransformHandle> Write;
        public FloatProperty jobWeight { get; set; }

        public void ProcessAnimation(AnimationStream stream)
        {
            Execute(stream);
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        private void Execute(AnimationStream stream)
        {
            var weight = jobWeight.Get(stream);
            for (int i = 0; i < Read.Length; i++)
            {
                var read = Read[i];
                var write = Write[i];
                var aScale = write.GetLocalScale(stream);
                var bScale = read.GetLocalScale(stream);
                var scale = Vector3.Lerp(aScale, bScale, weight);
                write.SetLocalScale(stream, scale);

                var aPos = read.GetLocalPosition(stream);
                var bPos = write.GetLocalPosition(stream);
                var lPos = Vector3.Lerp(aPos, bPos, weight);
                write.SetLocalPosition(stream, lPos);
            }
        }
    }

    public class ScalableRigConstraintBinder :
        AnimationJobBinder<ScalableRigConstraintJob, ScalableRigConstraintJobData>
    {
        public override ScalableRigConstraintJob Create(Animator animator, ref ScalableRigConstraintJobData data,
            Component component)
        {
            WeightedTransformArrayBinder.BindReadOnlyTransforms(animator,component,data.ReadData,out var readTransforms);
            WeightedTransformArrayBinder.BindReadWriteTransforms(animator, component,data.WriteData,out var writeTransforms);

            var job = new ScalableRigConstraintJob
            {
                Read = readTransforms,
                Write = writeTransforms,
                jobWeight = FloatProperty.Bind(animator, component, "m_Weight"),
            };
            return job;
        }


        public override void Destroy(ScalableRigConstraintJob job)
        {
            job.Read.Dispose();
            job.Write.Dispose();
        }
    }
}
