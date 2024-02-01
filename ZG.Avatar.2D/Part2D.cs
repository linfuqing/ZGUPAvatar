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
        
        public override ISkinWrapper[] GetSkinWrappers(GameObject gameObject)
        {
            var spriteSkins = gameObject.GetComponentsInChildren<SpriteSkin>();
            int numSpriteSkins = spriteSkins == null ? 0 : spriteSkins.Length;
            if (numSpriteSkins < 1)
                return null;

            var skinWrappers = new ISkinWrapper[numSpriteSkins];
            for (int i = 0; i < numSpriteSkins; ++i)
                skinWrappers[i] = new SkinWrapper(spriteSkins[i]);

            return skinWrappers;
        }
        
#if UNITY_EDITOR
        public override void Init(
            string folder, 
            Dictionary<string, UnityEngine.Object> assets, 
            Action<string> handler)
        {
            var spriteSkins = gameObject.GetComponentsInChildren<SpriteSkin>();
            if (spriteSkins != null)
            {
                foreach (var spriteSkin in spriteSkins)
                    spriteSkin.gameObject.AddComponent<SkinWrapper2D>();
            }
        }
#endif
    }
}