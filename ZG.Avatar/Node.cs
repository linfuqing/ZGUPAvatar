using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZG.Avatar
{
    public interface IAvatar
    {
        GameObject SetPart(Info info);
    }

    public partial class Node : MonoBehaviour, IAvatar
    {
        [Serializable]
        public struct Instance
        {
            public string partName;
            public int[] gameObjectOffsets;
            public List<GameObject> gameObjects;
        }

        [Serializable]
        private class Instances : Map<Instance>
        {

        }

        private class Loader : IEnumerator
        {
            public readonly string PartName;
            public readonly Database Database;
            public readonly IAvatar Avatar;

            public readonly IDictionary<string, Instance> Instances;

            public readonly Database.Part Part;
            public readonly AssetBundleLoader AssetBundleLoader;

            public event Action onComplete;

            private int __instanceIndex;

            public static Loader Create(
                IDictionary<string, Instance> instances,
                IAvatar avatar,
                AssetManager assetManager,
                Database database,
                string partName,
                out string group)
            {
                if (instances == null || avatar == null || database == null || database.parts == null)
                    throw new InvalidOperationException();

                if (database.parts.TryGetValue(partName, out var part) && !string.IsNullOrEmpty(part.group))
                {
                    group = part.group;

                    if (instances.TryGetValue(group, out var instance))
                    {
                        if (instance.gameObjects != null)
                        {
                            foreach (var gameObject in instance.gameObjects)
                                DestroyImmediate(gameObject);

                            instance.gameObjects = null;
                        }

                        if (database.parts.TryGetValue(instance.partName, out var temp) && !string.IsNullOrEmpty(temp.label))
                            assetManager.UnloadAssetBundle(temp.label.ToLower());

                        instances.Remove(group);
                    }

                    var assetBundleLoader = string.IsNullOrEmpty(part.label) ? null : assetManager.GetOrCreateAssetBundleLoader(part.label.ToLower());
                    if (assetBundleLoader == null)
                        group = null;
                    else
                        assetBundleLoader.Retain();

                    return new Loader(partName, database, avatar, instances, part, assetBundleLoader);
                }

                group = null;

                return null;
            }

            private Loader(
                string partName, 
                Database database, 
                IAvatar avatar, 
                IDictionary<string, Instance> instances,
                Database.Part part,
                AssetBundleLoader assetBundleLoader)
            {
                PartName = partName;
                Database = database;
                Avatar = avatar;
                Instances = instances;
                Part = part;
                AssetBundleLoader = assetBundleLoader;
            }

            /*public Loader(
                IDictionary<string, Instance> instances, 
                IAvatar avatar, 
                AssetManager assetManager, 
                Database database,
                string partName, 
                out string group)
            {
                if (instances == null || avatar == null || database == null || database.parts == null)
                    throw new InvalidOperationException();

                PartName = partName;
                Database = database;
                Avatar = avatar;
                Instances = instances;

                onComplete = null;

                if (database.parts.TryGetValue(partName, out __part) && !string.IsNullOrEmpty(__part.group))
                {
                    group = __part.group;

                    __instanceIndex = 0;
                    if (instances.TryGetValue(group, out var instance))
                    {
                        if (instance.gameObjects != null)
                        {
                            foreach (var gameObject in instance.gameObjects)
                                DestroyImmediate(gameObject);

                            instance.gameObjects = null;
                        }

                        if (database.parts.TryGetValue(instance.partName, out var part) && !string.IsNullOrEmpty(part.label))
                            assetManager.UnloadAssetBundle(part.label.ToLower());

                        Instances.Remove(group);
                    }

                    __loader = string.IsNullOrEmpty(__part.label) ? null : assetManager.GetOrCreateAssetBundleLoader(__part.label.ToLower());
                    if (__loader == null)
                        group = null;
                    else
                        __loader.Retain();
                }
                else
                {
                    __instanceIndex = -1;
                    __part = default;
                    __loader = null;

                    group = null;
                }
            }*/

            public bool MoveNext()
            {
                if (AssetBundleLoader == null)
                    return false;

                if (AssetBundleLoader.keepWaiting)
                    return true;

                int numInstances = Part.infos.Length;
                if (__instanceIndex < numInstances)
                {
                    var info = Part.infos[__instanceIndex];

                    var loader = new AssetBundleLoader<Info>(info.avatarName, AssetBundleLoader);
                    if (loader.MoveNext())
                        return true;

                    var partInfos = loader.values;
                    int numPartInfos = partInfos == null ? 0 : partInfos.Length;
                    if (numPartInfos < 1)
                    {
                        Debug.LogError(info.avatarName + " loaded fail.(Part Name: " + PartName + ')');

                        return false;
                    }

                    GameObject gameObject;
                    var gameObjects = new GameObject[numPartInfos];
                    for (int i = 0; i < numPartInfos; ++i)
                    {
                        gameObject = Avatar.SetPart(partInfos[i]);
                        if (gameObject == null)
                        {
                            Debug.LogError(info.avatarName + " loaded fail.(Part Name: " + PartName + ')');
                            
                            continue;
                        }

                        gameObjects[i] = gameObject;
                    }

                    {
                        bool isActive = !info.isNegative;
                        if (info.conditions != null && info.conditions.Length > 0)
                        {
                            isActive = info.isNegative;

                            var instances = Instances.Values;
                            foreach (var condition in info.conditions)
                            {
                                if (__Check(instances, Database, condition.label))
                                {
                                    if (!condition.isNegative)
                                        isActive = !info.isNegative;
                                }
                                else if (condition.isNegative)
                                    isActive = !info.isNegative;

                                if (isActive != info.isNegative)
                                    break;
                            }
                        }

                        for (int i = 0; i < numPartInfos; ++i)
                        {
                            gameObject = gameObjects[i];
                            if(gameObject == null)
                                continue;
                            
                            gameObject.SetActive(isActive);
                        }
                        
                        if(!Instances.TryGetValue(Part.group, out var instance))
                        {
                            instance.partName = PartName;
                            instance.gameObjectOffsets = new int[numInstances];
                            instance.gameObjects = new List<GameObject>();
                        }

                        instance.gameObjectOffsets[__instanceIndex] = instance.gameObjects.Count;
                        instance.gameObjects.AddRange(gameObjects);

                        Instances[Part.group] = instance;
                    }

                    if(++__instanceIndex == numInstances)
                    {
                        string label = Part.label;
                        if (!string.IsNullOrEmpty(label))
                        {
                            bool isActive;
                            int i, j, numGameObjects, numInfos;
                            Database.Part part;
                            //AvatarDatabase.Condition[] conditions;
                            var instances = Instances.Values;
                            foreach (var instance in instances)
                            {
                                if (Database.parts.TryGetValue(instance.partName, out part) && part.infos != null)
                                {
                                    numInfos = part.infos == null ? 0 : part.infos.Length;
                                    for (i = 0; i < numInfos; ++i)
                                    {
                                        info = part.infos[i];
                                        if (info.conditions != null && info.conditions.Length > 0)
                                        {
                                            if (instance.gameObjectOffsets != null &&
                                                instance.gameObjectOffsets.Length > i)
                                            {
                                                isActive = info.isNegative;
                                                foreach (var condition in info.conditions)
                                                {
                                                    if (condition.label == label)
                                                    {
                                                        if (!condition.isNegative)
                                                            isActive = !info.isNegative;
                                                    }
                                                    else if (__Check(instances, Database, condition.label))
                                                    {
                                                        if (!condition.isNegative)
                                                            isActive = !info.isNegative;
                                                    }
                                                    else if (condition.isNegative)
                                                        isActive = !info.isNegative;

                                                    if (isActive != info.isNegative)
                                                        break;
                                                }

                                                numGameObjects = i < instance.gameObjectOffsets.Length - 1
                                                    ? instance.gameObjectOffsets[i + 1]
                                                    : instance.gameObjects.Count;
                                                for (j = instance.gameObjectOffsets[i]; j < numGameObjects; ++j)
                                                {
                                                    gameObject = instance.gameObjects[j];
                                                    if(gameObject == null)
                                                        continue;

                                                    gameObject.SetActive(isActive);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (onComplete != null)
                            onComplete();

                        return false;
                    }

                    return true;
                }

                return false;
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            object IEnumerator.Current => null;
        }

        public AssetManager assetManager;
        public Database database;
        [SerializeField, HideInInspector]
        private Instances __instances;
        private Coroutine __instancesCoroutine;
        private Dictionary<string, Loader> __loaders;

        public IEnumerable<string> groupNames
        {
            get
            {
                return __instances == null ? null : __instances.Keys;
            }
        }
        
        private static bool __Check(IEnumerable<Instance> instances, Database database, string label)
        {
            if (instances == null || database == null || database.parts == null)
                return false;

            Database.Part part;
            foreach (var instance in instances)
            {
                if (database.parts.TryGetValue(instance.partName, out part) && part.label == label)
                    return true;
            }

            return false;
        }

        /*private static string __Set(
            string partName,
            Node instance,
            IDictionary<string, Part> partMap,
            Database database,
            AssetManager assetManager)
        {
            if (assetManager == null || instance == null || partMap == null || database == null || database.parts == null)
                return null;

            Database.Part part;
            if (!database.parts.TryGetValue(partName, out part) || part.infos == null)
                return null;

            AssetBundle assetBundle = assetManager.LoadAssetBundle(part.label.ToLower());
            if (assetBundle == null)
                return null;

            Part result;
            if (partMap.TryGetValue(part.group, out result))
            {
                if (result.name == partName)
                    return result.name;

                if (result.gameObjects != null)
                {
                    foreach (GameObject gameObject in result.gameObjects)
                        DestroyImmediate(gameObject);

                    result.gameObjects = null;
                }

                Database.Part temp;
                if (database.parts.TryGetValue(result.name, out temp))
                    assetManager.UnloadAssetBundle(temp.label.ToLower());
            }

            string name = result.name;
            result.name = partName;

            bool isActive;
            int numInfos = part.infos == null ? 0 : part.infos.Length, i;
            if (numInfos > 0)
            {
                Database.Info info;
                Info avatarInfo;
                GameObject gameObject;
                result.gameObjects = new GameObject[numInfos];
                for (i = 0; i < numInfos; ++i)
                {
                    info = part.infos[i];

                    avatarInfo = assetBundle.LoadAsset<Info>(info.avatarName) as Info;
                    gameObject = instance.Add(avatarInfo);
                    if (gameObject == null)
                        Debug.LogError(info.avatarName + " loaded fail.(Part Name: " + partName + ')');
                    else
                    {
                        isActive = !info.isNegative;
                        if (info.conditions != null && info.conditions.Length > 0)
                        {
                            isActive = info.isNegative;

                            foreach (Database.Condition condition in info.conditions)
                            {
                                if (__Check(partMap.Values, database, condition.label))
                                {
                                    if (!condition.isNegative)
                                        isActive = !info.isNegative;
                                }
                                else if (condition.isNegative)
                                    isActive = !info.isNegative;

                                if (isActive != info.isNegative)
                                    break;
                            }
                        }

                        gameObject.SetActive(isActive);
                    }

                    result.gameObjects[i] = gameObject;
                }
            }

            string group = part.group, label = part.label;
            if (!string.IsNullOrEmpty(label))
            {
                Part temp;
                Database.Info info;
                //AvatarDatabase.Condition[] conditions;
                GameObject gameObject;
                foreach (KeyValuePair<string, Part> pair in partMap)
                {
                    temp = pair.Value;
                    if (database.parts.TryGetValue(temp.name, out part) && part.infos != null)
                    {
                        numInfos = part.infos == null ? 0 : part.infos.Length;
                        for (i = 0; i < numInfos; ++i)
                        {
                            info = part.infos[i];
                            if (info.conditions != null && info.conditions.Length > 0)
                            {
                                if (temp.gameObjects != null && temp.gameObjects.Length > i)
                                {
                                    gameObject = temp.gameObjects[i];
                                    if (gameObject != null)
                                    {
                                        isActive = info.isNegative;
                                        foreach (Database.Condition condition in info.conditions)
                                        {
                                            if (condition.label == label)
                                            {
                                                if (!condition.isNegative)
                                                    isActive = !info.isNegative;
                                            }
                                            else if (__Check(partMap.Values, database, condition.label))
                                            {
                                                if (!condition.isNegative)
                                                    isActive = !info.isNegative;
                                            }
                                            else if (condition.isNegative)
                                                isActive = !info.isNegative;

                                            if (isActive != info.isNegative)
                                                break;
                                        }

                                        gameObject.SetActive(isActive);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            partMap[group] = result;
            
            return name;
        }*/

        private static string __Unset(string group, IDictionary<string, Instance> instances, Database database, AssetManager assetManager)
        {
            if (instances == null || !instances.TryGetValue(group, out var instance))
                return null;

            string partName = instance.partName;
            if (instance.gameObjects != null)
            {
                foreach (var gameObject in instance.gameObjects)
                    DestroyImmediate(gameObject);
            }

            instances.Remove(group);

            if (database != null && database.parts != null)
            {
                if (database.parts.TryGetValue(partName, out var part))
                {
                    string label = part.label;

                    if(assetManager != null)
                        assetManager.UnloadAssetBundle(label.ToLower());

                    if (!string.IsNullOrEmpty(part.label))
                    {
                        bool isActive;
                        int numInfos, i;
                        Database.Info info;
                        GameObject gameObject;
                        var values = instances.Values;
                        foreach (var value in values)
                        {
                            if (database.parts.TryGetValue(value.partName, out part))
                            {
                                numInfos = part.infos == null ? 0 : part.infos.Length;
                                for (i = 0; i < numInfos; ++i)
                                {
                                    info = part.infos[i];
                                    if (info.conditions != null && info.conditions.Length > 0)
                                    {
                                        gameObject = value.gameObjects[i];
                                        if (gameObject != null)
                                        {
                                            isActive = info.isNegative;
                                            foreach (var condition in info.conditions)
                                            {
                                                if (condition.label == label)
                                                {
                                                    if (condition.isNegative)
                                                        isActive = !info.isNegative;
                                                }
                                                else if (__Check(values, database, condition.label))
                                                {
                                                    if (!condition.isNegative)
                                                        isActive = !info.isNegative;
                                                }
                                                else if (condition.isNegative)
                                                    isActive = !info.isNegative;

                                                if (isActive != info.isNegative)
                                                    break;
                                            }

                                            gameObject = value.gameObjects[i];
                                            if (gameObject != null)
                                                gameObject.SetActive(isActive);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return partName;
        }

        public IReadOnlyList<GameObject> Get(string group)
        {
            if (__instances == null)
                return null;

            if (!__instances.TryGetValue(group, out var instance))
                return null;

            return instance.gameObjects;
        }

        public IReadOnlyList<GameObject> GetBy(string partName)
        {
            if (partName == null || database == null || database.parts == null)
                return null;

            if (!database.parts.TryGetValue(partName, out var part))
                return null;

            return Get(part.group);
        }

        public GameObject SetPart(Info info)
        {
            if (info == null)
                return null;

            int index = info.path == null ? -1 : info.path.LastIndexOf('/');
            string name = index == -1 ? info.path : info.path.Substring(index + 1, info.path.Length - index - 1);
            Transform root = this.transform, 
                parent = index == -1 ? root : root.Find(info.path.Substring(0, index)), 
                transform = parent == null ? null : parent.Find(name);
            GameObject gameObject = transform == null ? null : transform.gameObject;
            if (gameObject == null)
            {
                if(info.gameObject == null)
                {
                    gameObject = new GameObject(name); 
                    transform = gameObject.transform;
                    transform.SetParent(parent == null ? root : parent, false);
                }
                else
                {
                    gameObject = Instantiate(info.gameObject, parent == null ? root : parent, false);
                    gameObject.name = name;
                    //transform = gameObject.transform;
                }
            }

            if (info.bonePaths != null && info.bonePaths.Length > 0)
            {
                var skinWrapper = gameObject.GetComponent<SkinnedMeshRenderer>();
                if (skinWrapper == null)
                {
                    var skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
                    if (skinnedMeshRenderer != null)
                    {
                        List<Transform> bones = null;
                        if (info.bonePaths != null)
                        {
                            Transform bone;
                            foreach (string bonePath in info.bonePaths)
                            {
                                bone = root.Find(bonePath);
                                if (bone == null)
                                    Debug.LogWarning("The bone: " + bonePath + " is missing.");

                                if (bones == null)
                                    bones = new List<Transform>();

                                bones.Add(bone);
                            }
                        }

                        skinnedMeshRenderer.bones = bones.ToArray();

                        if (info.rootBonePath != null)
                            skinnedMeshRenderer.rootBone = root.Find(info.rootBonePath);
                    }
                }
                else
                {
                    List<Transform> bones = null;
                    if (info.bonePaths != null)
                    {
                        Transform bone;
                        foreach (string bonePath in info.bonePaths)
                        {
                            bone = root.Find(bonePath);
                            if (bone == null)
                                Debug.LogWarning("The bone: " + bonePath + " is missing.");

                            if (bones == null)
                                bones = new List<Transform>();

                            bones.Add(bone);
                        }
                    }

                    skinWrapper.bones = bones.ToArray();

                    if (info.rootBonePath != null)
                        skinWrapper.rootBone = root.Find(info.rootBonePath);
                }
            }

            return gameObject;
        }

        public string Set(string partName, Action onComplete = null)
        {
            if (__instances == null)
                __instances = new Instances();

            var loader = Loader.Create(__instances, this, assetManager, database, partName, out string group);
            if (!string.IsNullOrEmpty(group))
            {
                if (onComplete != null)
                    loader.onComplete += onComplete;

                if (__loaders == null)
                    __loaders = new Dictionary<string, Loader>();

                __loaders[group] = loader;

                if (__instancesCoroutine == null && isActiveAndEnabled)
                    __instancesCoroutine = StartCoroutine(__LoadInstances());
            }

            return group;

            /*if (__partMap == null)
                __partMap = new Parts();

            return __Set(partName, this, __partMap, database, assetManager);*/
        }

        public string Unset(string group)
        {
            if (__loaders != null)
                __loaders.Remove(group);

            return __Unset(group, __instances, database, assetManager);
        }

        public bool UnsetBy(string partName)
        {
            if (database == null || database.parts == null)
                return false;

            if (!database.parts.TryGetValue(partName, out var part))
                return false;

            return !string.IsNullOrEmpty(Unset(part.group));
        }

        public void Clear()
        {
            if(__loaders != null)
                __loaders.Clear();

            int count = __instances == null ? 0 : __instances.Count;
            if (count > 0)
            {
                string[] groups = new string[count];
                __instances.Keys.CopyTo(groups, 0);
                foreach (string group in groups)
                    Unset(group);
            }
        }

        protected void OnDestroy()
        {
            if (assetManager != null && database != null && database.parts != null)
            {
                var instances = __instances == null ? null : __instances.Values;
                if (instances != null)
                {
                    Database.Part part;
                    foreach (var instance in instances)
                    {
                        if (database.parts.TryGetValue(instance.partName, out part) && !string.IsNullOrEmpty(part.label))
                            assetManager.UnloadAssetBundle(part.label.ToLower());
                    }
                }
            }
        }

        private IEnumerator __LoadInstances()
        {
            int count;
            string[] groups;

            count = __loaders == null ? 0 : __loaders.Count;
            while (count > 0)
            {
                groups = new string[count];
                __loaders.Keys.CopyTo(groups, 0);

                Loader loader;
                foreach (string group in groups)
                {
                    if(!__loaders.TryGetValue(group, out loader) || !loader.MoveNext())
                        __loaders.Remove(group);
                }

                yield return null;

                count = __loaders == null ? 0 : __loaders.Count;
            }

            __instancesCoroutine = null;
        }
    }
}