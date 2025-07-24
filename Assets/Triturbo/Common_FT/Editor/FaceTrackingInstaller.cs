using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;



using VRC.SDK3.Avatars.Components;
using System.Linq;
using System;

//ver 1.0.0

namespace Triturbo.FaceTrackingAddon
{
    public class FaceTrackingInstaller
    {
        public FaceTrackingAddonData _addonData;
        public VRCAvatarDescriptor avatarDes;
        private LocalizationManager lm;
        int langID = 0;
        int eyeSelection = 1;
        int lipSelection = 1;
        private Texture bannerIconBlendShare;

        public VRCAvatarDescriptor lastAvatar;

        public FBXData fbxData;
        int avatarParametersUsage = 0;
        public FaceTrackingInstaller(FaceTrackingAddonData addonData)
        {
            _addonData = addonData;

            lm = new LocalizationManager(AssetDatabase.GetAssetPath(_addonData.localizationDirectory), _addonData.usingLang);

            langID = (int)_addonData.usingLang;


            bannerIconBlendShare = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("6ee731e02404e154694005c2442f2bf4"), typeof(Texture)) as Texture;


            avatarDes = Selection.activeGameObject?.GetComponent<VRCAvatarDescriptor>();

            lastAvatar = null;
            if (_addonData.m_BlendShapeData != null && _addonData.fbxData != null)
            {
                string hash = FbxDataSO.CalculateHash(_addonData.m_BlendShapeData.m_Original);
                fbxData = addonData.fbxData.GetFBXData(hash);


            }
        }

        public void ShowGUI(EditorWindow editor)
        {

            //Show Banner
            if (_addonData.bannerIcon != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace(); // Pushes the label to the center
                GUILayout.Label(_addonData.bannerIcon, GUILayout.Width(196), GUILayout.Height(49));
                GUILayout.FlexibleSpace(); // Pushes the label to the center
                GUILayout.EndHorizontal();
            }


            langID = EditorGUILayout.Popup(lm.GetLocalizedValue("language"), langID, LocalizationManager.GetLanguages());
            if ((int)_addonData.usingLang != langID)
            {
                lm.LoadLocalizedText(langID);

                _addonData.usingLang = (LocalizationManager.Language)langID;
                EditorUtility.SetDirty(_addonData);
            }


#if !ENABLE_BS
            EditorGUILayout.HelpBox("BlendShape Share not installed", MessageType.Error);
            return;
#elif ENABLE_MA



            if (_addonData.m_BlendShapeData == null)
            {
                EditorGUILayout.HelpBox("Blenshape data is missing!", MessageType.Error);
                return;
            }

            if(_addonData.fbxData != null)
            {
                if (fbxData != null)
                {
                    EditorGUILayout.LabelField(String.Format(lm.GetLocalizedValue("fbx_ver"), fbxData.m_Version));
                }
                else
                {
                    EditorGUILayout.HelpBox(lm.GetLocalizedValue("fbx_unknown"), MessageType.Warning);
                }
            }



            EditorGUILayout.LabelField(String.Format(lm.GetLocalizedValue("ft_ver"), _addonData.version));



            EditorGUI.BeginChangeCheck();
            avatarDes = EditorGUILayout.ObjectField(new GUIContent(lm.GetLocalizedValue("avatar"), lm.GetLocalizedValue("drag_here")), avatarDes, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;


            EditorGUILayout.Separator();

            if (EditorGUI.EndChangeCheck() && avatarDes != null)
            {
                Transform ftAddon = avatarDes.transform.Find("Face Tracking");
                if (ftAddon != null)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.ObjectField(lm.GetLocalizedValue("ft_addon"), ftAddon.gameObject, typeof(GameObject), true);



                    if (lastAvatar == avatarDes)
                    {

                        EditorGUILayout.HelpBox(lm.GetLocalizedValue("ft_installed"), MessageType.Info);
                        EditorGUILayout.EndVertical();
                        return;
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(lm.GetLocalizedValue("ft_exist"), MessageType.Warning);

                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Separator();
                }


                avatarParametersUsage = ParameterHelper.GetAvatarParameterMemoryUsed(avatarDes);
            }

            EditorGUILayout.TextArea(string.Format(lm.GetLocalizedValue("memory_cost"), avatarParametersUsage));
            if (avatarDes == null)
            {
                EditorGUILayout.HelpBox(lm.GetLocalizedValue("drag_here"), MessageType.Info);
                lastAvatar = null;
            }
            EditorGUILayout.Separator();

            EditorGUI.BeginDisabledGroup(avatarDes == null);
            int eye = _addonData.eyesInstallers[eyeSelection].cost;
            int mouth = _addonData.mouthInstallers[lipSelection].cost;

            GUILayout.BeginHorizontal();
            eyeSelection = EditorGUILayout.Popup(lm.GetLocalizedValue("eye_tracking"), eyeSelection, _addonData.eyesInstallers.Select(i => i.name).ToArray());
            EditorGUILayout.PrefixLabel(string.Format(lm.GetLocalizedValue("memory_cost_title"), eye));
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            lipSelection = EditorGUILayout.Popup(lm.GetLocalizedValue("lip_tracking"), lipSelection, _addonData.mouthInstallers.Select(i => i.name).ToArray());
            EditorGUILayout.PrefixLabel(string.Format(lm.GetLocalizedValue("memory_cost_title"), mouth));

            GUILayout.EndHorizontal();

            int totalCost = avatarParametersUsage + eye + mouth + 1;

            var current = GUI.backgroundColor;
            if (totalCost > 256)
            {
                GUI.backgroundColor = Color.red;
                //GUI.contentColor = Color.red;
            }

            if (avatarDes != null)
            {
                EditorGUILayout.Separator();

                GUILayout.BeginHorizontal(GUI.skin.box);
                EditorGUILayout.LabelField(string.Format(lm.GetLocalizedValue("ft_cost"), eye + mouth + 1));

  
                EditorGUILayout.LabelField(string.Format(lm.GetLocalizedValue("total"), totalCost));
                

                GUILayout.EndHorizontal();

            }

            if(totalCost > 256)
            {
                EditorGUILayout.HelpBox(lm.GetLocalizedValue("memory_not_enough") + "\n" + string.Format(lm.GetLocalizedValue("synced_limit"), totalCost), MessageType.Warning);
            }



            // apply button
            EditorGUILayout.Separator();

            EditorGUI.BeginDisabledGroup(eyeSelection==0 && lipSelection ==0);
            GUILayout.BeginHorizontal();

            bool isPrefab = avatarDes != null && AssetDatabase.Contains(avatarDes.transform);
            bool apply = false;
            using (new EditorGUI.DisabledGroupScope(isPrefab))
            {
                apply = GUILayout.Button(lm.GetLocalizedValue("ft_make"));
            }

            bool applyAsNew = GUILayout.Button(lm.GetLocalizedValue("ft_make_new"));
            GUI.backgroundColor = current;




            if (apply || applyAsNew)
            {


                string suffix = $"[{_addonData.eyesInstallers[eyeSelection].name}+{_addonData.mouthInstallers[lipSelection].name}]";

                var result = MakeFaceTrackingAvatar(
                    _addonData.eyesInstallers[eyeSelection].prefab,
                    _addonData.mouthInstallers[lipSelection].prefab,
                    suffix, avatarDes, _addonData, applyAsNew);

                EditorGUIUtility.PingObject(result);
                lastAvatar = avatarDes;


            }
            if (apply)
            {
                EditorUtility.DisplayDialog(lm.GetLocalizedValue("done"), lm.GetLocalizedValue("ft_installed"), "OK");
            }


            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndDisabledGroup();

            //

            if (avatarDes != null && avatarDes.gameObject.TryGetComponent(out VRC.Core.PipelineManager pipelineManager) &&
                pipelineManager.blueprintId != "")
            {
                EditorGUILayout.Separator();
                EditorGUILayout.HelpBox(lm.GetLocalizedValue("blueprint_warning"), MessageType.Warning);
            }


            GUILayout.FlexibleSpace();
            if (bannerIconBlendShare != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace(); // Pushes the label to the center

                GUILayout.Label(new GUIContent("Supported by", "The blndshapes in this product are extracted and distributed using BlendShape Share."));

                GUILayout.Label(bannerIconBlendShare, GUILayout.Height(30), GUILayout.Width(120));

               
                GUILayout.EndHorizontal();
                EditorGUILayout.Separator();

            }
#else
            EditorGUILayout.HelpBox("Modular Avatar not installed", MessageType.Error);
#endif
        }


#if ENABLE_BS
        public static GameObject DuplicateGameObject(GameObject source)
        {
            if (AssetDatabase.Contains(source))
            {
                return PrefabUtility.InstantiatePrefab(source) as GameObject;
            }


            UnityEngine.Object[] lastSelection = Selection.objects;


            Selection.activeGameObject = source;
            //Selection.objects = new UnityEngine.Object[]{source};


            Unsupported.DuplicateGameObjectsUsingPasteboard();
            Selection.objects = lastSelection;


            GameObject[] rootGameObjects = source.scene.GetRootGameObjects();
            int index = System.Array.IndexOf(rootGameObjects, source);
            if (index < 0)
            {
                index = rootGameObjects.Length - 1;
            }
            else
            {
                index += 1;
            }

            return rootGameObjects[index];
        }



     
        Transform MakeFaceTrackingAvatar(GameObject eyeTrackingPrefab, GameObject lipTrackingPrefab, string suffix, VRCAvatarDescriptor avatarDes, FaceTrackingAddonData ADDON_DATA, bool duplicate = true)
        {
            if (avatarDes == null)
            {
                return null;
            }

            Transform target;
            if (duplicate)
            {
                target = DuplicateGameObject(avatarDes.gameObject).transform;
                target.gameObject.SetActive(true);
                Undo.RegisterCreatedObjectUndo(target.gameObject, "Create face tracking avatar");
            }
            else
            {
                target = avatarDes.transform;
                Undo.RecordObject(target.gameObject, "Add face tracking addon to avatar");
            }

            if (!target.name.Contains("-FaceTracking"))
                target.name = avatarDes.gameObject.name + "-FaceTracking" + suffix;

            ADDON_DATA.MeshAssets.ApplyMesh(target);

            GameObject ftRootObject = new GameObject("Face Tracking");
            ftRootObject.transform.parent = target;

            PrefabUtility.InstantiatePrefab(ADDON_DATA.menuInstaller, ftRootObject.transform);

            if (eyeTrackingPrefab != null)
                PrefabUtility.InstantiatePrefab(eyeTrackingPrefab, ftRootObject.transform);

            if (lipTrackingPrefab != null)
                PrefabUtility.InstantiatePrefab(lipTrackingPrefab, ftRootObject.transform);


            Undo.RegisterCreatedObjectUndo(ftRootObject, "Add face tracking addon to avatar");




            VRCAvatarDescriptor avatarDescriptor = target.GetComponent<VRCAvatarDescriptor>();
            Undo.RecordObject(avatarDescriptor, "Change eyelidType to None");
            avatarDescriptor.customEyeLookSettings.eyelidType = VRCAvatarDescriptor.EyelidType.None;
            PrefabUtility.RecordPrefabInstancePropertyModifications(avatarDescriptor);
            return target;
        }
        
#endif
    }
}

