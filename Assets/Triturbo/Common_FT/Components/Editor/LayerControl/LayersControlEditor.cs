using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

using VRC.SDK3.Avatars.Components;

namespace Triturbo.FaceTrackingAddon.LayersControl
{
    [CustomEditor(typeof(Runtime.LayersControlComponent))]
    public class LayersControlEditor : Editor
    {
        public Texture banner;
        public Transform avatarRoot;
        public RuntimeAnimatorController controller;
        public AnimatorLayer[] layers;

        public SerializedProperty layerProp;
        public SerializedProperty animationsProp;
        public SerializedProperty parameterProp;
        public SerializedProperty blendDurationProp;
        public SerializedProperty excludeLayersProp;

        public SerializedProperty blendShapeBanListProp;
        public SerializedProperty removeBlendShapesInBanListProp;
        //public UnityEditorInternal.ReorderableList blendShapeBanListRL;
        private void OnHierarchyChanged()
        {
            var component = target as Runtime.LayersControlComponent;
            avatarRoot = GetRootParent(component.transform);

            controller = avatarRoot?.GetComponent<VRCAvatarDescriptor>()?.baseAnimationLayers[component.LayerIndex].animatorController;
            layers = GetTargetLayers(component, controller as AnimatorController);
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

        private void OnDestroy()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }
        void OnEnable()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;

            banner = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("c6c0a1f62c034f348b808ece613f7ee8"), typeof(Texture)) as Texture;
            layerProp = serializedObject.FindProperty(nameof(Runtime.LayersControlComponent.m_Layer));
            animationsProp = serializedObject.FindProperty(nameof(Runtime.LayersControlComponent.m_Animations));
            parameterProp = serializedObject.FindProperty(nameof(Runtime.LayersControlComponent.m_Parameter));
            blendDurationProp = serializedObject.FindProperty(nameof(Runtime.LayersControlComponent.m_BlendDuration));
            excludeLayersProp = serializedObject.FindProperty(nameof(Runtime.LayersControlComponent.m_ExcludeLayers));
            blendShapeBanListProp = serializedObject.FindProperty(nameof(Runtime.LayersControlComponent.m_BlendShapeBanList));
            removeBlendShapesInBanListProp = serializedObject.FindProperty(nameof(Runtime.LayersControlComponent.m_RemoveBlendShapesInBanList));
            var component = target as Runtime.LayersControlComponent;
            avatarRoot = GetRootParent(component.transform);

            controller = avatarRoot?.GetComponent<VRCAvatarDescriptor>()?.baseAnimationLayers[component.LayerIndex].animatorController;
            layers = GetTargetLayers(component, controller as AnimatorController);

            //blendShapeBanListRL = new UnityEditorInternal.ReorderableList(serializedObject, removeBlendShapesInBanListProp, true, true, true, true);
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

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(layerProp);

            if (controller != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(controller, typeof(RuntimeAnimatorController), false);
                EditorGUI.EndDisabledGroup();
            }
            else
                EditorGUILayout.Separator();


            GUIContent animationsLabel = new GUIContent("Animations", "Animations to find layers");

            EditorGUILayout.PropertyField(animationsProp, animationsLabel);
            


            if (EditorGUI.EndChangeCheck())
            {
                var component = target as Runtime.LayersControlComponent;
                controller = avatarRoot?.GetComponent<VRCAvatarDescriptor>()?.baseAnimationLayers[layerProp.intValue].animatorController;
                layers = GetTargetLayers(component, controller as AnimatorController);
            }

            EditorGUILayout.PropertyField(parameterProp);
            EditorGUILayout.PropertyField(blendDurationProp);


            EditorGUILayout.PropertyField(removeBlendShapesInBanListProp);
            if (removeBlendShapesInBanListProp.boolValue)
            {
                EditorGUILayout.PropertyField(blendShapeBanListProp);
            }


            serializedObject.ApplyModifiedProperties();

            if (layers == null || layers.Length == 0)
            {
                EditorGUILayout.PropertyField(excludeLayersProp);

                EditorGUILayout.HelpBox("No Layer Match", MessageType.Warning);
                return;
            }

            EditorGUILayout.Separator();

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 50;
    
            EditorGUILayout.LabelField("Index");
            EditorGUILayout.LabelField("Name");

            GUIContent label = new GUIContent("Excluded");
            EditorGUILayout.LabelField(label, GUILayout.Width(GUI.skin.label.CalcSize(label).x));
            EditorGUILayout.EndHorizontal();

            bool toggleChanged = false;
            List<int> excludedLayers = new List<int>();

            foreach (var layer in layers)
            {
                EditorGUIUtility.labelWidth = 50;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(layer.index.ToString());
                EditorGUILayout.LabelField(layer.layerName);

               
                EditorGUI.BeginChangeCheck();
                bool excluded = IsExclude(layer.index);
                excluded = EditorGUILayout.Toggle(excluded, GUILayout.Width(GUI.skin.label.CalcSize(label).x));

                if (excluded)
                {
                    excludedLayers.Add(layer.index);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    toggleChanged = true;
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            if(toggleChanged && excludedLayers != null)
            {
                excludeLayersProp.ClearArray();
                for (int i = 0; i < excludedLayers.Count; i++)
                {
                    excludeLayersProp.InsertArrayElementAtIndex(i);
                    excludeLayersProp.GetArrayElementAtIndex(i).intValue = excludedLayers[i];
                }
                serializedObject.ApplyModifiedProperties();
            }

        }

        private bool IsExclude(int index)
        {
            for (int i = 0; i < excludeLayersProp.arraySize; i++)
            {
                if (excludeLayersProp.GetArrayElementAtIndex(i).intValue == index)
                {
                    return true;
                }
            }
            return false;
        }


        static public AnimatorLayer[] GetTargetLayers(Runtime.LayersControlComponent component, AnimatorController animator)
        {
            if (animator == null)
            {
                return null;
            }

            List<AnimatorLayer> matchingLayers = new List<AnimatorLayer>();

            for (int i = 0; i < animator.layers.Length; i++)
            {
                AnimatorStateMachine stateMachine = animator.layers[i].stateMachine;

                foreach (ChildAnimatorState state in stateMachine.states)
                {
                    // Check if the state's motion is an AnimationClip and if it is in the list of animations
                    if (state.state.motion is AnimationClip animationClip && component.m_Animations.Contains(animationClip))
                    {
                        matchingLayers.Add(new AnimatorLayer() { index = i, layerName = animator.layers[i].name});
                        break;
                    }
                }
            }

            return matchingLayers.ToArray();
        }



        public struct AnimatorLayer
        {
            public int index;
            public string layerName;
        }
    }
}


