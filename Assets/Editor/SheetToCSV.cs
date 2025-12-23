#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public static class SheetToCSV
{
    private const string SpreadsheetId = "1Xf8VMoabEcP90C9WYsHbAA64ADr-lWzQH8UDTCtsYxY";
    
    private const string EnemyGid = "0";
    private static readonly string DefaultEnemyCsvUrl = $"https://docs.google.com/spreadsheets/d/{SpreadsheetId}/export?format=csv&gid={EnemyGid}";
    
    private const string RestGid = "161878381";
    private static readonly string DefaultRestCsvUrl = $"https://docs.google.com/spreadsheets/d/{SpreadsheetId}/export?format=csv&gid={RestGid}";

    private const string PotionGid = "893018057";
    private static readonly string DefaultPotionCsvUrl = $"https://docs.google.com/spreadsheets/d/{SpreadsheetId}/export?format=csv&gid={PotionGid}";

    private const string RelicGid = "809776826";
    private static readonly string DefaultRelicCsvUrl = $"https://docs.google.com/spreadsheets/d/{SpreadsheetId}/export?format=csv&gid={RelicGid}";

    private const string ElementalReactionsGid = "990517346";
    private static readonly string DefaultElementalReactionsCsvUrl = $"https://docs.google.com/spreadsheets/d/{SpreadsheetId}/export?format=csv&gid={ElementalReactionsGid}";

    private const string ElementalReactionEffectsGid = "1599028666";
    private static readonly string DefaultElementalReactionEffectsCsvUrl = $"https://docs.google.com/spreadsheets/d/{SpreadsheetId}/export?format=csv&gid={ElementalReactionEffectsGid}";

    private const string CharacterGid = "2103587809";
    private static readonly string DefaultCharacterCsvUrl = $"https://docs.google.com/spreadsheets/d/{SpreadsheetId}/export?format=csv&gid={CharacterGid}";

    private const string ResourcesDir = "Assets/Resources/Data";
    private const string EnemyCsvPath = ResourcesDir + "/enemy.csv";
    private const string RestCsvPath = ResourcesDir + "/rest.csv";
    private const string PotionCsvPath = ResourcesDir + "/potion.csv";
    private const string RelicCsvPath = ResourcesDir + "/relic.csv";
    private const string ElementalReactionsCsvPath = ResourcesDir + "/elementalReactions.csv";
    private const string ElementalReactionEffectsCsvPath = ResourcesDir + "/elementalReactionEffects.csv";
    private const string CharacterCsvPath = ResourcesDir + "/character.csv";

    [MenuItem("Tools/Data/Update All CSVs")]
    public static void UpdateAllCsvs()
    {
        var jobs = new System.Collections.Generic.List<(string path, string url)>
        {
            (EnemyCsvPath, DefaultEnemyCsvUrl),
            (RestCsvPath, DefaultRestCsvUrl),
            (PotionCsvPath, DefaultPotionCsvUrl),
            (RelicCsvPath, DefaultRelicCsvUrl),
            (ElementalReactionsCsvPath, DefaultElementalReactionsCsvUrl),
            (ElementalReactionEffectsCsvPath, DefaultElementalReactionEffectsCsvUrl),
            (CharacterCsvPath, DefaultCharacterCsvUrl),
        };
        int total = jobs.Count;
        bool anyError = false;
        try
        {
            for (int i = 0; i < total; i++)
            {
                var (path, url) = jobs[i];
                string title = $"Updating CSVs ({i + 1}/{total})";
                var ok = DownloadTo(path, url, title);
                if (!ok)
                {
                    anyError = true;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        if (anyError)
        {
            Debug.LogError("CSV update finished with errors. See logs above for details.");
        }
        else
        {
            Debug.Log("CSV update: all imports OK.");
        }
    }

    private static bool DownloadTo(string assetPath, string url, string progressTitle)
    {
        Directory.CreateDirectory(ResourcesDir);
        try
        {
            var cacheBusted = url + (url.Contains("?") ? "&" : "?") + "cb=" + DateTime.UtcNow.Ticks;
            var req = UnityWebRequest.Get(cacheBusted);
            req.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            req.SetRequestHeader("Pragma", "no-cache");
            req.SetRequestHeader("Expires", "0");
            req.timeout = 20;
            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                EditorUtility.DisplayProgressBar(progressTitle, "Downloading...", op.progress);
            }
#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError($"CSV download failed for {assetPath}: {req.error}");
                return false;
            }

            var tmpPath = assetPath + ".tmp";
            var sanitized = SanitizeCsv(req.downloadHandler.text);
            File.WriteAllText(tmpPath, sanitized);
            if (File.Exists(assetPath))
            {
                FileUtil.ReplaceFile(tmpPath, assetPath);
                if (File.Exists(tmpPath)) FileUtil.DeleteFileOrDirectory(tmpPath);
            }
            else
            {
                File.Move(tmpPath, assetPath);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"CSV updated: {assetPath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to download CSV for {assetPath}: {ex.Message}");
            return false;
        }
    }

    private static string SanitizeCsv(string csv)
    {
        if (string.IsNullOrEmpty(csv)) return csv;
        var lines = new System.Collections.Generic.List<string>();
        using (var sr = new System.IO.StringReader(csv))
        {
            string line;
            while ((line = sr.ReadLine()) != null) lines.Add(line);
        }
        if (lines.Count == 0) return csv;

        var header = SplitCsvLine(lines[0]);
        var keepIdx = new System.Collections.Generic.List<int>();
        for (int i = 0; i < header.Length; i++)
        {
            var h = header[i] ?? string.Empty;
            if (!h.TrimStart().StartsWith("*")) keepIdx.Add(i);
        }
        
        int statusColIdx = -1;
        for (int i = 0; i < header.Length; i++)
        {
            var h = header[i] ?? string.Empty;
            if (!keepIdx.Contains(i)) continue;
            if (string.Equals(h.Trim(), "Status", StringComparison.OrdinalIgnoreCase))
            {
                statusColIdx = i;
                break;
            }
        }
        
        if (keepIdx.Count == header.Length && statusColIdx < 0) return csv;

        var sb = new System.Text.StringBuilder();
        var outHeader = new System.Collections.Generic.List<string>(keepIdx.Count);
        foreach (var idx in keepIdx)
        {
            outHeader.Add(EscapeCsv(header[idx]));
        }
        sb.Append(string.Join(",", outHeader)).Append('\n');

        for (int li = 1; li < lines.Count; li++)
        {
            var cols = SplitCsvLine(lines[li]);
            if (statusColIdx >= 0)
            {
                string statusVal = statusColIdx < cols.Length ? cols[statusColIdx] : string.Empty;
                if (!string.Equals((statusVal ?? string.Empty).Trim(), "Done", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }
            var outCols = new System.Collections.Generic.List<string>(keepIdx.Count);
            foreach (var idx in keepIdx)
            {
                string val = idx < cols.Length ? cols[idx] : string.Empty;
                outCols.Add(EscapeCsv(val));
            }
            sb.Append(string.Join(",", outCols)).Append('\n');
        }
        return sb.ToString();
    }

    private static string[] SplitCsvLine(string line)
    {
        var list = new System.Collections.Generic.List<string>();
        if (line == null) return Array.Empty<string>();
        bool inQuotes = false;
        var cur = new System.Text.StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                else { inQuotes = !inQuotes; }
            }
            else if (c == ',' && !inQuotes)
            {
                list.Add(cur.ToString()); cur.Length = 0;
            }
            else { cur.Append(c); }
        }
        list.Add(cur.ToString());
        return list.ToArray();
    }

    private static string EscapeCsv(string value)
    {
        if (value == null) return string.Empty;
        bool mustQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
        if (!mustQuote) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
#endif
