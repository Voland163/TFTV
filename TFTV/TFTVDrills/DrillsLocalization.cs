namespace TFTV.TFTVDrills
{
    /// <summary>
    /// Every piece of text the drills screens put in front of the player, by key.
    ///
    /// The strings themselves live in TFTV_AbilitiesEffectsStatusesTactical_Localization.csv
    /// alongside the drill names and descriptions; nothing here should ever be a literal
    /// shown in the UI.
    /// </summary>
    internal static class DrillsText
    {
        internal static string Get(string key)
        {
            return TFTVCommonMethods.ConvertKeyToString(key);
        }

        internal static string Format(string key, params object[] arguments)
        {
            return string.Format(Get(key), arguments);
        }

        #region Confirmation header

        internal const string HeaderReplaceAbility = "TFTV_DRILLS_UI_HEADER_REPLACE_ABILITY";
        internal const string HeaderAcquireDrill = "TFTV_DRILLS_UI_HEADER_ACQUIRE_DRILL";
        internal const string HeaderReplaceDrill = "TFTV_DRILLS_UI_HEADER_REPLACE_DRILL";
        internal const string HeaderReplaceAbilityNamed = "TFTV_DRILLS_UI_HEADER_REPLACE_NAMED";
        internal const string HeaderAcquireAbilityNamed = "TFTV_DRILLS_UI_HEADER_ACQUIRE_NAMED";

        #endregion

        #region Drill list

        internal const string NoDrillsAvailable = "TFTV_DRILLS_UI_NO_DRILLS_AVAILABLE";
        internal const string AlreadyAcquired = "TFTV_DRILLS_UI_ALREADY_ACQUIRED";
        internal const string LevelRequirementNotMet = "TFTV_DRILLS_UI_LEVEL_REQUIREMENT_NOT_MET";
        internal const string NotEnoughSkillPoints = "TFTV_DRILLS_UI_NOT_ENOUGH_SKILL_POINTS";
        internal const string RequirementsNotMet = "TFTV_DRILLS_UI_REQUIREMENTS_NOT_MET";
        internal const string RequiresTrainingFacilityLine1 = "TFTV_DRILLS_UI_REQUIRES_FACILITY_LINE1";
        internal const string RequiresTrainingFacilityLine2 = "TFTV_DRILLS_UI_REQUIRES_FACILITY_LINE2";

        #endregion

        #region Swap prompts

        internal const string UnnamedAbility = "TFTV_DRILLS_UI_UNNAMED_ABILITY";
        internal const string CannotReplaceForAcquiredDrill = "TFTV_DRILLS_UI_CANNOT_REPLACE_ACQUIRED";
        internal const string CannotReplaceForSelectedDrill = "TFTV_DRILLS_UI_CANNOT_REPLACE_SELECTED";
        internal const string CannotReplaceForNamedDrills = "TFTV_DRILLS_UI_CANNOT_REPLACE_NAMED";
        internal const string NotEnoughSkillPointsPrompt = "TFTV_DRILLS_UI_NOT_ENOUGH_SP_PROMPT";

        #endregion

        #region Unlock requirements

        internal const string ProficiencyRequirement = "TFTV_DRILLS_UI_PROFICIENCY_REQUIREMENT";
        internal const string ProficiencyRequirementFallback = "TFTV_DRILLS_UI_PROFICIENCY_FALLBACK";
        internal const string ProficiencyRequirementSeparator = "TFTV_DRILLS_UI_PROFICIENCY_SEPARATOR";

        #endregion
    }
}
