using System.Globalization;
using System.Text;

namespace LearnLanguages.Learning.Tasks
{
    public static class AnswerNormalizer
    {
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var builder = new StringBuilder(text.Length);
            var previousSpace = true;
            foreach (var raw in text.Trim().ToLowerInvariant())
            {
                var c = raw == '’' || raw == '`' ? '\'' : raw;
                if (char.IsWhiteSpace(c))
                {
                    if (!previousSpace) builder.Append(' ');
                    previousSpace = true;
                    continue;
                }
                builder.Append(c);
                previousSpace = false;
            }
            return builder.ToString().TrimEnd();
        }

        public static string StripAccents(string text)
        {
            var decomposed = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(c);
            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }

    /// <summary>
    /// Vocabulary strings may hold several variants separated by '/'.
    /// </summary>
    public static class WordVariants
    {
        public static string[] Split(string text) =>
            string.IsNullOrEmpty(text) ? new string[0] : text.Split('/');

        public static string Display(string text) => string.IsNullOrEmpty(text) ? string.Empty : text.Replace("/", ", ");

        public static string First(string text) => Split(text)[0];
    }
}
