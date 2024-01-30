using UnityEngine;
using UnityEngine.U2D.Animation;

namespace ZG
{
    public static class SpriteSkinUtility
    {
        public static void SetRootBone(this SpriteSkin spriteSkin, Transform rootBone)
        {
            spriteSkin.rootBone = rootBone;
        }
        
        public static void SetBoneTransforms(this SpriteSkin spriteSkin, Transform[] transforms)
        {
            spriteSkin.boneTransforms = transforms;
        }
    }
}
