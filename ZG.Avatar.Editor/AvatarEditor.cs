using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

namespace ZG.Avatar
{
    public class AvatarEditor : EditorWindow
    {
        public const string FOLDER_KEY = "AvatarFolder";
        //public const string ROOT_GUID_KEY = "AvatarRootGUID";
        //public const string ROOT_PATH_KEY = "AvatarRootPath";

        private bool __isForceBuild;
        //private bool __isBuildAssetBundle;
        
        private string __folder;

        //private Transform __boneRoot;

        [MenuItem("Window/ZG/Avatar Editor")]
        public static void GetWindow()
        {
            GetWindow<AvatarEditor>();
        }

        [MenuItem("Assets/Create/ZG/Avatar Database")]
        public static void Create()
        {
            EditorHelper.CreateAsset<Database>("Avatar Database");
        }

        public static Transform GetRoot(Transform child, int level)
        {
            if (child == null)
                return null;

            if (level > 0)
            {
                Transform parent = child.parent;

                return parent == null ? child : GetRoot(parent, level - 1);
            }

            return child;
        }

        public static string GetPath(Transform transform, Transform root)
        {
            if (transform == null || transform == root)
                return null;

            string path = GetPath(transform.parent, root);
            return path == null ? transform.name : path + '/' + transform.name;
        }

        public static Mesh Clone(Mesh source, Dictionary<string, Mesh> map, string folder, string label, Action<string> handler)
        {
            if (source == null)
                return null;

            string path = AssetDatabase.GetAssetPath(source) + '/' + source.name;
            Mesh destination;
            if (map == null || !map.TryGetValue(path, out destination) || destination == null)
            {
                destination = Instantiate(source);

                try
                {
                    string fileName = folder + '/' + path;
                    EditorHelper.CreateFolder(Path.GetDirectoryName(fileName));
                    fileName += ".asset";

                    if (handler != null)
                        handler(fileName);

                    AssetDatabase.CreateAsset(destination, fileName);

                    AssetImporter assetImporter = AssetImporter.GetAtPath(fileName);
                    if (assetImporter != null)
                        assetImporter.SetAssetBundleNameAndVariant(label, string.Empty);
                }
                catch (Exception exception)
                {
                    if (exception != null)
                        Debug.LogError(exception.Message);
                }

                if (map != null)
                    map[path] = destination;
            }

            return destination;
        }

        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            __folder = EditorGUILayout.TextField("Build Folder", __folder);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(FOLDER_KEY, __folder);

                __isForceBuild = true;
            }

            /*EditorGUI.BeginChangeCheck();
            __boneRoot = EditorGUILayout.ObjectField("Bone Root", __boneRoot, typeof(Transform), false) as Transform;
            if (EditorGUI.EndChangeCheck())
            {
                if (__boneRoot == null)
                {
                    EditorPrefs.DeleteKey(ROOT_GUID_KEY);
                    EditorPrefs.DeleteKey(ROOT_PATH_KEY);
                }
                else
                {
                    string path = string.Empty;
                    Transform parent = __boneRoot.parent, root = __boneRoot;
                    while (parent != null)
                    {
                        path += root.name + '/' + path;
                        root = parent;
                        parent = parent.parent;
                    }

                    EditorPrefs.SetString(ROOT_GUID_KEY, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(root)));
                    EditorPrefs.SetString(ROOT_PATH_KEY, path);

                    __isForceBuild = true;
                }
            }*/

            __isForceBuild = EditorGUILayout.Toggle("Is Force Build", __isForceBuild);
            if (GUILayout.Button("Build"))
            {
                AssetDatabase.StartAssetEditing();

                string[] guids = AssetDatabase.FindAssets("t:prefab");
                int numGUIDs = guids == null ? 0 : guids.Length;
                if (numGUIDs > 0)
                {
                    string assetPath, fileName, filePath, path;
                    int i, j, numParts;
                    float progress, total;
                    Part part;
                    Info info;
                    AssetImporter assetImporter;
                    GameObject prefab, gameObject, temp;
                    Transform transform, root;
                    ISkinWrapper skinWrapper;
                    Action<string> handler;
                    MeshFilter[] meshFilters;
                    Part[] parts, targets;
                    Transform[] bones;
                    //HashSet<string> filePaths = null;
                    List<string> bonePaths = null;
                    Dictionary<string, string> filePaths = null;
                    Dictionary<string, UnityEngine.Object> map = null;
                    //Dictionary<string, HashSet<string>> assetMap = null;
                    for (i = 0; i < numGUIDs; ++i)
                    {
                        assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                        progress = i * 1.0f / numGUIDs;
                        if (EditorUtility.DisplayCancelableProgressBar("Build Avatar", assetPath, progress))
                            break;

                        prefab = AssetDatabase.LoadMainAssetAtPath(assetPath) as GameObject;
                        parts = prefab == null ? null : prefab.GetComponentsInChildren<Part>(true);
                        numParts = parts == null ? 0 : parts.Length;
                        if (numParts > 0)
                        {
                            total = numParts * numGUIDs;
                            for (j = 0; j < numParts; ++j)
                            {
                                part = parts[j];
                                gameObject = part == null ? null : part.gameObject;
                                if (gameObject == null)
                                    continue;

                                if (EditorUtility.DisplayCancelableProgressBar("Build Avatar", part.avatarName, progress + j * 1.0f / total))
                                    break;

                                //Debug.Log("Build Avatar Name: " + part.avatarName + ", Avatar Label: " + part.avatarLabel + " From: " + assetPath);

                                if (!AssetDatabase.IsValidFolder(__folder))
                                    __folder = AssetDatabase.CreateFolder(Path.GetDirectoryName(__folder), Path.GetFileName(__folder));

                                fileName = __folder + '/' + part.avatarName;
                                filePath = fileName + ".asset";

                                /*if (assetMap == null)
                                    assetMap = new Dictionary<string, HashSet<string>>();

                                if (!assetMap.TryGetValue(part.avatarLabel, out assetNames) || assetNames == null)
                                {
                                    assetNames = new HashSet<string>();

                                    assetMap[part.avatarLabel] = assetNames;
                                }*/

                                if (filePaths == null)
                                    filePaths = new Dictionary<string, string>();

                                if (filePaths.TryGetValue(filePath, out path))
                                    Debug.LogError("The Same Name Of: " + assetPath + " Avatar Name: " + part.avatarName + ", Avatar Label: " + part.avatarLabel + " From: " + path);
                                else
                                    filePaths[filePath] = assetPath;

                                if (!__isForceBuild && !part.isBuild)
                                {
                                    Debug.Log("Ignore Avatar Name: " + part.avatarName + ", Avatar Label: " + part.avatarLabel + " From: " + assetPath);

                                    continue;
                                }

                                if (part.isBuild)
                                {
                                    part.isBuild = false;

                                    EditorUtility.SetDirty(prefab);
                                }

                                info = CreateInstance<Info>();
                                if (info == null)
                                {
                                    Debug.LogError("Fail To Build Avatar Name: " + part.avatarName + ", Avatar Label: " + part.avatarLabel + " From: " + assetPath);

                                    continue;
                                }

                                transform = gameObject.transform;
                                root = GetRoot(transform, part.level);

                                if (bonePaths != null)
                                    bonePaths.Clear();

                                skinWrapper = part.GetSkinWrapper(gameObject);
                                if (skinWrapper != null)
                                {
                                    bones = skinWrapper.bones;
                                    if (bones == null)
                                    {
                                        /*Debug.LogError("Fail To Build Avatar Name: " + part.avatarName + ", Avatar Label: " + part.avatarLabel + " From: " + assetPath);

                                        DestroyImmediate(info);

                                        continue;*/
                                    }
                                    else
                                    {
                                        info.rootBonePath = GetPath(skinWrapper.rootBone, root);

                                        foreach (Transform bone in bones)
                                        {
                                            path = GetPath(bone, root);
                                            if (path == null)
                                            {
                                                Debug.LogError("The bone: " + bone == null ? string.Empty : bone.name + " is missing");

                                                bonePaths.Clear();

                                                break;
                                            }

                                            if (bonePaths == null)
                                                bonePaths = new List<string>();

                                            bonePaths.Add(path);
                                        }
                                    }
                                }
                                else
                                    info.rootBonePath = null;

                                gameObject = Instantiate(gameObject, null, false);
                                if (gameObject != null)
                                {
                                    if (map == null)
                                        map = new Dictionary<string, UnityEngine.Object>();

                                    string tempPath = assetPath;
                                    handler = x =>
                                    {
                                        filePaths[x] = tempPath;
                                    };

                                    /*skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
                                    if (skinnedMeshRenderer != null)
                                    {
                                        skinnedMeshRenderer.rootBone = null;
                                        skinnedMeshRenderer.bones = null;

                                        skinnedMeshRenderer.sharedMesh = Clone(skinnedMeshRenderer.sharedMesh, meshMap, __folder, part.avatarLabel, handler);
                                    }

                                    meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
                                    if (meshFilters != null)
                                    {
                                        foreach (MeshFilter meshFilter in meshFilters)
                                        {
                                            if (meshFilter != null)
                                                meshFilter.mesh = Clone(meshFilter.sharedMesh, meshMap, __folder, part.avatarLabel, handler);
                                        }
                                    }*/

                                    targets = gameObject.GetComponents<Part>();
                                    if (targets != null)
                                    {
                                        foreach (var target in targets)
                                        {
                                            target.Init(__folder, map, handler);
                                            
                                            DestroyImmediate(target);
                                        }
                                    }

                                    path = fileName + ".prefab";

                                    filePaths[path] = assetPath;

                                    temp = PrefabUtility.SaveAsPrefabAsset(gameObject, path, out bool isSuccess);

                                    if (isSuccess)
                                    {
                                        if (temp == null)
                                            temp = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                                    }
                                    else
                                    {
                                        temp = null;

                                        Debug.LogError(path + " Save Fail.");
                                    }

                                    if (temp != gameObject)
                                    {
                                        DestroyImmediate(gameObject);

                                        gameObject = temp;
                                    }
                                }

                                info.path = GetPath(transform, root);
                                info.gameObject = gameObject;
                                info.bonePaths = bonePaths == null || bonePaths.Count < 1 ? null : bonePaths.ToArray();

                                try
                                {
                                    AssetDatabase.CreateAsset(info, filePath);

                                    assetImporter = AssetImporter.GetAtPath(filePath);
                                    if (assetImporter != null)
                                    {
                                        assetImporter.SetAssetBundleNameAndVariant(part.avatarLabel, string.Empty);
                                        assetImporter.SaveAndReimport();
                                    }
                                }
                                catch (Exception exception)
                                {
                                    if (exception != null)
                                        Debug.LogError(exception.Message);
                                }
                            }

                            if (j < numParts)
                                break;
                        }
                    }


                    if (filePaths != null)
                    {
                        string assetsPath = Path.GetDirectoryName(Path.GetDirectoryName(Application.streamingAssetsPath)) + Path.DirectorySeparatorChar;
                        DirectoryInfo direction = new DirectoryInfo(assetsPath + __folder);

                        FileInfo[] fileInfos = direction.GetFiles("*", SearchOption.AllDirectories);
                        if (fileInfos != null)
                        {
                            bool isMeta;
                            int index, length = assetsPath == null ? 0 : assetsPath.Length;
                            foreach (FileInfo fileInfo in fileInfos)
                            {
                                filePath = fileInfo.FullName;
                                index = filePath == null ? 0 : filePath.Length;
                                index -= 5;
                                isMeta = index > 0 && filePath.Remove(0, index) == ".meta";
                                if (isMeta)
                                    filePath = filePath.Remove(index);

                                filePath = filePath.Remove(0, length);
                                filePath = filePath.Replace('\\', '/');
                                if (!filePaths.ContainsKey(filePath))
                                {
                                    if (!isMeta)
                                        Debug.Log("Delete Temp File: " + filePath);

                                    fileInfo.Delete();
                                }
                            }
                        }
                    }

                    EditorUtility.ClearProgressBar();
                }
                
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }
        }

        void OnEnable()
        {
            __folder = EditorPrefs.GetString(FOLDER_KEY, "Assets/Temp");
            //Transform transform = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString(ROOT_GUID_KEY)), typeof(Transform)) as Transform;
            //__boneRoot = transform == null ? null : transform.Find(EditorPrefs.GetString(ROOT_PATH_KEY));
        }
    }
}