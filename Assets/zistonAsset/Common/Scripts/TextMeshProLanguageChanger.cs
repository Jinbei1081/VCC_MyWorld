
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace ziston
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TextMeshProLanguageChanger : UdonSharpBehaviour
    {
        [SerializeField] private TextMeshProUGUI[] texts;
        [SerializeField] private TextAsset languageDataCsv;
        private string _currentLanguage = "en";
        public string CurrentLanguage
        {
            get => _currentLanguage;
        }

        void Start()
        {
            ChangeLanguage(VRCPlayerApi.GetCurrentLanguage());
        }
        public override void OnLanguageChanged(string language)
        {
            ChangeLanguage(language);
        }
        public void ChangeLanguage(string language)
        {
            if (languageDataCsv == false) return;
            string raw = languageDataCsv.text;
            raw = raw.Replace("\r", "");
            raw.Trim();
            string[] lines = raw.Split('\n');
            _currentLanguage = language;

            for(int i = 0; i < lines.Length; i++) {
                if (lines[i].StartsWith(_currentLanguage)) {
                    string[] languageStrings = lines[i].Split(',');
                    for(int j = 0; j < texts.Length; j++) {
                        if (texts[j] == false) continue;
                        texts[j].text = languageStrings[j + 1];
                    }
                    return;
                }
            }
            _currentLanguage = "en";
            for(int i = 0; i < lines.Length; i++) {
                if (lines[i].StartsWith(_currentLanguage)) {
                    string[] languageStrings = lines[i].Split(',');
                    for(int j = 0; j < texts.Length; j++) {
                        if (texts[j] == false) continue;
                        texts[j].text = languageStrings[j + 1];
                    }
                    return;
                }
            }
        }
    }
}
