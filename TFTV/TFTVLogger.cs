using Base.Core;
using Base.UI.MessageBox;
using System;

namespace TFTV
{
    public class TFTVLogger
    {
        private static int _debugLevel;
        private static string _modName;
        private static bool _awake;

        private static readonly TFTVLogPrefix _prefix = new TFTVLogPrefix();

        public static void Initialize(string logPath, bool debugLevel, string modDirectory, string modName)
        {
            if (debugLevel)
            {
                _debugLevel = 1;
            }
            else
            {
                _debugLevel = 0;
            }
            ;
            _modName = modName;
            _awake = true;

            TFTVLogFile.SetPath(logPath);
            _prefix.SetModName(modName);

            Cleanup();
            Always(TFTVLogFile.Separator, false);
            Always($"Logger.Initialize({logPath}, {debugLevel}, {modDirectory}, {modName})");
            Always(TFTVLogFile.Separator, false);
        }


        public static void Sleep()
        {
            _awake = false;
        }

        public static void Wake()
        {
            _awake = true;
        }


        public static void Cleanup()
        {
            TFTVLogFile.Truncate();
            TFTVLogFile.WriteLine(null, TFTVLogFile.Separator);
            TFTVLogFile.WriteLine(null, $"[{_modName} @ {DateTime.Now}] CLEANED UP");
            TFTVLogFile.WriteLine(null, TFTVLogFile.Separator);
        }


        public static void Error(Exception ex)
        {
            if (_awake && _debugLevel >= 1)
            {
                TFTVLogFile.WriteLine(null, TFTVLogFile.Separator);
                TFTVLogFile.WriteLine(null, $"[{_modName} @ {DateTime.Now}] EXCEPTION:");
                TFTVLogFile.WriteLine(null, "Message: " + ex.Message + "<br/>" + Environment.NewLine + "StackTrace: " + ex.StackTrace);
                TFTVLogFile.WriteLine(null, TFTVLogFile.Separator);

                GameUtl.GetMessageBox().ShowSimplePrompt($"<b>An error has occurred in the Terror from the Void mod!</b>\nPlease report it in our #bug-reporting channel at the Terror from the Void Discord server by posting the log you can find at {TFTVMain.LogPath}." +
                    $"\n\n<b>CAUTION:</b>\nContinuing this run may result in unstable behavior or even cause the game to crash", MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
            }
        }


        public static void Debug(string line, bool showPrefix = true)
        {
            if (_awake && _debugLevel >= 2)
            {
                TFTVLogFile.WriteLine(showPrefix ? _prefix.Get() : null, line);
            }
        }


        public static void Info(string line, bool showPrefix = true)
        {
            if (_awake && _debugLevel >= 3)
            {
                Debug(line, showPrefix);
            }
        }


        public static void Always(string line, bool showPrefix = true)
        {
            TFTVLogFile.WriteLine(showPrefix ? _prefix.Get() : null, line);
        }
    }
}
