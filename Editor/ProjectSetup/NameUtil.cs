using System.Linq;
using System.Text;

namespace StuMillman.Toolkit.Editor.ProjectSetup
{
    public static class NameUtil
    {
        /// <summary>"space game 2" -> "SpaceGame2". Safe for namespaces and assembly names.</summary>
        public static string ToIdentifier(string value, string fallback = "Game")
        {
            var sb = new StringBuilder();
            foreach (var part in Split(value))
                sb.Append(char.ToUpperInvariant(part[0])).Append(part.Substring(1));

            var result = sb.ToString();
            if (result.Length == 0) return fallback;
            return char.IsDigit(result[0]) ? "_" + result : result;
        }

        /// <summary>"Space Game" -> "spacegame". Safe for bundle identifier segments.</summary>
        public static string ToBundlePart(string value, string fallback = "game")
        {
            var result = string.Concat(Split(value)).ToLowerInvariant();
            if (result.Length == 0) return fallback;
            return char.IsDigit(result[0]) ? "x" + result : result;
        }

        static string[] Split(string value) =>
            new string((value ?? string.Empty).Select(c => char.IsLetterOrDigit(c) && c < 128 ? c : ' ').ToArray())
                .Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
    }
}
