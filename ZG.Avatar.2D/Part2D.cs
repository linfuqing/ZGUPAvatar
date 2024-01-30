using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace ZG.Avatar
{
    public class Part2D : Part
    {
        private struct SkinWrapper : ISkinWrapper
        {
            private SpriteSkin __spriteSkin;

            public Transform rootBone
            {
                get => __spriteSkin.rootBone;

                set => __spriteSkin.SetRootBone(value);
            }

            public Transform[] bones
            {
                get => __spriteSkin.boneTransforms;

                set => __spriteSkin.SetBoneTransforms(value);
            }
            
            public SkinWrapper(SpriteSkin spriteSkin)
            {
                __spriteSkin = spriteSkin;
            }
        }
        
        public override ISkinWrapper GetSkinWrapper(GameObject gameObject)
        {
            var spriteSkin = gameObject.GetComponent<SpriteSkin>();
            if (spriteSkin == null)
                return null;

            return new SkinWrapper(spriteSkin);
        }
        
#if UNITY_EDITOR
        public override void Init(
            string folder, 
            Dictionary<string, UnityEngine.Object> assets, 
            Action<string> handler)
        {
            gameObject.AddComponent<SkinWrapper2D>();
        }
#endif
    }
}