using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace ScaleRigConstraintAnimation
{
    public struct ScaleRigConstraintJob : IWeightedAnimationJob
    {
        public NativeArray<ReadWriteTransformHandle> bones;

        public NativeArray<TransformData> customTransforms;
        public NativeArray<TransformData> defaultTransforms;
        
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
            for (int i = 0; i < customTransforms.Length; i++)
            {
                var aScale = defaultTransforms[i].Scale;
                var bScale = customTransforms[i].Scale;
                var lScale = Vector3.Lerp(aScale, bScale, weight);
                bones[i].SetLocalScale(stream, lScale);

                var offset = customTransforms[i].Position - defaultTransforms[i].Position;
                var aPos = bones[i].GetLocalPosition(stream);
                var bPos = aPos + offset;
                var lPos = Vector3.Lerp(aPos, bPos, weight);
                bones[i].SetLocalPosition(stream, lPos);
            }
        }
    }

    public class ScaleRigConstraintBinder :
        AnimationJobBinder<ScaleRigConstraintJob, ScaleRigConstraintJobData>
    {
        public override ScaleRigConstraintJob Create(Animator animator, ref ScaleRigConstraintJobData data,
            Component component)
        {
            var count = data.ScaleData.Count;
            var bones = new NativeArray<ReadWriteTransformHandle>(count, Allocator.Persistent);
            var defaultTransforms = new NativeArray<TransformData>(count, Allocator.Persistent);
            var customTransforms = new NativeArray<TransformData>(data.ScaleData.Count, Allocator.Persistent);
            for (int i = 0; i < data.ScaleData.Count; i++)
            {
                var transform = data.ScaleData[i].Transform;
                defaultTransforms[i] = new TransformData(transform.localScale, transform.localPosition);
                customTransforms[i] = data.ScaleData[i].ToTransformData();
                bones[i] = ReadWriteTransformHandle.Bind(animator, transform);
            }

            var job = new ScaleRigConstraintJob
            {
                bones = bones,
                customTransforms = customTransforms,
                jobWeight = FloatProperty.Bind(animator, component, ScaleRigConstraint.WeightPropertyName),
                defaultTransforms = defaultTransforms,
            };
            return job;
        }


        public override void Destroy(ScaleRigConstraintJob job)
        {
            job.customTransforms.Dispose();
            job.defaultTransforms.Dispose();
            job.bones.Dispose();
        }
    }
}