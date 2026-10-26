using System.Text;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Localization;
using UnityEngine;

namespace LearnLanguages.UI.Common
{
    /// <summary>
    /// Text formatting shared by the interface.
    /// </summary>
    public sealed class UiFormat
    {
        private static readonly string[] ResourceKeys = { "res.gold", "res.food", "res.wood", "res.stone" };
        private static readonly string[] ResourceColors = { "#E8C33A", "#9BD35A", "#C08A4A", "#B5B5B5" };

        private readonly ILocalizationService _localization;

        public UiFormat(ILocalizationService localization)
        {
            _localization = localization;
        }

        public string ResourceName(ResourceType type) => _localization.Get(ResourceKeys[(int)type]);

        public string Resource(ResourceType type, int amount) =>
            $"<color={ResourceColors[(int)type]}>{_localization.Get(ResourceKeys[(int)type] + ".short")}</color> {amount}";

        /// <summary>Non-zero parts only, e.g. "G 30  S 10".</summary>
        public string Cost(ResourceBundle bundle)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < 4; i++)
            {
                var amount = bundle.Get((ResourceType)i);
                if (amount == 0) continue;
                if (builder.Length > 0) builder.Append("  ");
                builder.Append(Resource((ResourceType)i, amount));
            }
            return builder.Length == 0 ? "-" : builder.ToString();
        }

        public string Signed(int value) => value > 0 ? "+" + value : value.ToString();

        public static string Colored(string text, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";

        public static string Percent(float value) => Mathf.RoundToInt(value * 100f) + "%";
    }
}
