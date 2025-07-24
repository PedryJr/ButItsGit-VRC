using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using VRC.Core;
using VRC.SDK3A.Editor;
using VRC.SDKBase.Editor;

namespace Triturbo.QuantizationParameters{
    public class DeleteOscConfig
    {
        [InitializeOnLoadMethod]
        public static void RegisterCallback()
        {
            VRCSdkControlPanel.OnSdkPanelEnable += OnSdkPanelEnable;
        }

        private static void OnSdkPanelEnable(object sender, EventArgs args)
        {
            if (VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder))
            {
                builder.OnSdkUploadSuccess += OnSdkUploadSuccess;
            }
        }

        private static void OnSdkUploadSuccess(object sender, string avatarId)
        {
            Debug.Log($"[TriturboFT] Upload success. Try to remove OSC config");
            TryDeleteOscConfigFile(avatarId);
        }


        public static bool TryDeleteOscConfigFile(string avatarId)
        {
            if (string.IsNullOrEmpty(avatarId)) return false;
            if (!APIUser.IsLoggedIn) return false;

            var userId = APIUser.CurrentUser.id;
            if (ContainsPathTraversalElements(userId) || ContainsPathTraversalElements(avatarId))
            {
                // Prevent the remote chance of a path traversal
                return false;
            }

            var endbit = $"/VRChat/VRChat/OSC/{userId}/Avatars/{avatarId}.json";
            var oscConfigFile = $"{VRC_SdkBuilder.GetLocalLowPath()}{endbit}";
            var printLocation = $"%LOCALAPPDATA%Low{endbit}"; // Doesn't print the account name to the logs
            if (!File.Exists(oscConfigFile)) return false;

            var fileAttributes = File.GetAttributes(oscConfigFile);
            if (fileAttributes.HasFlag(FileAttributes.Directory)) return false;

            try
            {
                File.Delete(oscConfigFile);
                Debug.Log($"[TriturboFT] Removed the OSC config file located at {printLocation}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TriturboFT] Failed to remove the OSC config file at {printLocation}; {e.ToString()}");
                return false;
            }

            return true;
        }

        private static bool ContainsPathTraversalElements(string susStr)
        {
            return susStr.Contains("/") || susStr.Contains("\\") || susStr.Contains(".") || susStr.Contains("*");
        }
    }
}