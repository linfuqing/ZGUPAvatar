using UnityEngine;

namespace ZG.Avatar
{
    public interface ISkinWrapper
    {
        Transform rootBone { get; set; }

        Transform[] bones { get; set; }
    }

    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public sealed class SkinWrapper : MonoBehaviour, ISkinWrapper
    {
        private SkinnedMeshRenderer __skinnedMeshRenderer;

        public SkinnedMeshRenderer skinnedMeshRenderer
        {
            get
            {
                if (__skinnedMeshRenderer == null)
                    __skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

                return __skinnedMeshRenderer;
            }
        }

        public Transform rootBone
        {
            get => skinnedMeshRenderer.rootBone;

            set => skinnedMeshRenderer.rootBone = value;
        }

        public Transform[] bones
        {
            get => skinnedMeshRenderer.bones;

            set => skinnedMeshRenderer.bones = value;
        }
    }
}