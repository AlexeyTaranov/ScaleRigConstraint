using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace ScalableRig
{
    public struct ScalableRigConstraintJob : IWeightedAnimationJob
    {
        public ReadOnlyTransformHandle[] Read;
        public ReadWriteTransformHandle[] Write;
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
            var arrayLength = data.TransferData.Length;
            var readScales = new ReadOnlyTransformHandle[arrayLength];
            var writeScales = new ReadWriteTransformHandle[arrayLength];
            for (int i = 0; i < data.TransferData.Length; i++)
            {
                var pair = data.TransferData[i];
                readScales[i] = ReadOnlyTransformHandle.Bind(animator, pair.Read);
                writeScales[i] = ReadWriteTransformHandle.Bind(animator, pair.Write);
            }

            var job = new ScalableRigConstraintJob
            {
                Read = readScales,
                Write = writeScales,
                jobWeight = FloatProperty.Bind(animator, component, "m_Weight"),
            };
            return job;
        }

        public override void Destroy(ScalableRigConstraintJob job)
        {
        }
    }
}
