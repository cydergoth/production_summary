using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ProductionSummary
{
    /// <summary>
    /// The mod's own UI strings, translated with the game's language setting.
    ///
    /// Files use the game's <c>Language/&lt;language&gt;_main.txt</c> format: entries are
    /// "<c>NNN text|</c>", lines starting with <c>#</c> are comments, and <c>&lt;par1&gt;</c>,
    /// <c>&lt;par2&gt;</c>... are replaced with values. They live in the plugin's
    /// <c>Language</c> folder and are named after <see cref="Lang.GetLanguageName"/>
    /// (english, german, chinese...). Unlike the game, entries are looked up by their number,
    /// not their position, so a translation may leave entries out; missing entries fall back to
    /// the English text built into the DLL.
    /// </summary>
    internal static class Loc
    {
        public const int Title = 0;
        public const int NoBases = 1;
        public const int PlayerBase = 2;
        public const int DockedHere = 3;
        public const int Inactive = 4;
        public const int UsesStorageOf = 5;
        public const int NothingSelected = 6;
        public const int Refining = 7;
        public const int Cycle = 8;
        public const int InStock = 9;
        public const int Limit = 10;
        public const int ResourcesLeft = 11;
        public const int NoMaterials = 12;
        public const int NeedsPerCycle = 13;
        public const int Supply = 14;
        public const int Stashed = 15;
        public const int Cycles = 16;
        public const int Unpowered = 17;
        public const int Producing = 18;
        public const int LimitReached = 19;
        public const int Stalled = 20;
        public const int UnknownItem = 21;
        public const int Seconds = 22;
        public const int Minutes = 23;
        public const int Hours = 24;

        private const string EnglishResource = "ProductionSummary.Language.english_main.txt";

        private static Dictionary<int, string> english;
        private static Dictionary<int, string> current;
        private static int loadedLanguage = -1;

        public static string Get(int code)
        {
            Validate();
            if (current.TryGetValue(code, out string text) || english.TryGetValue(code, out text))
            {
                return text;
            }
            return "LANG. ERROR on [" + Plugin.Name + "] " + code;
        }

        public static string Get(int code, params object[] pars)
        {
            string text = Get(code);
            for (int i = 0; i < pars.Length; i++)
            {
                text = text.Replace("<par" + (i + 1) + ">", pars[i]?.ToString() ?? "");
            }
            return text;
        }

        /// <summary>Reloads the translation if the game language changed since the last load.</summary>
        public static void Validate()
        {
            int language = Lang.current;
            if (language == loadedLanguage && current != null)
            {
                return;
            }
            loadedLanguage = language;
            if (english == null)
            {
                english = LoadEnglish();
            }
            string name = Lang.GetLanguageName(language);
            current = name == "english" ? english : LoadFile(name) ?? english;
        }

        private static Dictionary<int, string> LoadEnglish()
        {
            // The English text is embedded so the mod still works if the Language folder is missing.
            // Entries in a file on disk override it, which lets people fix wording without rebuilding.
            Dictionary<int, string> entries;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EnglishResource))
            using (var reader = new StreamReader(stream))
            {
                entries = Parse(reader.ReadToEnd());
            }
            Dictionary<int, string> onDisk = LoadFile("english");
            if (onDisk != null)
            {
                foreach (KeyValuePair<int, string> entry in onDisk)
                {
                    entries[entry.Key] = entry.Value;
                }
            }
            return entries;
        }

        private static Dictionary<int, string> LoadFile(string languageName)
        {
            string dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Language");
            if (!Directory.Exists(dir))
            {
                return null;
            }
            // The game ships "Japanese_main.txt" but asks for "japanese", so match case-insensitively
            // (Linux file systems are case-sensitive).
            string wanted = languageName + "_main.txt";
            string path = Directory.GetFiles(dir, "*_main.txt")
                .FirstOrDefault(f => string.Equals(Path.GetFileName(f), wanted, StringComparison.OrdinalIgnoreCase));
            if (path == null)
            {
                return null;
            }
            try
            {
                return Parse(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not read translation {path}: {e.Message}");
                return null;
            }
        }

        private static Dictionary<int, string> Parse(string contents)
        {
            var entries = new Dictionary<int, string>();
            foreach (string raw in contents.Split('|'))
            {
                string entry = raw.TrimStart('﻿', '\r', '\n');
                if (entry.Length == 0 || entry[0] == '#' || entry[0] == '[')
                {
                    continue;
                }
                int digits = 0;
                while (digits < entry.Length && char.IsDigit(entry[digits]))
                {
                    digits++;
                }
                if (digits == 0 || !int.TryParse(entry.Substring(0, digits), out int code))
                {
                    continue;
                }
                // "NNN text": skip the single separator space after the number.
                entries[code] = digits < entry.Length && entry[digits] == ' ' ? entry.Substring(digits + 1) : entry.Substring(digits);
            }
            return entries;
        }
    }
}
