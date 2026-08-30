namespace TFTV.TFTVBaseRework
{
    /// <summary>
    /// Every piece of text the personnel screen puts in front of the player, by key.
    ///
    /// The strings themselves live in TFTV_BaseReworkPersonnel_Localization.csv; nothing here should
    /// ever be a literal shown in the UI.
    /// </summary>
    internal static class PersonnelText
    {
        internal static string Get(string key)
        {
            return TFTVCommonMethods.ConvertKeyToString(key);
        }

        internal static string Format(string key, params object[] arguments)
        {
            return string.Format(Get(key), arguments);
        }

        #region Roster

        internal const string RosterTitle = "KEY_TFTV_PERSONNEL_ROSTER_TITLE";
        internal const string RosterEmpty = "KEY_TFTV_PERSONNEL_ROSTER_EMPTY";
        internal const string FilterAll = "KEY_TFTV_PERSONNEL_FILTER_ALL";
        internal const string FilterAssigned = "KEY_TFTV_PERSONNEL_FILTER_ASSIGNED";
        internal const string FilterUnassigned = "KEY_TFTV_PERSONNEL_FILTER_UNASSIGNED";
        internal const string FilterCount = "KEY_TFTV_PERSONNEL_FILTER_COUNT";
        internal const string AutoAssign = "KEY_TFTV_PERSONNEL_AUTO_ASSIGN";

        internal const string StatusFieldDuty = "KEY_TFTV_PERSONNEL_STATUS_FIELD_DUTY";
        internal const string StatusResearch = "KEY_TFTV_PERSONNEL_STATUS_RESEARCH";
        internal const string StatusFabrication = "KEY_TFTV_PERSONNEL_STATUS_FABRICATION";
        internal const string StatusDismissed = "KEY_TFTV_PERSONNEL_STATUS_DISMISSED";
        internal const string StatusIdle = "KEY_TFTV_PERSONNEL_STATUS_IDLE";
        internal const string StatusUnknownName = "KEY_TFTV_PERSONNEL_STATUS_UNKNOWN_NAME";

        internal const string ActionDismiss = "KEY_TFTV_PERSONNEL_ACTION_DISMISS";
        internal const string ActionDeploy = "KEY_TFTV_PERSONNEL_ACTION_DEPLOY";

        internal const string DismissPrompt = "KEY_TFTV_PERSONNEL_DISMISS_PROMPT";
        internal const string DismissRedeployCost = "KEY_TFTV_PERSONNEL_DISMISS_REDEPLOY_COST";
        internal const string DismissTrainingCapped = "KEY_TFTV_PERSONNEL_DISMISS_TRAINING_CAPPED";
        internal const string DismissTrainingAvailable = "KEY_TFTV_PERSONNEL_DISMISS_TRAINING_AVAILABLE";
        internal const string DismissGrunt = "KEY_TFTV_PERSONNEL_DISMISS_GRUNT";
        internal const string DismissCivilian = "KEY_TFTV_PERSONNEL_DISMISS_CIVILIAN";
        internal const string DismissFailed = "KEY_TFTV_PERSONNEL_DISMISS_FAILED";

        #endregion

        #region Work panels

        internal const string ResearchTitle = "KEY_TFTV_PERSONNEL_RESEARCH_TITLE";
        internal const string FabricationTitle = "KEY_TFTV_PERSONNEL_FABRICATION_TITLE";
        internal const string UnassignAll = "KEY_TFTV_PERSONNEL_UNASSIGN_ALL";
        internal const string ResearchBoost = "KEY_TFTV_PERSONNEL_RESEARCH_BOOST";
        internal const string ManufacturingBoost = "KEY_TFTV_PERSONNEL_MANUFACTURING_BOOST";
        internal const string LabsBuilt = "KEY_TFTV_PERSONNEL_LABS_BUILT";
        internal const string PlantsBuilt = "KEY_TFTV_PERSONNEL_PLANTS_BUILT";
        internal const string NoWorkers = "KEY_TFTV_PERSONNEL_NO_WORKERS";

        #endregion

        #region Training panel

        internal const string TrainingTitle = "KEY_TFTV_PERSONNEL_TRAINING_TITLE";
        internal const string TrainingEmpty = "KEY_TFTV_PERSONNEL_TRAINING_EMPTY";
        internal const string TrainingReady = "KEY_TFTV_PERSONNEL_TRAINING_READY";
        internal const string PhoenixSkillPoints = "KEY_TFTV_PERSONNEL_PHOENIX_SP";
        internal const string ButtonTrain = "KEY_TFTV_PERSONNEL_BUTTON_TRAIN";
        internal const string ButtonDeploy = "KEY_TFTV_PERSONNEL_BUTTON_DEPLOY";
        internal const string WhoTrains = "KEY_TFTV_PERSONNEL_WHO_TRAINS";
        internal const string WhoDeploys = "KEY_TFTV_PERSONNEL_WHO_DEPLOYS";
        internal const string NoTrainCandidates = "KEY_TFTV_PERSONNEL_NO_TRAIN_CANDIDATES";
        internal const string NoDeployCandidates = "KEY_TFTV_PERSONNEL_NO_DEPLOY_CANDIDATES";
        internal const string CandidateLevel = "KEY_TFTV_PERSONNEL_CANDIDATE_LEVEL";
        internal const string CandidateDismissed = "KEY_TFTV_PERSONNEL_CANDIDATE_DISMISSED";
        internal const string CandidateTraining = "KEY_TFTV_PERSONNEL_CANDIDATE_TRAINING";

        #endregion

        #region Dialogs

        internal const string DialogNotice = "KEY_TFTV_PERSONNEL_DIALOG_NOTICE";
        internal const string DialogConfirm = "KEY_TFTV_PERSONNEL_DIALOG_CONFIRM";
        internal const string DialogClose = "KEY_TFTV_PERSONNEL_DIALOG_CLOSE";
        internal const string DialogYes = "KEY_TFTV_PERSONNEL_DIALOG_YES";
        internal const string DialogNo = "KEY_TFTV_PERSONNEL_DIALOG_NO";

        internal const string ChooseAction = "KEY_TFTV_PERSONNEL_CHOOSE_ACTION";
        internal const string DeployNow = "KEY_TFTV_PERSONNEL_DEPLOY_NOW";
        internal const string TrainFirst = "KEY_TFTV_PERSONNEL_TRAIN_FIRST";
        internal const string TrainFirstNoSlots = "KEY_TFTV_PERSONNEL_TRAIN_FIRST_NO_SLOTS";
        internal const string SelectBase = "KEY_TFTV_PERSONNEL_SELECT_BASE";
        internal const string SelectClass = "KEY_TFTV_PERSONNEL_SELECT_CLASS";
        internal const string SelectLevel = "KEY_TFTV_PERSONNEL_SELECT_LEVEL";
        internal const string NoFacilitySlot = "KEY_TFTV_PERSONNEL_NO_FACILITY_SLOT";
        internal const string ClassUnknown = "KEY_TFTV_PERSONNEL_CLASS_UNKNOWN";

        internal const string EarlyDeploy = "KEY_TFTV_PERSONNEL_EARLY_DEPLOY";
        internal const string EarlyDeployRedeployCost = "KEY_TFTV_PERSONNEL_EARLY_DEPLOY_REDEPLOY_COST";
        internal const string EarlyDeployConfirm = "KEY_TFTV_PERSONNEL_EARLY_DEPLOY_CONFIRM";
        internal const string RedeployConfirm = "KEY_TFTV_PERSONNEL_REDEPLOY_CONFIRM";
        internal const string NotEnoughSpRedeploy = "KEY_TFTV_PERSONNEL_NOT_ENOUGH_SP_REDEPLOY";
        internal const string NotEnoughSp = "KEY_TFTV_PERSONNEL_NOT_ENOUGH_SP";
        internal const string AlreadyMaxLevel = "KEY_TFTV_PERSONNEL_ALREADY_MAX_LEVEL";
        internal const string TrainOption = "KEY_TFTV_PERSONNEL_TRAIN_OPTION";
        internal const string TrainOptionUnaffordable = "KEY_TFTV_PERSONNEL_TRAIN_OPTION_UNAFFORDABLE";
        internal const string TrainConfirm = "KEY_TFTV_PERSONNEL_TRAIN_CONFIRM";
        internal const string TrainConfirmDismissed = "KEY_TFTV_PERSONNEL_TRAIN_CONFIRM_DISMISSED";

        internal const string LivingQuartersFull = "KEY_TFTV_PERSONNEL_LIVING_QUARTERS_FULL";
        internal const string DutyResearch = "KEY_TFTV_PERSONNEL_DUTY_RESEARCH";
        internal const string DutyManufacturing = "KEY_TFTV_PERSONNEL_DUTY_MANUFACTURING";
        internal const string DutyTraining = "KEY_TFTV_PERSONNEL_DUTY_TRAINING";

        internal const string AssignmentTrainingQueued = "KEY_TFTV_PERSONNEL_ASSIGNMENT_TRAINING_QUEUED";
        internal const string AssignmentTrainingComplete = "KEY_TFTV_PERSONNEL_ASSIGNMENT_TRAINING_COMPLETE";
        internal const string AssignmentTrainingProgress = "KEY_TFTV_PERSONNEL_ASSIGNMENT_TRAINING_PROGRESS";
        internal const string ClassFallback = "KEY_TFTV_PERSONNEL_CLASS_FALLBACK";

        #endregion

        #region Dossier

        internal const string DossierAbilities = "KEY_TFTV_PERSONNEL_DOSSIER_ABILITIES";
        internal const string DossierStorage = "KEY_TFTV_PERSONNEL_DOSSIER_STORAGE";
        internal const string DossierNone = "KEY_TFTV_PERSONNEL_DOSSIER_NONE";
        internal const string DossierNothing = "KEY_TFTV_PERSONNEL_DOSSIER_NOTHING";
        internal const string DossierNoClass = "KEY_TFTV_PERSONNEL_DOSSIER_NO_CLASS";
        internal const string DossierSkillPoints = "KEY_TFTV_PERSONNEL_DOSSIER_SP";
        internal const string DossierExperience = "KEY_TFTV_PERSONNEL_DOSSIER_XP";
        internal const string DossierSkillPointsTooltip = "KEY_TFTV_PERSONNEL_DOSSIER_SP_TOOLTIP";
        internal const string DossierExperienceTooltip = "KEY_TFTV_PERSONNEL_DOSSIER_XP_TOOLTIP";

        internal const string StatStrength = "KEY_TFTV_PERSONNEL_STAT_STRENGTH";
        internal const string StatWillpower = "KEY_TFTV_PERSONNEL_STAT_WILLPOWER";
        internal const string StatSpeed = "KEY_TFTV_PERSONNEL_STAT_SPEED";
        internal const string StatPerception = "KEY_TFTV_PERSONNEL_STAT_PERCEPTION";
        internal const string StatAccuracy = "KEY_TFTV_PERSONNEL_STAT_ACCURACY";
        internal const string StatStealth = "KEY_TFTV_PERSONNEL_STAT_STEALTH";
        internal const string StatDelirium = "KEY_TFTV_PERSONNEL_STAT_DELIRIUM";

        internal const string StatNoteTrained = "KEY_TFTV_PERSONNEL_STAT_NOTE_TRAINED";
        internal const string StatNoteTraining = "KEY_TFTV_PERSONNEL_STAT_NOTE_TRAINING";
        internal const string StatNotePerception = "KEY_TFTV_PERSONNEL_STAT_NOTE_PERCEPTION";
        internal const string StatNoteAccuracy = "KEY_TFTV_PERSONNEL_STAT_NOTE_ACCURACY";
        internal const string StatNoteStealth = "KEY_TFTV_PERSONNEL_STAT_NOTE_STEALTH";

        #endregion
    }

    /// <summary>
    /// Text the rest of the base rework shows the player - base activation, ransacking,
    /// the initial loot preview, the training facility and the fallen operatives panel.
    ///
    /// Same rule as <see cref="PersonnelText"/>: the strings live in
    /// TFTV_BaseReworkPersonnel_Localization.csv, never as literals here.
    /// </summary>
    internal static class BaseReworkText
    {
        internal static string Get(string key)
        {
            return TFTVCommonMethods.ConvertKeyToString(key);
        }

        internal static string Format(string key, params object[] arguments)
        {
            return string.Format(Get(key), arguments);
        }

        internal const string ActivationFailed = "KEY_TFTV_BASE_ACTIVATION_FAILED";
        internal const string AmbushChance = "KEY_TFTV_BASE_AMBUSH_CHANCE";
        internal const string RansackPayout = "KEY_TFTV_BASE_RANSACK_PAYOUT";
        internal const string LootRandomEquipment = "KEY_TFTV_BASE_LOOT_RANDOM_EQUIPMENT";
        internal const string TrainingNoStatGains = "KEY_TFTV_BASE_TRAINING_NO_STAT_GAINS";
        internal const string TrainingStatGains = "KEY_TFTV_BASE_TRAINING_STAT_GAINS";

        internal const string FallenHeader = "KEY_TFTV_BASE_FALLEN_HEADER";
        internal const string FallenProjectOsiris = "KEY_TFTV_BASE_FALLEN_PROJECT_OSIRIS";
        internal const string FallenMissionsAndKills = "KEY_TFTV_BASE_FALLEN_MISSIONS_KILLS";
        internal const string FallenFavouriteWeapon = "KEY_TFTV_BASE_FALLEN_FAVOURITE_WEAPON";
        internal const string FallenFavouriteSkill = "KEY_TFTV_BASE_FALLEN_FAVOURITE_SKILL";
        internal const string FallenSkillPointsReturned = "KEY_TFTV_BASE_FALLEN_SP_RETURNED";
    }
}
