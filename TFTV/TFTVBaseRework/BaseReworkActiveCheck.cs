

namespace TFTV.TFTVBaseRework
{
    internal static class BaseReworkCheck
    {
        /// <summary>
        /// The base rework only ever runs alongside the aircraft rework.
        ///
        /// Requiring both is what makes the aircraft rework switch a hard off for the main branch.
        /// The per-game flag on its own is not enough: it is written into the save, so a save made
        /// during the beta carries BaseRework = true and would switch the whole system back on when
        /// loaded on a build where the aircraft rework is off - and the legacy-save fallback in
        /// TFTVGeoscape sets it deliberately. Gating here means every check of this property is a
        /// check of both, rather than relying on each of the eighty-odd call sites to remember.
        /// </summary>
        internal static bool BaseReworkEnabled => TFTVAircraftReworkMain.AircraftReworkOn && TFTVNewGameOptions.BaseRework;
    }
}
