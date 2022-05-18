using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace ScaleConstraintAnimation
{
    public class ScaleConstraint : RigConstraint<
        ScaleConstraintJob,
        ScaleConstraintJobData,
        ScalableRigConstraintBinder>
    {
        internal static string WeightPropertyName => nameof(m_Weight);
    }

    [Serializable]
    public struct ScaleConstraintJobData : IAnimationJobData
    {
        public ScalePosition[] scaleData;
        [SyncSceneToStream, Range(0, 1)] public WeightedTransformArray bones;

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
    }
}
