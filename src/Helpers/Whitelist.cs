using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Guardian2.Helpers;

public static class Whitelist
{
    private static List<ulong> Whitelisted;

    /// <summary>
    ///     Check if the whitelist file does exists.
    /// </summary>
    /// <returns>A boolean if the whitelist file exists.</returns>
    public static bool Exists()
    {
        return File.Exists(Path.Combine(Plugin.PluginPath, Plugin.PluginWhitelistFileName));
    }

    /// <summary>
    ///     Create whitelist file of the plugin.
    /// </summary>
    public static void Initialize()
    {
        var builder = new StringBuilder();

        builder.AppendLine("#");
        builder.AppendLine("# Edit this file to add or remove player from the whitelist.");
        builder.AppendLine("#");
        builder.AppendLine("# If you have enabled HotReload in configuration file, it'll be automatically reloaded");
        builder.AppendLine("# without needing server restart. And if you use KickPlayer option, removed SteamID will");
        builder.AppendLine("# be automatically kicked out the server if connected.");
        builder.AppendLine("#");
        builder.AppendLine("# You can use comments in this file with # character, but be aware, you cannot put a");
        builder.AppendLine("# comment on the same line of an SteamID or it'll be skipped by the plugin.");
        builder.AppendLine("#");
        builder.AppendLine("# 10101010101010101");
        builder.AppendLine("#");

        File.WriteAllText(Path.Combine(Plugin.PluginPath, Plugin.PluginWhitelistFileName), builder.ToString());
    }

    /// <summary>
    ///     Read the plugin whitelist file.
    /// </summary>
    public static object[] Read()
    {
        if (!Exists())
        {
            Plugin.Logger.LogWarning(
                $"No whitelist file found at [{Path.Combine(Plugin.PluginPath, Plugin.PluginWhitelistFileName)}], initializing it with default settings.");

            Initialize();
        }

        // Keep the old Whitelisted list if we already have an list.
        var _Whitelisted = Whitelisted is {Count: > 0} ? Whitelisted : null;

        // Reinitialize the Whitelisted list each read.
        Whitelisted = new List<ulong>();

        // Reading all the lines contained in whitelist file.
        var lines = File.ReadAllLines(Path.Combine(Plugin.PluginPath, Plugin.PluginWhitelistFileName));

        if (lines.Length <= 0)
        {
            Plugin.Logger.LogWarning(
                $"There's no SteamIDs in [{Path.Combine(Plugin.PluginPath, Plugin.PluginWhitelistFileName)}]");
            return new object[] {null, null};
        }

        foreach (var line in lines)
        {
            // We are ignoring line starting with and including "#" character, and if the line empty.
            if (line.StartsWith("#") || line.Contains("#") || string.IsNullOrEmpty(line)) continue;

            if (ulong.TryParse(line, out var result))
            {
                // If there's a duplicate, we simply skipping it.
                if (Whitelisted.Contains(result)) continue;

                // This line should only be showed in Debug build, otherwise, the plugin will
                // take more time to read the file if you have a lot of SteamIDs.
#if DEBUG
                Plugin.Logger.LogInfo($"Adding [{result}] to the whitelist object.");
#endif

                Whitelisted.Add(result);
            }
            else
            {
                // This line should only be showed in Debug build, otherwise, the plugin will
                // take more time to read the file if you have a lot of SteamIDs.
#if DEBUG
                Plugin.Logger.LogWarning($"Unable to parse [{line}] from whitelist file, does this is an SteamID ?");
#endif
            }
        }

        return new object[] {Whitelisted, _Whitelisted};
    }

    /// <summary>
    ///     Gets the loaded whitelist content using Read() method.
    /// </summary>
    /// <returns>A List object containing SteamIDs.</returns>
    public static List<ulong> Get()
    {
        return Whitelisted;
    }
}