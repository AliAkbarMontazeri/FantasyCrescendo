﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Hourai.Editor;
using UnityEditor;

using Google.GData.Client;
using Google.GData.Spreadsheets;

namespace Hourai.Localization.Editor {

    public class LocalizationGenerator : ScriptableObject {

        [MenuItem("Hourai/Localization/Generate")]
        static void Create() {
            var generatorPath = AssetUtil.FindAssetPaths<LocalizationGenerator>();
            LocalizationGenerator generator;
            if (generatorPath.Length <= 0)
                AssetUtil.CreateAssetInProjectWindow<LocalizationGenerator>();
            else {
                generator = AssetDatabase.LoadAssetAtPath<LocalizationGenerator>(generatorPath[0]);
                if(generator)
                    generator.Generate();
            }
        }
        
        [SerializeField]
        private string GoogleLink;

        [SerializeField]
        private string[] _ignoreColumns;

        [SerializeField]
        private Object _saveFolder;

        public void Generate() {
            ListFeed test = GDocService.GetSpreadsheet(GoogleLink);
            var languageMap = new Dictionary<string, Dictionary<string, string>>();
            HashSet<string> ignore = new HashSet<string>(_ignoreColumns);
            foreach (ListEntry row in test.Entries) {
                foreach (ListEntry.Custom element in row.Elements) {
                    string lang = element.LocalName;
                    if (ignore.Contains(lang))
                        continue;
                    if(!languageMap.ContainsKey(lang))
                        languageMap[lang] = new Dictionary<string, string>();
                    languageMap[lang][row.Title.Text] = element.Value;
                }
            }
            string folderPath = _saveFolder ? AssetDatabase.GetAssetPath(_saveFolder) : "Assets/Resources/Lang";
            foreach (var lang in languageMap) {
                Debug.Log(string.Format("Generating language files for: {0}",CultureInfo.GetCultureInfo(lang.Key).EnglishName));
                Language language = Language.FromDictionary(lang.Value);
                AssetDatabase.CreateAsset(language, string.Format("{0}/{1}.asset", folderPath, lang.Key));
            }
        }

    }

}