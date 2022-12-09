using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Object = UnityEngine.Object;

namespace ScaleRigConstraintAnimation.ScaleRigConstraintEditorActions
{
    public class CopyLocalTransformsAsNewConstraints : BaseScaleRigConstraintEditorAction
    {
        private struct PathTransform
        {
            public string Path;
            public Transform Transform;
        }

        private Animator sourceAnimator;
        private Transform sourceRootBone;

        public CopyLocalTransformsAsNewConstraints(ScaleRigConstraintEditor scaleRigEditor) : base(scaleRigEditor)
        {
        }


        public override bool IsCompleteModification()
        {
            sourceAnimator =
                (Animator)EditorGUILayout.ObjectField("Source Animator", sourceAnimator, typeof(Animator), true);
            sourceRootBone =
                (Transform)EditorGUILayout.ObjectField("Source Root Bone", sourceRootBone, typeof(Transform), true);

            if (sourceAnimator == null || sourceRootBone == null)
            {
                GUILayout.Label("Select source values for copy local transforms");
            }
            else
            {
                if (GUILayout.Button("Copy local transforms"))
                {
                    GenerateNewConstraintData();
                    return true;
                }
            }

            if (GUILayout.Button("Cancel"))
            {
                return true;
            }

            return false;
        }

        private void GenerateNewConstraintData()
        {
            //Generate pairs - bone path and bone transforms
            var sourceBoneRootPath = GenerateUnityHierarchyPathToParent(sourceRootBone, sourceAnimator.transform);

            //Skip non bone objects (for example - rig components is child of animator, but i think - is not bone)
            var thisRig = ScaleRigEditor.rigConstraint;
            var rigAnimator = thisRig.gameObject.GetComponentInParent<Animator>();
            var rigBones = GeneratePathPairsForChildTransforms(rigAnimator.transform)
                .Where(t => t.Path.StartsWith(sourceBoneRootPath));

            var sourceBones = GeneratePathPairsForChildTransforms(sourceAnimator.transform)
                .Where(t => t.Path.StartsWith(sourceBoneRootPath));

            //Select all bones with any local modification
            var sourceAndRigTransforms = GetTransformByEqualHierarchyLocalPath(sourceBones, rigBones);

            var updatedTransforms = sourceAndRigTransforms.Where(t =>
                TransformValue.GetLocalTransformValue(t.rig).IsHaveLocalTransformOffset(t.source));

            var customScaleDatas = updatedTransforms.Select(t => (new ScalePosition(t.source), t.rig)).ToArray();

            //Cleanup old rig data
            var maxScalesCount = WeightedTransformArray.k_MaxLength;
            if (customScaleDatas.Count() > maxScalesCount)
            {
                var otherRigComponents =
                    thisRig.gameObject.GetComponents<ScaleRigConstraint>().Where(t => t != thisRig);
                foreach (var scaleRigConstraint in otherRigComponents)
                {
                    Object.Destroy(scaleRigConstraint);
                }
            }

            //Save new rig data
            var firstScaleData = customScaleDatas.Take(maxScalesCount).ToList();
            thisRig.data.SetScaleData(firstScaleData);

            //Avoid limits in WeightTransformArray - create multiple scale rig constraints
            var otherScales = customScaleDatas.Skip(maxScalesCount);
            var otherScaleDatasCount = customScaleDatas.Count() - maxScalesCount;
            for (int i = 0; i < otherScaleDatasCount; i += maxScalesCount)
            {
                var otherScaleLimit = otherScales.Skip(i).Take(maxScalesCount);
                var newScaleRig = thisRig.gameObject.AddComponent<ScaleRigConstraint>();
                newScaleRig.data.SetScaleData(otherScaleLimit.ToList());
            }
        }

        private HashSet<PathTransform> GeneratePathPairsForChildTransforms(Transform parent)
        {
            var childTransforms = parent.GetComponentsInChildren<Transform>();
            return childTransforms.Select(t => new PathTransform()
                { Path = GenerateUnityHierarchyPathToParent(t, parent), Transform = t }).ToHashSet();
        }

        private string GenerateUnityHierarchyPathToParent(Transform obj, Transform parent)
        {
            var hierarchy = new List<Transform>();
            var currentObject = obj;
            while (currentObject.parent != null)
            {
                hierarchy.Add(currentObject);
                currentObject = currentObject.parent;
                if (currentObject == parent)
                {
                    break;
                }

                if (currentObject == null)
                {
                    //Object is not child for selected parent!
                    return String.Empty;
                }
            }

            var fromObjectToParent = hierarchy;
            fromObjectToParent.Reverse();
            var fromParentToObject = fromObjectToParent;
            var transformNames = fromParentToObject.Select(t => t.name);
            return string.Join(Path.DirectorySeparatorChar, transformNames);
        }

        private IReadOnlyList<(Transform source, Transform rig)> GetTransformByEqualHierarchyLocalPath(
            IEnumerable<PathTransform> sources, IEnumerable<PathTransform> rigs)
        {
            var rigDict = rigs.ToDictionary(t => t.Path, t => t.Transform);
            var sourceRigList = new List<(Transform src, Transform rig)>();
            foreach (var sourcePair in sources)
            {
                if (rigDict.TryGetValue(sourcePair.Path, out var rigTransform))
                {
                    sourceRigList.Add((sourcePair.Transform, rigTransform));
                }
            }

            return sourceRigList;
        }
    }
}
