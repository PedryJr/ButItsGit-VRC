using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using VRC.SDKBase;


#if ENABLE_MA
using nadena.dev.modular_avatar.core;
#endif


namespace Triturbo.FaceTrackingAddon.LayersControl.Runtime
{
    [HelpURL("https://triturbo.notion.site/LayerControl-1ab038b30276421eab63ac3ae78430b1")]

    [AddComponentMenu("TriturboFT/Layers Control")]
    public class LayersControlComponent :
#if ENABLE_MA
        AvatarTagComponent
#else
        MonoBehaviour, IEditorOnly
#endif

    {
        public AnimationClip[] m_Animations;
        public BasePlayableLayer m_Layer;

        public string m_Parameter;
        public float m_BlendDuration = 0.1f;
        public float m_GoalWeight = 0;

        public int[] m_ExcludeLayers;

        public string[] m_BlendShapeBanList;
        public bool m_RemoveBlendShapesInBanList;


        public bool IsExclude(int index)
        {
            return m_ExcludeLayers.Contains(index);
        }

        public int LayerIndex
        {
            get
            {
                return (int)m_Layer;
            }
        }


        public enum BasePlayableLayer
        {
            Base,
            Additive,
            Gesture,
            Action,
            FX
        }


    }




}


