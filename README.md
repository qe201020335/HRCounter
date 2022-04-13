# HRCounter
A Beat Saber custom counter that displays your heart rate in game.

**Supports BLE HR Monitor, Apple Watch, Fitbit, Galaxy Watch, WearOS and more!** Check out instructions on [HR monitors](#hr-monitors).

It also supports **anything** that **Pulsoid**, **HypeRate**, **YUR Desktop App / MOD** support! Check out instructions on [data sources](#data-sources).

It can even **pause** the game for you if your heart is beating **too fast**!

## Requirements
These can be downloaded from [BeatMods](https://beatmods.com/#/mods) or using [Mod Assistant](https://github.com/Assistant/ModAssistant/releases/latest) (Recommended!).

Alternatively, you can also download them from their own site.
* [BSIPA](https://github.com/bsmg/BeatSaber-IPA-Reloaded) v4.2.0+
* [BSML](https://github.com/monkeymanboy/BeatSaberMarkupLanguage) v1.6.0+
* [SiraUtil](https://github.com/Auros/SiraUtil) v3.0.0+
* [Counters+](https://github.com/Caeden117/CountersPlus) v2.0.0+ (Recommended Optional, install if you want to use Counters+ custom counter system)
* Websocket-sharp (Optional, install if you want to use Pulsoid, HypeRate or FitbitHRtoWS)
* YUR Mod (Optional, install if you want to enable YUR Mod support)
* [YUR Desktop App](https://store.steampowered.com/app/1188920/YUR/) (Optional, install if you want to enable YUR App support)

## HOW TO USE
1. Make sure all the required mods are working correctly
2. Download the [latest release](https://github.com/qe201020335/HRCounter/releases/latest) and extract the files into `Beat Saber/Plugins/`
3. Run the game once
4. Depend on your devices, follow the instructions below to configure the auto generated config file.
5. Run the game 
6. If you are using Counters+, enable this counter in Counter+'s counter configuration page
7. Click the "HRCounter" button in the main menu mods section to see the full configuration page of the mod
8. Toggle on **Auto Pause** in the "Health & Safty" tab if you want the mod to pause the game for you. Also set the **Pause Heart Rate** for yourself!

The config file is at `Beat Saber/UserData/HRCounter.json`. You can use notepad to open and edit it.

## DATA SOURCES
### Pulsoid
⚠️ Pulsoid Feed Link is **DEPRECATED** and will stop working at the end of 2022.
#### Pulsoid Widget (Experimental, A proper authentication method will be implemented in the future)
1. Open your pulsoid [widgets configuration page](https://pulsoid.net/ui/configuration) and configure your default widget.
2. Copy the last part of the widget link as shown in this here.
![pulsoid widget config screenshot](https://i.imgur.com/KWtfIH2.png)
3. Paste this in the config file as `"PulsoidWidgetID": "ff7936d8-██████████████20086244"`.
4. Set the value of `DataSource` to `"Pulsoid"`.
#### Pulsoid JSON Feed Link (DEPRECATED, will stop working at the end of 2022)
1. Open your pulsoid [widgets configuration page](https://pulsoid.net/ui/configuration).
2. Copy the link behine `Feed reference` in the **Advanced** section at the bottom of the page.
![pulsoid json feed config screenshot](https://i.imgur.com/HS9bXRU.png)
3. Paste this link in the config file as `"FeedLink": "https://pulsoid.net/v1/api/feed/..."`.
4. Set the value of `DataSource` to `"WebRequest"`.

Notice: The link should start with `https://pulsoid.net/v1/api/feed/...`.


### HypeRate
1. In the HypeRate app on your phone or watch, there is the session ID, which is also the few hex digits at the end of your overlay link.
2. Change the value of `HypeRateSessionID` to yours in the config file.
3. Set the value of `DataSource` to `"HypeRate"`.

For example, if your overlay link is `https://app.hyperate.io/12ab`, then your session ID is `12ab` and the config will look like `"HypeRateSessionID": "12ab"`

### Fitbit

For support on how to set up FitbitHRtoWS, please ask for help in [200Tigersbloxed's Discord server](https://www.fortnite.lol/discord)
1. Follow the instruction on [FitbitHRtoWS](https://github.com/200Tigersbloxed/FitbitHRtoWS/wiki/Setup) and set up your heart rate broadcast.
2. Change the value of `FitbitWebSocket` to `"ws://YOUR_IP:YOUR_PORT/"`.
3. Set the value of `DataSource` to `"FitbitHRtoWS"`

For example, `"FitbitWebSocket": "ws://localhost:8080/",` or `"FitbitWebSocket": "ws://192.168.1.100:8080/",`

### YUR App
1. Download and start YUR from [Steam](https://store.steampowered.com/app/1188920/YUR/)
2. Set the value of `DataSource` to `"YUR APP"`

## HR MONITORS

### BLE Compatible HR Monitor
There are 2 options
1. Download [Pulsoid](https://pulsoid.net/) on your phone and set up heart rate broadcast. Then follow instruction for Pulsoid [above](#Pulsoid).
2. Download [HypeRate](https://www.hyperate.io/#download) on your phone and connect to your BLE device. Then follow instructions for HypeRate [above](#HypeRate)

### Galaxy Watch
1. Use [HeartRateToWeb](https://github.com/loic2665/HeartRateToWeb) and [this](https://galaxystore.samsung.com/geardetail/tUhSWQRbmv) app on your watch.
2. Follow the instrucrion on [their github page](https://github.com/loic2665/HeartRateToWeb) to set up heart rate broadcast. Make sure you download the [**first**](https://github.com/loic2665/HeartRateToWeb/releases/tag/v1.0.0) realease, the csharp one is bugged (filelock issue, and requires admin for no reason).
3. Make sure your watch and your computer is in the **SAME** network.
4. Use either [from file](https://github.com/loic2665/HeartRateToWeb#from-file) or [from /hr endpoint](https://github.com/loic2665/HeartRateToWeb#from-hr-endpoint) in the [OBS](https://github.com/loic2665/HeartRateToWeb#obs) section of HeartRateToWeb instructions.
    - If you decide to use "from file", then use the path to the `hr.txt` file generated by HeartRateToWeb as `file:///PATH/TO/THE/FILE`.
    - If you decide to use "/hr endpoint", then enter the feedlink as `http://YOUR_IP:YOUR_PORT/hr`

For example
  - **from file**: `"FeedLink": "file:///D:/example/folder/hr.txt",`
  - **from /hr**: `"FeedLink": "http://192.168.1.100:6547/hr",`


### Apple Watch
1. Download [HypeRate](https://www.hyperate.io/#download) on your iPhone and Apple Watch.
2. Follow instructions for HypeRate [above](#HypeRate)

Note: You need to have testflight since HypeRate is still in beta.

Special thanks to iPixelGalaxy for testing!

### WearOS Smart Watch
There are 2 options.
1. Use [Heart for Bluetooth](https://play.google.com/store/apps/details?id=lukas.the.coder.heartforbluetooth) on your watch and use it as a BLE heart rate monitor. Then follow the instructions for BLE Compatible HR Monitor [above](#ble-compatible-hr-monitor)
2. Use [HypeRate](https://hyperate.io/) which you can download it on the [play store](https://play.google.com/store/apps/details?id=de.locxserv.hyperatewearos) and follow instructions for HypeRate [above](#HypeRate)

Depending on your watch, monitoring quality may not be as good as a dedicated heart rate monitor. 


### Others
When this mod is requesting hr data, it expects a string containing one of these:
* A json contains key `bpm` with int type value and an optional key `measured_at` with string type value.
* Only numerical digits. (Regex `^\d+$`)

It can be in a file or can be requested from a link. Then set `DataSource` to `"WebRequest"` and the link.

More data sources and devices are planned to be supported. See [below](#Data-Dources-To-Be-Supported).

Open an [issue](https://github.com/qe201020335/HRCounter/issues) if there is a device or data source you want me to support!


## Settings
### Most options can be changed in game. 
Here is a table for all the setting options if you want to edit config file instead.
| Field       	  | Type      | Default       	    | Description |
| --------------- |:---------:|:-------------------:| ----------- |
| `ModEnable`     | bool      | `true`           	| DUH |
| `LogHR`         | bool      | `false`           	| Whether the received HR data will be logged |
| `DataSource`    | string    | `"YUR MOD"`         | The data source you want to use to get hr data. Use `Random` if you want to test things|
| `HypeRateSessionID`| string | `"-1"`              | Session ID for HypeRate, it is also the the few hex digits at the end of your overlay link. |
| `PulsoidWidgetID`| string   | `"NotSet"`          | Widget ID for HypeRate, it is also the last part of your widget link. |
| `FitbitWebSocket` | string  | `"ws://localhost:8080/"`| WebSocket Link for FitbitHRtoWS |
| `FeedLink`      | string    | `"NotSet"`   	   	| Your pulsoid feed link |
| `Colorize`      | bool      | `true`   	       	| Whether the hr value will be colorized by the following 4 detail settings |
| `HideDuringReplay`| bool    | `true`   	       	| Hide this counter while in a replay |
| `HRLow`         | int       | `120`           		| The lower bound heart rate for when the color gredient will start |
| `HRHigh`        | int       | `180`              	| The upper bound heart rate for when the color gredient will end |
| `LowColor`      | string    | `"#00FF00"` (Green)	| The RGB color in hex that where your hr is not higher than `HRLow`. This is also the starting point of color gredient. |
| `MidColor`      | string    | `"#FFFF00"` (Yellow)| The RGB color in hex which is the middle point of color gredient. |
| `HighColor`     | string    | `"#FF0000"` (Red) 	| The RGB color in hex that where your hr is higher than `HRHigh`. This is also the end point of color gredient.  |
| `PauseHR`       | int       | `200`              	| The heart rate that game pause will be triggered |
| `AutoPause`     | bool      | `false`             | Whether the mod will pause the game if heart rate reaches `PauseHR` |
| `IgnoreCountersPlus`| bool  | `false`             | Ignore whether Counters+ is installed, ignore it to allow 2 hr counters to present at the same time |
| `DebugSpam`     | bool      | `false`             | Only effective in Debug build, toggle spamming of debug message in logs |
| `StaticCounterPosition` | 3D Vector | `(0, 1.2, 7)` | Location of the standalone static counter, has no effect on the counters+ counter |

## Data Sources To Be Supported
* <s>[HypeRate](https://hyperate.io/)</s>
* <s>Apple Watch (via HypeRate)</s>
* <s>WearOS</s>
* <s>Fitbit (via [FitbitHRtoWS](https://github.com/200Tigersbloxed/FitbitHRtoWS))</s> (By [200Tigersbloxed](https://github.com/200Tigersbloxed))

Open an [issue](https://github.com/qe201020335/HRCounter/issues) if there is a device or data source you want me to support!

## Notes
* Remember to exit the game first before editing config files.
* Please open an [issue](https://github.com/qe201020335/HRCounter/issues) if you have problem using this or found a bug.
* The beautiful lightshow map in the screenshot is [!bsr 17726](https://beatsaver.com/maps/17726)

