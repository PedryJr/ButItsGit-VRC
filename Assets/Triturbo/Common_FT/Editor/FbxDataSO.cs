using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace Triturbo.FaceTrackingAddon{
    [CustomEditor(typeof(FbxDataSO))]
    public class FbxDataSOEditor : Editor
    {
        string sha256 = "";
        UnityObject obj = null;
        public override void OnInspectorGUI()
        {
            obj = EditorGUILayout.ObjectField("FBX", obj, typeof(UnityObject), false);

            if (GUILayout.Button("Calculate Hash"))
            {
                sha256 = FbxDataSO.CalculateHash(obj);
            }
            GUILayout.TextField(sha256);
            DrawDefaultInspector();
        }

    }

    [CreateAssetMenu(fileName = "FbxData", menuName = "TriturboFaceTrackingAddon/FbxData")]

    public class FbxDataSO : ScriptableObject
    {
        public FBXData[] m_Datas;

        public FBXData GetFBXData(string sha256)
        {
            return m_Datas.FirstOrDefault(fbx => fbx.m_Sha256 == sha256);
        }

        public string GetFBXVersion(string sha256, string unknown = "custom edit")
        {
            var data = GetFBXData(sha256);
            if(data != null)
            {
                return data.m_Version;
            }
            return unknown;
        }

        public static string CalculateHash(UnityObject obj)
        {
            if(obj == null)
            {
                return "";
            }

            string filePath = AssetDatabase.GetAssetPath(obj);

            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hashBytes).ToLower().Replace("-", "");
                }
            }
        }



    }

    [System.Serializable]
    public class FBXData
    {
        public string m_Version;

        public string m_Sha256;
    }

}

