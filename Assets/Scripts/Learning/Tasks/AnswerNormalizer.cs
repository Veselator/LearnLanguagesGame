using System.Text;

namespace LearnLanguages.Learning.Tasks
{
    /// <summary>
    /// Makes typed answers comparable: case, spaces and apostrophe variants are ignored, accents are not.
    /// </summary>
    public static class AnswerNormalizer
    {
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var builder = new StringBuilder(text.Length);
            var previousSpace = true;
            // FormC: "é" typed as one character or as "e" + combining accent is the same letter.
            foreach (var raw in text.Normalize(NormalizationForm.FormC).Trim().ToLowerInvariant())
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
