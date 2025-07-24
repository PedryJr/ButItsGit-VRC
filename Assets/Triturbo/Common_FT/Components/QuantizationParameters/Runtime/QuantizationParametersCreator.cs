using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using System.Linq;
using System;

#if ENABLE_MA
using nadena.dev.modular_avatar.core;
#else
using VRC.SDKBase;
#endif


namespace Triturbo.QuantizationParameters.Runtime
{

    [AddComponentMenu("TriturboFT/Quantization Parameters Creator")]
    public class QuantizationParametersCreator :
#if ENABLE_MA
        AvatarTagComponent
#else
        MonoBehaviour, IEditorOnly
#endif
    {
        public QuantizationParametersSO m_QuantizationParameters;
        public QuantizationParameterOverrideSettings[] m_OverrideSettings;
        public QuantizationParameterDriverConfig[] m_QuantizationParameterDrivers;

        public UnityEngine.Object[] m_LocalBlendTrees;
        public UnityEngine.Object[] m_RemoteBlendTrees;

        public float[] m_RemoteSmoothnessList;
        public float[] m_LocalSmoothnessList;

        //public string prefix;

        public List<QuantizationParameterSettings> GetParameterSettingsList()
        {
            List<QuantizationParameterSettings> parameterSettings = new List<QuantizationParameterSettings>();
            if (m_QuantizationParameters == null)
            {
                return parameterSettings;
            }

            string outputPrefix = m_QuantizationParameters.m_Prefix;
            for (int i = 0; i < m_QuantizationParameters.m_Parameters.Length; i++)
            {

                var param = m_QuantizationParameters.m_Parameters[i];


                var overrideSettings = m_OverrideSettings.FirstOrDefault(p => p.m_ParameterName == param.m_ParameterName);

                int resolution = overrideSettings != null ? overrideSettings.m_Resolution : param.m_Resolution;
                bool combinedLocal = overrideSettings != null ? overrideSettings.m_CombinedLocal : false;
                bool combinedRemote = overrideSettings != null ? overrideSettings.m_CombinedRemote : false;

                float scaleFactor = overrideSettings != null ? overrideSettings.m_ScaleFactor : 1;

                string localSmoother = GetValueSafe(param.m_LocalSmootherIndex, m_QuantizationParameters.m_LocalSmoothers, "LocalSmoother");
                float localSmoothness = GetValueSafe(param.m_LocalSmootherIndex, m_LocalSmoothnessList, 0.08f);

                string remoteSmoother = GetValueSafe(param.m_RemoteSmootherIndex, m_QuantizationParameters.m_RemoteSmoothers, "RemoteSmoother");
                float remoteSmoothness = GetValueSafe(param.m_RemoteSmootherIndex, m_RemoteSmoothnessList, 0.7f);


  
                parameterSettings.Add(new QuantizationParameterSettings() {

                    parameterName = param.m_ParameterName,
                    subParameters = param.m_SubParameters,
                    defaultValue = param.m_DefaultValue,
                    symmetric = param.m_Symmetric,
                    localSmoother = localSmoother,
                    remoteSmoother = remoteSmoother,

                    //Override settings
                    combinedLocal = combinedLocal,
                    combinedRemote = combinedRemote,

                    remoteSmoothness = remoteSmoothness,
                    localSmoothness = localSmoothness,
                    scaleFactor = scaleFactor,
                    resolution = resolution,
                });



            }
            return parameterSettings;
        }


        private T GetValueSafe<T>(int index, T[] array, T defaultValue)
        {
            if (array == null) return defaultValue;

            return index >= 0 && index < array.Length ? array[index] : defaultValue;
        }


    }



    [System.Serializable]
    public class QuantizationParameterDriverConfig
    {
        public string m_Target;
        public string m_Controller;
        public string m_Toggle;
    }

    [System.Serializable]
    public class QuantizationParameterOverrideSettings
    {
        public string m_ParameterName;
        public int m_Resolution;
        public float m_ScaleFactor = 1f;
        public bool m_CombinedLocal = false;
        public bool m_CombinedRemote = false;
    }

    public class QuantizationParameterSettings
    {
        public string[] GetParameters(string prefix = "")
        {

            if(subParameters.Length > 0)
            {
                return subParameters.Select(s => prefix + s).ToArray();

            }

            return new string[] { prefix + parameterName };
        }


        public Dictionary<string, string[]> GetInputOutputPairs(bool isLocal, string inputPrefix = "", string outputPrefix = "")
        {
            Dictionary<string, string[]> parameterDict = new Dictionary<string, string[]>();  
            if(isLocal && combinedLocal)
            {
                parameterDict.Add(inputPrefix + parameterName, GetParameters(outputPrefix));
                return parameterDict;
            }

            if (!isLocal && combinedRemote)
            {
                parameterDict.Add(inputPrefix + parameterName, GetParameters(outputPrefix));
                return parameterDict;
            }

            foreach(string p in GetParameters())
            {
                parameterDict.Add(inputPrefix + p, new string[] { outputPrefix + p });
            }

            return parameterDict;
        }


        public string parameterName;
        public string[] subParameters;



        public bool combinedLocal;
        public bool combinedRemote;


        public float defaultValue;
        public float scaleFactor;

        public bool symmetric;
        public int resolution;




        public string localSmoother;
        public string remoteSmoother;

        public float remoteSmoothness;
        public float localSmoothness;

        public string[] BinaryParameters
        {
            get
            {
                if(resolution < 1)
                {
                    return new string[0];
                }
                string[] binary = new string[resolution + (symmetric ? 1 : 0)];
                for (int i = 0; i < resolution; i++)
                {
                    binary[i] = parameterName + Mathf.Pow(2, i).ToString();
                }

                if (symmetric)
                {
                    binary[resolution] = parameterName + "Negative";

                }

                return binary;
            }

        }

        public float[] BinaryValues
        {
            get
            {
                float[] binary = new float[resolution + (symmetric ? 1 : 0)];

                if (symmetric)
                {
                    binary[resolution] = defaultValue < 0 ? 1 : 0;
                }


                float f = Math.Abs(defaultValue);


                // Multiply the float by 2 and extract the integer part
                for (int i = 0; i < resolution; i++)
                {
                    f *= 2;
                    binary[resolution - i - 1] = (float)Math.Floor(f);
                    f -= (float)Math.Floor(f);
                }

                return binary;

            }
        }


    }




}


