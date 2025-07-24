using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Triturbo.QuantizationParameters.Runtime
{
    [CreateAssetMenu(menuName = "TriturboFaceTrackingAddon/Quantization Parameters")]

    public class QuantizationParametersSO : ScriptableObject
    {
        public string m_Prefix;
        public QuantizationParameter[] m_Parameters;

        public string[] m_LocalSmoothers;
        public string[] m_RemoteSmoothers;
        public ParametersPair[] m_ParametersPairs;

        [System.Serializable]
        public class QuantizationParameter
        {

            public string m_ParameterName;
            public bool m_Symmetric;

            public int m_Resolution;
            public float m_DefaultValue;
            public string[] m_SubParameters;

            public int m_LocalSmootherIndex;
            public int m_RemoteSmootherIndex;

        }

        [System.Serializable]
        public class ParametersPair
        {
            public string[] m_Parameters;
        }


    }
}


