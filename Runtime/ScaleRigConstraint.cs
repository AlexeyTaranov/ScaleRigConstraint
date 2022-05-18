using System;
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
        [SyncSceneToStream, WeightRange(0f, 1f)] public WeightedTransformArray bones;

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
