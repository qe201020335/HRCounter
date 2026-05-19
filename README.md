# HRCounter <a href="https://github.com/qe201020335/HRCounter/releases"><img src="https://img.shields.io/github/v/release/qe201020335/HRCounter?include_prereleases&style=for-the-badge&label=PRE-RELEASE" align="right"></a> <a href="https://github.com/qe201020335/HRCounter/releases/latest"><img src="https://img.shields.io/github/v/release/qe201020335/HRCounter?style=for-the-badge&label=RELEASE" align="right"></a>
A Beat Saber custom counter that displays your heart rate in-game.

![HR Counter](Assets/hrc2.0_cmp.png)

## Features

- Supports [Pulsoid](https://pulsoid.net/), [HypeRate](https://www.hyperate.io/), **OSC**, and more (see [Data Sources](#data-sources))
- Automatically **pauses the game** when your heart rate goes too high
- Configurable gradient colored heart rate text with customizable counter icon
- **Counters+** custom counter support alongside a world-space standalone counter
- Records heart rate into BeatLeader replays and plays it back when watching

## Requirements

Install via mod installers like [ModAssistant](https://github.com/Assistant/ModAssistant/releases/latest) and [BSManager](https://www.bsmanager.io/)

* [BSIPA](https://github.com/bsmg/BeatSaber-IPA-Reloaded) v4.3.6+
* [BSML](https://github.com/monkeymanboy/BeatSaberMarkupLanguage) v1.12.0+
* [SiraUtil](https://github.com/Auros/SiraUtil) v3.0.0+
* [Counters+](https://github.com/Caeden117/CountersPlus) v2.0.0+ (*optional, recommended* Required to use the Counters+ custom counter system)
* YUR Mod (*optional* Required for the YUR Mod data source)
* [YUR Desktop App](https://store.steampowered.com/app/1188920/YUR/) (*optional* Required for the YUR App data source)

## How to Use

> [!IMPORTANT]
> Make sure all required mods are working before installing HRCounter.

1. Download the [latest release](https://github.com/qe201020335/HRCounter/releases/latest).
2. Extract into your `Beat Saber` game directory.
3. Launch the game.
4. Open the **HRCounter** settings menu from the main menu's mods section.
5. Pick a data source in the right panel and configure it. See [Data Sources](#data-sources) for details.
    - Pulsoid users: click **Authorize Pulsoid** to sign in via OAuth, no manual token editing needed.
    - HypeRate users: enter your session ID (the few characters at the end of your overlay link).
6. *(Optional)* Toggle **Auto Pause** in the **Safety** tab to pause the game when your heart rate is too high.
7. If you have Counters+ installed, enable the **Heart Rate Counter** in the Counters+ configuration page.

> [!NOTE]
> Please open an [issue](https://github.com/qe201020335/HRCounter/issues) if you run into a problem or find a bug.

## Data Sources

Support matrix for common devices and services:

| Device Type    | Pulsoid | HypeRate | YUR App |
|----------------|:-------:|:--------:|:-------:|
| BLE HR Monitor |    ✅    |    ✅     |    ✅    |
| Apple Watch    |    ✅    |    ✅     |    ❓    |
| WearOS Watch   |   ✅*    |   ✅**    |    ❓    |
| Fitbit         |   ✅*    |   ✅**    |    ❓    |

*Check whether your device is supported by [Pulsoid](https://blog.pulsoid.net/monitors?from=mheader).

**Check whether your device is supported by [HypeRate](https://www.hyperate.io/supported-devices.html).

### Other Sources

#### WebRequest

Polls an http/https endpoint that returns HR data in either of these formats:

* A JSON object containing `bpm` (int), with an optional `measured_at` (string)
* A plain integer body (regex `^\d+$`)

Set `FeedLink` to your URL in the config file. See [Manual Config Editing](#manual-config-editing).

#### HTTP Server

HRCounter runs a built-in HTTP server that by default listens on `http://localhost:65302`.
Select `HttpServer` as your data source. This data source expects `POST` requests with an integer body.

#### OSC Protocol

HRCounter runs a built-in OSC server that by default listens on UDP port `9000` on all interfaces.
Select `OSC Protocol` as your data source to see the accepted OSC addresses. Addresses can be edited
via [Manual Config Editing](#manual-config-editing).

### More?

Open an [issue](https://github.com/qe201020335/HRCounter/issues) if there's a device or service you'd like supported.

## Manual Config Editing

> [!IMPORTANT]
> Due to a BSIPA bug, edits made to the config file while the game is running won't be detected. Exit the game before editing.

Most options can be changed in-game but if you want to edit the config directly, the config file is at `Beat Saber/UserData/HRCounter.json`. See the full field reference below

| Field                    |   Type    |       Default        | Description                                                                                            |
|--------------------------|:---------:|:--------------------:|--------------------------------------------------------------------------------------------------------|
| `ModEnable`              |   bool    |        `true`        | Enables the mod (DUH).                                                                                 |
| `DataSource`             |  string   |   `"OSC Protocol"`   | Data source used to get heart rate.                                                                    |
| `StreamerMode`           |   bool    |        `true`        | Hide tokens and ids in the data source menu.                                                           |
| `PulsoidToken`           |  string   |         `""`         | Pulsoid OAuth access token.                                                                            |
| `HypeRateSessionID`      |  string   |         `""`         | HypeRate session ID (the few characters at the end of your overlay link)                               |
| `PulsoidWidgetID`        |  string   |         `""`         | Widget ID for the experimental `PulsoidWidget` data source (the last part of your widget link)         |
| `HRProxyID`              |  string   |         `""`         | Custom reader ID for the `HRProxy` data source.                                                        |
| `FeedLink`               |  string   |         `""`         | URL for the `WebRequest` data source.                                                                  |
| `NoBloom`                |   bool    |       `false`        | Do you want no bloom on the text?                                                                      |
| `Colorize`               |   bool    |        `true`        | Colorize the heart rate number.                                                                        |
| `HRLow`                  |    int    |        `120`         | Start of the color gradient.                                                                           |
| `HRHigh`                 |    int    |        `180`         | End of the color gradient.                                                                             |
| `LowColor`               |  string   | `"#00FF00"` (Green)  | Color for HR ≤ `HRLow`; gradient start.                                                                |
| `MidColor`               |  string   | `"#FFFF00"` (Yellow) | Middle color of the gradient.                                                                          |
| `HighColor`              |  string   |  `"#FF0000"` (Red)   | Color for HR ≥ `HRHigh`; gradient end.                                                                 |
| `AutoPause`              |   bool    |       `false`        | Automatically pause the game if your heart rate is too high.                                           |
| `PauseHR`                |    int    |        `200`         | Pause heart rate threshold for auto-pause.                                                             |
| `IgnoreCountersPlus`     |   bool    |       `false`        | Always show the standalone counter regardless whether Counters+ is installed.                          |
| `IgnoreZeroValues`       |   bool    |        `true`        | Ignore zero heart rate values as if the data did not exist.                                            |
| `CustomIcon`             |  string   |         `""`         | Filename of a custom icon under `UserData/HRCounter/Icons/`. Empty string uses the default heart icon. |
| `StaticCounterPosition`  | 3D Vector |    `(0, 1.2, 7)`     | World-space position of the standalone counter. No effect on the Counters+ counter.                    |
| `ReplayRecordHr`         |   bool    |        `true`        | Record heart rate into BeatLeader replays.                                                             |
| `ReplayPlaybackSelfHr`   |   bool    |        `true`        | Allow heart rate recorded in your own replays to be shown.                                             |
| `ReplayPlaybackOthersHr` |   bool    |       `false`        | Allow heart rate recorded in other players' replays to be shown.                                       |
| `ReplayFallbackLiveHr`   |   bool    |       `false`        | Show your live heart rate when recorded replay data is not available or not used.                      |
| `EnableHttpServer`       |   bool    |        `true`        | Enable the built-in HTTP server (Required for the `HTTPServer` data source).                           |
| `HttpLocalOnly`          |   bool    |        `true`        | Restrict the built-in HTTP server to localhost only.                                                   |
| `HttpPort`               |    int    |       `65302`        | TCP port the built-in HTTP server listens on.                                                          |
| `EnableOscServer`        |   bool    |        `true`        | Enable the built-in OSC server (Required for the `OSC Protocol` data source).                          |
| `OscBindIP`              |  string   |     `"0.0.0.0"`      | IP address the OSC server binds to. `0.0.0.0` listens on all interfaces.                               |
| `OscPort`                |    int    |        `9000`        | UDP port the OSC server listens on.                                                                    |
| `OscAddress`             |   list    |       `[...]`        | OSC address paths the server accepts heart rate messages on. Each message must carry a single `int32`. |

