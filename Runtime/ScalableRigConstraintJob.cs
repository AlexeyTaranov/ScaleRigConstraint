using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace ScalableRig
{
    public struct ScalableRigConstraintJob : IWeightedAnimationJob
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

                var aPos = defaultLocalPositions[i];
                var bPos = readHandle.Position;
                var lPos = Vector3.Lerp(aPos, bPos, weight);
                boneHandle.SetLocalPosition(stream, lPos);
            }
        }
    }

    public class ScalableRigConstraintBinder :
        AnimationJobBinder<ScalableRigConstraintJob, ScalableRigConstraintJobData>
    {
        public override ScalableRigConstraintJob Create(Animator animator, ref ScalableRigConstraintJobData data,
            Component component)
        {
            WeightedTransformArrayBinder.BindWeights(animator, component, data.Bones,
                ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(ScalableRigConstraintJobData.Bones)),
                out var readWeights);
            WeightedTransformArrayBinder.BindReadWriteTransforms(animator, component, data.Bones,
                out var boneTransforms);
            var defaultLocalScales = new NativeArray<Vector3>(data.ScaleData.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            var defaultLocalPositions = new NativeArray<Vector3>(data.ScaleData.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            var customScalesAndPositions = new NativeArray<ScalePosition>(data.ScaleData.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < data.Bones.Count; i++)
            {
                var transform = data.Bones[i].transform;
                defaultLocalScales[i] = transform.localScale;
                defaultLocalPositions[i] = transform.localPosition;
                customScalesAndPositions[i] = data.ScaleData[i];
            }

            var job = new ScalableRigConstraintJob
            {
                bones = boneTransforms,
                readWeightHandles = readWeights,
                scalePositions = customScalesAndPositions,
                weightBuffers = new NativeArray<float>(data.Bones.Count,Allocator.Persistent,NativeArrayOptions.UninitializedMemory),
                jobWeight = FloatProperty.Bind(animator, component, ScalableRigConstraint.WeightPropertyName),
                defaultLocalPositions = defaultLocalPositions,
                defaultLocalScales = defaultLocalScales
            };
            return job;
        }


        public override void Destroy(ScalableRigConstraintJob job)
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
