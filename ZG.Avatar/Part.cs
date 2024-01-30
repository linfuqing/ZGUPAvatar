using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ZG.Avatar
{
    public class Part : MonoBehaviour
    {
        private struct SkinWrapper : ISkinWrapper
        {
            private SkinnedMeshRenderer __skinnedMeshRenderer;

            public Transform rootBone
            {
                get => __skinnedMeshRenderer.rootBone;

                set => __skinnedMeshRenderer.rootBone = value;
            }

            public Transform[] bones
            {
                get => __skinnedMeshRenderer.bones;

                set => __skinnedMeshRenderer.bones = value;
            }
            
            public SkinWrapper(SkinnedMeshRenderer skinnedMeshRenderer)
            {
                __skinnedMeshRenderer = skinnedMeshRenderer;
            }
        }

        public string avatarName;
        public string avatarLabel;

        public int level;

        public bool isBuild = true;

        public virtual ISkinWrapper GetSkinWrapper(GameObject gameObject)
        {
            var skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer == null)
                return null;

            return new SkinWrapper(skinnedMeshRenderer);
        }

#if UNITY_EDITOR
        public virtual void Init(
            string folder, 
            Dictionary<string, UnityEngine.Object> assets, 
            Action<string> handler)
        {
            var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                skinnedMeshRenderer.rootBone = null;
                skinnedMeshRenderer.bones = null;

                skinnedMeshRenderer.sharedMesh = (Mesh)PartUtility.Clone(skinnedMeshRenderer.sharedMesh, assets, folder, avatarLabel, handler);
            }

            var meshFilters = GetComponentsInChildren<MeshFilter>();
            if (meshFilters != null)
            {
                foreach (MeshFilter meshFilter in meshFilters)
                {
                    if (meshFilter != null)
                        meshFilter.mesh = (Mesh)PartUtility.Clone(meshFilter.sharedMesh, assets, folder, avatarLabel, handler);
                }
            }
        }
#endif
    }

#if UNITY_EDITOR
    public static class PartUtility
    {
        
        public static UnityEngine.Object Clone(
            UnityEngine.Object source, 
            Dictionary<string, UnityEngine.Object> map, 
            string folder, 
            string label, 
            Action<string> handler)
        {
            if (source == null)
                return null;

            string path = AssetDatabase.GetAssetPath(source) + '/' + source.name;
            if (map == null || !map.TryGetValue(path, out var destination) || destination == null)
            {
                destination = UnityEngine.Object.Instantiate(source);

                try
                {
                    string fileName = folder + '/' + path;
                    CreateFolder(Path.GetDirectoryName(fileName));
                    fileName += ".asset";

                    if (handler != null)
                        handler(fileName);

                    AssetDatabase.CreateAsset(destination, fileName);

                    AssetImporter assetImporter = AssetImporter.GetAtPath(fileName);
                    if (assetImporter != null)
                        assetImporter.SetAssetBundleNameAndVariant(label, string.Empty);
                }
                catch (System.Exception exception)
                {
                    if (exception != null)
                        Debug.LogError(exception.Message);
                }

                if (map != null)
                    map[path] = destination;
            }

            return destination;
        }

        public static void CreateFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string directoryName = Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(directoryName))
                CreateFolder(directoryName);

            AssetDatabase.CreateFolder(directoryName, Path.GetFileName(path));
        }
    }
#endif

}