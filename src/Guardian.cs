using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.IL2CPP.Utils.Collections;
using Guardian2.Helpers;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using RCONServerLib.Utils;
using Steamworks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Guardian2;

public class Guardian : MonoBehaviour
{
    /// <summary>
    ///     Most of the variables of this class.
    /// </summary>
    public static Guardian Instance;

    private FileSystemWatcher FileWatcher;
    private List<ulong> OldWhitelisted;
    private ServerBootstrapSystem ServerBootstrap;
    private World ServerWorld;
    private List<ulong> Whitelisted;
    private EntityManager WorldEntityManager;

    /// <summary>
    ///     Gets the Server's world.
    ///     Thanks to Sheraf for sharing this bit of code.
    /// </summary>
    /// <returns>A World object from Unity's entities.</returns>
    private static World GetServerWorld()
    {
        foreach (var world in World.All)
            if (world.Name == "Server")
                return world;

        return null;
    }

    /// <summary>
    ///     The Awake function from MonoBehaviour, an in-built Unity class,
    ///     this is here that we prepare all the data we need for Guardian.
    /// </summary>
    private void Awake()
    {
        // If the plugin is disabled, then no need to go further.
        if (!PluginConfiguration.Get().Enabled)
        {
            Plugin.Logger.LogWarning("Guardian is disabled.");
            return;
        }

        // Otherwise, we're processing.
        Instance = this;
        new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
        StartCoroutine(Initialize().WrapToIl2Cpp());
    }

    /// <summary>
    ///     We are initializing what we are needing for Guardian, in this case,
    ///     we wait for Server's world to get object such EntityManager.
    /// </summary>
    private IEnumerator Initialize()
    {
        yield return StartCoroutine(WaitForServerWorld().WrapToIl2Cpp());

        var result = Whitelist.Read();
        Whitelisted = (List<ulong>) result[0];
        OldWhitelisted = (List<ulong>) result[1];

        if (PluginConfiguration.Get().HotReload) InitializeFileWatcher();
        else Plugin.Logger.LogWarning("HotReload is disabled.");

        while (true)
        {
            KickPlayers(OldWhitelisted);
            yield return 0;
        }
    }

    /// <summary>
    ///     The Coroutine that gets the Server's world, EntityManager and ServerBootstrapSystem.
    /// </summary>
    private IEnumerator WaitForServerWorld()
    {
        while (GetServerWorld() == null) yield return 0;

#if DEBUG
        Plugin.Logger.LogInfo("Server's world was found, processing...");
#endif

        ServerWorld = GetServerWorld();
        WorldEntityManager = ServerWorld.EntityManager;
        ServerBootstrap = ServerWorld.GetExistingSystem<ServerBootstrapSystem>();
    }

    /// <summary>
    ///     This is the system we need to hot reload our whitelist file.
    /// </summary>
    private void InitializeFileWatcher()
    {
        FileWatcher = new FileSystemWatcher(Plugin.PluginPath)
        {
            NotifyFilter = NotifyFilters.LastWrite,
            Filter = Plugin.PluginWhitelistFileName
        };

        FileWatcher.Changed += (_, _) =>
        {
            var result = Whitelist.Read();
            Whitelisted = (List<ulong>) result[0];
            OldWhitelisted = (List<ulong>) result[1];
        };

        FileWatcher.EnableRaisingEvents = true;

#if DEBUG
        Plugin.Logger.LogInfo(
            $"FileWatcher now watch [{Path.Combine(Plugin.PluginPath, Plugin.PluginWhitelistFileName)}].");
#endif
    }

    /// <summary>
    ///     This function watch is KickPlayer is enabled, if so, then we're
    ///     looping through the oldWhitelisted parameter and kick player if necessary.
    /// </summary>
    /// <param name="oldWhitelisted">The list containing all old whitelisted SteamIDs.</param>
    public void KickPlayers(List<ulong> oldWhitelisted)
    {
        if (!PluginConfiguration.Get().KickPlayer || oldWhitelisted == null) return;

        var query = WorldEntityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());
        var entities = query.ToEntityArray(Allocator.Temp);

        foreach (var steamId in oldWhitelisted)
            if (!Whitelist.Get().Contains(steamId))
                foreach (var entity in entities)
                {
                    var user = WorldEntityManager.GetComponentData<User>(entity);
                    if (user.PlatformId != steamId || !user.IsConnected) continue;
                    KickPlayer(steamId);
                }
    }

    /// <summary>
    ///     Custom function that we use using ServerBootstrapSystem to kick players.
    /// </summary>
    /// <param name="steamId">The SteamID of the player that we want to kick.</param>
    private void KickPlayer(ulong steamId)
    {
        ServerBootstrap.Kick(steamId);
        Plugin.Logger.LogInfo($"[{steamId}] was kicked because it was removed from the whitelist.");
    }
}

[HarmonyPatch(typeof(SteamGameServer))]
public class SteamGameServer_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch("BeginAuthSession")]
    public static void BeginAuthSession(object[] __args, ref object __result)
    {
        var steamId = (CSteamID) __args[2];

        if (!Whitelist.Get().Contains(steamId.m_SteamID))
            __result = EBeginAuthSessionResult.k_EBeginAuthSessionResultInvalidTicket;
    }
}

[HarmonyPatch(typeof(RconListenerSystem))]
public class RconListenerSystem_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch("OnCreate")]
    public static void OnCreate(RconListenerSystem __instance)
    {
        if (SettingsManager.ServerHostSettings.Rcon.Enabled)
        {
            // Thanks to molenzwiebel & Soliel for helping me here.
            CommandHandler guardianCommandHandler =
                new Func<string, Il2CppSystem.Collections.Generic.IList<string>, string>((command, args) =>
                {
                    if (args.Cast<Il2CppSystem.Collections.Generic.List<string>>().Count <= 0)
                        return $"Invalid or no arguments was passed for [{command}] command.";

                    if (args.Cast<Il2CppSystem.Collections.Generic.List<string>>().Count < 2)
                        return $"There are missing arguments for [{command}] command.";

                    switch (args[0])
                    {
                        case "add":
                        case "a":
                            return AddToWhitelist(args[1]);
                        case "remove":
                        case "r":
                        case "delete":
                        case "d":
                            return RemoveFromWhitelist(args[1]);
                        default:
                            return $"[{args[0]}] is an unknown argument for [{command}].";
                    }
                });

            __instance._Server.CommandManager.Add("guardian", "(add/remove) (steamId)",
                "Add or remove an SteamID from the Guardian's whitelist.", guardianCommandHandler);
        }
    }

    private static string AddToWhitelist(string steamId)
    {
        if (ulong.TryParse(steamId, out var result))
        {
            var whitelistFile =
                File.ReadAllLines(Path.Combine(Plugin.PluginPath, Plugin.PluginWhitelistFileName)).ToList();

            if (whitelistFile.ToList().Contains(result.ToString()))
                return $"[{result}] is already in your whitelist.";

            whitelistFile.Add(result.ToString());
            File.WriteAllLines(Path.Combine(Plugin.PluginPath, Plugin.PluginWhitelistFileName), whitelistFile);

            return $"[{result}] has been added into your whitelist.";
        }

        return $"[{steamId}] is not a valid SteamID.";
    }

    private static string RemoveFromWhitelist(string steamId)
    {
        if (ulong.TryParse(steamId, out var result))
        {
            var whitelistFile =
                File.ReadAllLines(Path.Combine(Plugin.PluginPath, Plugin.PluginWhitelistFileName)).ToList();

            if (!whitelistFile.ToList().Contains(result.ToString()))
                return $"[{result}] is not in your whitelist.";

            whitelistFile.Remove(result.ToString());
            File.WriteAllLines(Path.Combine(Plugin.PluginPath, Plugin.PluginWhitelistFileName), whitelistFile);

            return $"[{result}] has been removed into your whitelist.";
        }

        return $"[{steamId}] is not a valid SteamID.";
    }
}