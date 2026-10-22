using Base.Core;
using Base.Serialization;
using Base.UI.MessageBox;
using System;
using System.Linq;
using System.Text;

namespace TFTV
{
    /// <summary>
    /// Saves from a campaign started without TFTV can't be played with it (TFTV's defs, events and save data are
    /// all missing), so loading one is refused with a message to start a new game.
    ///
    /// A TFTV save is recognised by TFTV in the save header's mod list (or the Aircraft Rework marker TFTV adds
    /// there). Older saves don't always list it in the header, so the save body is checked next: TFTV's own mod
    /// data is stored under its mod ID, and its serialized classes are named "TFTV." (every one of the 19 local
    /// TFTV saves checked on 2026-10-21 had both). A tactical save is checked in its embedded geoscape too. Only a
    /// save that could be read and has none of this is refused; anything unreadable loads as before.
    /// </summary>
    internal static class TFTVVanillaSaveGuard
    {
        private const string TFTVModId = "phoenixrising.tftv";

        private static readonly byte[][] TFTVTraces = new[]
        {
            TFTVModId,
            "TFTV.",
        }.Select(Encoding.ASCII.GetBytes).ToArray();

        /// <summary>Called from TFTV's LoadGame prefix before anything is cleared.</summary>
        internal static bool CanLoad(PPSavegameMetaData save)
        {
            try
            {
                if (save == null)
                {
                    return true;
                }

                if (save.Mods != null && save.Mods.Any(entry => entry != null && (entry.ID == TFTVModId || entry.ID == AircraftReworkMode.SaveMarkerId)))
                {
                    return true;
                }

                string found = AircraftReworkMode.FindTraceInSaveFile(save, TFTVTraces, out bool readable, out string where);

                if (!readable)
                {
                    TFTVLogger.Always($"[VanillaSaveGuard] could not read '{save.Name}' to check it was made with TFTV; loading it");
                    return true;
                }

                if (found != null)
                {
                    TFTVLogger.Always($"[VanillaSaveGuard] '{save.Name}' has no TFTV header entry but '{found}' in the {where}: a TFTV save");
                    return true;
                }

                TFTVLogger.Always($"[VanillaSaveGuard] refusing to load '{save.Name}': it was not made with TFTV");
                GameUtl.GetMessageBox().ShowSimplePrompt(TFTVCommonMethods.ConvertKeyToString("TFTV_VANILLA_SAVE_REFUSED"), MessageBoxIcon.Warning, MessageBoxButtons.OK, null);
                return false;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return true;
            }
        }
    }
}
