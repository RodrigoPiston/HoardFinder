using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace HoardFinder.I18n
{
    /// <summary>
    /// UI strings in the game's selected language. One embedded <c>Language.txt</c> (key=value, UTF-8) per Valheim
    /// language name, as returned by <c>Localization.GetSelectedLanguage()</c>. Missing languages or keys fall back
    /// to English, then to the key itself. <see cref="Version"/> changes when the language does, so callers can
    /// rebuild cached text and styles.
    /// </summary>
    internal static class Tr
    {
        private const string Fallback = "English";
        private const string ResourcePrefix = "HoardFinder.I18n.";

        // Languages Averia Serif Libre (Latin-1 only) can render; the rest use Unity's default font with OS fallback.
        private static readonly HashSet<string> Latin1Languages = new HashSet<string>
        {
            "English", "Swedish", "French", "Italian", "German", "Spanish", "Finnish", "Danish", "Norwegian",
            "Icelandic", "Dutch", "Portuguese_European", "Portuguese_Brazilian", "Indonesian",
        };

        private static Dictionary<string, string> _english;
        private static Dictionary<string, string> _current;
        private static string _language;

        public static int Version { get; private set; }

        public static bool UsesLatin1 { get; private set; } = true;

        public static string T(string key)
        {
            Refresh();
            if (_current.TryGetValue(key, out string value) || _english.TryGetValue(key, out value))
            {
                return value;
            }
            return key;
        }

        public static string F(string key, object arg0) => Format(T(key), arg0, null, null);

        public static string F(string key, object arg0, object arg1, object arg2) => Format(T(key), arg0, arg1, arg2);

        private static string Format(string pattern, object a, object b, object c)
        {
            try
            {
                return string.Format(pattern, a, b, c);
            }
            catch (FormatException)
            {
                return pattern;
            }
        }

        /// <summary>Cheap when the language did not change: one string comparison.</summary>
        public static void Refresh()
        {
            string language = Localization.instance != null ? Localization.instance.GetSelectedLanguage() : Fallback;
            if (string.IsNullOrEmpty(language))
            {
                language = Fallback;
            }
            if (_current != null && language == _language)
            {
                return;
            }
            _english = _english ?? Load(Fallback) ?? new Dictionary<string, string>();
            _current = Load(language) ?? _english;
            _language = language;
            UsesLatin1 = Latin1Languages.Contains(language);
            Version++;
            Log.Debug("UI language: " + language + (ReferenceEquals(_current, _english) ? " (English fallback)" : ""));
        }

        private static Dictionary<string, string> Load(string language)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(ResourcePrefix + language + ".txt"))
            {
                if (stream == null)
                {
                    return null;
                }
                using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                {
                    return Parse(reader);
                }
            }
        }

        private static Dictionary<string, string> Parse(TextReader reader)
        {
            var table = new Dictionary<string, string>(32);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int split = line.IndexOf('=');
                if (line.Length == 0 || line[0] == '#' || split <= 0)
                {
                    continue;
                }
                table[line.Substring(0, split).Trim()] = line.Substring(split + 1);
            }
            return table;
        }
    }
}
