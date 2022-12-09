using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace ScaleRigConstraintAnimation
{
    public class ScaleRigConstraint : RigConstraint<
        ScaleRigConstraintJob,
        ScaleRigConstraintJobData,
        ScaleRigConstraintBinder>
    {
        internal static string WeightPropertyName => nameof(m_Weight);
    }

    [Serializable]
    public struct ScaleRigConstraintJobData : IAnimationJobData
    {
        public ScalePosition[] scaleData;

        [SyncSceneToStream, WeightRange(0f, 1f)]
        public WeightedTransformArray bones;

        public bool IsValid()
        {
            if (scaleData.Length != bones.Count)
            {
                return false;
            }

            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i].transform == null)
                {
                    return false;
                }
            }

            return true;
        }

        public void SetDefaultValues()
        {
        }

        public void SetScaleData(IReadOnlyList<(ScalePosition scaleData, Transform transform)> localScaleData)
        {
            var weightBones = new WeightedTransformArray();
            var max = Mathf.Min(WeightedTransformArray.k_MaxLength, localScaleData.Count);
            for (int i = 0; i < max; i++)
            {
                weightBones.Add(new WeightedTransform(localScaleData[i].transform, 1));
            }

            bones = weightBones;
            scaleData = localScaleData.Select(t => t.scaleData).Take(max).ToArray();
        }
    }

    [Serializable]
    public struct ScalePosition
    {
        [SerializeField] private Vector3 scale;
        [SerializeField] private Vector3 position;

        public Vector3 Scale => scale;
        public Vector3 Position => position;


        public ScalePosition(Vector3 scale, Vector3 position)
        {
            this.scale = scale;
            this.position = position;
        }

        public ScalePosition(Transform transform)
        {
            scale = transform.localScale;
            position = transform.localPosition;
        }
    }
}
