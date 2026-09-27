using Base.Core;
using Base.Platforms;
using Base.Serialization;
using Base.UI.MessageBox;
using HarmonyLib;
using PhoenixPoint.Common.Game;
using PhoenixPoint.Modding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TFTV
{
    /// <summary>
    /// Whether this run of the game plays with the Aircraft Rework Beta, and how a campaign keeps the
    /// setting it was started with.
    ///
    /// The rework changes defs once at startup, in many places and in both directions (the main
    /// branch's own branches create defs too), and its Harmony patches are gated by Prepare. None of
    /// that can be undone cleanly while the game runs, so the mode is fixed for a whole game launch:
    /// it is read from <see cref="ModeFileName"/> before anything else in OnModEnabled, and switching
    /// it means writing that file and restarting Phoenix Point.
    ///
    /// Every save records the mode it was made with as an extra entry in the save metadata's mod
    /// list (<see cref="SaveMarkerId"/>), which the game reads from the save header before loading
    /// anything. Loading a save made in the other mode is refused with a prompt to switch and restart.
    ///
    /// Saves without the entry predate this option, and nothing else in the header tells the modes
    /// apart (TFTV's own mod entry carries the same version numbers on main and on the beta). For
    /// those the save file itself is read (<see cref="DetectReworkInSaveFile"/>): any trace the
    /// rework leaves means beta, none means main branch. Only if the file can't be read is the
    /// player asked, on every load, and the answer is never remembered.
    /// </summary>
    internal static class AircraftReworkMode
    {
        /// <summary>
        /// The mode the game launches in until the player picks one: OFF, since most players are on
        /// the main branch. Keep TFTVAircraftReworkMain.AircraftReworkOn's initialiser in step.
        /// </summary>
        private const bool DefaultOn = false;

        /// <summary>The legacy save the player just confirmed; its reload skips the question.</summary>
        private static PPSavegameMetaData _loadApprovedByPlayer = null;

        /// <summary>
        /// Kept next to the game's mod config (and not in it: TFTVConfig ignores the whole saved
        /// config when its field count changes, which would reset every other setting once).
        /// </summary>
        private const string ModeFileName = "TFTV_AircraftReworkBeta.txt";

        private const string SaveMarkerId = "TFTV_AircraftReworkBeta";
        private const string SaveMarkerOn = "on";
        private const string SaveMarkerOff = "off";

        /// <summary>The mode this game launch plays with. Never changes until the game restarts.</summary>
        internal static bool RunningOn => TFTVAircraftReworkMain.AircraftReworkOn;

        /// <summary>The choice on the new game screen; starting a campaign with a different value than
        /// <see cref="RunningOn"/> needs a restart.</summary>
        internal static bool SelectedForNewGame { get; set; } = DefaultOn;

        /// <summary>
        /// Called first thing in OnModEnabled, before any def is touched or any patch applied.
        /// </summary>
        internal static void LoadAtStartup()
        {
            try
            {
                bool on = DefaultOn;
                string path = GetModeFilePath();

                if (path != null && File.Exists(path))
                {
                    string stored = File.ReadAllText(path).Trim();

                    if (string.Equals(stored, SaveMarkerOn, StringComparison.OrdinalIgnoreCase))
                    {
                        on = true;
                    }
                    else if (string.Equals(stored, SaveMarkerOff, StringComparison.OrdinalIgnoreCase))
                    {
                        on = false;
                    }
                    else
                    {
                        TFTVLogger.Always($"[AircraftReworkMode] unreadable value '{stored}' in {path}, using the default");
                    }
                }

                TFTVAircraftReworkMain.AircraftReworkOn = on;
                SelectedForNewGame = on;

                TFTVLogger.Always($"[AircraftReworkMode] Aircraft Rework Beta is {(on ? "ON" : "OFF")} for this launch (mode file: {path ?? "<none>"})");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static bool StoreForNextLaunch(bool on)
        {
            try
            {
                string path = GetModeFilePath();

                if (path == null)
                {
                    return false;
                }

                File.WriteAllText(path, on ? SaveMarkerOn : SaveMarkerOff);
                TFTVLogger.Always($"[AircraftReworkMode] next launch will run with the Aircraft Rework Beta {(on ? "ON" : "OFF")} ({path})");
                return true;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return false;
            }
        }

        private static string GetModeFilePath()
        {
            return GetDataFilePath(ModeFileName);
        }

        private static string GetDataFilePath(string fileName)
        {
            string root = null;

            try
            {
                root = GameUtl.GameComponent<PlatformComponent>()?.Platform?.GetPlatformData()?.GetFilePathRoot();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            if (string.IsNullOrEmpty(root))
            {
                root = Application.persistentDataPath;
            }

            return string.IsNullOrEmpty(root) ? null : Path.Combine(root, fileName);
        }

        /// <summary>Null for a save made before this option existed.</summary>
        private static bool? ReadSaveMarker(PPSavegameMetaData save)
        {
            if (save?.Mods != null)
            {
                foreach (SaveModEntry entry in save.Mods)
                {
                    if (entry != null && entry.ID == SaveMarkerId)
                    {
                        return !string.Equals(entry.Version, SaveMarkerOff, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Strings only a campaign played with the rework writes into its save: TFTV's geoscape save
        /// data is stored as JSON, and the rest are geoscape event variables and the names of rework
        /// classes serialized with the level. Beta campaigns since the base rework have all of the
        /// JSON ones from day one; earlier beta saves still carry at least one of the others.
        /// Checked against 39 real saves on 2026-09-27: every beta save matched, and none of the
        /// four main-branch ones did.
        /// </summary>
        private static readonly byte[][] _reworkTraces = new[]
        {
            "\"BaseRework\":true",
            "\"NewPowerManagement\":true",
            "\"PersonnelPool\":[",
            "\"AircraftScanningSites\":{\"",
            "\"OperativeAffinities\":[{",
            "TFTV_BaseRework_",
            "TFTV_INCIDENT_",
            "TFTV_AFFINITY_",
            "TFTV.TFTVAircraftRework.",
        }.Select(Encoding.ASCII.GetBytes).ToArray();

        /// <summary>
        /// A tactical save carries the geoscape save it was launched from as a byte[] whose elements
        /// are each written as a 0x03 type tag followed by the byte, so its gzip header reads
        /// 03 1f 03 8b 03 08. The element count is the int32 just before it.
        /// </summary>
        private static readonly byte[] _embeddedGeoscapeStart = { 0x03, 0x1f, 0x03, 0x8b, 0x03, 0x08 };

        /// <summary>
        /// For a save made before the mode marker existed: true if the save, or for a tactical save
        /// the geoscape embedded in it, carries any trace of the rework; false if none; null if the
        /// file could not be read.
        /// </summary>
        private static bool? DetectReworkInSaveFile(PPSavegameMetaData save)
        {
            try
            {
                string path = GetSaveFilePath(save);

                if (path == null)
                {
                    TFTVLogger.Always($"[AircraftReworkMode] could not find the file of '{save?.Name}' (Path '{save?.Path}')");
                    return null;
                }

                Stopwatch stopwatch = Stopwatch.StartNew();
                string found;

                using (FileStream file = File.OpenRead(path))
                {
                    found = FindReworkTraceInSave(file, out string where);
                    found = found == null ? null : $"'{found}' in the {where}";
                }

                TFTVLogger.Always($"[AircraftReworkMode] legacy save '{save?.Name}' ({path}): " +
                    (found != null ? $"found rework trace {found}, so it was played with the Aircraft Rework Beta ON" : "no rework trace, so it was played with the Aircraft Rework Beta OFF") +
                    $" ({stopwatch.ElapsedMilliseconds} ms)");

                return found != null;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

        /// <summary>
        /// The first rework trace in a .zsav stream (gzip), looking in the save itself and then, for a
        /// tactical save, in the geoscape embedded in it; null if there is none. No game state is
        /// touched, so this can run on any save file.
        /// </summary>
        internal static string FindReworkTraceInSave(Stream zsav, out string where)
        {
            byte[] data = Gunzip(zsav);

            where = "save";
            string found = FindReworkTrace(data, 0, data.Length);

            if (found != null)
            {
                return found;
            }

            int start = IndexOf(data, _embeddedGeoscapeStart, 0, data.Length);

            if (start < 5 || data[start - 5] != 0x10)
            {
                return null;
            }

            int count = BitConverter.ToInt32(data, start - 4);

            if (count <= 0 || start + 2L * count > data.Length)
            {
                return null;
            }

            byte[] packed = new byte[count];

            for (int i = 0; i < count; i++)
            {
                packed[i] = data[start + 1 + 2 * i];
            }

            byte[] geoscape;

            using (MemoryStream packedStream = new MemoryStream(packed))
            {
                geoscape = Gunzip(packedStream);
            }

            where = "embedded geoscape";
            return FindReworkTrace(geoscape, 0, geoscape.Length);
        }

        /// <summary>The same file the game opens: the platform's file root + "/" + the record path.</summary>
        private static string GetSaveFilePath(PPSavegameMetaData save)
        {
            if (string.IsNullOrEmpty(save?.Path))
            {
                return null;
            }

            string root = null;

            try
            {
                root = GameUtl.GameComponent<PlatformComponent>()?.Platform?.GetPlatformData()?.GetFilePathRoot();
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
            List<string> candidates = new List<string>();

            if (!string.IsNullOrEmpty(root))
            {
                candidates.Add(root + "/" + save.Path);
            }

            candidates.Add(save.Path);

            foreach (string candidate in candidates.ToList())
            {
                if (!Path.HasExtension(candidate))
                {
                    candidates.Add(candidate + ".zsav");
                }
            }

            return candidates.FirstOrDefault(File.Exists);
        }

        private static byte[] Gunzip(Stream compressed)
        {
            using (GZipStream gzip = new GZipStream(compressed, CompressionMode.Decompress))
            using (MemoryStream output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }

        private static string FindReworkTrace(byte[] data, int start, int end)
        {
            foreach (byte[] trace in _reworkTraces)
            {
                if (IndexOf(data, trace, start, end) >= 0)
                {
                    return Encoding.ASCII.GetString(trace);
                }
            }

            return null;
        }

        private static int IndexOf(byte[] data, byte[] pattern, int start, int end)
        {
            int last = end - pattern.Length;
            byte first = pattern[0];

            for (int i = start; i <= last; i++)
            {
                if (data[i] != first)
                {
                    continue;
                }

                int k = 1;

                while (k < pattern.Length && data[i + k] == pattern[k])
                {
                    k++;
                }

                if (k == pattern.Length)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// False when the save was made in the other mode, or when it is a legacy save whose mode is
        /// not known yet; the player is then asked or offered to switch and restart. Called from TFTV's
        /// LoadGame prefix before it clears anything, so a refused load leaves the running campaign
        /// untouched.
        /// </summary>
        internal static bool CanLoad(PPSavegameMetaData save)
        {
            try
            {
                if (save != null && ReferenceEquals(save, _loadApprovedByPlayer))
                {
                    _loadApprovedByPlayer = null;
                    return true;
                }

                bool? saveOn = ReadSaveMarker(save);

                if (saveOn == null)
                {
                    saveOn = DetectReworkInSaveFile(save);

                    if (saveOn == null)
                    {
                        TFTVLogger.Always($"[AircraftReworkMode] '{save?.Name}' predates the Aircraft Rework Beta setting and could not be read; asking the player");
                        AskAboutLegacySave(save);
                        return false;
                    }
                }

                if (saveOn.Value == RunningOn)
                {
                    return true;
                }

                TFTVLogger.Always($"[AircraftReworkMode] refusing to load '{save?.Name}': made with the Aircraft Rework Beta {(saveOn.Value ? "ON" : "OFF")}, running {(RunningOn ? "ON" : "OFF")}");

                PromptSwitchAndRestart(saveOn.Value, saveOn.Value ? "TFTV_AIRCRAFT_REWORK_SAVE_NEEDS_ON" : "TFTV_AIRCRAFT_REWORK_SAVE_NEEDS_OFF");
                return false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return true;
            }
        }

        /// <summary>
        /// The load was refused while the question is open. If the answer matches this launch's mode,
        /// the load is started again, approved for that save; otherwise the player is offered to
        /// switch and restart, and is asked again when loading the save after the restart.
        /// </summary>
        private static void AskAboutLegacySave(PPSavegameMetaData save)
        {
            GameUtl.GetMessageBox().ShowSimplePrompt(TFTVCommonMethods.ConvertKeyToString("TFTV_AIRCRAFT_REWORK_LEGACY_SAVE_PROMPT"), MessageBoxIcon.Question, MessageBoxButtons.YesNo, res =>
            {
                try
                {
                    // Dismissed without an answer: nothing is loaded.
                    if (res.DialogResult != MessageBoxResult.Yes && res.DialogResult != MessageBoxResult.No)
                    {
                        return;
                    }

                    bool saveOn = res.DialogResult == MessageBoxResult.Yes;
                    TFTVLogger.Always($"[AircraftReworkMode] player says '{save?.Name}' was played with the Aircraft Rework Beta {(saveOn ? "ON" : "OFF")}");

                    if (saveOn != RunningOn)
                    {
                        PromptSwitchAndRestart(saveOn, saveOn ? "TFTV_AIRCRAFT_REWORK_SAVE_NEEDS_ON" : "TFTV_AIRCRAFT_REWORK_SAVE_NEEDS_OFF");
                        return;
                    }

                    _loadApprovedByPlayer = save;
                    PhoenixGame game = GameUtl.GameComponent<PhoenixGame>();
                    game.Timing.Start(game.SaveManager.LoadGame(save));
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            });
        }

        /// <summary>
        /// False when the new game screen is set to the other mode; the player is then offered to
        /// switch and restart.
        /// </summary>
        internal static bool CanStartNewGame()
        {
            try
            {
                if (SelectedForNewGame == RunningOn)
                {
                    return true;
                }

                PromptSwitchAndRestart(SelectedForNewGame, SelectedForNewGame ? "TFTV_AIRCRAFT_REWORK_RESTART_TO_ENABLE" : "TFTV_AIRCRAFT_REWORK_RESTART_TO_DISABLE");
                return false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return true;
            }
        }

        private static void PromptSwitchAndRestart(bool targetOn, string textKey)
        {
            GameUtl.GetMessageBox().ShowSimplePrompt(TFTVCommonMethods.ConvertKeyToString(textKey), MessageBoxIcon.Warning, MessageBoxButtons.YesNo, res =>
            {
                try
                {
                    if (res.DialogResult != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    if (!StoreForNextLaunch(targetOn))
                    {
                        GameUtl.GetMessageBox().ShowSimplePrompt(TFTVCommonMethods.ConvertKeyToString("TFTV_AIRCRAFT_REWORK_SWITCH_FAILED"), MessageBoxIcon.Error, MessageBoxButtons.OK, null);
                        return;
                    }

                    RestartGame();
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            });
        }

        private static void RestartGame()
        {
            ScheduleRelaunch();
            QuitGame();
        }

        /// <summary>
        /// Starts a hidden helper that waits for this process to exit and then runs the same
        /// executable with the same command line. The executable is launched directly rather than
        /// through steam://: Steam may count the helper, a child of the game, as the game still
        /// running and refuse. The helper inherits the environment Steam launched the game with
        /// (SteamAppId and friends), so the new instance still connects to Steam. Epic passes its
        /// login on the command line, which is reused as is.
        /// If this fails, the game just quits and the player starts it again.
        /// </summary>
        private static void ScheduleRelaunch()
        {
            try
            {
                Process current = Process.GetCurrentProcess();
                string exe = current.MainModule?.FileName;

                if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
                {
                    TFTVLogger.Always($"[AircraftReworkMode] cannot find the game executable ('{exe}'), not relaunching");
                    return;
                }

                string arguments = string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Select(QuoteArgument));

                string script =
                    $"Wait-Process -Id {current.Id} -ErrorAction SilentlyContinue; " +
                    "Start-Sleep -Seconds 2; " +
                    $"Start-Process -FilePath {QuotePowerShell(exe)} -WorkingDirectory {QuotePowerShell(Path.GetDirectoryName(exe))}" +
                    (arguments.Length > 0 ? $" -ArgumentList {QuotePowerShell(arguments)}" : "");

                ProcessStartInfo startInfo = new ProcessStartInfo("powershell.exe",
                    "-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -EncodedCommand " +
                    Convert.ToBase64String(Encoding.Unicode.GetBytes(script)))
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process.Start(startInfo);
                TFTVLogger.Always($"[AircraftReworkMode] relaunch scheduled: {exe} {arguments}");
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>Windows command-line quoting for one argument.</summary>
        private static string QuoteArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                return "\"\"";
            }

            if (argument.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
            {
                return argument;
            }

            return "\"" + argument.Replace("\"", "\\\"") + "\"";
        }

        private static string QuotePowerShell(string value)
        {
            return "'" + value.Replace("'", "''") + "'";
        }

        private static void QuitGame()
        {
            try
            {
                // The game's own quit: drops the loading curtain, then Application.Quit.
                MethodInfo quitGame = AccessTools.Method(typeof(Base.Core.Game), "QuitGame");
                PhoenixGame game = GameUtl.GameComponent<PhoenixGame>();

                if (quitGame != null && game != null)
                {
                    quitGame.Invoke(game, null);
                    return;
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            Application.Quit();
        }

        /// <summary>
        /// Asked every time the new game screen opens. The answer sets the AIRCRAFT REWORK BETA
        /// option on that screen, which the player can still change before starting.
        /// </summary>
        internal static void ShowNewGamePrompt(Action<bool> onAnswer)
        {
            try
            {
                GameUtl.GetMessageBox().ShowSimplePrompt(TFTVCommonMethods.ConvertKeyToString("TFTV_AIRCRAFT_REWORK_NEW_GAME_PROMPT"), MessageBoxIcon.Question, MessageBoxButtons.YesNo, res =>
                {
                    try
                    {
                        // Dismissed without an answer: leave the option as it was.
                        if (res.DialogResult != MessageBoxResult.Yes && res.DialogResult != MessageBoxResult.No)
                        {
                            return;
                        }

                        bool on = res.DialogResult == MessageBoxResult.Yes;
                        TFTVLogger.Always($"[AircraftReworkMode] new game prompt answered: Aircraft Rework Beta {(on ? "ON" : "OFF")}");
                        onAnswer?.Invoke(on);
                    }
                    catch (Exception e)
                    {
                        TFTVLogger.Error(e);
                    }
                });
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        /// <summary>
        /// Every save path (manual, quick, auto, ironman) fills the metadata's mod list here, and
        /// the list is written to the save header.
        /// </summary>
        [HarmonyPatch(typeof(ModManager), nameof(ModManager.SetSaveUsingMods))]
        internal static class ModManager_SetSaveUsingMods_Patch
        {
            private static void Postfix(PPSavegameMetaData save)
            {
                try
                {
                    if (save?.Mods == null)
                    {
                        return;
                    }

                    save.Mods.RemoveAll(entry => entry != null && entry.ID == SaveMarkerId);
                    save.Mods.Add(new SaveModEntry { ID = SaveMarkerId, Version = RunningOn ? SaveMarkerOn : SaveMarkerOff });
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        /// <summary>
        /// The marker is not a mod, so it must not show up in the "save uses missing mods" warning.
        /// </summary>
        [HarmonyPatch(typeof(ModManager), nameof(ModManager.GetMissingModsInSave))]
        internal static class ModManager_GetMissingModsInSave_Patch
        {
            private static void Postfix(List<SaveModEntry> __result)
            {
                try
                {
                    __result?.RemoveAll(entry => entry != null && entry.ID == SaveMarkerId);
                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }
    }
}
