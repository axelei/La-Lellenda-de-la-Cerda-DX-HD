using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjectZ.InGame.Things
{
    public class Language
    {
        private static readonly Dictionary<string, string> SpecialChars = new Dictionary<string, string>
        {
            { "á", "\u0081" },
            { "é", "\u0084" },
            { "í", "\u0086" },
            { "ó", "\u0087" },
            { "ú", "\u008a" },
            { "Á", "\u008c" },
            { "É", "\u008f" },
            { "Í", "\u0091" },
            { "Ó", "\u0092" },
            { "Ú", "\u0095" },
            { "ç", "\u0096" },
            { "Ç", "\u0097" },
            { "¡", "\u0098" },
            { "¿", "\u0099" },
            { "ñ", "\u009a" },
            { "Ñ", "\u009b" },
            { "ü", "\u009c" },
            { "Ü", "\u009d" },
            { "º", "¡" } // Fix yoshi
        };

        public Dictionary<string, string> Strings => _languageStrings[CurrentLanguageIndex];

        private Dictionary<string, string>[] _languageStrings;

        public int CurrentLanguageIndex;

        public void Load()
        {
            // go through the .lng files and fill the _languageStrings dictionary array
            var files = Directory.GetFiles(Values.PathLanguageFolder);
            var languageStrings = new Dictionary<string, Dictionary<string, string>>();
            // the default (first) entry is charnego
            languageStrings.Add("cha", new Dictionary<string, string>());

            for (var i = 0; i < files.Length; i++)
            {
                var extension = Path.GetExtension(files[i]);
                if (extension == ".lng")
                {
                    var fileName = Path.GetFileNameWithoutExtension(files[i]);
                    var split = fileName.Split('_');
                    var lngName = "";

                    // por.lng
                    if (split.Length == 1)
                        lngName = split[0];
                    // dialog_por.lng
                    if (split.Length == 2)
                        lngName = split[1];

                    languageStrings.TryGetValue(lngName, out Dictionary<string, string> dict);

                    if (dict == null)
                    {
                        dict = new Dictionary<string, string>();
                        languageStrings.Add(lngName, dict);
                    }

                    if (split.Length == 1 || (split.Length == 2 && split[0] == "dialog"))
                        LoadFile(dict, files[i]);
                }
            }

            _languageStrings = languageStrings.Values.ToArray();
            CurrentLanguageIndex = Math.Clamp(CurrentLanguageIndex, 0, _languageStrings.Length - 1);
        }

        public void LoadFile(Dictionary<string, string> dictionary, string fileName)
        {
            var reader = new StreamReader(fileName);

            while (!reader.EndOfStream)
            {
                var strLine = reader.ReadLine();
                var spacePosition = strLine.IndexOf(' ');

                if (spacePosition < 0 || strLine.StartsWith("//"))
                    continue;

                var strKey = strLine.Substring(0, spacePosition);

                // empty string
                if (spacePosition + 1 >= strLine.Length)
                {
                    dictionary.Add(strKey, "");
                    continue;
                }

                var strValue = strLine.Substring(spacePosition + 1);

                // replace special characters with proper characters
                foreach ( string key in SpecialChars.Keys )
                {
                    strValue = strValue.Replace(key, SpecialChars[key]);
                }

                dictionary.Add(strKey, strValue);
            }

            reader.Close();
        }

        public string GetString(string strKey, string defaultString)
        {
            if (strKey == null)
                return "null";

            if (Strings.ContainsKey(strKey))
                defaultString = Strings[strKey];

            // use the english text if there is no translation
            else if (_languageStrings[1].ContainsKey(strKey))
                defaultString = _languageStrings[1][strKey];

            return defaultString;
        }

        public void ToggleLanguage()
        {
            CurrentLanguageIndex = (CurrentLanguageIndex + 1) % _languageStrings.Length;
        }
    }
}
