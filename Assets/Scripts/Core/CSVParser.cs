using System;
using System.Collections.Generic;
using UnityEngine;

public static class CSVParser
{
    public static List<Dictionary<string, string>> Parse(string csvText)
    {
        var results = new List<Dictionary<string, string>>();
        var lines = new List<string>();
        
        using (var reader = new System.IO.StringReader(csvText))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
            }
        }

        if (lines.Count < 2) return results;

        var headers = SplitCsvLine(lines[0]);

        for (int i = 1; i < lines.Count; i++)
        {
            var values = SplitCsvLine(lines[i]);
            var row = new Dictionary<string, string>();
            
            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                row[headers[j].Trim()] = values[j].Trim();
            }
            
            results.Add(row);
        }

        return results;
    }

    private static string[] SplitCsvLine(string line)
    {
        var list = new List<string>();
        if (line == null) return Array.Empty<string>();
        
        bool inQuotes = false;
        var cur = new System.Text.StringBuilder();
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    cur.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                list.Add(cur.ToString());
                cur.Length = 0;
            }
            else
            {
                cur.Append(c);
            }
        }
        
        list.Add(cur.ToString());
        return list.ToArray();
    }

    public static int ParseInt(Dictionary<string, string> row, string key)
    {
        return int.Parse(row[key]);
    }
    
    public static int ParseInt(Dictionary<string, string> row, string key, int defaultValue)
    {
        if (row.TryGetValue(key, out var value) && int.TryParse(value, out var result))
            return result;
        return defaultValue;
    }

    public static string ParseString(Dictionary<string, string> row, string key)
    {
        return row[key];
    }
    
    public static string ParseString(Dictionary<string, string> row, string key, string defaultValue)
    {
        if (row.TryGetValue(key, out var value))
            return value;
        return defaultValue;
    }

    public static float ParseFloat(Dictionary<string, string> row, string key)
    {
        return float.Parse(row[key], System.Globalization.CultureInfo.InvariantCulture);
    }
    
    public static float ParseFloat(Dictionary<string, string> row, string key, float defaultValue)
    {
        if (row.TryGetValue(key, out var value) && 
            float.TryParse(value, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        return defaultValue;
    }
}
