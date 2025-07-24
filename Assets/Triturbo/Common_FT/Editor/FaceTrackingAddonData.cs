using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if ENABLE_BS
using Triturbo.BlendShapeShare.BlendShapeData;
#endif

//ver 1.0.0

namespace Triturbo.FaceTrackingAddon
{
    [CustomEditor(typeof(FaceTrackingAddonData))]
    public class FaceTrackingAddonDataEditor : Editor
    {
        bool locked = true;

        public void OnEnable()
        {
            var data = target as FaceTrackingAddonData;

            foreach (var i in data.eyesInstallers)
            {

                i.cost = ParameterHelper.GetMAParameterMemoryUsed(i.prefab);

            }

            foreach (var i in data.mouthInstallers)
            {

                i.cost = ParameterHelper.GetMAParameterMemoryUsed(i.prefab);

            }

            EditorUtility.SetDirty(target);
        }
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button(locked ? "Unlock" : "Lock")) locked = !locked;
            GUI.enabled = !locked;
            DrawDefaultInspector();

            if (GUILayout.Button("Update Cost"))
            {
                var data = target as FaceTrackingAddonData;

                foreach(var i in data.eyesInstallers)
                {

                    i.cost =  ParameterHelper.GetMAParameterMemoryUsed(i.prefab);

                }

                foreach (var i in data.mouthInstallers)
                {

                    i.cost = ParameterHelper.GetMAParameterMemoryUsed(i.prefab);

                }

                EditorUtility.SetDirty(target);
            }


        }

    }



    [CreateAssetMenu(fileName = "Settings", menuName = "TriturboFaceTrackingAddon/Settings", order = 1)]
    public class FaceTrackingAddonData : ScriptableObject
    {

        public string version = "1.0";
        public UnityEngine.Object addonRoot;
        public UnityEngine.Object localizationDirectory;
        public Texture bannerIcon;
        public GameObject menuInstaller;
        public List<Installer> eyesInstallers;
        public List<Installer> mouthInstallers;
        public string generatedAssetsDirectory = "Generated";
        public string meshAssetsName = "_Mesh.asset";
        public string fbxName = "_FT.fbx";
        public FbxDataSO fbxData;

#if ENABLE_BS
        public BlendShapeDataSO m_BlendShapeData;

        [SerializeField]
        private GeneratedMeshAssetSO meshAssets;
        public GeneratedMeshAssetSO MeshAssets
        {
            get
            {
                if (meshAssets == null && m_BlendShapeData != null)
                {
                    string root = AssetDatabase.GetAssetPath(addonRoot);

                    string folder = System.IO.Path.Combine(root, generatedAssetsDirectory);
                    if (!AssetDatabase.IsValidFolder(folder))
                    {
                        AssetDatabase.CreateFolder(root, generatedAssetsDirectory);
                    }
                    string meshAssetPath = System.IO.Path.Combine(folder, meshAssetsName);


                    if (System.IO.File.Exists(meshAssetPath))
                    {
                        string extension = System.IO.Path.GetExtension(meshAssetsName);
                        string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(meshAssetsName);


                        fileNameWithoutExtension += $"_{version}";
                        meshAssetPath = System.IO.Path.Combine(folder,  $"{fileNameWithoutExtension}{extension}");


                        int counter = 1;
                        while (System.IO.File.Exists(meshAssetPath))
                        {
                            meshAssetPath = System.IO.Path.Combine(folder, $"{fileNameWithoutExtension}_{counter}{extension}");
                            counter++;
                        }
                    }
                    Undo.RecordObject(this, "Generate Mesh Assets");
                    meshAssets = m_BlendShapeData.CreateMeshAsset(meshAssetPath);
                    EditorUtility.SetDirty(this);
                    if (meshAssets == null)
                    {
                        string path = System.IO.Path.Combine(folder, fbxName);
                        if (m_BlendShapeData.CreateFbx(path))
                        {
                            meshAssets = m_BlendShapeData.CreateMeshAsset(meshAssetPath, AssetDatabase.LoadAssetAtPath<GameObject>(path));
                        }
                    }
                }
                return meshAssets;
            }
        }
#else
        public ScriptableObject m_BlendShapeData;
#endif


        public LocalizationManager.Language usingLang;


        public Dictionary<string, string> localization;

        public static FaceTrackingAddonData Load(string path, string guid = "")
        {
            var asset = AssetDatabase.LoadAssetAtPath<FaceTrackingAddonData>(path);
            if (asset == null && guid != "")
            {
                path = AssetDatabase.GUIDToAssetPath(guid);
                return AssetDatabase.LoadAssetAtPath<FaceTrackingAddonData>(path);
            }

            return asset;
        }


        [Serializable]
        public class Installer
        {
            public GameObject prefab;
            public string name;
            public int cost;
        }


    }
}

