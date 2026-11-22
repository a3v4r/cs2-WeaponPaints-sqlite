# WeaponPaints (SQLite fork)

Skins, knives, gloves, agents, music kits and pins for CS2 servers on [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp).
Fork of [Nereziel/cs2-WeaponPaints](https://github.com/Nereziel/cs2-WeaponPaints), made for LAN tournaments and servers without a web host or MySQL.

## What is different from the original

- **SQLite by default.** The database is one file next to the plugin (`weapon_paints.db`), created on the first start. MySQL still works (`"DatabaseType": "MySQL"`).
- **Built-in menu.** `!ws`, `!knife`, `!gloves` and the rest open an on-screen menu with item pictures: A/D to scroll, E to pick, R to close. [MenuManager](https://github.com/NickFox007/MenuManagerCS2) is optional; with it, the other menu types still work (`MenuType`).
- **Skins website without logins.** Works with the skins website of [Matchdesk](https://github.com/a3v4r/Matchdesk) (`skins-site/`): a player types `!site`, opens the address on the computer he plays from and lands in his profile. A change on the site applies in game at once.
- **Neutral chat prefix** `[Server]`, set with `ChatPrefix`.
- **Gamedata and gloves fixed** for the current CS2. When a skin cannot be applied (usually outdated gamedata after a CS2 update), the plugin writes it to the log instead of failing silently.

## Install

1. Install [Metamod:Source](https://www.sourcemm.net/downloads.php?branch=dev) and [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/releases) (the `with-runtime` build).
2. Download `WeaponPaints.zip` from [Releases](https://github.com/a3v4r/cs2-WeaponPaints-sqlite/releases) and extract it into `game/csgo` (it contains `addons/counterstrikesharp/...`).
3. In `addons/counterstrikesharp/configs/core.json` set `"FollowCS2ServerGuidelines": false`. With `true` (the default) CounterStrikeSharp blocks item changes and no skin is applied; the plugin warns about it in the log.
4. Start the server. The config appears in `addons/counterstrikesharp/configs/plugins/WeaponPaints/WeaponPaints.json`.

[Matchdesk](https://github.com/a3v4r/Matchdesk) does steps 1-3 by itself and keeps the plugins updated.

## Commands

| Command | What it does |
|---|---|
| `!ws` | skins of the weapon in hand (or the weapon list) |
| `!skins` | weapon list |
| `!knife`, `!gloves`, `!agents`, `!music`, `!pin` | the matching menu |
| `!st` | StatTrak on or off |
| `!wp` | reload your skins from the database |
| `!site` | address of the skins website (only while it is running) |
| `!kill` | suicide (`CommandKillEnabled`) |

Command names can be changed in `Additional` (`CommandSkin`, `CommandKnife` and so on).

Server console only, used by the skins website over RCON:

| Command | Reply |
|---|---|
| `css_wp_who <ip\|local>` | SteamID of the one player playing from that IP, `none` or `many` |
| `css_wp_ip <steamid>` | IP of an online player or `offline` |
| `css_wp_reload <steamid>` | reloads the player's items from the database and gives them at once |

## Settings (`WeaponPaints.json`)

| Field | Default | Meaning |
|---|---|---|
| `SkinsLanguage` | `en` | language of item names, from `data/skins_<lang>.json` (`ru`, `de`, `zh-CN`...) |
| `DatabaseType` | `SQLite` | `SQLite` or `MySQL` (then `DatabaseHost`, `DatabasePort`, `DatabaseUser`, `DatabasePassword`, `DatabaseName`) |
| `SqliteFilePath` | `weapon_paints.db` | database file, relative to the plugin folder |
| `MenuType` | `screen` | `screen` (built in) or, with MenuManager, `selectable`, `dynamic`, `center`, `chat`, `console` |
| `ChatPrefix` | `[{lightblue}Server{default}]` | prefix of the plugin's chat messages; empty: the one from `lang` (`wp_prefix`) |
| `Website` | `auto` | address `!site` shows. `auto`: the skins website on this machine on port 80 (computer name, or the LAN IP for names like `DESKTOP-XXXXX`), shown only while it answers; the in-game menu then loads pictures from it. Any other value is shown as is; empty turns it off |
| `CmdRefreshCooldownSeconds` | `3` | pause between `!wp` uses |
| `Additional.*Enabled` | `true` | turn knives, gloves, agents, music, pins, skins, `!wp`, `!kill` on or off |
| `Additional.ShowSkinImage` | `true` | pictures in the menu |
| `Additional.GiveRandomKnife`, `GiveRandomSkin` | `false` | random items for players who chose nothing |

## Skins website

The website lives in the Matchdesk repository: [`skins-site/`](https://github.com/a3v4r/Matchdesk/tree/ui-redesign/skins-site). Matchdesk runs it by itself; without the panel it runs on Node.js 22.13+ with a `config.json` (see its README). It needs the servers' RCON passwords and the path to this plugin's folder, and keeps copies of the item pictures so the LAN does not wait for GitHub.

## After a CS2 update

If skins stop working after a CS2 update, the log says `Skins are not applied`. The signatures in `addons/counterstrikesharp/gamedata/weaponpaints.json` are then out of date: take the file from the original plugin or from a newer release of this fork.

## Building

```
dotnet build -c Release
```

The output is in `bin/Release/net8.0`. Pushing to `main` builds a release with the version from `ModuleVersion` (GitHub Actions). `3rd_party/MenuManagerApi.dll` is the MenuManager API the plugin is compiled against; it has to stay next to the plugin even when MenuManager is not installed.

## Credits

[Nereziel](https://github.com/Nereziel) and daffyy, authors of the original WeaponPaints. Same license as the original (see `LICENSE`).
