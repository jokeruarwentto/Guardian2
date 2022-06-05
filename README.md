<img width="120" height="120" align="left" src="./assets/guardian.png" alt="Guardian2 Logo">

#### V Rising - Server Plugin

# Guardian 2

### Description

Do you want to make a private server password protected, and fearing about players that share your password ? Or you just want to have a whitelist ? 

Guardian is here for you, a simple whitelist system to let you the choice of who can connect on your server.

### Features
🗒️ Configurable

👟 Automatic Kick system

📜 Whitelist system

⚙️ Hot reloading

🤖 RCON command

### Installation
- Install [BepInExPack V Rising](https://v-rising.thunderstore.io/package/BepInEx/BepInExPack_V_Rising/) on your server
- Extract ``me.arwent.Guardian2`` folder into _(VRising Server folder)/BepInEx/plugins_

### Configuration
- Open ``me.arwent.Guardian2.Configuration.xml`` with your favorite text editor

### Usage
- Open ``me.arwent.Guardian2.Whitelist.txt`` with your favorite text editor
- Add the player ``SteamID`` into it
- Save

### Usage of remote lists
This allows for the mod to load remote whitelist files or data from apis.
The data returned need to be newline separated `steamID64` ids.

An example of a site that can be used is [whitelist.gorymoon.se](https://whitelist.gorymoon.se/) (twitch subscriber whitelist).

Example on how to setup the config for this:
```xml
<RemoteWhitelist>
    <string>https://example.com/link/to/file.txt</string>
    <string>https://example.com/link/to/api/endpoint</string>
</RemoteWhitelist>
```

### Usage RCON
- `guardian (add/remove) (steamId)`

### Support
- [V Rising Mod Community](https://discord.gg/CWzkHvekg3) and ping `@Arwent#6190`

### Changelog
`2.1.0` Added RCON command

`2.0.1` Fixed configuration not taked in count

`2.0.0` Totally reworked the plugin and open sourced

`1.1.0` Added automatic Kick feature

`1.0.0` First release