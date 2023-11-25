using System.Collections.Generic;

namespace PRMBetterClasses
{
    public class BCSettings
    {
        // Dictionary of special Characters identified by their name to give them a special skill set on the 3rd row (personal perk row)
        // The string key for the outer dictionary is the name of the character
        // The int key of the inner dictionary (value of the outer dictionary) is the level spot where the ability should get on
        // The string value of the inner dictionagy is finally the ability def name of the skill
        public Dictionary<string, Dictionary<int, string>> SpecialCharacterPersonalSkills = new Dictionary<string, Dictionary<int, string>>();

        public Dictionary<string, float> BuffsForAdditionalProficiency = new Dictionary<string, float>
        {
            { Proficiency.Buff, 0.0f }
        };
        public List<ClassSpecDef> ClassSpecializations = new List<ClassSpecDef>
        {
            new ClassSpecDef(
                classDef: ClassKeys.Assault,
                mainSpec: new string[]
                {
                    "",
                    "QUICK AIM",
                    "KILL'N'RUN",
                    "",
                    "READY FOR ACTION",
                    "ONSLAUGHT",
                    "RAPID CLEARANCE"
                }),
            new ClassSpecDef(
                classDef: ClassKeys.Heavy,
                mainSpec: new string[]
                {
                    "",
                    "RETURN FIRE",
                    "HUNKER DOWN",
                    "",
                    "SKIRMISHER",
                    "SHRED RESISTANCE",
                    "RAGE BURST"
                }),
            new ClassSpecDef(
                classDef: ClassKeys.Sniper,
                mainSpec: new string[]
                {
                    "",
                    "EXTREME FOCUS",
                    "ARMOR BREAK", // ARMOR BREAK
                    "",
                    "MASTER MARKSMAN",
                    "INSPIRE",
                    "MARKED FOR DEATH"
                }),
            new ClassSpecDef(
                classDef: ClassKeys.Berserker,
                mainSpec: new string[]
                {
                    "",
                    "DASH",
                    "CLOSE QUARTERS EVADE",
                    "",
                    "BLOODLUST",
                    "IGNORE PAIN",
                    "ADRENALINE RUSH"
                }),
            new ClassSpecDef(
                classDef: ClassKeys.Priest,
                mainSpec: new string[]
                {
                    "",
                    "MIND CONTROL",
                    "INDUCE PANIC",
                    "",
                    "MIND SENSE",
                    "PSYCHIC WARD",
                    "MIND CRUSH"
                }),
            new ClassSpecDef(
                classDef: ClassKeys.Technician,
                mainSpec: new string[]
                {
                    "",
                    "FAST USE",
                    "ELECTRIC REINFORCEMENT",
                    "",
                    "STABILITY",
                    "FIELD MEDIC",
                    "AMPLIFY PAIN"
                }),
            new ClassSpecDef(
                classDef: ClassKeys.Infiltrator,
                mainSpec: new string[]
                {
                    "",
                    "SURPRISE ATTACK",
                    "DEPLOY DECOY",
                    "",
                    "NEURAL FEEDBACK", // NEURAL FEEDBACK
                    "JAMMING FIELD", // JAMMING FIELD
                    "PARAPSYCHOSIS" // PARAPSYCHOSIS ex SNEAK ATTACK
                }),
        };
        public string[] OrderOfPersonalPerks = new string[]
        {
                PerkType.Background,
                PerkType.Faction_1,
                PerkType.Class_1,
                PerkType.Proficiency,
                PerkType.Background,
                PerkType.Class_2,
                PerkType.Faction_2,
        };
        public List<PersonalPerksDef> PersonalPerks = new List<PersonalPerksDef>()
        {   new PersonalPerksDef(
                key: PerkType.Background,
                //isRandom: true,
                spc: 10,
                rngList: new List<string>
                {
                    "SURVIVOR",
                    "NURSE",
                    "SCAV",
                    "CORPSE DISPOSER",
                    "HARD LABOR",
                    "SQUATTER",
                    "VOLUNTEERED",
                    "CONDO RAIDER",
                    "TUNNEL RAT",
                    "HUNTER",
                    "TROUBLEMAKER",
                    "PARANOID",
                    "PRIVILEGED",
                    "A HISTORY OF VIOLENCE",
                    "DAREDEVIL",
                    "DAMAGED AMYGDALA",
                    "SANITATION EXPERT",
                    "LAB ASSISTANT",
                    "ROCKETEER",
                    "TRUE GRIT"
                }),
            new PersonalPerksDef(
                key: PerkType.Proficiency,
                //isRandom: true,
                spc: 15,
                rngList: new List<string>
                {
                    "HANDGUN PROFICIENCY",
                    "PDW PROFICIENCY",
                    "MELEE WEAPON PROFICIENCY",
                    "ASSAULT RIFLE PROFICIENCY",
                    "SHOTGUN PROFICIENCY",
                    "SNIPER RIFLE PROFICIENCY",
                    "HEAVY WEAPON PROFICIENCY",
                    //"MOUNTED WEAPON PROFICIENCY"
                }),
            new PersonalPerksDef(
                perkKey: PerkType.Class_1,
                isRandom: false,
                spCost: 20,
                relList: new Dictionary<string, Dictionary<string, string>>
                {{ FactionKeys.All, new Dictionary<string,string> {
                    { ClassKeys.Assault.Name, "QUARTERBACK" },
                    { ClassKeys.Heavy.Name, "JETPACK CONTROL" }, // JETPACK CONTROL
                    { ClassKeys.Sniper.Name, "GUNSLINGER" },
                    { ClassKeys.Berserker.Name, "GUN KATA" },
                    { ClassKeys.Priest.Name, "BIOCHEMIST" },
                    { ClassKeys.Technician.Name, "REMOTE DEPLOYMENT" },
                    { ClassKeys.Infiltrator.Name, "VANISH" } // VANISH ex PHANTOM PROTOCOL
                } } }),
            new PersonalPerksDef(
                perkKey: PerkType.Class_2,
                isRandom: false,
                spCost: 20,
                relList: new Dictionary<string, Dictionary<string, string>>
                {{ FactionKeys.All, new Dictionary<string,string> {
                    { ClassKeys.Assault.Name, "AIMED BURST" },
                    { ClassKeys.Heavy.Name, "BOOM BLAST" },
                    { ClassKeys.Sniper.Name, "KILL ZONE" },
                    { ClassKeys.Berserker.Name, "KILLER INSTINCT" }, // KILLER INSTINCT ex EXERTION
                    { ClassKeys.Priest.Name, "LAY WASTE" },
                    { ClassKeys.Technician.Name, "REMOTE CONTROL" },
                    { ClassKeys.Infiltrator.Name, "SPIDER DRONE PACK" }
                } } }),
            new PersonalPerksDef(
                perkKey: PerkType.Faction_1,
                isRandom: false,
                spCost: 15,
                relList: new Dictionary<string, Dictionary<string, string>>
                {
                    { FactionKeys.PX, new Dictionary<string, string>
                    {
                        { ClassKeys.Assault.Name, "DIE HARD" }, //DIE HARD ex OVERWATCH FOCUS
                        { ClassKeys.Heavy.Name, "DIE HARD" },
                        { ClassKeys.Sniper.Name, "DIE HARD" },
                        { ClassKeys.Berserker.Name, "DIE HARD" },
                        { ClassKeys.Priest.Name, "DIE HARD" },
                        { ClassKeys.Technician.Name, "DIE HARD" },
                        { ClassKeys.Infiltrator.Name, "DIE HARD" }
                    } },
                    { FactionKeys.Anu, new Dictionary<string, string>
                    {
                        { ClassKeys.Assault.Name, "BREATHE MIST" },
                        { ClassKeys.Berserker.Name, "BREATHE MIST" },
                        { ClassKeys.Priest.Name, "BREATHE MIST" },
                    } },
                    { FactionKeys.NJ, new Dictionary<string, string>
                    {
                        { ClassKeys.Assault.Name, "TAKEDOWN" },
                        { ClassKeys.Heavy.Name, "TAKEDOWN" },
                        { ClassKeys.Sniper.Name, "TAKEDOWN" },
                        { ClassKeys.Technician.Name, "TAKEDOWN" },
                    } },
                    { FactionKeys.Syn, new Dictionary<string, string>
                    {
                        { ClassKeys.Assault.Name, "OVERWATCH FOCUS" }, // ex PAIN CHAMELEON
                        { ClassKeys.Sniper.Name, "OVERWATCH FOCUS" },
                        { ClassKeys.Infiltrator.Name, "OVERWATCH FOCUS" }
                    } },
                    { FactionKeys.IN, new Dictionary<string, string>
                    {
                        { ClassKeys.Assault.Name, "BREATHE MIST" },
                        { ClassKeys.Heavy.Name, "TAKEDOWN" },
                        { ClassKeys.Sniper.Name, "OVERWATCH FOCUS" },
                    } },
                    { FactionKeys.PU, new Dictionary<string, string>
                    {
                        { ClassKeys.Assault.Name, "TAKEDOWN" },
                        { ClassKeys.Heavy.Name, "TAKEDOWN" },
                        { ClassKeys.Sniper.Name, "TAKEDOWN" },
                        { ClassKeys.Technician.Name, "TAKEDOWN" },
                        { ClassKeys.Infiltrator.Name, "TAKEDOWN" }
                    } },
                    { FactionKeys.FS, new Dictionary<string, string>
                    {
                        { ClassKeys.Assault.Name, "BREATHE MIST" },
                        { ClassKeys.Berserker.Name, "BREATHE MIST" },
                        { ClassKeys.Priest.Name, "BREATHE MIST" },
                    } },
                }),
            new PersonalPerksDef(
                perkKey: PerkType.Faction_2,
                isRandom: false,
                spCost: 20,
                relList: new Dictionary<string, Dictionary<string, string>>
                {
                    { FactionKeys.PX, new Dictionary<string, string>
                    {
                        { ClassKeys.Assault.Name, "BATTLE HARDENED" },
                        { ClassKeys.Heavy.Name, "BATTLE HARDENED" },
                        { ClassKeys.Sniper.Name, "BATTLE HARDENED" },
                        { ClassKeys.Berserker.Name, "BATTLE HARDENED" },
                        { ClassKeys.Priest.Name, "BATTLE HARDENED" },
                        { ClassKeys.Technician.Name, "BATTLE HARDENED" },
                        { ClassKeys.Infiltrator.Name, "BATTLE HARDENED" }
                    } },
                    { FactionKeys.Anu, new Dictionary<string, string>
                    {
                        { ClassKeys.Assault.Name, "SOWER OF CHANGE" },
                        { ClassKeys.Berserker.Name, "SOWER OF CHANGE" },
                        { ClassKeys.Priest.Name, "RESURRECT" },
                    } },
                    { FactionKeys.NJ, new Dictionary<string, string>
                    {
                        { ClassKeys.Assault.Name, "PUNISHER" },
                        { ClassKeys.Heavy.Name, "PUNISHER" },
                        { ClassKeys.Sniper.Name, "PUNISHER" },
                        { ClassKeys.Technician.Name, "AR TARGETING" },
                    } },
                    { FactionKeys.Syn, new Dictionary<string, string>
                    {
                        { ClassKeys.Assault.Name, "ENDURANCE" },
                        { ClassKeys.Sniper.Name, "ENDURANCE" },
                        { ClassKeys.Infiltrator.Name, "SABOTEUR" }
                    } },
                    { FactionKeys.IN, new Dictionary<string, string>
                    {
                        { ClassKeys.Assault.Name, "SOWER OF CHANGE" },
                        { ClassKeys.Heavy.Name, "PUNISHER" },
                        { ClassKeys.Sniper.Name, "ENDURANCE" },
                    } },
                    { FactionKeys.PU, new Dictionary<string, string>
                    {
                        { ClassKeys.Assault.Name, "PUNISHER" },
                        { ClassKeys.Heavy.Name, "PUNISHER" },
                        { ClassKeys.Sniper.Name, "PUNISHER" },
                        { ClassKeys.Technician.Name, "PUNISHER" },
                        { ClassKeys.Infiltrator.Name, "PUNISHER" }
                    } },
                    { FactionKeys.FS, new Dictionary<string, string>
                    {
                        { ClassKeys.Assault.Name, "SOWER OF CHANGE" },
                        { ClassKeys.Berserker.Name, "SOWER OF CHANGE" },
                        { ClassKeys.Priest.Name, "SOWER OF CHANGE" },
                    } },
                })
        };

        // Exclusion map for random distributed skills
        public Dictionary<string, List<string>> RadomSkillExclusionMap = new Dictionary<string, List<string>>
        {
            { "HANDGUN PROFICIENCY", new List<string> { ClassKeys.Sniper.Name, ClassKeys.Berserker.Name } },
            { "PDW PROFICIENCY", new List<string> { ClassKeys.Technician.Name } },
            { "MELEE WEAPON PROFICIENCY", new List<string> { ClassKeys.Berserker.Name } },
            { "ASSAULT RIFLE PROFICIENCY", new List<string> { ClassKeys.Assault.Name } },
            { "SHOTGUN PROFICIENCY", new List<string> { ClassKeys.Assault.Name } },
            { "SNIPER RIFLE PROFICIENCY", new List<string> { ClassKeys.Sniper.Name } },
            { "HEAVY WEAPON PROFICIENCY", new List<string> { ClassKeys.Heavy.Name } },
            { "MOUNTED WEAPON PROFICIENCY", new List<string> { ClassKeys.Heavy.Name } },
            { Proficiency.VW, new List<string> { ClassKeys.Priest.Name } },
            { Proficiency.SW, new List<string> { ClassKeys.Infiltrator.Name } },
            { "ROCKETEER", new List<string> { ClassKeys.Heavy.Name } },
            { "DAMAGED AMYGDALA", new List<string> { ClassKeys.Priest.Name } },
            { "STEALTH SPECIALIST", new List<string> { ClassKeys.Infiltrator.Name } }
        };

        // Learn the first personal ability = is set rigth from the start
      //  public bool LearnFirstPersonalSkill = true;


        /// <summary>
        /// commented out BCSettings that are set in TFTV Settings (auto standby + learn first personal skill), 
        /// plus removed crossbow ammo settings and activate story rework because no longer used. - Voland
        /// </summary>
        // Deactivate auto standy in tactical missions
     //   public bool DeactivateTacticalAutoStandby = false;

        // Activate story rework
       // public bool ActivateStoryRework = false;

        // Infiltrator Crossbow Ammo changes
      //  public int BaseCrossbow_Ammo = 6;
      //  public int VenomCrossbow_Ammo = 4;

        // Flag if UI texts should be changed to default (Enlish) text or set by localization
        public bool DoNotLocalizeChangedTexts = true;
        // Create new ability dictionary as json file in mod directory
        internal bool CreateNewJsonFiles = false;
        // DebugLevel (0: nothing, 1: error, 2: debug, 3: info)
        public int Debug = 1;
    }
}
