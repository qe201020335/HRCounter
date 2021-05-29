# HRCounter
A Beat Saber Counters+ custom counter that displays your heart rate by using pulsoid.net feed.

**Now come with customizable colored text!**

# ATTENTION
The feed link should shart with `https://pulsoid.net/v1/api/feed/...` **NOT** `https://pulsoid.net/widget/view/...`

## Requirements
These can be downloaded from [BeatMods](https://beatmods.com/#/mods) or using [Mod Assistant](https://github.com/Assistant/ModAssistant/releases/latest).

Alternatively, you can also download them from their github repo.
* [BSIPA](https://github.com/bsmg/BeatSaber-IPA-Reloaded) v4.1.0+
* [Counters+](https://github.com/Caeden117/CountersPlus) v2.0.0+
* [BSML](https://github.com/monkeymanboy/BeatSaberMarkupLanguage) v1.3.4+



## HOW TO USE
1. Make sure all the required mods are working correctly
2. Download and extract the files into `Beat Saber/Plugins/`
3. Run the game once
4. Add your pulsoid feed link in the auto generated config file `Beat Saber/UserData/HRCounter.json`
5. Run the game and enable this counter in Counter+'s counter configuration page

## Settings
### All the options except the color values can now be changed in game. 
Here is a table for all the setting options.
| Field       		| Type      | Default       	    | Description |
| --------------- |:---------:|:-------------------:| ----------- |
| `LogHR`       	| bool      | `false`           	| Whether the received HR data will be logged |
| `FeedLink`      | string    | `"NotSet"`   	    	| Your pulsoid feed link |
| `Colorize`      | bool      | `true`   	        	| Whether the hr value will be colorized by the following 4 detail settings |
| `HRLow`         | int       | `120`           	 	| The lower bound heart rate for when the color gredient will start |
| `HRHigh`        | int       | `180`              	| The upper bound heart rate for when the color gredient will end |
| `LowColor`      | string    | `"#00FF00"` (Green)	| The RGB color in hex that where your hr is not higher than `HRLow`. This is also the starting point of color gredient. |
| `HighColor`     | string    | `"#FF0000"` (Red) 	| The RGB color in hex that where your hr is higher than `HRHigh`. This is also the end point of color gredient.  |
| `MidColor`      | string    | `"#FFFF00"` (Yellow)| The RGB color in hex which is the middle point of color gredient. |

## Notes
1. Remember to exit the game first before editing config files.
2. You can find your pulsoid feed link at the bottom of the [widgets configuration page](https://pulsoid.net/ui/configuration).
3. Please open an [issue](https://github.com/qe201020335/HRCounter/issues) if you have problem using this or found a bug.
