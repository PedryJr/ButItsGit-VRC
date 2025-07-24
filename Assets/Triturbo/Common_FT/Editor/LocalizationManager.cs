using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;


namespace Triturbo.FaceTrackingAddon
{
    public class LocalizationManager
    {
        public enum Language
        {
            en,
            zh_t,
            zh_s,
            ja,
            ko
        }

        public static string[] GetLanguages()
        {
            return new string[] { "English", "繁體中文", "简体中文", "日本語", "한국어" };
        }

        private Dictionary<string, string> info;
        private string path;

        public Language Lang { get; private set; }


        public LocalizationManager(string path, Language lang = Language.en)
        {
            this.path = path;
            Lang = lang;

            LoadLocalizedText(Lang);
        }




        public void LoadLocalizedText(int language)
        {
            LoadLocalizedText((Language)language);
        }
        public void LoadLocalizedText(Language language)
        {
            Lang = language;
            //SaveLanguage(Lang);

            string filePath = Path.Combine(path, language.ToString() + ".json");


            if (File.Exists(filePath))
            {
                string dataAsJson = File.ReadAllText(filePath);
                info = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataAsJson);

            }
            else if (File.Exists(Path.Combine(path, "en" + ".json")))
            {
                string dataAsJson = File.ReadAllText(Path.Combine(path, "en" + ".json"));
                info = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataAsJson);

                Debug.LogError("Localization file not found: " + filePath + "; Fall back to en");

            }
            else
            {
                Debug.LogError("Localization file not found: " + filePath);
            }
        }

        public string GetLocalizedValue(string key)
        {
            if (info.TryGetValue(key, out string value))
            {
                return value;
            }
            else
            {
                return key; // Return the key itself if not found for easier debugging
            }
        }

        public static string Format(string input, Dictionary<string, string> replacementDict)
        {
            string pattern = @"\{\{([^}]+)\}\}";

            return Regex.Replace(input, pattern, match =>
            {
                string key = match.Groups[1].Value;
                return replacementDict.TryGetValue(key, out string replacement) ? replacement : key;
            });
        }
    }
}
