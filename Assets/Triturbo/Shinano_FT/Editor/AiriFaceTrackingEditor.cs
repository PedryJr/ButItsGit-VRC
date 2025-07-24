using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace Triturbo.FaceTrackingAddon {
    public class ShinanoFaceTrackingEditor : EditorWindow
    {

        FaceTrackingInstaller ftInstaller;

        [MenuItem("TriturboFT/Shinano FT")]
        public static void ShowWindow()
        {
            GetWindow<ShinanoFaceTrackingEditor>("Shinano FT");
        }

        private void OnEnable()
        {
            FaceTrackingAddonData addonData = FaceTrackingAddonData.Load("Assets/Triturbo/Shinano_FT/Editor/Assets/Settings.asset", "f83d6c675adcd7e48bc81ed40806c3f8");

            ftInstaller = new FaceTrackingInstaller(addonData);
        }

        private void OnGUI()
        {
            ftInstaller.ShowGUI(this);
        }

    }


}


