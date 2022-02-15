using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace ScalableRig
{
    public struct ScalableRigConstraintJob : IWeightedAnimationJob
    {
        public NativeArray<ReadOnlyTransformHandle> read;
        public NativeArray<ReadWriteTransformHandle> write;

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
            for (int i = 0; i < read.Length; i++)
            {
                var readHandle = read[i];
                var writeHandle = write[i];
                var weight = weightBuffers[i] * weightAll;
                var aScale = defaultLocalScales[i];
                var bScale = readHandle.GetLocalScale(stream);
                var scale = Vector3.Lerp(aScale, bScale, weight);
                writeHandle.SetLocalScale(stream, scale);

                var aPos = defaultLocalPositions[i];
                var bPos = readHandle.GetLocalPosition(stream);
                var lPos = Vector3.Lerp(aPos, bPos, weight);
                writeHandle.SetLocalPosition(stream, lPos);
            }
        }
    }

    public class ScalableRigConstraintBinder :
        AnimationJobBinder<ScalableRigConstraintJob, ScalableRigConstraintJobData>
    {
        public override ScalableRigConstraintJob Create(Animator animator, ref ScalableRigConstraintJobData data,
            Component component)
        {
            WeightedTransformArrayBinder.BindReadOnlyTransforms(animator, component, data.ReadData,
                out var readTransforms);
            WeightedTransformArrayBinder.BindReadWriteTransforms(animator, component, data.WriteData,
                out var writeTransforms);
            WeightedTransformArrayBinder.BindWeights(animator, component, data.ReadData,
                PropertyUtils.ConstructConstraintDataPropertyName(nameof(ScalableRigConstraintJobData.ReadData)),
                out var readWeights);
            var defaultLocalScales = new NativeArray<Vector3>(data.WriteData.Count, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            var defaultLocalPositions = new NativeArray<Vector3>(data.WriteData.Count, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < data.WriteData.Count; i++)
            {
                var transform = data.WriteData[i].transform;
                defaultLocalScales[i] = transform.localScale;
                defaultLocalPositions[i] = transform.localPosition;
            }

            var job = new ScalableRigConstraintJob
            {
                read = readTransforms,
                write = writeTransforms,
                readWeightHandles = readWeights,
                weightBuffers = new NativeArray<float>(data.ReadData.Count,Allocator.Persistent,NativeArrayOptions.UninitializedMemory),
                jobWeight = FloatProperty.Bind(animator, component, ConstraintProperties.s_Weight),
                defaultLocalPositions = defaultLocalPositions,
                defaultLocalScales = defaultLocalScales
            };
            return job;
        }


        public override void Destroy(ScalableRigConstraintJob job)
        {
            job.read.Dispose();
            job.write.Dispose();
            job.weightBuffers.Dispose();
            job.readWeightHandles.Dispose();
            job.defaultLocalPositions.Dispose();
            job.defaultLocalScales.Dispose();
        }
    }
}
