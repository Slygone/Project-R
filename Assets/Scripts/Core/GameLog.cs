using System;
using UnityEngine;

[Flags]
public enum GameLogCategory
{
    None = 0,
    Combat = 1 << 0,
    Reaction = 1 << 1,
    Status = 1 << 2,
    Data = 1 << 3,
    System = 1 << 4,
    All = ~0
}

public enum GameLogVerbosity
{
    Minimal = 0,
    Normal = 1,
    Verbose = 2
}

public static class GameLog
{
    public static GameLogCategory EnabledCategories = GameLogCategory.Combat | GameLogCategory.Reaction | GameLogCategory.Status | GameLogCategory.Data | GameLogCategory.System;
    public static GameLogVerbosity Verbosity = GameLogVerbosity.Normal;

    public static void Combat(string msg, GameLogVerbosity minVerbosity = GameLogVerbosity.Minimal) => Log(GameLogCategory.Combat, "[Combat]", msg, minVerbosity);
    public static void Reaction(string msg, GameLogVerbosity minVerbosity = GameLogVerbosity.Minimal) => Log(GameLogCategory.Reaction, "[Reaction]", msg, minVerbosity);
    public static void Status(string msg, GameLogVerbosity minVerbosity = GameLogVerbosity.Minimal) => Log(GameLogCategory.Status, "[Status]", msg, minVerbosity);
    public static void Data(string msg, GameLogVerbosity minVerbosity = GameLogVerbosity.Minimal) => Log(GameLogCategory.Data, "[Data]", msg, minVerbosity);
    public static void System(string msg, GameLogVerbosity minVerbosity = GameLogVerbosity.Minimal) => Log(GameLogCategory.System, "[System]", msg, minVerbosity);

    public static void Warn(GameLogCategory category, string tag, string msg)
    {
        if (!IsEnabled(category)) return;
        Debug.LogWarning($"{tag} {msg}");
    }

    public static void Error(GameLogCategory category, string tag, string msg)
    {
        if (!IsEnabled(category)) return;
        Debug.LogError($"{tag} {msg}");
    }

    public static string KV(string key, object value)
    {
        string v = value == null ? "null" : value.ToString();
        return $"{key}={v}";
    }

    public static string Join(params string[] parts)
    {
        return string.Join(" | ", parts);
    }

    private static void Log(GameLogCategory category, string tag, string msg, GameLogVerbosity minVerbosity)
    {
        if (!IsEnabled(category)) return;
        if (Verbosity < minVerbosity) return;
        Debug.Log($"{tag} {msg}");
    }

    private static bool IsEnabled(GameLogCategory category)
    {
        return (EnabledCategories & category) != 0;
    }
}
