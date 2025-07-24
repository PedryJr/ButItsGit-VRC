using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;




using Triturbo.FaceTrackingAddon.EyeTrackingSettings.Runtime;
using VRC.SDK3.Avatars.ScriptableObjects;

#if ENABLE_MA
using nadena.dev.modular_avatar.core;
#endif

namespace Triturbo.FaceTrackingAddon.EyeTrackingSettings
{
    [CustomEditor(typeof(EyeTrackingSettingsComponent))]
    public class TriturboEyeTrackingEditor : Editor
    {
        public static Texture banner;
        EyeTrackingSettingsComponent component;
        public SerializedProperty eyeWeightProp;
        public SerializedProperty eyeWeightParameterProp;
        public SerializedProperty expressionBlendShapesProp;
        public SerializedProperty resettingGroupsProp;

        public Transform root;

        private static List<SkinnedMeshRenderer> previewProxys = new List<SkinnedMeshRenderer>();
        private bool showHiddenSettings = false;

        public float previewEyeClosedStrength = 1;
        public ResetBlendShapesGroup[] resetBlendShapesGroups;
        public struct ResetBlendShapesGroup
        {
            public string[] resetBlendShapes;
            public float[] initialWeights;

            public string[] resetBlendShapesLeft;
            public float[] initialWeightsLeft;

            public string[] resetBlendShapesRight;
            public float[] initialWeightsRight;

            public void SetInitialWeights(SkinnedMeshRenderer skinnedMeshRenderer)
            {
                initialWeights = GetInitialWeight(skinnedMeshRenderer, resetBlendShapes);
                initialWeightsLeft = GetInitialWeight(skinnedMeshRenderer, resetBlendShapesLeft);
                initialWeightsRight = GetInitialWeight(skinnedMeshRenderer, resetBlendShapesRight);
            }
        }
        public static Transform GetRootParent(Transform source)
        {
            Transform current = source;

            while (current.parent != null)
            {
                // Move up to the parent GameObject
                current = current.parent;
            }
            // Return the root parent GameObject
            return current;
        }


        void init(Runtime.EyeTrackingSettingsComponent component)
        {
            if (component.GetBody() == null) return;


            resetBlendShapesGroups = new ResetBlendShapesGroup[component.m_ResettingGroups.Length];

            for(int i =0; i < resetBlendShapesGroups.Length; i++)
            {
                resetBlendShapesGroups[i] = new ResetBlendShapesGroup()
                {
                    resetBlendShapes = GetBlendShapes(component.m_ResettingGroups[i].m_ResetClipBoth),
                    resetBlendShapesLeft = GetBlendShapes(component.m_ResettingGroups[i].m_ResetClipLeft),
                    resetBlendShapesRight = GetBlendShapes(component.m_ResettingGroups[i].m_ResetClipRight)
                };
            }
        }
        void OnEnable()
        {
            if(banner == null)
                banner = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("c6c0a1f62c034f348b808ece613f7ee8"), typeof(Texture)) as Texture;

            component = target as Runtime.EyeTrackingSettingsComponent;

            eyeWeightProp = serializedObject.FindProperty(nameof(Runtime.EyeTrackingSettingsComponent.m_EyeWeight));
            eyeWeightParameterProp = serializedObject.FindProperty(nameof(Runtime.EyeTrackingSettingsComponent.m_EyeWeightParameter));


            expressionBlendShapesProp = serializedObject.FindProperty(nameof(Runtime.EyeTrackingSettingsComponent.m_ExpressionBlendShapes));
            resettingGroupsProp = serializedObject.FindProperty(nameof(Runtime.EyeTrackingSettingsComponent.m_ResettingGroups));



            init(component);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        }
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                //Debug.Log("OnExitingEditMode");
                ExistPreview(component);
                ApplyParameters(onDestroy: true);
                //ApplyMenu();
            }
            //EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        void ExistPreview(Runtime.EyeTrackingSettingsComponent component)
        {
            component.isPreview = false;

            if (component.Body != null)
                component.Body.enabled = true;

            if (component.previewProxy != null)
            {
                DestroyImmediate(component.previewProxy.gameObject);
                component.previewProxy = null;
            }

            foreach(var p in previewProxys)
            {
                if(p != null) DestroyImmediate(p.gameObject);
            }
            previewProxys.Clear();

        }

        void OnPreview(Runtime.EyeTrackingSettingsComponent component)
        {
            if (component.previewProxy == null && component.Body != null)
            {
                component.previewProxy = Instantiate(component.Body);

                component.m_ExpressionBlendShapes.SplitExpressionWeight(component.previewProxy);

                component.previewProxy.gameObject.SetActive(true);
                component.previewProxy.enabled = true;
                component.previewProxy.name = "preview(press Delete to delete me)";

                component.previewProxy.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                component.previewProxy.hideFlags = HideFlags.NotEditable;
                component.Body.enabled = false;

                previewProxys.Add(component.previewProxy);

                //initialWeights = GetInitialWeight(component.previewProxy, resetBlendShapes);
                //initialWeightsLeft = GetInitialWeight(component.previewProxy, resetBlendShapesLeft);
                //initialWeightsRight = GetInitialWeight(component.previewProxy, resetBlendShapesRight);

                for(int i = 0; i < resetBlendShapesGroups.Length; i++)
                {
                    resetBlendShapesGroups[i].SetInitialWeights(component.previewProxy);
                }
            }

            if (component.previewProxy != null)
            {
                int blendShapeIndexRight = component.previewProxy.sharedMesh.GetBlendShapeIndex("EyeClosedRight");
                int blendShapeIndexLeft = component.previewProxy.sharedMesh.GetBlendShapeIndex("EyeClosedLeft");

                float eyeCloseLeft = 0;
                float eyeCloseRight = 0;


                float previewEyeClosedLeftStrength = 0;
                float previewEyeClosedRightStrength = 0;

                if (component.previewMode == PreviewMode.BothEye)
                {
                    eyeCloseLeft = component.m_EyeWeight * 100 * previewEyeClosedStrength;
                    eyeCloseRight = eyeCloseLeft;

                    previewEyeClosedLeftStrength =  previewEyeClosedStrength;
                    previewEyeClosedRightStrength = previewEyeClosedStrength;


                }
                else if(component.previewMode == PreviewMode.LeftEye)
                {
                    eyeCloseLeft = component.m_EyeWeight * 100 * previewEyeClosedStrength;

                    previewEyeClosedLeftStrength = previewEyeClosedStrength;
                }
                else if (component.previewMode == PreviewMode.RightEye)
                {

                    eyeCloseRight = component.m_EyeWeight * 100 * previewEyeClosedStrength;

                    previewEyeClosedRightStrength = previewEyeClosedStrength;
                }

                if (blendShapeIndexLeft != -1)
                {
                    component.previewProxy.SetBlendShapeWeight(blendShapeIndexLeft, eyeCloseLeft);
                }
                if (blendShapeIndexRight != -1)
                {
                    component.previewProxy.SetBlendShapeWeight(blendShapeIndexRight, eyeCloseRight);
                }

                for(int i = 0; i < resetBlendShapesGroups.Length; i++ )
                {
                    var group = resetBlendShapesGroups[i];
                    SetBlendShapesWeight(component.previewProxy, group.resetBlendShapes, group.initialWeights, previewEyeClosedStrength * component.m_ResettingGroups[i].m_weight);
                    SetBlendShapesWeight(component.previewProxy, group.resetBlendShapesLeft, group.initialWeightsLeft, previewEyeClosedLeftStrength * component.m_ResettingGroups[i].m_weight);
                    SetBlendShapesWeight(component.previewProxy, group.resetBlendShapesRight, group.initialWeightsRight, previewEyeClosedRightStrength * component.m_ResettingGroups[i].m_weight);
                }

            }

        }
         
        private void OnDestroy()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            if (component == null) return;

            ExistPreview(component);
            ApplyParameters(onDestroy: true);
        }
        public override void OnInspectorGUI()
        {
            //Show Banner
            if (banner != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace(); // Pushes the label to the center
                GUILayout.Label(banner, GUILayout.Width(196), GUILayout.Height(49));
                GUILayout.FlexibleSpace(); // Pushes the label to the center
                GUILayout.EndHorizontal();
            }

            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(component.Body == null);
            EditorGUI.BeginChangeCheck();


            //EyeClosed Weight Slider
            EditorGUILayout.Slider(eyeWeightProp, 0, 1, new GUIContent("Eye Weight", $"The weight of EyeClosed blendshape from face tracking."));

            //Reset Weight Slider

            
            for (int i = 0; i < resettingGroupsProp.arraySize; i++)
            {
                var property = resettingGroupsProp.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(Runtime.ResettingBlendshapesGroup.m_weight));
                string name = component.m_ResettingGroups[i].m_GroupName;
                EditorGUILayout.Slider(property , 0, 1, new GUIContent($"Reset {name}", $"The weight of blending expression blendshapes group: {name} to 0 when eye is closed."));
            }
            if (EditorGUI.EndChangeCheck())
            {
                component.isPreview = true;
            }

            Color defaultColor = GUI.backgroundColor;

            if(component.isPreview)
            {
                GUI.backgroundColor = Color.green;

            }


            if (GUILayout.Button(component.isPreview ? "End Preview" : "Preview"))
            {
                component.isPreview = !component.isPreview;
                init(component);
                ApplyParameters();
            }


            GUI.backgroundColor = defaultColor;
            EditorGUI.EndDisabledGroup();

            if (component.isPreview)
            {
                OnPreview(component);
                component.previewMode = (PreviewMode)EditorGUILayout.EnumPopup(component.previewMode);
                previewEyeClosedStrength = EditorGUILayout.Slider("Eye Closed", previewEyeClosedStrength, 0, 1);
            }
            else
            {
                ExistPreview(component);
                EditorGUILayout.Space();
            }

            //Show weight setting in expression menu settings
            EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.PrefixLabel(new GUIContent("Adjustable in game", "It will need to sync parameters over network, not recomanded"));
            //EditorGUILayout.PropertyField(syncWeightProp, GUIContent.none);



            //if (lastSyncWeight != syncWeightProp.boolValue)
            //{
            //    Debug.Log("Show In Game Menu (16 bits) edited");
            //    ApplyParameters();
            //    //ApplyMenu();
            //}
            //lastSyncWeight = syncWeightProp.boolValue;
            EditorGUILayout.EndHorizontal();
            //Show weight setting in expression menu settings End


            //Creator Settings
            showHiddenSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showHiddenSettings, "Creator Settings");
            if (showHiddenSettings)
            {
                EditorGUILayout.PropertyField(expressionBlendShapesProp);
                EditorGUILayout.PropertyField(eyeWeightParameterProp);


                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.PropertyField(resettingGroupsProp);
            }
            serializedObject.ApplyModifiedProperties();
        }

        void ApplyParameters(bool onDestroy = false)
        {
#if ENABLE_MA
            if (component == null) return;
            var map = component.GetComponent<ModularAvatarParameters>();
            if (map == null) return;

            Undo.RecordObject(map, "Apply MA Parameters");


            if(!onDestroy)
                serializedObject.ApplyModifiedProperties();

            PrefabUtility.RecordPrefabInstancePropertyModifications(component.ApplyParameters());
#endif
        }


        private void OnSceneGUI()
        {
            if (!component.isPreview) return;
            var sceneView = SceneView.currentDrawingSceneView;

            if (sceneView == null) return;
            Rect rect = sceneView.camera.pixelRect;

            Handles.BeginGUI();
            // Calculate the position for the bottom left
            float x = 10;
            float y = rect.height - 30; // Adjust 30 to place the label higher if needed
            float width = 200;
            float height = 20; // Height of the label

            GUILayout.BeginArea(new Rect(x, y, width, height));
            GUILayout.Label("Eye Tracking Preview");
            GUILayout.EndArea();
            Handles.EndGUI();
        }


        public void SetBlendShapesWeight(SkinnedMeshRenderer skinnedMeshRenderer, string[] targets, float[] initial, float weight)
        {
            if(skinnedMeshRenderer == null || targets == null || initial == null)
            {
                return;
            }
            for (int i = 0; i < targets.Length; i++)
            {
                int blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(targets[i]);

                skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, initial[i] * (1 - weight));
            }
        }
        public static float[] GetInitialWeight(SkinnedMeshRenderer skinnedMeshRenderer, string[] targets)
        {
            if (skinnedMeshRenderer == null)
            {
                return null;
            }
            float[] weight = new float[targets.Length];
            for (int i =0; i < targets.Length; i++)
            {
                int blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(targets[i]);
                weight[i] = skinnedMeshRenderer.GetBlendShapeWeight(blendShapeIndex);
            }
            return weight;
        }


        public string[] GetBlendShapes(AnimationClip clip)
        {
            if(clip == null)
            {
                return System.Array.Empty<string>();
            }
            // Get all the bindings (curves) of the animation clip
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            return bindings.Where(b => b.propertyName
                .StartsWith("blendShape."))
                .Select(b => b.propertyName.Substring("blendShape.".Length))
                .ToArray();
        }
    }


    static class EyeTrackingSettingExtension
    {

#if ENABLE_MA
        static public ModularAvatarParameters ApplyParameters(this EyeTrackingSettingsComponent component)
        {
            var map = component.GetComponent<ModularAvatarParameters>();
            if (map == null)
            {
                Debug.LogWarning("No MA Parameters");
                return null;
            }

            string eyeParameter = component.m_EyeWeightParameter;
            //string resetParameter = component.m_ResetWeightParameter;

            int eyeWeight = map.parameters.FindIndex(p => p.nameOrPrefix == eyeParameter);
            if (eyeWeight != -1)
            {
                var edited = map.parameters[eyeWeight];
                edited.defaultValue =  component.m_EyeWeight;
                edited.hasExplicitDefaultValue = true;
                edited.localOnly =  true;
                edited.syncType = ParameterSyncType.NotSynced;
                edited.saved = true;
                map.parameters[eyeWeight] = edited;
            }
            else
            {
                map.parameters.Add(new ParameterConfig() {
                    nameOrPrefix = eyeParameter,
                    defaultValue = component.m_EyeWeight,
                    hasExplicitDefaultValue = true,
                    localOnly = true,
                    syncType = ParameterSyncType.NotSynced,
                    saved = true
                });
            }


            foreach(var group in component.m_ResettingGroups)
            {
                 
                int resetWeight = map.parameters.FindIndex(p => p.nameOrPrefix == group.m_Parameter);
                if (resetWeight != -1)
                {
                    var edited = map.parameters[resetWeight];
                    edited.defaultValue = group.m_weight;
                    edited.hasExplicitDefaultValue = true;
                    edited.localOnly = true;
                    edited.syncType = ParameterSyncType.NotSynced;
                    edited.saved = true;
                    map.parameters[resetWeight] = edited;
                }
                else
                {
                    map.parameters.Add(new ParameterConfig()
                    {
                        nameOrPrefix = group.m_Parameter,
                        defaultValue = group.m_weight,
                        hasExplicitDefaultValue = true,
                        localOnly = true,
                        syncType = ParameterSyncType.NotSynced,
                        saved = true
                    });
                }
            }


            return map;
        }
#endif
    }

}

