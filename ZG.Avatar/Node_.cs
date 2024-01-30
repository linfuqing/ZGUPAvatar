using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZG.Avatar
{
    public partial class Node : MonoBehaviour
    {
        private struct MaterialKey : IEquatable<MaterialKey>
        {
            [ColorUsage(true, true)]
            public Color color;
            public string colorParameterName;
            public string textureParameterName;
            public Texture texture;
            public Material material;

            public bool Equals(MaterialKey other)
            {
                return color == other.color && 
                    colorParameterName == other.colorParameterName && 
                    textureParameterName == other.textureParameterName && 
                    texture == other.texture && 
                    material == other.material;
            }

            public override bool Equals(object obj)
            {
                return Equals((MaterialKey)obj);
            }

            public override int GetHashCode()
            {
                return material == null ? texture == null ? color.GetHashCode() : texture.GetHashCode() : material.GetHashCode();
            }
        }

        [Serializable]
        public struct MaterialInstance
        {
#if UNITY_EDITOR
            public string name;
#endif
            public Material source;

            [HideInInspector]
            public Material destination;
        }

        /*public struct Selector : IDisposable
        {
            private Node __node;

            public Selector(Node node)
            {
                __node = node;
            }

            public void Select(string partName) => __node.__Select(partName);

            public void Dispose()
            {
                var animator = __node.animator;
                if (animator != null)
                    animator.Rebind();
            }
        }*/

        [SerializeField]
        internal MaterialInstance[] _materials = null;

        [SerializeField]
        internal string _dataName = null;

        [SerializeField, HideInInspector]
        private int __value;

        private Coroutine __materialsCoroutine;
        private Queue<Database.MaterialOption> __materialOptions;

        private static Dictionary<MaterialKey, Material> __materials;

        public Animator animator
        {
            get;

            private set;
        }

        public int value
        {
            get
            {
                return __value;
            }

            set
            {
                if (database == null || database.nodes == null)
                    return;

                Database.Node node;
                if (!database.nodes.TryGetValue(_dataName, out node))
                    return;
                
                __value = value;

                node.Get(value, option =>
                {
                    node.Select(option, __OnSelect, __OnSelect);
                });
            }
        }

        /*public int normalizedValue
        {
            get
            {
                if (database == null || database.nodes == null)
                    return 0;

                Database.Node node;
                if (!database.nodes.TryGetValue(_dataName, out node))
                    return 0;

                return node.Normalize(__value);
            }

            set
            {
                if (database == null || database.nodes == null)
                    return;

                Database.Node node;
                if (!database.nodes.TryGetValue(_dataName, out node))
                    return;

                value = node.Convert(value);

                node.Get(value, option =>
                {
                    node.Select(option, __OnSelect, __OnSelect);
                });

                __value = value;
            }
        }*/

        public string dataName
        {
            get
            {
                return _dataName;
            }
        }

        public bool Set(int groupIndex, int optionIndex)
        {
            if (database == null || database.nodes == null)
                return false;

            Database.Node node;
            if(!database.nodes.TryGetValue(_dataName, out node))
                return false;

            string option;
            __value = node.Set(__value, groupIndex, optionIndex, out option);

            node.Select(option, __OnSelect, __OnSelect);

            return true;
        }

        public void Random()
        {
            if (database == null || database.nodes == null)
                return;

            Database.Node node;
            if (!database.nodes.TryGetValue(_dataName, out node))
                return;

            int value = node.randValue;
            node.Get(value, option =>
            {
                node.Select(option, __OnSelect, __OnSelect);
            });

            __value = value;
        }

        /*public void OnDestroy()
        {
            if (_materials != null)
            {
                foreach (MaterialInstance material in _materials)
                {
                    if (material.destination != null)
                        Destroy(material.destination);
                }
            }
        }*/

        public string Select(string partName, Action onComplete = null)
        {
            string oldPartName = Set(partName, () =>
            {
                if (_materials != null)
                {
                    var partInfos = GetBy(partName);
                    if (partInfos != null)
                    {
                        foreach (var material in _materials)
                        {
                            if (material.destination == null)
                                continue;

                            foreach (var partInfo in partInfos)
                                partInfo.Replace(material.source, material.destination);
                        }
                    }
                }

                if (animator != null)
                    animator.Rebind();

                if (onComplete != null)
                    onComplete();
            });

            return oldPartName;
        }

        protected void Start()
        {
            animator = GetComponentInParent<Animator>();
        }

        protected void OnEnable()
        {
            if (__loaders != null && __loaders.Count > 0)
                __instancesCoroutine = StartCoroutine(__LoadInstances());

            if(__materialOptions != null && __materialOptions.Count > 0)
                __materialsCoroutine = StartCoroutine(__LoadMaterials());
        }

        protected void OnDisable()
        {
            __instancesCoroutine = null;
            __materialsCoroutine = null;
        }

        private void __OnSelect(Database.MaterialOption materialOption)
        {
            if (__materialOptions == null)
                __materialOptions = new Queue<Database.MaterialOption>();

            __materialOptions.Enqueue(materialOption);

            if(__materialsCoroutine == null && isActiveAndEnabled)
                __materialsCoroutine = StartCoroutine(__LoadMaterials());

            /*if (materialOption.index < 0)
                return;

            int numMaterials = _materials == null ? 0 : _materials.Length;
            if (numMaterials <= materialOption.index)
                return;

            var materialInstance = _materials[materialOption.index];

            Material material;
            MaterialKey materialKey;
            materialKey.color = Color.Lerp(materialOption.minColor, materialOption.maxColor, (float)((double)(__value ^ materialOption.seed) / int.MaxValue));
            materialKey.colorParameterName = materialOption.colorParameterName;
            materialKey.textureParameterName = materialOption.textureParameterName;
            materialKey.texture = assetManager == null ? null : assetManager.Load<Texture>(materialOption.textureLabel, materialOption.textureName);
            materialKey.material = materialInstance.source;

            if (__materials == null)
                __materials = new Dictionary<MaterialKey, Material>();

            if (!__materials.TryGetValue(materialKey, out material) || material == null)
            {
                material = Instantiate(materialInstance.source);
                if (material != null)
                {
                    if (string.IsNullOrWhiteSpace(materialOption.colorParameterName))
                        material.color = materialKey.color;
                    else
                        material.SetColor(materialOption.colorParameterName, materialKey.color);

                    if (string.IsNullOrWhiteSpace(materialOption.textureParameterName))
                        material.mainTexture = materialKey.texture;
                    else
                        material.SetTexture(materialOption.textureParameterName, materialKey.texture);
                }

                __materials[materialKey] = material;
            }

            if (materialInstance.destination != material)
            {
                if (materialInstance.destination == null)
                    materialInstance.destination = materialInstance.source;

                if (gameObject.Replace(materialInstance.destination, material) < 1)
                    return;

                MaterialInstance temp;
                for (int i = 0; i < numMaterials; ++i)
                {
                    temp = _materials[i];
                    if (temp.source != materialInstance.source)
                        continue;

                    temp.destination = material;

                    _materials[i] = temp;
                }

                materialInstance.destination = material;

                _materials[materialOption.index] = materialInstance;
            }*/
        }

        private void __OnSelect(Database.PartOption partOption)
        {
            Select(partOption.partName);
        }

        private IEnumerator __LoadMaterials()
        {
            int i, numMaterials;
            Database.MaterialOption materialOption;
            MaterialInstance materialInstance, temp;
            MaterialKey materialKey;
            Material material;
            while (__materialOptions != null && __materialOptions.Count > 0)
            {
                materialOption = __materialOptions.Dequeue();

                if (materialOption.index >= 0)
                {
                    numMaterials = _materials == null ? 0 : _materials.Length;
                    if (numMaterials > materialOption.index)
                    {
                        var assetLoader = new AssetBundleLoader<Texture>(materialOption.textureLabel, materialOption.textureName, assetManager);
                        while (assetLoader.MoveNext())
                            yield return null;

                        materialKey.color = Color.Lerp(materialOption.minColor, materialOption.maxColor, (float)((double)(__value ^ materialOption.seed) / int.MaxValue));
                        materialKey.colorParameterName = materialOption.colorParameterName;
                        materialKey.textureParameterName = materialOption.textureParameterName;
                        materialKey.texture = assetLoader.value;

                        materialInstance = _materials[materialOption.index];

                        materialKey.material = materialInstance.source;

                        if (__materials == null)
                            __materials = new Dictionary<MaterialKey, Material>();

                        if (!__materials.TryGetValue(materialKey, out material) || material == null)
                        {
                            material = Instantiate(materialInstance.source);
                            if (material != null)
                            {
                                if (string.IsNullOrWhiteSpace(materialOption.colorParameterName))
                                    material.color = materialKey.color;
                                else
                                    material.SetColor(materialOption.colorParameterName, materialKey.color);

                                if (string.IsNullOrWhiteSpace(materialOption.textureParameterName))
                                    material.mainTexture = materialKey.texture;
                                else
                                    material.SetTexture(materialOption.textureParameterName, materialKey.texture);
                            }

                            __materials[materialKey] = material;
                        }

                        if (materialInstance.destination != material)
                        {
                            if (materialInstance.destination == null)
                                materialInstance.destination = materialInstance.source;

                            if (gameObject.Replace(materialInstance.destination, material) > 0)
                            {
                                for (i = 0; i < numMaterials; ++i)
                                {
                                    temp = _materials[i];
                                    if (temp.source != materialInstance.source)
                                        continue;

                                    temp.destination = material;

                                    _materials[i] = temp;
                                }

                                materialInstance.destination = material;

                                _materials[materialOption.index] = materialInstance;
                            }
                        }
                    }
                }
            }

            __materialsCoroutine = null;
        }
    }
}