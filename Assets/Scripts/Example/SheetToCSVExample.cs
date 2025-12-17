// #if UNITY_EDITOR
// using System;
// using System.IO;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.Networking;

// public static class SheetToCSVExample
// {
//     // Meta sheet (provided by user)
//     private const string MetaFileId = "1tulhRLkZRVJ7zJW5NITjg7imYkP-QPx-VBAVpHDKMtk";
//     private const string MetaGid = "1619292081";
//     private static readonly string DefaultMetaCsvUrl = $"https://docs.google.com/spreadsheets/d/{MetaFileId}/export?format=csv&gid={MetaGid}";

//     // Items now come from the same spreadsheet as meta, using a different sheet gid
//     private const string ItemsGid = "234995314";
//     private static readonly string DefaultCsvUrl = $"https://docs.google.com/spreadsheets/d/{MetaFileId}/export?format=csv&gid={ItemsGid}";

//     // Enemies from the same spreadsheet, designer-provided gid
//     private const string EnemiesGid = "2082314646";
//     private static readonly string DefaultEnemiesCsvUrl = $"https://docs.google.com/spreadsheets/d/{MetaFileId}/export?format=csv&gid={EnemiesGid}";

//     // Relics from the same spreadsheet, designer-provided gid
//     private const string RelicsGid = "466859353";
//     private static readonly string DefaultRelicsCsvUrl = $"https://docs.google.com/spreadsheets/d/{MetaFileId}/export?format=csv&gid={RelicsGid}";

//     // Events from the same spreadsheet, designer-provided gid
//     private const string EventsGid = "1787337680";
//     private static readonly string DefaultEventsCsvUrl = $"https://docs.google.com/spreadsheets/d/{MetaFileId}/export?format=csv&gid={EventsGid}";

//     // Node map tuning (new sheet), designer-provided gid
//     private const string NodeMapGid = "1338659506";
//     private static readonly string DefaultNodeMapCsvUrl = $"https://docs.google.com/spreadsheets/d/{MetaFileId}/export?format=csv&gid={NodeMapGid}";

//     // Keywords for tooltip system
//     private const string KeywordsGid = "1423513153";
//     private static readonly string DefaultKeywordsCsvUrl = $"https://docs.google.com/spreadsheets/d/{MetaFileId}/export?format=csv&gid={KeywordsGid}";

//     private const string ResourcesDir = "Assets/Resources/Data";
//     private const string ItemsCsvPath = ResourcesDir + "/itemBuilder.csv";
//     private const string MetaCsvPath = ResourcesDir + "/meta.csv";
//     private const string EnemiesCsvPath = ResourcesDir + "/enemies.csv";
//     private const string RelicsCsvPath = ResourcesDir + "/relics.csv";
//     private const string EventsCsvPath = ResourcesDir + "/events.csv";
//     // Legacy Level Progression removed in favor of NodeMap
//     private const string NodeMapCsvPath = ResourcesDir + "/nodeMap.csv";
//     private const string BaseStatsCsvPath = ResourcesDir + "/player.csv";
//     private const string KeywordsCsvPath = ResourcesDir + "/keywords.csv";

//     // Base Stats from the same spreadsheet, designer-provided gid
//     private const string BaseStatsGid = "1988808566";
//     private static readonly string DefaultBaseStatsCsvUrl = $"https://docs.google.com/spreadsheets/d/{MetaFileId}/export?format=csv&gid={BaseStatsGid}";

//     [MenuItem("Tools/Data/Update All CSVs")]
//     public static void UpdateAllCsvs()
//     {
//         var jobs = new System.Collections.Generic.List<(string path, string url)>
//         {
//             (ItemsCsvPath, DefaultCsvUrl),
//             (MetaCsvPath, DefaultMetaCsvUrl),
//             (EnemiesCsvPath, DefaultEnemiesCsvUrl),
//             (RelicsCsvPath, DefaultRelicsCsvUrl),
//             (EventsCsvPath, DefaultEventsCsvUrl),
//             (NodeMapCsvPath, DefaultNodeMapCsvUrl),
//             (BaseStatsCsvPath, DefaultBaseStatsCsvUrl),
//             (KeywordsCsvPath, DefaultKeywordsCsvUrl),
//         };
//         int total = jobs.Count;
//         bool anyError = false;
//         try
//         {
//             for (int i = 0; i < total; i++)
//             {
//                 var (path, url) = jobs[i];
//                 string title = $"Updating CSVs ({i + 1}/{total})";
//                 var ok = DownloadTo(path, url, title);
//                 if (!ok)
//                 {
//                     anyError = true;
//                 }
//             }
//         }
//         finally
//         {
//             EditorUtility.ClearProgressBar();
//         }
//         if (anyError)
//         {
//             Debug.LogError("CSV update finished with errors. See logs above for details.");
//         }
//         else
//         {
//             Debug.Log("CSV update: all imports OK.");
//         }
//     }

//     private static bool DownloadTo(string assetPath, string url, string progressTitle)
//     {
//         Directory.CreateDirectory(ResourcesDir);
//         try
//         {
//             // Cache-busting query and no-cache headers to avoid stale content
//             var cacheBusted = url + (url.Contains("?") ? "&" : "?") + "cb=" + DateTime.UtcNow.Ticks;
//             var req = UnityWebRequest.Get(cacheBusted);
//             req.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
//             req.SetRequestHeader("Pragma", "no-cache");
//             req.SetRequestHeader("Expires", "0");
//             req.timeout = 20;
//             var op = req.SendWebRequest();
//             while (!op.isDone)
//             {
//                 EditorUtility.DisplayProgressBar(progressTitle, "Downloading...", op.progress);
//             }
// #if UNITY_2020_2_OR_NEWER
//             if (req.result != UnityWebRequest.Result.Success)
// #else
//             if (req.isNetworkError || req.isHttpError)
// #endif
//             {
//                 Debug.LogError($"CSV download failed for {assetPath}: {req.error}");
//                 return false;
//             }

//             // Write atomically and force reimport to ensure Unity sees the change
//             var tmpPath = assetPath + ".tmp";
//             var sanitized = SanitizeCsv(req.downloadHandler.text);
//             File.WriteAllText(tmpPath, sanitized);
//             if (File.Exists(assetPath))
//             {
//                 FileUtil.ReplaceFile(tmpPath, assetPath);
//                 if (File.Exists(tmpPath)) FileUtil.DeleteFileOrDirectory(tmpPath);
//             }
//             else
//             {
//                 File.Move(tmpPath, assetPath);
//             }

//             // Force import/refresh so changes are picked up immediately
//             AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
//             AssetDatabase.SaveAssets();
//             AssetDatabase.Refresh();
//             Debug.Log($"CSV updated: {assetPath}");
//             return true;
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError($"Failed to download CSV for {assetPath}: {ex.Message}");
//             return false;
//         }
//     }

//     // CSV sanitation helpers (handles simple quoted CSV)
//     private static string SanitizeCsv(string csv)
//     {
//         if (string.IsNullOrEmpty(csv)) return csv;
//         var lines = new System.Collections.Generic.List<string>();
//         using (var sr = new System.IO.StringReader(csv))
//         {
//             string line;
//             while ((line = sr.ReadLine()) != null) lines.Add(line);
//         }
//         if (lines.Count == 0) return csv;

//         var header = SplitCsvLine(lines[0]);
//         var keepIdx = new System.Collections.Generic.List<int>();
//         for (int i = 0; i < header.Length; i++)
//         {
//             var h = header[i] ?? string.Empty;
//             if (!h.TrimStart().StartsWith("*")) keepIdx.Add(i);
//         }
//         // Detect a non-ignored 'Status' column (case-insensitive). If it exists, filter rows to Status == 'Done'.
//         int statusColIdx = -1;
//         for (int i = 0; i < header.Length; i++)
//         {
//             var h = header[i] ?? string.Empty;
//             if (!keepIdx.Contains(i)) continue; // ignored via '*'
//             if (string.Equals(h.Trim(), "Status", StringComparison.OrdinalIgnoreCase))
//             {
//                 statusColIdx = i;
//                 break;
//             }
//         }
//         // If there are no '*' columns to remove AND no Status filtering, return original csv early.
//         if (keepIdx.Count == header.Length && statusColIdx < 0) return csv;

//         var sb = new System.Text.StringBuilder();
//         // Write filtered header (remove '*' columns)
//         var outHeader = new System.Collections.Generic.List<string>(keepIdx.Count);
//         foreach (var idx in keepIdx)
//         {
//             outHeader.Add(EscapeCsv(header[idx]));
//         }
//         sb.Append(string.Join(",", outHeader)).Append('\n');

//         // Write filtered rows
//         for (int li = 1; li < lines.Count; li++)
//         {
//             var cols = SplitCsvLine(lines[li]);
//             // Apply Status filtering if present
//             if (statusColIdx >= 0)
//             {
//                 string statusVal = statusColIdx < cols.Length ? cols[statusColIdx] : string.Empty;
//                 if (!string.Equals((statusVal ?? string.Empty).Trim(), "Done", StringComparison.OrdinalIgnoreCase))
//                 {
//                     continue; // omit this row
//                 }
//             }
//             var outCols = new System.Collections.Generic.List<string>(keepIdx.Count);
//             foreach (var idx in keepIdx)
//             {
//                 string val = idx < cols.Length ? cols[idx] : string.Empty;
//                 outCols.Add(EscapeCsv(val));
//             }
//             sb.Append(string.Join(",", outCols)).Append('\n');
//         }
//         return sb.ToString();
//     }

//     private static string[] SplitCsvLine(string line)
//     {
//         var list = new System.Collections.Generic.List<string>();
//         if (line == null) return Array.Empty<string>();
//         bool inQuotes = false;
//         var cur = new System.Text.StringBuilder();
//         for (int i = 0; i < line.Length; i++)
//         {
//             char c = line[i];
//             if (c == '"')
//             {
//                 if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
//                 else { inQuotes = !inQuotes; }
//             }
//             else if (c == ',' && !inQuotes)
//             {
//                 list.Add(cur.ToString()); cur.Length = 0;
//             }
//             else { cur.Append(c); }
//         }
//         list.Add(cur.ToString());
//         return list.ToArray();
//     }

//     private static string EscapeCsv(string value)
//     {
//         if (value == null) return string.Empty;
//         bool mustQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
//         if (!mustQuote) return value;
//         return "\"" + value.Replace("\"", "\"\"") + "\"";
//     }
// }
// #endif
