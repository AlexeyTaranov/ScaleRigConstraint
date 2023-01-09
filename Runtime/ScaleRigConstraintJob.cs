using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace ScaleRigConstraintAnimation
{
    public struct ScaleRigConstraintJob : IWeightedAnimationJob
    {
        public NativeArray<ScalePosition> scalePositions;
        public NativeArray<ReadWriteTransformHandle> bones;

        public NativeArray<PropertyStreamHandle> readWeightHandles;
        public NativeArray<float> weightBuffers;
        public NativeArray<Vector3> defaultLocalScales;
        public NativeArray<Vector3> defaultLocalPositions;
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
            AnimationStreamHandleUtility.ReadFloats(stream, readWeightHandles, weightBuffers);
            var weightAll = jobWeight.Get(stream);
            for (int i = 0; i < scalePositions.Length; i++)
            {
                var readHandle = scalePositions[i];
                var boneHandle = bones[i];
                var weight = weightBuffers[i] * weightAll;
                var aScale = defaultLocalScales[i];
                var bScale = readHandle.Scale;
                var scale = Vector3.Lerp(aScale, bScale, weight);
                boneHandle.SetLocalScale(stream, scale);

                var offset = readHandle.Position - defaultLocalPositions[i];
                var aPos = boneHandle.GetLocalPosition(stream);
                var bPos = aPos + offset;
                var lPos = Vector3.Lerp(aPos, bPos, weight);
                boneHandle.SetLocalPosition(stream, lPos);
            }
        }
    }

    public class ScaleRigConstraintBinder :
        AnimationJobBinder<ScaleRigConstraintJob, ScaleRigConstraintJobData>
    {
        public override ScaleRigConstraintJob Create(Animator animator, ref ScaleRigConstraintJobData data,
            Component component)
        {
            WeightedTransformArrayBinder.BindWeights(animator, component, data.bones,
                ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(ScaleRigConstraintJobData.bones)),
                out var readWeights);
            WeightedTransformArrayBinder.BindReadWriteTransforms(animator, component, data.bones,
                out var boneTransforms);
            var defaultLocalScales = new NativeArray<Vector3>(data.scaleData.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            var defaultLocalPositions = new NativeArray<Vector3>(data.scaleData.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            var customScalesAndPositions = new NativeArray<ScalePosition>(data.scaleData.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < data.bones.Count; i++)
            {
                var transform = data.bones[i].transform;
                defaultLocalScales[i] = transform.localScale;
                defaultLocalPositions[i] = transform.localPosition;
                customScalesAndPositions[i] = data.scaleData[i];
            }

            var job = new ScaleRigConstraintJob
            {
                bones = boneTransforms,
                readWeightHandles = readWeights,
                scalePositions = customScalesAndPositions,
                weightBuffers = new NativeArray<float>(data.bones.Count, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory),
                jobWeight = FloatProperty.Bind(animator, component, ScaleRigConstraint.WeightPropertyName),
                defaultLocalPositions = defaultLocalPositions,
                defaultLocalScales = defaultLocalScales
            };
            return job;
        }


        public override void Destroy(ScaleRigConstraintJob job)
        {
            job.scalePositions.Dispose();
            job.bones.Dispose();
            job.weightBuffers.Dispose();
            job.readWeightHandles.Dispose();
            job.defaultLocalPositions.Dispose();
            job.defaultLocalScales.Dispose();
        }
    }
}
