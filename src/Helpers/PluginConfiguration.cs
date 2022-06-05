using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Guardian2.Helpers;

public struct PluginConfigurationData
{
    [XmlAnyElement("EnabledComment")]
    public XmlComment EnabledComment
    {
        get => new XmlDocument().CreateComment(
            "Turn this to false to disable whitelist, otherwise use true to enable it.");
        set { }
    }

    public bool Enabled { get; set; } = true;

    [XmlAnyElement("HotReloadComment")]
    public XmlComment HotReloadComment
    {
        get => new XmlDocument().CreateComment("Automatically reload the whitelist file upon saving.");
        set { }
    }

    public bool HotReload { get; set; } = true;

    [XmlAnyElement("KickPlayerComment")]
    public XmlComment KickPlayerComment
    {
        get => new XmlDocument().CreateComment(
            "Automatically kick online players that was removed from the whitelist file at reload.");
        set { }
    }

    public bool KickPlayer { get; set; } = true;

    [XmlAnyElement("RemoteRefreshIntervalComment")]
    public XmlComment RemoteRefreshIntervalComment
    {
        get => new XmlDocument().CreateComment("How often the whitelist should be refreshed in seconds, will default to 5 seconds if set lower then that.");
        set { }
    }
    
    public int RemoteRefreshInterval { get; set; } = 300;
    
    [XmlAnyElement("RemoteWhitelistComment")]
    public XmlComment RemoteWhitelistComment
    {
        get => new XmlDocument().CreateComment("A list of links to remote whitelists containing STEAMID64 ids separated with a newline.");
        set { }
    }
    
    public string[] RemoteWhitelist { get; set; } = {""};
    
    public PluginConfigurationData()
    {
    }
}

public static class PluginConfiguration
{
    /// <summary>
    ///     Most of the variables of this class.
    ///     Data => The configuration data of the plugin that we read with Read() method.
    /// </summary>
    private static PluginConfigurationData Data { get; set; }

    /// <summary>
    ///     Create configuration file of the plugin using default settings.
    /// </summary>
    public static void Initialize()
    {
        var serializer = new XmlSerializer(typeof(PluginConfigurationData));
        using var writer = new StreamWriter(Path.Combine(Plugin.PluginPath, Plugin.PluginConfigurationFileName));
        serializer.Serialize(writer, new PluginConfigurationData());
    }

    /// <summary>
    ///     Check if the configuration file does exists.
    /// </summary>
    /// <returns>A boolean if the configuration file exists.</returns>
    public static bool Exists()
    {
        return File.Exists(Path.Combine(Plugin.PluginPath, Plugin.PluginConfigurationFileName));
    }

    /// <summary>
    ///     Read the plugin configuration file.
    ///     If not existing, then it initializing it.
    /// </summary>
    public static void Read()
    {
        if (!Exists())
        {
            Plugin.Logger.LogWarning(
                $"No configuration file found at [{Path.Combine(Plugin.PluginPath, Plugin.PluginConfigurationFileName)}], initializing it with default settings.");
            Initialize();
        }

        var serializer = new XmlSerializer(typeof(PluginConfigurationData));
        using var reader = new FileStream(Path.Combine(Plugin.PluginPath, Plugin.PluginConfigurationFileName),
            FileMode.Open);
        Data = (PluginConfigurationData) serializer.Deserialize(reader);
        reader.Close();
        
        // Write again for new configs
        using var writer = new StreamWriter(Path.Combine(Plugin.PluginPath, Plugin.PluginConfigurationFileName));
        serializer.Serialize(writer, Data);
    }

    /// <summary>
    ///     Gets the loaded PluginConfigurationData using Read() method.
    /// </summary>
    /// <returns>A PluginConfigurationData object.</returns>
    public static PluginConfigurationData Get()
    {
        return Data;
    }
}