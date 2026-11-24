# WeaponPaints (форк с SQLite)

[English](README.md) | **Русский**

Скины, ножи, перчатки, агенты, музыка MVP и значки для серверов CS2 на [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp).
Форк [Nereziel/cs2-WeaponPaints](https://github.com/Nereziel/cs2-WeaponPaints) для турниров в локальной сети и серверов без веб-хостинга и MySQL.

## Чем отличается от оригинала

- **SQLite по умолчанию.** База - один файл рядом с плагином (`weapon_paints.db`), создаётся при первом запуске. MySQL по-прежнему работает (`"DatabaseType": "MySQL"`).
- **Встроенное меню.** `!ws`, `!knife`, `!gloves` и остальные открывают экранное меню с картинками предметов: A/D - листать, E - выбрать, R - закрыть. [MenuManager](https://github.com/NickFox007/MenuManagerCS2) не обязателен; с ним работают и другие типы меню (`MenuType`).
- **Сайт скинов без логина.** Работает с сайтом скинов [Matchdesk](https://github.com/a3v4r/Matchdesk) (`skins-site/`): игрок пишет `!site`, открывает адрес на компьютере, с которого играет, и попадает в свой профиль. Изменение на сайте сразу применяется в игре.
- **Нейтральный префикс чата** `[Server]`, задаётся в `ChatPrefix`.
- **Исправлены gamedata и перчатки** под текущую версию CS2. Если скин не удаётся применить (обычно из-за устаревших gamedata после обновления CS2), плагин пишет об этом в лог, а не молчит.

## Установка

1. Установите [Metamod:Source](https://www.sourcemm.net/downloads.php?branch=dev) и [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/releases) (сборку `with-runtime`).
2. Скачайте `WeaponPaints.zip` из [Releases](https://github.com/a3v4r/cs2-WeaponPaints-sqlite/releases) и распакуйте в `game/csgo` (внутри `addons/counterstrikesharp/...`).
3. В `addons/counterstrikesharp/configs/core.json` поставьте `"FollowCS2ServerGuidelines": false`. С `true` (по умолчанию) CounterStrikeSharp запрещает менять предметы и ни один скин не применяется; плагин предупреждает об этом в логе.
4. Запустите сервер. Настройки появятся в `addons/counterstrikesharp/configs/plugins/WeaponPaints/WeaponPaints.json`.

[Matchdesk](https://github.com/a3v4r/Matchdesk) делает шаги 1-3 сам и обновляет плагины.

## Команды

| Команда | Что делает |
|---|---|
| `!ws` | скины оружия в руках (или список оружия) |
| `!skins` | список оружия |
| `!knife`, `!gloves`, `!agents`, `!music`, `!pin` | соответствующее меню |
| `!st` | включить или выключить StatTrak |
| `!wp` | заново загрузить свои скины из базы |
| `!site` | адрес сайта скинов (только пока он работает) |
| `!kill` | самоубийство (`CommandKillEnabled`) |

Названия команд меняются в `Additional` (`CommandSkin`, `CommandKnife` и так далее).

Только для консоли сервера, их по RCON использует сайт скинов:

| Команда | Ответ |
|---|---|
| `css_wp_who <ip\|local>` | SteamID единственного игрока, играющего с этого IP, `none` или `many` |
| `css_wp_ip <steamid>` | IP игрока на сервере или `offline` |
| `css_wp_reload <steamid>` | заново загружает предметы игрока из базы и сразу выдаёт их |

## Настройки (`WeaponPaints.json`)

| Поле | По умолчанию | Что это |
|---|---|---|
| `SkinsLanguage` | `en` | язык названий предметов из `data/skins_<язык>.json` (`ru`, `de`, `zh-CN`...) |
| `DatabaseType` | `SQLite` | `SQLite` или `MySQL` (тогда `DatabaseHost`, `DatabasePort`, `DatabaseUser`, `DatabasePassword`, `DatabaseName`) |
| `SqliteFilePath` | `weapon_paints.db` | файл базы относительно папки плагина |
| `MenuType` | `screen` | `screen` (встроенное) или, с MenuManager, `selectable`, `dynamic`, `center`, `chat`, `console` |
| `ChatPrefix` | `[{lightblue}Server{default}]` | префикс сообщений плагина в чате; пусто - префикс из `lang` (`wp_prefix`) |
| `Website` | `auto` | адрес, который показывает `!site`. `auto`: сайт скинов на этой машине на порту 80 (имя компьютера или LAN IP для имён вида `DESKTOP-XXXXX`), показывается, только пока сайт отвечает; меню в игре тогда берёт картинки с него. Любое другое значение показывается как есть; пусто - выключено |
| `CmdRefreshCooldownSeconds` | `3` | пауза между вызовами `!wp` |
| `Additional.*Enabled` | `true` | включить или выключить ножи, перчатки, агентов, музыку, значки, скины, `!wp`, `!kill` |
| `Additional.ShowSkinImage` | `true` | картинки в меню |
| `Additional.GiveRandomKnife`, `GiveRandomSkin` | `false` | случайные предметы игрокам, которые ничего не выбрали |

## Сайт скинов

Сайт лежит в репозитории Matchdesk: [`skins-site/`](https://github.com/a3v4r/Matchdesk/tree/ui-redesign/skins-site). Matchdesk запускает его сам; без панели он работает на Node.js 22.13+ с файлом `config.json` (см. его README). Ему нужны RCON-пароли серверов и путь к папке этого плагина; копии картинок предметов он хранит у себя, чтобы локальная сеть не ждала GitHub.

## После обновления CS2

Если после обновления CS2 скины перестали работать, в логе будет `Skins are not applied`. Значит, сигнатуры в `addons/counterstrikesharp/gamedata/weaponpaints.json` устарели: возьмите файл из оригинального плагина или из более нового релиза этого форка.

## Сборка

```
dotnet build -c Release
```

Результат - в `bin/Release/net8.0`. Пуш в `main` собирает релиз с версией из `ModuleVersion` (GitHub Actions). `3rd_party/MenuManagerApi.dll` - API MenuManager, с которым собран плагин; он должен лежать рядом с плагином, даже если MenuManager не установлен.

## Авторы

[Nereziel](https://github.com/Nereziel) и daffyy - авторы оригинального WeaponPaints. Лицензия та же, что у оригинала (см. `LICENSE`).
