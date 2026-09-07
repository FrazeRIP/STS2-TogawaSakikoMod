using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using System.Text.Json;
using System.Text.RegularExpressions;
using GameLogger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;

namespace TogawaSakiko.NativeCode.Diagnostics;

internal static partial class N4LocalizationDiagnostics
{
    public const int ExpectedEntryCount = 353;

    private static readonly string[] Tables =
    [
        "cards",
        "powers",
        "relics",
        "potions",
        "card_keywords"
    ];

    private static readonly GameLogger Logger = new(Bootstrap.ModEntryPoint.ModId, LogType.Generic);

    [GeneratedRegex(@"\{([A-Za-z][A-Za-z0-9]*)")]
    private static partial Regex VariableRegex();

    [GeneratedRegex(@"\[(/?)(gold|blue|green|red|purple)\]")]
    private static partial Regex ColorTagRegex();

    public static void RunIfRequested()
    {
        if (!NativeSmokeTrace.CatalogEnabled)
        {
            return;
        }

        string language = LocManager.Instance.Language.ToString();
        if (language is not ("eng" or "zhs"))
        {
            throw new InvalidOperationException($"Phase N4 localization diagnostics require eng or zhs, found '{language}'.");
        }

        int entryCount = 0;
        int formattedCount = 0;
        foreach (string table in Tables)
        {
            string path = $"res://TogawaSakiko/localization/{language}/{table}.json";
            using Godot.FileAccess? file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            if (file is null)
            {
                throw new InvalidOperationException($"Phase N4 localization catalog is missing from the mounted PCK: {path}.");
            }

            using JsonDocument document = JsonDocument.Parse(file.GetAsText());
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"Phase N4 localization catalog is not a flat JSON object: {path}.");
            }

            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                string key = property.Name;
                string raw = property.Value.GetString()
                    ?? throw new InvalidOperationException($"Localization value is not a string: {table}.{key}.");
                entryCount++;

                if (!LocValidator.ValidateFormatString(raw, out string? error))
                {
                    throw new InvalidOperationException(
                        $"Invalid native localization format for {table}.{key}: {error}");
                }

                ValidateColorTags(table, key, raw);
                if (!LocString.Exists(table, key))
                {
                    throw new InvalidOperationException(
                        $"Mounted localization entry was not merged into LocManager: {table}.{key}.");
                }

                string mergedRaw = new LocString(table, key).GetRawText();
                if (!string.Equals(raw, mergedRaw, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Mounted localization entry differs after merge: {table}.{key}.");
                }

                FormatAndValidate(table, key, raw, UpgradeDisplay.Normal);
                FormatAndValidate(table, key, raw, UpgradeDisplay.Upgraded);
                formattedCount += 2;
            }
        }

        if (entryCount != ExpectedEntryCount)
        {
            throw new InvalidOperationException(
                $"Phase N4 localization expected {ExpectedEntryCount} entries, found {entryCount}.");
        }

        Logger.Info(
            $"Phase N4 localization catalogs passed. Language={language}, Entries={entryCount}, FormattedVariants={formattedCount}, LegacyMarkers=0");
    }

    private static void FormatAndValidate(
        string table,
        string key,
        string raw,
        UpgradeDisplay upgradeDisplay)
    {
        LocString value = new(table, key);
        HashSet<string> variableNames = VariableRegex()
            .Matches(raw)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string variableName in variableNames)
        {
            switch (variableName)
            {
                case "IfUpgraded":
                    value.Add(new IfUpgradedVar(upgradeDisplay));
                    break;
                case "energyPrefix":
                    value.Add(variableName, "ironclad");
                    break;
                default:
                    if (raw.Contains($"{{{variableName}:diff", StringComparison.Ordinal))
                    {
                        value.Add(new DynamicVar(variableName, 2m));
                    }
                    else
                    {
                        value.Add(variableName, 2m);
                    }
                    break;
            }
        }

        string formatted = value.GetFormattedText();
        if (VariableRegex().IsMatch(formatted))
        {
            throw new InvalidOperationException(
                $"Native localization left an unresolved variable for {table}.{key} ({upgradeDisplay}): {formatted}");
        }
    }

    private static void ValidateColorTags(string table, string key, string raw)
    {
        Stack<string> tags = new();
        foreach (Match match in ColorTagRegex().Matches(raw))
        {
            string tag = match.Groups[2].Value;
            bool closing = match.Groups[1].Value.Length > 0;
            if (!closing)
            {
                tags.Push(tag);
                continue;
            }

            if (tags.Count == 0 || !string.Equals(tags.Pop(), tag, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Native localization contains mismatched color tags for {table}.{key}: {raw}");
            }
        }

        if (tags.Count > 0)
        {
            throw new InvalidOperationException(
                $"Native localization contains unclosed color tags for {table}.{key}: {raw}");
        }
    }
}
