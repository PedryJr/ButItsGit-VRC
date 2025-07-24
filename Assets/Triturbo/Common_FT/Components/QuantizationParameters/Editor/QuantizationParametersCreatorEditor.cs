using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using nadena.dev.modular_avatar.core;
using UnityEditorInternal;
using System;
using VRC.SDK3.Avatars.Components;
using nadena.dev.ndmf;

namespace Triturbo.QuantizationParameters
{
    [CustomEditor(typeof(Runtime.QuantizationParametersCreator))]

    public class QuantizationParametersCreatorEditor : Editor
    {

        public Runtime.QuantizationParametersCreator component;
        public ReorderableList quantizationParameterDriverList;

        public BlendTreeList localBlendTreeList;
        public BlendTreeList remoteBlendTreeList;

        public SerializedProperty soProp;

        public SerializedProperty overrideSettingsProp;
        public SerializedProperty remoteSmoothnessProp;
        public SerializedProperty localSmoothnessProp;

        public bool expandAdvanced = false;
        bool showQuanParamDriverList = false;
        public Texture banner;        public Transform root;        public int totalAvatarParams = 0;
        private void DrawElement(Rect rect, int index, bool isActive, bool isFocus)
        {
            var element = quantizationParameterDriverList.serializedProperty.GetArrayElementAtIndex(index);

            var targetProp = element.FindPropertyRelative(nameof(Runtime.QuantizationParameterDriverConfig.m_Target));
            var controllerProp = element.FindPropertyRelative(nameof(Runtime.QuantizationParameterDriverConfig.m_Controller));

            var toggleProp = element.FindPropertyRelative(nameof(Runtime.QuantizationParameterDriverConfig.m_Toggle));

            overrideSettingsProp = serializedObject.FindProperty(nameof(Runtime.QuantizationParametersCreator.m_OverrideSettings));
            string[] options = new string[overrideSettingsProp.arraySize];
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = overrideSettingsProp.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(Runtime.QuantizationParameterOverrideSettings.m_ParameterName)).stringValue;
            }

            int current = Array.IndexOf(options, targetProp.stringValue);
            var aRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            int selectedIndex = EditorGUI.Popup(aRect, "Target", current, options);
            aRect.y += EditorGUIUtility.singleLineHeight;
            if (selectedIndex >= 0 && selectedIndex < options.Length)
            {
                targetProp.stringValue = options[selectedIndex];
            }

            EditorGUI.PropertyField(aRect, controllerProp);
            aRect.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.PropertyField(aRect, toggleProp);
        }


        public static Transform FindAvatarInParents(Transform target)
        {
            while (target != null)
            {
                if (target.GetComponent<VRCAvatarDescriptor>()) return target;
                target = target.parent;
            }
            return null;
        }


        private void UpdateParametersUsage()
        {
            root = FindAvatarInParents((target as Component).transform);
            if(root == null)
            {
                totalAvatarParams = 0;
                return;
            }
            totalAvatarParams = ParameterInfo.ForUI.GetParametersForObject(root.gameObject).Sum(p => p.BitUsage);
        }
        public void OnEnable()
        {
            component = target as Runtime.QuantizationParametersCreator;

            UpdateParametersUsage();

            InitOverrideSettings(component);

            soProp = serializedObject.FindProperty(nameof(Runtime.QuantizationParametersCreator.m_QuantizationParameters));

            overrideSettingsProp = serializedObject.FindProperty(nameof(Runtime.QuantizationParametersCreator.m_OverrideSettings));

            var drivers = serializedObject.FindProperty(nameof(Runtime.QuantizationParametersCreator.m_QuantizationParameterDrivers));

            quantizationParameterDriverList = new ReorderableList(serializedObject, drivers, true, true, true, true);
            quantizationParameterDriverList.drawElementCallback = DrawElement;
            quantizationParameterDriverList.drawHeaderCallback = (rect) => { EditorGUI.LabelField(rect, "Parameter Driver Configs"); };
            quantizationParameterDriverList.elementHeight = EditorGUIUtility.singleLineHeight * 3;

            localSmoothnessProp = serializedObject.FindProperty(nameof(Runtime.QuantizationParametersCreator.m_LocalSmoothnessList));
            remoteSmoothnessProp = serializedObject.FindProperty(nameof(Runtime.QuantizationParametersCreator.m_RemoteSmoothnessList));

            SerializedProperty localBlendTreesProp = serializedObject.FindProperty(nameof(Runtime.QuantizationParametersCreator.m_LocalBlendTrees));
            SerializedProperty remoteBlendTreesProp = serializedObject.FindProperty(nameof(Runtime.QuantizationParametersCreator.m_RemoteBlendTrees));

            localBlendTreeList = new BlendTreeList(serializedObject, localBlendTreesProp, "Merge BlendTree Local");
            remoteBlendTreeList = new BlendTreeList(serializedObject, remoteBlendTreesProp, "Merge BlendTree Remote");


            //prefixProp = serializedObject.FindProperty(nameof(Runtime.QuantizationParametersCreator.prefix));

            banner = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath("c6c0a1f62c034f348b808ece613f7ee8"), typeof(Texture)) as Texture;

            EditorApplication.hierarchyChanged += UpdateParametersUsage;
        }
        public void OnDestroy()
        {
            EditorApplication.hierarchyChanged -= UpdateParametersUsage;
        }
        private void InitOverrideSettings(Runtime.QuantizationParametersCreator component)
        {

            if (component.m_QuantizationParameters == null)
            {
                return;
            }

            HashSet<string> settings = component.m_OverrideSettings.Select(e => e.m_ParameterName).ToHashSet();
            HashSet<string> paramNames = component.m_QuantizationParameters.m_Parameters.Select(e => e.m_ParameterName).ToHashSet();

            if (!paramNames.Equals(settings))
            {
                List<Runtime.QuantizationParameterOverrideSettings> overrideSettings = component.m_OverrideSettings.ToList();
                foreach (var parameters in component.m_QuantizationParameters.m_Parameters)
                {
                    if (!settings.Contains(parameters.m_ParameterName))
                    {
                        overrideSettings.Add(new Runtime.QuantizationParameterOverrideSettings()
                        {
                            m_ParameterName = parameters.m_ParameterName,
                            m_Resolution = parameters.m_Resolution
                        });
                    }
                }

                component.m_OverrideSettings = overrideSettings
                    .Where(s => paramNames.Contains(s.m_ParameterName))
                    .GroupBy(s => s.m_ParameterName)
                    .Select(g => g.First())
                    .ToArray();
            }

            component.m_RemoteSmoothnessList = AdjustArrayLength(component.m_RemoteSmoothnessList, component.m_QuantizationParameters.m_RemoteSmoothers.Length);

            component.m_LocalSmoothnessList = AdjustArrayLength(component.m_LocalSmoothnessList, component.m_QuantizationParameters.m_LocalSmoothers.Length, 0.08f);


        }

        public static float[] AdjustArrayLength(float[] array, int length, float defaultValue = 0.7f)
        {
            if(array == null)
            {
                return null;
            }
            if (length > array.Length)
            {
                
                return array.Concat(Enumerable.Repeat(defaultValue, length - array.Length)).ToArray();
            }
            else if (length < array.Length)
            {
                return array.Take(length).ToArray();
            }
            // If length is equal to array.Length, return the array unchanged
            return array;
        }

        private void DrawRect(Color color, int height = 1, int padding = 4)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding * 2 + height));
            r.height = height;
            r.y += padding;
            //r.x -= 2;
            //r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
        public override void OnInspectorGUI()
        {
            if (banner != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace(); // Pushes the label to the center
                GUILayout.Label(banner, GUILayout.Width(196), GUILayout.Height(49));
                GUILayout.FlexibleSpace(); // Pushes the label to the center
                GUILayout.EndHorizontal();
            }


#if !ENABLE_MA
            EditorGUILayout.HelpBox($"Modular Avatar is either not imported or its version is lower than 1.9", MessageType.Error);
#endif

            EditorGUILayout.HelpBox($"Avatar Parameters: {totalAvatarParams} bits", totalAvatarParams > 256 ? MessageType.Warning : MessageType.Info);
            if(totalAvatarParams > 256)
            {
                EditorGUILayout.HelpBox("Parameters memory not enough!", MessageType.Error);
            }            DrawRect(Color.grey);

            if (component.m_QuantizationParameters == null)
            {
                DrawDefaultInspector();

                return;
            }
            else if(component.m_OverrideSettings == null || component.m_OverrideSettings.Length == 0)
            {
                InitOverrideSettings(component);
            }

            int totalBits = 0;
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < overrideSettingsProp.arraySize; i++)
            {
                var property = overrideSettingsProp.GetArrayElementAtIndex(i);
                var name = property.FindPropertyRelative(nameof(Runtime.QuantizationParameterOverrideSettings.m_ParameterName));

                var factor = property.FindPropertyRelative(nameof(Runtime.QuantizationParameterOverrideSettings.m_ScaleFactor));

                var resolution = property.FindPropertyRelative(nameof(Runtime.QuantizationParameterOverrideSettings.m_Resolution));
                var combined = property.FindPropertyRelative(nameof(Runtime.QuantizationParameterOverrideSettings.m_CombinedRemote));
                var combinedLocal = property.FindPropertyRelative(nameof(Runtime.QuantizationParameterOverrideSettings.m_CombinedLocal));
                var parameter = component.m_QuantizationParameters.m_Parameters.FirstOrDefault(p => p.m_ParameterName == name.stringValue);

                if (parameter == null)
                {
                    EditorGUILayout.HelpBox($"Override settings: {name.stringValue} not in Original Parameters", MessageType.Error);
                    continue;
                }

                bool hasNegative = parameter.m_Symmetric;
                int subParametersCount = parameter.m_SubParameters.Length;


                string[] subParameters = parameter.m_SubParameters;



                GUILayout.BeginVertical(GUI.skin.box);

                if (!combined.boolValue && subParametersCount > 0)
                {
                    GUILayout.BeginHorizontal();
                    foreach (string sub in subParameters)
                    {
                        GUILayout.Label(sub.Split('/').Last());
                    }
                    GUILayout.EndHorizontal();

                }
                else
                {
                    GUILayout.Label(name.stringValue.Split('/').Last());
                }

                GUILayout.EndVertical();


                EditorGUI.indentLevel++;



                if (subParametersCount > 0)
                {
                    GUILayout.BeginHorizontal();

                    EditorGUILayout.PropertyField(combined);
                    EditorGUILayout.PropertyField(combinedLocal);
                    GUILayout.EndHorizontal();

                }


                EditorGUILayout.IntSlider(resolution, 0, 8);

               // EditorGUILayout.Slider(remoteSmoothness, 0, 0.99f);

                if(factor.floatValue >= 2)
                {
                    EditorGUILayout.PropertyField(factor);
                }
                else
                {
                    EditorGUILayout.Slider(factor, 0, 2);
                }
                int bitsResolution = resolution.intValue;

                if (bitsResolution > 8) bitsResolution = 8;

                int mutiply = !combined.boolValue && (subParametersCount > 0) ? subParametersCount : 1;

                string usage = "Parameters: ";


                if (bitsResolution <= 0)
                {
                    usage = "Not synced";
                }
                if (hasNegative && bitsResolution < 8 && bitsResolution > 0)
                {

                    usage += $"{bitsResolution * mutiply} + {mutiply} bits";

                    totalBits += bitsResolution * mutiply + mutiply;

                }
                else if(bitsResolution > 0) 
                {
                    
                    usage +=  $"{bitsResolution * mutiply} bits";
                    totalBits += bitsResolution * mutiply;
                }
                

                EditorGUILayout.SelectableLabel(usage);

                EditorGUI.indentLevel--;


                EditorGUILayout.Separator();
            }

            if(EditorGUI.EndChangeCheck() && root != null)
            {
               totalAvatarParams = ParameterInfo.ForUI.GetParametersForObject(root.gameObject).Sum(p => p.BitUsage);
            }

            EditorGUILayout.HelpBox($"Parameters: {totalBits} bits", totalAvatarParams > 256 ? MessageType.Warning : MessageType.Info);

            EditorGUILayout.Separator();

            // Smoothness Settings
            DrawSmoothness("Default Local Smoothness", localSmoothnessProp, component.m_QuantizationParameters.m_RemoteSmoothers);
            EditorGUILayout.Separator();
            DrawSmoothness("Remote Smoothness", remoteSmoothnessProp, component.m_QuantizationParameters.m_RemoteSmoothers);

            DrawRect(Color.grey);

            //prefixProp.isExpanded = EditorGUILayout.Foldout(prefixProp.isExpanded, "Advanced");



            expandAdvanced = EditorGUILayout.Foldout(expandAdvanced, "Advanced");
            if (expandAdvanced)
            {
                EditorGUILayout.HelpBox("Danger! This area is not recommand to edit.", MessageType.Warning);


                EditorGUILayout.PropertyField(soProp);

                //EditorGUILayout.PropertyField(prefixProp);
                var quanParamDriverListProp = quantizationParameterDriverList.serializedProperty;
                
                if (quanParamDriverListProp.arraySize == 0 && !showQuanParamDriverList)
                {
                    if (GUILayout.Button("Add Parameter Driver"))
                    {
                        showQuanParamDriverList = true;
                    }
                }
                else
                {
                    quanParamDriverListProp.isExpanded = EditorGUILayout.Foldout(quanParamDriverListProp.isExpanded, "Parameter Driver Configs");
                    if (quanParamDriverListProp.isExpanded)
                    {
                        quantizationParameterDriverList.DoLayoutList();
                    }
                    else if(quanParamDriverListProp.arraySize == 0)
                    {
                        showQuanParamDriverList = false;
                        quanParamDriverListProp.isExpanded = true;
                    }
                }

                EditorGUILayout.Separator();

                localBlendTreeList.DoLayoutList();
                remoteBlendTreeList.DoLayoutList();
            }

            serializedObject.ApplyModifiedProperties();
        }

        public void DrawSmoothness(string label, SerializedProperty smoothnessList,  string[] smootherList)
        {
            if(smoothnessList == null || smootherList == null || smootherList.Length == 0)
            {
                return;
            }
            GUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField(label);
            EditorGUI.indentLevel++;
           
            for (int i = 0; i < smootherList.Length; i++)
            {
                if (smoothnessList.arraySize <= i) continue;
                var smoothnessProp = smoothnessList.GetArrayElementAtIndex(i);
                string name = smootherList[i];
                EditorGUILayout.Slider(smoothnessProp, 0, 0.999f, new GUIContent(name.Split('/').Last()));
                
            }

            EditorGUI.indentLevel--;

            GUILayout.EndVertical();

        }
    }


    public class BlendTreeList: ReorderableList
    {
        public BlendTreeList(SerializedObject serializedObject, SerializedProperty elements, string header): base(serializedObject, elements, true, true, true, true)
        {

            drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = elements.GetArrayElementAtIndex(index);
                var aRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.ObjectField(aRect, element, typeof(UnityEditor.Animations.BlendTree), GUIContent.none);
            };

            drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, header);
            };

            elementHeight = EditorGUIUtility.singleLineHeight;


        }

    }

}


