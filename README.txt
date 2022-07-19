1. DESCRIPTION
This is demo project for creating mod for Phoenix Point.

Mods are code-only with .NET assembly and meta file.

2. HOW MODS ARE DISCOVERED AND LOADED
On game startup (and if mods are enabled), mods are being discovered in <game-dir>/Mods.
Each mod is in it's own folder and contains at least single meta.json. Using "meta.json" mods are detected and shown to user.
Any mods that have been enabled before will have their assembly loaded, configuration restored, and code callbacks will begin.
At this point player can turn enable/disable mods on demand through in-game UI.

3. META FILE
Each mod must have "meta.json" file in their own directory. Meta file holds some description about the mod itself and dependencies.
File is in json format and must contain at least:
- ID: Unique ID for the mod;
Optional field are:
- Name: Localized name of mod;
- Version: Version of the mod, see System.Version class for details;
- Author: Localized mod author's name;
- Description: Localized desription of mod;
- Dependencies: list of other mods' ID that need to be enabled first;
Localized fields are array of Key & Value. Key is english name of language, Value is the localized text.
Phoenix Point has localizations for English, Chinese (Simplified), French, German, Italian, Polish, Russian & Spanish.
Not all language have to be added to the localization array. If game's current language is not found, then English will be used. If English is not found, then first element of the array will be used.

4. MOD ASSEMBLY
In order to load and use mod assembly, there are some specific requirements.
To have mod assembly detected and loaded by the game, it must be placed at:
	<game-dir>/Mods/<mod-name>/<mod-loader-name>.dll
From Visual Studio, make sure Assembly name is an unique name (under Project Properties\Application);
Mod assemblies are not required to have a mod (only meta.json is needed), but without code a mod will do nothing.
Mod assemlby is loaded when a mod is enabled, after game is started. Assemblies are never unloaded runtime.

5. MOD DEPENDENCIES
In meta.json, mod can delcare any number of other mods as dependencies as an array of mod IDs.
Mod can only be enabled (and have assembly loaded) if all of his dependencies exist and can be enabled.
When a mod is enabled, game makes sure all of it's dependencies are enabled before that.
If a mod, that is a dependency of another mod, is disabled, then all mods that depend on it will be disabled as well.

6. MOD CONFIGURATIONS
If modders want to provide some settings for players, then they should create a ModConfig class.
ModConfig allows some fields to be edited in-game UI through "Mod Settings" section.
Mod will recieve callback when field is changed through ModMain.OnConfigChanged.
Mod config is serialized, settings are loaded and reapplied when mod is enabled.

7. HARMONY
While Phoenix Point has many data-driven settings and game events for custom logic, modders may need to change parts of the game that are not exposed to modificaiton.
A popular way to do that is using Harmony (https://harmony.pardeike.net/). To ease Harmony setup, each mod is provided with their own instance Harmony object.
Be careful making modifications with Harmony. It is very powerful tool and can make unstable changes to the game.

8. USING THIS PROJECT
This project is a quick demo of how to create a custom mod and use all features provied by Phoenix Point's modding SDK.
Following files are provded with detailed documentation:
- meta.json: Required meta file for mod to be detected;
- TFTVConfig.cs: Example of ModConfig for custom mod settings;
- TFTVGeoscape.cs: Example of ModGeoscape for modifying geoscape level;
- TFTVMain.cs: Main mod class, recieves mod management and general events;
- TFTVTactical.cs: Example of ModTactical for modifying tactical level;
It is adviced modders to start with this demo project, as it is already pre-configured with proper SDK references, building, and deployment.
