#if USING_AVATAR_D2
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace ZG.Avatar
{
    public class SkinWrapper2D : MonoBehaviour, ISkinWrapper
    {
        private SpriteSkin __spriteSkin;

        public SpriteSkin spriteSkin
        {
            get
            {
                if (__spriteSkin == null)
                    __spriteSkin = GetComponent<SpriteSkin>();

                return __spriteSkin;
            }
        }

        public Transform rootBone
        {
            get => spriteSkin.rootBone;

            set => spriteSkin.SetRootBone(value);
        }

        public Transform[] bones
        {
            get => spriteSkin.boneTransforms;

            set => spriteSkin.SetBoneTransforms(value);
        }
    }
}
#endif