using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;


#if ENABLE_MA
using nadena.dev.modular_avatar.core;
#endif


namespace Triturbo.FaceTrackingAddon.EyeTrackingSettings.Runtime
{
    [HelpURL("https://triturbo.notion.site/Eye-Tracking-Settings-e3b483a97a9141bea75867901ca82540?pvs=4")]
    [AddComponentMenu("TriturboFT/Eye Tracking Settings")]
    [DisallowMultipleComponent]
    public class EyeTrackingSettingsComponent :
#if ENABLE_MA
        AvatarTagComponent
#else
        MonoBehaviour, IEditorOnly
#endif
    {
        //For user
        //public bool m_SyncWeight = false;
        public float m_EyeWeight = 1;
        public float m_ResetWeight = 0;


        //For creator

        //public AnimationClip m_ResetClipBoth;
        //public AnimationClip m_ResetClipLeft;
        //public AnimationClip m_ResetClipRight;


        

        public ResettingBlendshapesGroup[] m_ResettingGroups;
        //public VRCExpressionsMenu m_MenuWithWeight;
        //public VRCExpressionsMenu m_MenuWithoutWeight;

        public ExpressionBlendShapesMap m_ExpressionBlendShapes;
        public string m_BodyName = "Body";
        public string m_EyeWeightParameter = "FT/Control/EyeWeight";
        //public string m_ResetWeightParameter = "FT/Control/ResetWeight";



        //[System.NonSerialized]
        //public float previewEyeClosedStrength = 1;

        [System.NonSerialized]
        private SkinnedMeshRenderer body;

        [System.NonSerialized]
        public SkinnedMeshRenderer previewProxy;

        [System.NonSerialized]
        public bool isPreview = false;

        [System.NonSerialized]
        public PreviewMode previewMode = PreviewMode.BothEye;

        public SkinnedMeshRenderer Body
        {
            get
            {
                if (body == null)
                    body = GetRootParent(transform)?.Find("Body")?.GetComponent<SkinnedMeshRenderer>();

                return body;
            }
        }
        public SkinnedMeshRenderer GetBody()
        {
            body = GetRootParent(transform)?.Find("Body")?.GetComponent<SkinnedMeshRenderer>();
            return body;
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

    }
    public enum PreviewMode
    {
        BothEye,
        LeftEye,
        RightEye
    }


    [System.Serializable]
    public class ResettingBlendshapesGroup
    {
        public string m_GroupName;
        public string m_Parameter;
        public float m_weight;

        public AnimationClip m_ResetClipBoth;
        public AnimationClip m_ResetClipLeft;
        public AnimationClip m_ResetClipRight;
    }

}

