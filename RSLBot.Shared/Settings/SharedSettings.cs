namespace RSLBot.Shared.Settings
{
    using System.Diagnostics;
    using static RSLBot.Shared.Settings.SharedSettings.LanguageSettings;

    public class SharedSettings
    {
        public class LanguageSettings(string originalName, string code)
        {
            public enum LanguageId
            {
                Eng,
                Ukr
            }

            public string OriginalName { get; init; } = originalName;
            public string Code { get; init; } = code;
        }

        public Process? RaidProcess { get; set; }

        public LanguageSettings Language { get; set; } = SupportedLanguages[LanguageId.Ukr];

        public CancellationTokenSource CancellationTokenSource { get; set; } = new ();

        private static readonly Dictionary<LanguageId, LanguageSettings> SupportedLanguages =
            new()
            {
                { LanguageId.Ukr, new LanguageSettings(originalName: "Українська", code: "Ukr") },
                { LanguageId.Eng, new LanguageSettings(originalName: "English", code: "Eng") }
            };
    }
}
