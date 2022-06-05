using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Guardian2.Helpers;

public static class RemoteWhitelist
{
    public static List<ulong> Whitelisted { get; private set; }
    
    /// <summary>
    /// Update the whitelist based on the remote links 
    /// </summary>
    public static (List<ulong> newWhitelist, List<ulong> oldWhitelist) Update()
    {
        // Keep the old Whitelisted list if we already have an list.
        var whitelisted = Whitelisted is {Count: > 0} ? Whitelisted : null;

        // Reinitialize the Whitelisted list each read.
        Whitelisted = new List<ulong>();

        var lines = new List<string>();
        var errored = false;
        
        foreach (var link in PluginConfiguration.Get().RemoteWhitelist)
        {
            var url = link.Trim();
            if (url.Length > 0)
            {
                try
                {
                    var request = WebRequest.Create(url);
                    request.Timeout = 10000;
                    using var response = request.GetResponse();
                    using var responseStream = response.GetResponseStream();
                    if (responseStream != null)
                    {
                        using var streamReader = new StreamReader(responseStream);
                        var data = streamReader
                            .ReadToEnd()
                            .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                        
                        if (data.Any())
                            lines.AddRange(data);
                    }
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogError($"Something went wring during fetching of remote whitelist: {url}");
                    Plugin.Logger.LogError(e.Message);
                    errored = true;
                }
            }
        }

        foreach (var line in lines.Where(line => !string.IsNullOrEmpty(line)))
        {
            if (ulong.TryParse(line, out var result))
            {
                // If there's a duplicate, we simply skipping it.
                if (Whitelisted.Contains(result)) continue;

#if DEBUG
                Plugin.Logger.LogInfo($"Adding [{result}] to the remote whitelist.");
#endif

                Whitelisted.Add(result);
            }
            else
            {
#if DEBUG
                Plugin.Logger.LogWarning($"Unable to parse [{line}] from remote whitelist, is this a SteamID ?");
#endif
            }
        }

        // Don't remove whitelisted people if there was an error with fetching new ones
        if (whitelisted != null && errored)
            Whitelisted.AddRange(whitelisted.Where(o => !Whitelisted.Contains(o)));
        
        whitelisted?.RemoveAll(o => Whitelisted.Contains(o));

        return (Whitelisted, whitelisted);
    }
    
}