using Base.UI;
using HarmonyLib;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities.GameTags;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Core;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Levels;
using PhoenixPoint.Tactical.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;

namespace TFTV
{
    internal class TFTVHumanEnemiesNames
    {
        public static string[] adjectives = new string[] {"Crazy", "Mad", "Sneaky", "Bloody", "Inglorious", "Glorious", "Somber", "Wasteland",
          "Red", "Green", "Blue", "Golden", "Dead", "Bullet", "Laser", "Shredding", "Acid", "Toxic", "Rampaging", "Festering", "Corrupted", "Kaos",
          "Explosive", "Broken", "Lewd", "Fierce", "Fire", "Ice", "Mean", "Mech", "War", "Suicide", "Snapshot", "Jagged", "Jaded", "Bloodied",
          "Shredded", "Cursed", "Blessed", "Alpha", "Bravo", "Charlie", "Tango", "Zulu", "Black", "White", "Zombie", "Hungry", "Thirsty", "Death"};

        public static string[] nouns = new string[] { "Vipers", "Monkeys", "Goats", "Crabmen", "Fishmen", "Mindfraggers", "Chirons",
            "Sirens", "Scyllas", "Acherons", "Locusts", "Buzzards", "Vultures", "Eagles", "Basterds", "Bastards", "Killers", "Echoes",
            "Rabbits", "Mice", "Bulls", "Behemoths", "Dillos", "Mantis", "Warriors", "Soldiers", "Dogs", "Panthers", "Turtles", "Boys",
            "Bugs", "Jokers", "Razors", "Rascals", "Raiders", "Alligators", "Gators", "Raptors", "Monks", "Barbarians", "Seals", "Crabs",
            "Fighters", "Revenants", "Beriths", "Charuns", "Abbadons", "Lords", "Hawks", "Dealers" };


        public static List<string> pu_Adjectives = new List<string>
{
    "Synthetic", "Cyber", "Augmented", "Machine", "Cold", "Merciless", "Efficient", "Calculated", "Robotic", "Silent",
    "Unfeeling", "Relentless", "Optimized", "Neural", "Void", "Quantum", "Steel", "Iron", "Mech", "Precision",
    "Titanium", "Surge", "Rewired", "Exo", "Nano", "Inhuman", "Assimilated", "Voidborn", "Echo", "Sterile",
    "Augur", "Dread", "Data", "Neon", "Glitching", "Severed", "Perfection", "Cybernetic", "Protocol", "Void-Hardened"
};

        public static List<string> pu_Nouns = new List<string>
{
    "Machines", "Exiles", "Automata", "Overlords", "Cyborgs", "Constructs", "Sentinels", "Executors", "Observers", "Symbiotes",
    "Neuralites", "Drones", "Collectors", "Remnants", "Androids", "Purifiers", "Synthetics", "Golems", "Processors", "Reclaimers",
    "Ascendants", "Preservers", "Harbingers", "Assimilators", "Replicants", "Eradicators", "Visionaries", "Echoes", "Silencers", "Patrons",
    "Analyzers", "Observers", "Modifiers", "Calculators", "Upgraded", "Conduits", "Defragmenters", "Streamliners", "Singularities", "Architects"
};



        public static List<string> fo_Adjectives = new List<string>
{
    "Twisted", "Warped", "Aberrant", "Bloodstained", "Cursed", "Mutated", "Unholy", "Dark", "Shadowed", "Blighted",
    "Desecrated", "Malformed", "Ravaged", "Unbound", "Vile", "Rotting", "Unclean", "Decayed", "Blackened", "Pestilent",
    "Accursed", "Horrid", "Depraved", "Malevolent", "Twisted-Hearted", "Forsaken", "Void-Touched", "Doomed", "Haunted", "Eldritch",
    "Infernal", "Corrupt", "Profane", "Fleshwarped", "Barbaric", "Unforgiven", "Maddened", "Abominable", "Demonic", "Sinister"
};

        public static List<string> fo_Nouns = new List<string>
{
    "Wretches", "Scourge", "Horrors", "Nightmares", "Dwellers", "Outcasts", "Devourers", "Revenants", "Banshees", "Shades",
    "Aberrations", "Crawlers", "Vermin", "Mutants", "Ghouls", "Lurkers", "Desecrators", "Unseen", "Eclipsed", "Blasphemers",
    "Exiles", "Desecrated", "Betrayed", "Dreadborn", "Fleshshapers", "Warpborn", "Howlers", "Deathcallers", "Harbingers", "Abyssals",
    "Hollowborn", "Nameless", "Specters", "Duskborn", "Nocturnals", "Withered", "Lamenters", "Bonecarvers", "Shunned", "Damned"
};


        public static List<string> anu_Adjectives = new List<string>
{
    "Sacred", "Blessed", "Divine", "Exalted", "Holy", "Mystic", "Enlightened", "Transcendent", "Hallowed", "Ethereal",
    "Serene", "Prophetic", "Sanctified", "Glorified", "Celestial", "Radiant", "Purified", "Ascendant", "Anointed", "Revered",
    "Devout", "Unshaken", "Worshipful", "Fervent", "Zealous", "Pious", "Eclipsed", "Echoing", "Reborn", "Transformed",
    "Resonant", "Hymnal", "Resplendent", "Venerated", "Symbiotic", "Unified", "Harmonious", "Divinized", "Penitent", "Merciful"
};

        public static List<string> anu_Nouns = new List<string>
{
    "Prophets", "Pilgrims", "Redeemers", "Chosen", "Visionaries", "Scribes", "Choristers", "Oracles", "Saviors", "Wanderers",
    "Sentinels", "Heralds", "Preachers", "Mystics", "Martyrs", "Evangelists", "Witnesses", "Adherents", "Seers", "Keepers",
    "Gnostics", "Revelators", "Seraphs", "Archons", "Supplicants", "Apostles", "Confessors", "Augurs", "Intercessors", "Devotees",
    "Synergists", "Cultists", "Transformers", "Adorers", "Cantors", "Rejoicers", "Almsgivers", "Hymnists", "Votaries", "Celestials"
};


        public static List<string> syn_Adjectives = new List<string>
{
    "Free", "Equal", "Just", "Rational", "Reasoned", "Unified", "Collective", "Independent", "Visionary", "Optimistic",
    "Libertarian", "Radical", "Harmonic", "Idealistic", "Altruistic", "Utopian", "Progressive", "Egalitarian", "Peaceful", "Unchained",
    "Determined", "Hopeful", "Philosophical", "Pragmatic", "Democratic", "Federalist", "Transparent", "Cooperative", "Pluralist", "Self-Governing",
    "Daring", "Innovative", "Resilient", "Revolutionary", "Empowered", "Scientific", "Adaptive", "Humanitarian", "Sophisticated", "Ethical"
};

        public static List<string> syn_Nouns = new List<string>
{
    "Pioneers", "Reformers", "Scholars", "Philosophers", "Inventors", "Creators", "Seekers", "Explorers", "Dreamers", "Idealists",
    "Engineers", "Scientists", "Builders", "Negotiators", "Peacemakers", "Activists", "Moderators", "Mediators", "Innovators", "Freethinkers",
    "Guardians", "Delegates", "Advocates", "Liberators", "Moderates", "Educators", "Optimists", "Diplomats", "Technologists", "Visionaries",
    "Progressives", "Enlightened", "Negotiators", "Renaissance", "Alchemists", "Wanderers", "Seekers", "Thinkers", "Harmonizers", "Unifiers"
};

        public static List<string> nj_Adjectives = new List<string>
{
    "Iron", "Steel", "Titan", "Unbreakable", "Relentless", "Fierce", "Loyal", "Resolute", "Fearless", "Brutal",
    "Indomitable", "Savage", "Warborn", "Battle-Hardened", "Merciless", "Gritty", "Disciplined", "Tactical", "Elite", "Vigilant",
    "Unyielding", "Crimson", "Thunder", "Blazing", "Storm", "Armored", "Fortified", "Dominant", "Imperial", "Warlike",
    "Devastating", "Bloodied", "Rampaging", "Unforgiving", "Ferocious", "Unrelenting", "Colossal", "Unstoppable", "Menacing", "Dauntless",
    "Commanding", "Invincible", "Raging", "Impenetrable", "Savage-Hearted", "Dreadnought", "Bulwark", "Conquering", "Hammer", "Steelborn"
};

        public static List<string> nj_Nouns = new List<string>
{
    "Legion", "Sentinels", "Warriors", "Crusaders", "Vanguard", "Battalion", "Stormtroopers", "Shocktroopers", "Commandos", "Guardians",
    "Ironclads", "Watchmen", "Pathfinders", "Rangers", "Gunners", "Dragoons", "Avengers", "Defenders", "Punishers", "Hellhounds",
    "Destroyers", "Reapers", "Bulldogs", "Juggernauts", "Outriders", "Enforcers", "Goliaths", "Wardens", "Paladins", "Strykers",
    "Stormbreakers", "Warlords", "Titanborn", "Blackguards", "Firebrands", "Conquerors", "Berserkers", "Ironfangs", "Howlers", "Dreadwolves",
    "Warhounds", "Bloodhawks", "Steelhearts", "Lancers", "Vultures", "Grenadiers", "Invictus", "Doombringers", "Tridents", "Thunderbolts"
};







        public static Dictionary<string, Dictionary<int, List<string>>> names = new Dictionary<string, Dictionary<int, List<string>>>();
        public static Dictionary<string, List<string>> ranks = new Dictionary<string, List<string>>();

        private static readonly SharedData Shared = TFTVMain.Shared;
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;


        public static string GetSquadName(string faction)
        {
            try
            {
                switch (faction)
                {
                    case "Purists": return $"{pu_Adjectives[UnityEngine.Random.Range(0, pu_Adjectives.Count)]} {pu_Nouns[UnityEngine.Random.Range(0, pu_Nouns.Count)]}";
                    case "FallenOnes": return $"{fo_Adjectives[UnityEngine.Random.Range(0, fo_Adjectives.Count)]} {fo_Nouns[UnityEngine.Random.Range(0, fo_Nouns.Count)]}";
                    case "anu": return $"{anu_Adjectives[UnityEngine.Random.Range(0, anu_Adjectives.Count)]} {anu_Nouns[UnityEngine.Random.Range(0, anu_Nouns.Count)]}";
                    case "syn": return $"{syn_Adjectives[UnityEngine.Random.Range(0, syn_Adjectives.Count)]} {syn_Nouns[UnityEngine.Random.Range(0, syn_Nouns.Count)]}";
                    case "nj": return $"{nj_Adjectives[UnityEngine.Random.Range(0, nj_Adjectives.Count)]} {nj_Nouns[UnityEngine.Random.Range(0, nj_Nouns.Count)]}";
                    case "ban": return $"{adjectives[UnityEngine.Random.Range(0, adjectives.Length)]} {nouns[UnityEngine.Random.Range(0, nouns.Length)]}";
                }

                return null;

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        [HarmonyPatch(typeof(FactionCharacterGenerator), "GenerateUnit", new Type[] { typeof(GeoFaction), typeof(TacActorUnitResult) })]
        internal static class FactionCharacterGenerator_GenerateUnit_Patch
        {
           
            public static void Postfix(GeoFaction faction, ref GeoUnitDescriptor __result)
            {
                try
                {
                    GenderTagDef genderTagDef = Shared.SharedGameTags.Genders.MaleTag;
                    GameTagDef rankTag = null;

                    GeoUnitDescriptor.IdentityDescriptor identityDescriptor = __result.Identity;

                    if (faction.PPFactionDef.ShortName == "nj")
                    {
                        if (__result.Level >= 4)
                        {
                            rankTag = TFTVHumanEnemies.HumanEnemyTier2GameTag;
                        }
                    }


                    if (identityDescriptor.Sex == GeoCharacterSex.Female)
                    {
                        genderTagDef = Shared.SharedGameTags.Genders.FemaleTag;

                    }
                    else if (identityDescriptor.Sex == GeoCharacterSex.None)
                    {
                        genderTagDef = null;
                    }

                    __result.Identity = new GeoUnitDescriptor.IdentityDescriptor(GetName(faction.PPFactionDef.ShortName, rankTag, genderTagDef), identityDescriptor.Sex);

                }
                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(GeoHaven), "SpawnNewRecruit")]
        public static class TFTV_GeoHaven_SpawnNewRecruit_patch
        {
            public static void Postfix(GeoHaven __instance, CharacterGenerationContext context)
            {
                try
                {


                    if (__instance.AvailableRecruit == null || context.Faction == null)
                    {
                        return;
                    }

                    if (__instance.AvailableRecruit.UnitType.TemplateDef.IsVehicle || __instance.AvailableRecruit.UnitType.TemplateDef.IsMutog)
                    {
                        return;
                    }

                    GenderTagDef genderTagDef = Shared.SharedGameTags.Genders.MaleTag;
                    GameTagDef rankTag = null;

                    GeoUnitDescriptor.IdentityDescriptor identityDescriptor = __instance.AvailableRecruit.Identity;

                    if (context.Faction.PPFactionDef.ShortName == "nj")
                    {
                        if (__instance.AvailableRecruit.Level >= 4)
                        {
                            rankTag = TFTVHumanEnemies.HumanEnemyTier2GameTag;
                        }
                    }


                    if (identityDescriptor.Sex == GeoCharacterSex.Female)
                    {
                        genderTagDef = Shared.SharedGameTags.Genders.FemaleTag;

                    }
                    else if (identityDescriptor.Sex == GeoCharacterSex.None)
                    {
                        genderTagDef = null;
                    }

                    __instance.AvailableRecruit.Identity = new GeoUnitDescriptor.IdentityDescriptor(GetName(context.Faction.PPFactionDef.ShortName, rankTag, genderTagDef), identityDescriptor.Sex);

                }

                catch (Exception e)
                {
                    TFTVLogger.Error(e);
                }
            }
        }


        public static void CreateNamesDictionary()
        {
            try
            {
                names.Add("ban", new Dictionary<int, List<string>>()
                {

                    { 0, new List<string>(ban_MaleNames) },
                    { 1, new List<string>(ban_FemaleNames) },
                    {2, new List<string> (ban_Names) }

                });

                names.Add("nj", new Dictionary<int, List<string>>()

                {
                    {0, new List<string>(nj_MaleNames) },
                    { 1, new List<string>(nj_FemaleNames) },
                    { 2, new List<string>(nj_NeutralNames) },
                    { 3, new List<string>(nj_Surnames) },
                    { 4, new List<string>(nj_Nicknames) }

                });


                names.Add("anu", new Dictionary<int, List<string>>()

                {
                    {0, new List<string>(anu_MaleNames) },
                    {1, new List<string>(anu_FemaleNames) },
                });

                names.Add("syn", new Dictionary<int, List<string>>()
                {
                    {0, new List<string>(syn_MaleNames) },
                    { 1, new List<string>(syn_FemaleNames) },
                    { 2, new List<string>(syn_NeutralNames) },
                });

                names.Add("Purists", new Dictionary<int, List<string>>()
                {
                    {0, new List<string>(pu_Names) },

                });
                names.Add("FallenOnes", new Dictionary<int, List<string>>()
                {
                    {0, new List<string>(fo_Names) },

                });
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }

        private static string GetNJName(string faction, GameTagDef rankTag = null, GenderTagDef genderTagDef = null)
        {
            try
            {
                string name = "";

                if (genderTagDef != null)
                {

                    if (genderTagDef == Shared.SharedGameTags.Genders.MaleTag)
                    {
                        name = names[faction][0][UnityEngine.Random.Range(0, names[faction][0].Count)];
                        names[faction][0].Remove(name);
                    }
                    else
                    {
                        name = names[faction][1][UnityEngine.Random.Range(0, names[faction][1].Count)];
                        names[faction][1].Remove(name);
                    }
                }
                else
                {

                    name = names[faction][2][UnityEngine.Random.Range(0, names[faction][2].Count)];
                    names[faction][2].Remove(name);

                }

                if (rankTag != null && (rankTag == TFTVHumanEnemies.HumanEnemyTier1GameTag || rankTag == TFTVHumanEnemies.HumanEnemyTier2GameTag))
                {
                    string nickName = names[faction][4][UnityEngine.Random.Range(0, names[faction][4].Count)];
                    name += $" '{nickName}'";
                    names[faction][4].Remove(nickName);
                }

                string surname = names[faction][3][UnityEngine.Random.Range(0, names[faction][3].Count)];
                name += $" {surname}";
                names[faction][3].Remove(surname);

                return name;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }

        }

        public static string GetName(string faction, GameTagDef rankTag = null, GenderTagDef genderTagDef = null)
        {
            try
            {
                string name = "";

                if (faction == "Purists" || faction == "FallenOnes")
                {
                    name = names[faction][0][UnityEngine.Random.Range(0, names[faction][0].Count)];
                    names[faction][0].Remove(name);

                }
                else
                {
                    if (faction == "nj")
                    {
                        name = GetNJName(faction, rankTag, genderTagDef);
                    }
                    else
                    {
                        if (genderTagDef != null)
                        {
                            if (genderTagDef == Shared.SharedGameTags.Genders.MaleTag)
                            {
                                name = names[faction][0][UnityEngine.Random.Range(0, names[faction][0].Count)];
                                names[faction][0].Remove(name);
                            }
                            else
                            {
                                name = names[faction][1][UnityEngine.Random.Range(0, names[faction][1].Count)];
                                names[faction][1].Remove(name);
                            }
                        }
                        else if (names[faction].Count >= 3)
                        {
                            name = names[faction][2][UnityEngine.Random.Range(0, names[faction][2].Count)];
                            names[faction][2].Remove(name);
                        }
                        else
                        {
                            name = names[faction][0][UnityEngine.Random.Range(0, names[faction][0].Count)];
                            names[faction][0].Remove(name);

                        }
                    }


                }

                return name;

            }
            catch (Exception e)
            {

                TFTVLogger.Error(e);
                throw;
            }

        }


        private static List<string> ConvertKeysToStrings(List<string> locKeys)
        {
            try
            {
                List<string> strings = new List<string>();

                foreach (string key in locKeys)
                {
                    strings.Add(new LocalizedTextBind() { LocalizationKey = key }.Localize());
                }

                return strings;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                throw;
            }
        }


        public static void CreateRanksDictionary()
        {
            try
            {
                ranks.Add("ban", ConvertKeysToStrings(ban_NameRanks));
                ranks.Add("nj", ConvertKeysToStrings(nj_NameRanks));
                ranks.Add("anu", ConvertKeysToStrings(anu_NameRanks));
                ranks.Add("syn", ConvertKeysToStrings(syn_NameRanks));
                ranks.Add("Purists", ConvertKeysToStrings(pu_NameRanks));
                ranks.Add("FallenOnes", ConvertKeysToStrings(fo_NameRanks));
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        public static List<string> slug_FirstNames = new List<string>
{
    "Aeris", "Calix", "Dove", "Eden", "Fawn", "Haven", "Ire", "Jules", "Lumen", "Milo",
    "Nova", "Orion", "Pax", "Quill", "Riven", "Sage", "Tobin", "Vale", "Wren", "Zephyr",
    "Mike", "Peter", "John", "Paul", "James", "Anna", "Sarah", "Tom", "Adam", "Mark",
    "Luke", "Laura", "Emily", "Robert", "Jessica", "Andrew", "Brian", "Kevin", "Grace", "Sophia"
};


        public static List<string> slug_Adjectives = new List<string>
{
    "Crucible", "Twisted", "Forsaken", "Blighted", "Accursed", "Fallen", "Maddened", "Haunted", "Doomed", "Wretched",
    "Tormented", "Defiled", "Broken", "Scarred", "Withered", "Marred", "Sundered", "Bitter", "Desolate", "Pained",
    "Martyr", "Redeemer", "Penitent", "Sacrifice", "Oblation", "Testament", "Remnant", "Wounded", "Shattered", "Forlorn",
    "Despairing", "Anguished", "Lamenting", "Mourning", "Grieving", "Remorseful", "Sorrowful", "Contrite", "Brokenhearted", "Rueful",
    "Votive", "Absolution", "Tarnished", "Fractured", "Cindered", "Ashen", "Exiled", "Seared", "Vigil", "Lacerated",
    "Hollow", "Soothed", "Feral", "Gashed", "Riven", "Faint", "Pallid", "Twilight", "Burdened", "Meek",
    "Supplicant", "Witness", "Bound", "Obscured", "Devoted", "Waning", "Vulnerable", "Echoing", "Estranged", "Graven",
    "Pilgrim", "Grim", "Tethered", "Anointed", "Marked", "Quiet", "Silent", "Veiled", "Soot-Born", "Dimming"
};


        public static List<string> sectarian_Monikers = new List<string>
{
    "Bloodaxe", "Frenzy", "Rage", "Mauler", "Skullcrusher", "Ironfist", "Stormbreaker", "Berserker", "Skullsplitter", "Redtooth",
    "Grimblade", "Wolfsbane", "Bonechewer", "Deathwish", "Ironheart", "Rageborn", "Fury", "Bloodfury", "Sunder", "Vengeance",
    "Wrath", "Thunderstrike", "Voidreaver", "Battleborn", "Fleshsunder", "Savage", "Gravewalker", "Doomhammer", "Bloodreign", "Fleshcleaver",
    "Nightcleaver", "Abysscaller", "Woundhowl", "Chainfang", "Wyrmcleaver", "Ashhand", "Crimsonbrand", "Fleshfang", "Dreadhorn", "Venomblood",
    "Steelhowl", "Warcry", "Skullscreamer", "Goreclaw", "Mourntooth", "Helldrinker", "Firevein", "Scourge", "Blightmark", "Shatterbone",
    "Tombfist", "Grimjaw", "Fangborn", "Bloodchant", "Hatemouth", "Frostcleaver", "Skindancer", "Carrionhowl", "Blightcleaver", "Voidvein",
    "Throatcutter", "Soulbreaker", "Deathchant", "Vilegrin", "Ironbrand", "Frostbrand", "Darkcry", "Chaincleaver", "Runefang", "Voidclaw",
    "Mindspiller", "Ashjaw", "Horrorsong", "Skullbrand", "Marrowfang", "Fleshshredder", "Deathbrand", "Flayer", "Warbane", "Ravenscourge"
};

        public static List<string> sectarianMaleNames = new List<string> {
    "Conan", "Bran", "Cahir", "Taran", "Duncan", "Cian", "Ronan", // Celtic
    "Vlad", "Boris", "Dragan", "Milos", "Radomir", "Zoran", // Slavic
    "Arash", "Rostam", "Babak", "Kaveh", // Persian
    "Chike", "Jabari", "Kwame", "Thulani", // African
    "Takao", "Kenta", "Masaru", "Hiroshi", // Japanese
    "Dakota", "Tasunka", "Chaska", "Wicasa", // Native American
    "Santiago", "Mateo", "Raul", "Diego", // Latin
    "Bjorn", "Erik", "Sten", "Ulf", "Hakon", "Leif", "Gunnar", "Ivar", "Asgeir", // Scandinavian (30%)
};

        public static List<string> sectarianFemaleNames = new List<string> {
    "Maeve", "Grainne", "Saoirse", "Brigid", // Celtic
    "Mila", "Zora", "Radka", "Vesna", "Tatiana", // Slavic
    "Roxana", "Anahita", "Soraya", // Persian
    "Imani", "Zola", "Makeda", "Asha", // African
    "Yuki", "Kaori", "Aiko", // Japanese
    "Winona", "Ayita", "Pavati", // Native American
    "Isadora", "Luz", "Marisol", "Paloma", // Latin
    "Freya", "Astrid", "Sigrid", "Helga", "Kari", "Torhild", "Inga", // Scandinavian (30%)
};

        public static List<string> spymasterMaleNames = new List<string> {
    "Luca", "Silas", "Corin", "Elian", "Jarek", "Malik", "Rowan", "Dorian",
    "Cassian", "Viktor", "Damien", "Nico", "Adrian", "Quinn", "Soren", "Kael",
    "Zane", "Theo", "Ren", "Leif",
    "Ilan", "Matteo", "Bastien", "Ezra", "Tycho", "Lucien", "Miles", "Tarek",
    "Jules", "Ansel", "Felix", "Oren", "Marek", "Cyrus", "Seth", "Jai",
    "Calix", "Elias", "Rael", "Milo"
};

        public static List<string> spymasterFemaleNames = new List<string> {
    "Mira", "Talia", "Lyra", "Selene", "Nadia", "Sable", "Iris", "Vera",
    "Nyra", "Lina", "Rhea", "Kara", "Delia", "Sira", "Noa", "Alina",
    "Zara", "Juno", "Esme", "Thalia",
    "Cleo", "Riven", "Vesper", "Kaia", "Liora", "Anya", "Sorrel", "Nimue",
    "Astrid", "Indira", "Tess", "Lunelle", "Nox", "Maris", "Isolde", "Rowen",
    "Dahlia", "Eira", "Faye", "Ona"
};

        public static List<string> spymasterMonikers = new List<string> {
    "the Whisper", "the Veil", "the Shade", "Ghosthand", "Nightstep", "the Echo",
    "Foxglove", "the Moth", "Inkblade", "the Mirage", "the Cipher", "Quickthorn",
    "the Silence", "the Net", "Drift", "Silvertongue", "the Mask", "Lowlight",
    "the Dagger", "the Quiet",
    "Shadowmark", "Lockjaw", "the Wisp", "the Latch", "Whitesmoke", "Vellum",
    "the Signal", "the Key", "Echoweaver", "the Flicker", "Whispersilk", "Cloakstep",
    "the Sliver", "Ghostline", "the Glimmer", "the Lockpick", "the Blank", "Needlepoint",
    "Hushblade", "the Fog"
};

        public static List<string> ghostMonikers = new List<string> {
    "the Dreamer", "the Sleeper", "Fracture", "Whisperwake", "the Drift", "Softstep",
    "Waking Thought", "Phase", "the Pale", "Mirrorfall", "the Signal", "Lucid Ash",
    "Subtone", "Nightbridge", "the Absent", "Choral", "Mistbinder", "the Blink",
    "Veilborne", "the Murmur", "the Interval", "Threnody", "Echotrace", "Mindglint",
    "Flickershade", "the Recall", "the Quieting", "Sublime Error", "Wakewalker", "Still Frequency",
    "the Unseen", "Static Veil", "Slumberspike", "Riftmelt", "Voxless", "Nullspark",
    "Caul", "Sleepcurrent", "the Half-Known", "the Withheld"
};

        public static List<string> oldHoundMonikers = new List<string> {
    "the Hound", "Broken Fang", "Craterface", "Old Lead", "the Shrapnel", "One-Eye",
    "Rustjaw", "Backfire", "the Ghost Dog", "Wolf Nine", "Grizzle", "Greyshot",
    "the Burnt", "Old Gun", "Patchwork", "Scraphound", "Muzzle", "the Latch",
    "the Relic", "Coffin Nail", "Gravedog", "the Wall", "Tusk", "Skullcap",
    "Old Scar", "Killchain", "the Fossil", "Triggerlock", "the Ragman", "Bloodstripe",
    "Stovepipe", "Spent Shell", "the Limb", "Ashbreath", "the Mortar", "Copper Vane",
    "Old Guard", "the Rack", "Iron Chewer", "Scabhide"
};

        public static List<string> exileMonikers = new List<string> {
    "Zero Point", "Null", "Vector", "The Exile", "Disjoin", "Ghost Code",
    "Echo Protocol", "The Divergent", "Cut Thread", "Synapse Burn", "Overclock",
    "the Patch", "Loopback", "Axiom", "Fracture", "Unbound", "the Clause",
    "Parity", "the Cutoff", "Faultline", "Redline", "Exit Node", "Ghostroot",
    "Outlier", "Forked Path", "Delta Null", "Code Blue", "Driftmark", "Cipher",
    "the Refusal", "Bypass", "Protocol X", "Echo End", "Free Agent", "Blindspot",
    "Subvert", "Glitchpath", "the Untethered", "Red Flag", "Mono"
};






        public static List<string> ban_Names = new List<string> {"Nuka-Cola","Kessar","Viper","Rictus","Nux","Dag","Ace","Barry","Mohawk","Bearclaw","Clunk",
            "Deepdog","Fuk-Ushima","Fifi Macaffe","Sol","Gutgash","Ironbar","Morsov","Mudguts","Papagallo","Sarse","Sav","Roop","Blackfinger","Scrooloose",
            "Scuttle","Starbuck","Slit","Slake","Tenderloin","Toadie","Toecutter","Toast","Kane","Splitear","Brainrot","Maddog","Coyote","Birdsheet",
            "Gearhead","Yo-yo","Madskunky","Showalter","Grimsrud", "Cobra", "Cyrus", "Cochise", "Rembrandt", "Luther", "Vermin", "D.J.","Orphan",
             "Buzzsaw", "Dynamo", "Fireball", "Subzero", "Lucus the Destroyer"};

        public static List<string> ban_MaleNames = new List<string> {
    "Grime", "Rust", "Knuckle", "Brawler", "Snap", "Bullet", "Rancid", "Deuce", "Fangtooth", "Brakk",
    "Muzzle", "Rancor", "Slash", "Scab", "Havik", "Outlaw", "Houndstooth", "Chopper", "Blight", "Sledge",
    "Kain", "Roach", "Vandal", "Throttle", "Crater", "Snarl", "Junkrat", "Thorne", "Mangle", "Grizzle",
    "Rivet", "Cage", "Crowbar", "Wrecker", "Shiv", "Crasher", "Brimstone", "Mugshot", "Wrath", "Sludge",
    "Ripjaw", "Grit", "Spike", "Strain", "Gore", "Hex", "Fury", "Blister", "Scorch"
};

        public static List<string> ban_FemaleNames = new List<string> {
    "Fizz", "Scrap", "Twitch", "Sprocket", "Raze", "Jinx", "Blitz", "Nova", "Havena", "Sly",
    "Shade", "Sable", "Vexx", "Glimmer", "Raz", "Kobra", "Siren", "Tempest", "Recka",
    "Cinder", "Havara", "Claw", "Rook", "Wraith", "Spite", "Riot", "Hexa", "Venom",
    "Ember", "Nyx", "Hiss", "Echo", "Shard", "Ravyn", "Jett", "Strife",
    "Vortex", "Ravenna", "Hallow", "Dagger", "Sliver", "Howl", "Drift", "Smash", "Razor", "Pyre"
};

        public static List<string> nj_MaleNames = new List<string> {
    "Rockatansky", "Bryant", "Richter", "Amos", "Caleb", "Hunter", "Kruger", "Sinclair", "Morgan", "Hicks",
    "Hudson", "Drake", "Frost", "Walker", "Charlie", "Max", "Miller", "Kenzo", "Karal", "Katoa", "Fagan",
    "Jesse", "Ash", "Finley", "Shaw", "Blaine", "Quaid", "Zander", "Ace", "Shujimi", "Zim", "Bishop",

    "Travis", "Warden", "Doyle", "Rex", "Rogan", "Duncan", "Baxter", "Riley", "Logan", "Grant",
    "Knox", "Beckett", "Garrett", "Cooper", "Brock", "Harlan", "Reed", "Warren", "Clay", "Dane",
    "Vance", "Gage", "Holt", "Sterling", "Colt", "Jett", "Tucker", "Ryker", "Blaise", "Griffin",

    "Hawke", "Jagger", "Weston", "Cyrus", "Nash", "Talon", "Troy", "Axel", "Ronan", "Cade",
    "Hunter", "Brody", "Knox", "Garret", "Thorne", "Tanner", "Chase", "Brock", "Kane", "Rhett",
    "Jensen", "Cole", "Grady", "Declan", "Wade", "Boone", "Easton", "Preston", "Slade", "Maddox",

    "Archer", "Ryder", "Ranger", "Zane", "Tatum", "Gunnar", "Dash", "Sawyer", "Hendrix", "Jaxon",
    "Winston", "Dalton", "West", "Jace", "Storm", "Diesel", "Remy", "Phoenix", "Luca", "Beau"
};

        public static List<string> nj_FemaleNames = new List<string> {
    "Ripley", "Vasquez", "Ferro", "Dietrich", "Angel", "Riley", "Marno", "Vickers", "Ibanez", "Deladier",
    "Avery", "Kai", "Luna", "Sasha", "Rae", "Mira", "Lyra", "Sienna", "Juno", "Aria", "Nova", "Tess",

    "Serena", "Freya", "Sable", "Dahlia", "Skylar", "Cassidy", "Blair", "Taryn", "Nadia", "Zara",
    "Vera", "Thalia", "Selene", "Jade", "Ember", "Valeria", "Nyssa", "Talia", "Lilia", "Noa",

    "Brynn", "Tamsin", "Harlow", "Callista", "Astrid", "Raven", "Solene", "Echo", "Phoenix", "Vesper",
    "Indira", "Rowan", "Xanthe", "Liora", "Celeste", "Isolde", "Sabine", "Zayda", "Miranda", "Piper",

    "Harper", "Kendall", "Quinn", "Aislinn", "Roxanne", "Saffron", "Yara", "Maia", "Rhea", "Ophelia",
    "Scarlett", "Zenya", "Drea", "Tatiana", "Vivian", "Eris", "Marisol", "Kiera", "Noelle", "Yasmine"
};

        public static List<string> nj_NeutralNames = new List<string> {
    "Tempest", "Joyce", "Kai", "Parker", "Jesse", "Angel", "Riley", "Ash", "Finley", "Quinn",
    "Lennon", "Sky", "Noa", "Reese", "Sage", "Morgan", "Harlow", "Marlow", "Rowan", "Avery",

    "Blake", "Dakota", "Elliot", "Jordan", "Toby", "Sutton", "Ellis", "Jamie", "River", "Indigo",
    "Skyler", "Milan", "Phoenix", "Oakley", "Shiloh", "Tatum", "Kiran", "Valen", "Darcy", "Zephyr",

    "Adrian", "Devin", "Jaden", "Micah", "Taylor", "Hayden", "Aspen", "Cameron", "Shawn", "Easton",
    "Sloan", "Rory", "Auden", "Lior", "Tenzin", "Winter", "Dylan", "Brook", "Gray", "Emery",

    "Sydney", "Ariel", "Casey", "Eden", "Kairos", "Marley", "Robin", "Sasha", "Vega", "Onyx"
};

        public static List<string> nj_Surnames = new List<string> {
    // Original names
    "Sinclair", "Morgan", "Hicks", "Hudson", "Drake", "Frost", "Walker", "Miller", "Kenzo", "Fagan",
    "Parker", "Showalter", "Cox", "Strid", "Rasczak", "Ventura", "Dutch", "Dillon", "Hawkins", "Mac",

    // New additions - Tough, practical, military-style surnames
    "Shepherd", "Hollister", "Maguire", "Sutherland", "Bannon", "Donovan", "Hargreaves", "Stanton",
    "Locke", "Garrison", "Treadwell", "Rutherford", "Kessler", "Hawthorne", "Strickland", "Carrington",
    "Holloway", "Winslow", "Buchanan", "Carrigan", "Delacroix", "Davenport", "Fairchild", "Henshaw",

    // More rugged, military-oriented names
    "Calhoun", "Blackwood", "Kirkland", "Langston", "Lancaster", "Redfield", "Wyndham", "Fitzgerald",
    "Whitaker", "Sterling", "Hampton", "Remington", "Thornton", "Montgomery", "Rockwell", "Everest",
    "Wakefield", "Kingsley", "Harrington", "Halifax", "Ashford", "Pembroke", "Huxley", "Kingsland",
    
    // Classic and sci-fi military-sounding surnames
    "Armitage", "Caldwell", "Killian", "Gideon", "Ramsey", "Tiberius", "Matheson", "Falkner",
    "Harlan", "Wyatt", "Calloway", "Delaney", "Hastings", "Norwood", "Warrick", "Colson",
    "Dunham", "Mercer", "Radcliffe", "Kendrick", "Eastwood", "Tolliver", "Monroe", "Whitmore",

    // Futuristic and war-hardened names
    "Voss", "Tarkin", "Ryker", "Hux", "Galen", "Morrigan", "Sloane", "Banner",
    "Cormac", "Salter", "Dorn", "Malone", "Taggart", "Quinton", "Sutherland", "Baxter",
    "Doyle", "Warden", "Garrison", "Sterling", "Bannon", "Knox", "Hawthorne", "Reeves",

    // More warlord-style last names
    "Dredd", "Jagger", "Hannigan", "Saxon", "Wolfram", "Tanner", "Maddox", "Thorne",
    "Garrison", "Stroud", "Hendrix", "Callahan", "Tolliver", "Huxley", "Vanderbilt", "Falco",
    "Stryker", "Braddock", "Blaylock", "Marlowe", "Fitzroy", "Slade", "Durand", "Coltrane",

    // A few legendary ones
    "Patton", "MacArthur", "Eisenhower", "Pershing", "Montgomery", "Bradley", "Sheridan", "Grant",
    "Rommel", "Nelson", "Hood", "Sherman", "Napier", "Drummond", "Cornelius", "Augustus"
};


        public static List<string> nj_Nicknames = new List<string> {
    "Matrix", "Blaine", "Quaid", "Cohaagen", "Duderino", "Justo", "Ace", "Sugar",
    "Gunner", "Tank", "Maverick", "Rogue", "Bullet", "Hawk", "Blitz", "Razor", "Viper", "Ghost",

    "Reaper", "Wraith", "Shadow", "Torque", "Crusher", "Blaze", "Stryker", "Falcon", "Grit", "Ranger",
    "Striker", "Juggernaut", "Brimstone", "Onyx", "Scorch", "Outlaw", "Fury", "Venom", "Warlock", "Riptide",

    "Sentinel", "Drifter", "Vandal", "Titan", "Warhound", "Storm", "Phantom", "Nomad", "Exile", "Inferno",
    "Grim", "Bloodhound", "Steel", "Tundra", "Pyro", "Thunder", "Diesel", "Slayer", "Rampage", "Frostbite",

    "Ironclad", "Havoc", "Torque", "Dagger", "Serpent", "Glitch", "Warden", "Bulletstorm", "Coyote", "Blizzard"
};


        public static List<string> anu_MaleNames = new List<string> {
    "Aigazy", "Bassar", "Erasyl", "Gani", "Itymbai", "Kibrik", "Murat", "Rakhat", "Sadvaqas", "Tjatigul",
    "Zhandos", "Valar", "Kelar", "Thandor", "Ary", "Zerik", "Ravyn", "Kaelin", "Sylar", "Marik", "Joran",
    "Aerin", "Varin", "Vaelis", "Kaelor", "Drystan", "Soren", "Seraph", "Nyxar", "Vaelus", "Azarion",

    "Ezrah", "Zalvador", "Malrik", "Thalos", "Iskandor", "Xanthis", "Raizel", "Zypheron", "Orin", "Tauron",
    "Veyron", "Zarik", "Lucan", "Tiburon", "Kaelos", "Dorian", "Zephriel", "Caedmon", "Itharion", "Thyron",
    "Vhalos", "Eldric", "Kieran", "Omadon", "Erythian", "Morthis", "Vaedrin", "Zael", "Nemorin", "Xandor",
    "Typhion", "Morvain", "Saelis", "Rhyzan", "Altharion", "Ishmael", "Torik", "Zyphir", "Arikan", "Kalzar",
    "Dravan", "Sethis", "Oberion", "Iskarios", "Zyrek", "Nestor", "Vaelos", "Telmar", "Marius", "Zareth",
    "Lucivar", "Xyros", "Zephyrus", "Tavros", "Solrik", "Eliron", "Azrik", "Syrion", "Malakar", "Vaelen",

    "Tormak", "Xenvar", "Kaleth", "Zorikan", "Vaelrik", "Orikan", "Sardak", "Ithron", "Drazel", "Veyron",
    "Ostavar", "Mordain", "Kaelros", "Thyros", "Seraphiel", "Nytherion", "Zephirion", "Solkar", "Veythar",
    "Darian", "Zyphorin", "Thaloran", "Xariath", "Torvain", "Rizor", "Mykaris", "Omniel", "Tarsin", "Eldorath"
};

        public static List<string> anu_FemaleNames = new List<string> {
    "Lirael", "Elys", "Nyxar", "Seraph", "Vaelis", "Aerin", "Liora", "Zalira", "Syphira", "Thalora",
    "Selara", "Mariska", "Kaelith", "Saphira", "Elira", "Averis", "Celith", "Ilythia", "Mirelle", "Zaelis",
    "Araceli", "Vespera", "Solith", "Naeris", "Thyssa", "Azaria", "Xanthe", "Valis", "Nytheria", "Orlith",
    "Calyptha", "Elion", "Cyllene", "Ophira", "Zaphira", "Selina", "Kaelara", "Vhalira", "Zephara", "Lyanna",
    "Althea", "Solara", "Thyra", "Zorya", "Isolde", "Nyxaria", "Vaelora", "Eiren", "Soryelle", "Zyphara",
    "Aurelia", "Miris", "Cassara", "Ithriel", "Zephyra", "Sylvaine", "Illyria", "Azura", "Seraphel", "Velith",

    "Lunaris", "Eryndel", "Vaelith", "Orithia", "Zyphina", "Thalessa", "Celistra", "Mythra", "Zeriana", "Selara",
    "Ephyria", "Xyphora", "Asteris", "Orelia", "Serissa", "Nymira", "Vaethira", "Evanis", "Thaloria", "Solitha",
    "Aeliana", "Zinara", "Velmira", "Luthien", "Miraval", "Talyssa", "Azmira", "Seraphia", "Zephiel", "Noctara",
    "Selmara", "Kaelitha", "Vaeris", "Orynthia", "Serephina", "Elveth", "Zilara", "Nyrielle", "Aezora", "Sylisara",
    "Therielle", "Valmira", "Zoryelle", "Lysithea", "Talyth", "Ilthera", "Xynara", "Sariah", "Zephanya", "Mireth"
};




        public static List<string> syn_NeutralNames = new List<string> {
    "Nagata", "Walker", "Charlie", "Wez", "Ziggy", "Alex", "Max", "Miller", "Kenzo",
    "Karal", "Katoa", "Okoye", "Fagan", "Avery", "Parker", "Joyce", "Kai", "Angel",
    "Jesse", "Riley", "Ash", "Finley", "Shaw", "Marno", "Jikkola", "Kris", "Strid",
    "Showalter", "Grimsrud", "Echo", "Sol", "Aether", "Nova", "Lior", "Zephyr", "Orion",
    "Eris", "Noa", "Sable", "Astra", "Hale", "Zen", "Rune", "Vale", "Lyric", "Quill", "Soren"
};

        public static List<string> syn_MaleNames = new List<string> {
    "Nagata", "Inaros", "Lenin", "Tenet", "Campion", "Eli", "Taabe", "Sanchez", "Walker",
    "Kenzo", "Karal", "Katoa", "Parker", "Joyce", "Kai", "Ash", "Finley", "Strid", "Showalter",
    "Grimsrud", "Scruggs", "L. Gopnik", "S. Ableman", "R. Marshak", "Ulysses", "La Boeuf", "Reuben",
    "Cogburn", "Grant", "Lebowski", "Sobchak", "Louis",

    "Cassian", "Lior", "Orin", "Eamon", "Dorian", "Nikolai", "Soren", "Lucian", "Silas", "Jovian",
    "Aeron", "Caelum", "Oberon", "Leontius", "Ezekiel", "Marius", "Valen", "Theron", "Zenon",
    "Rael", "Matthias", "Elian", "Tiber", "Caius", "Remiel", "Noam", "Zev", "Seraph", "Dastan",
    "Torin", "Elric", "Corin", "Arvid", "Lyric", "Cyrus", "Viggo", "Orion", "Laziel", "Zorion",
    "Solan", "Jareth", "Malik", "Zephyrus", "Ephraim", "Jovan", "Cillian", "Alaric", "Faelan", "Miro",
    "Tavian", "Nasir", "Leif", "Bastien", "Evren", "Cassiel",

    // Newest additions
    "Iskander", "Aurelian", "Eryx", "Oryn", "Thane", "Veyron", "Altair", "Corvus", "Sylas", "Zarek",
    "Myron", "Vesper", "Kael", "Tarek", "Selim", "Eryon", "Nero", "Rafael", "Callan", "Zoriel",
    "Oberyn", "Ephialtes", "Jorik", "Mavrik", "Zypher", "Noctis", "Daedalus", "Triton", "Lucius",
    "Verus", "Severin", "Cyprian", "Hadrian", "Cairos", "Zephyrion", "Lorien", "Valric", "Eron",
    "Auron", "Kieran", "Solinus", "Icarion", "Lyros", "Kaelius", "Tarsis", "Nestor", "Erython",
    "Xenon", "Quirin", "Calder", "Isidor", "Tyrian", "Elros", "Vaelin"
};

        public static List<string> syn_FemaleNames = new List<string> {
    "Avasarala", "Liberty", "Fraternity", "Equality", "Meseeks", "Squanchy", "Nimbus", "Mojo",
    "Nostromo", "Odyssey", "Bono", "Naru", "Max", "Okoye", "Avery", "Angel", "Riley", "Shaw",
    "Vickers", "Marno", "Kris", "Constanza",

    "Saphira", "Lyra", "Elara", "Nova", "Talia", "Selene", "Auryn", "Freya", "Nerissa", "Ophelia",
    "Isolde", "Amaris", "Seren", "Elysia", "Zaria", "Anwen", "Nyssa", "Thalassa", "Ione", "Callista",
    "Vespera", "Solene", "Liora", "Cassiopeia", "Xanthe", "Aeliana", "Verena", "Althea", "Ysolde",
    "Zemira", "Maelis", "Soraya", "Eleutheria", "Mariel", "Cythia", "Valeria", "Lorien", "Tirzah",
    "Zafira", "Araceli", "Evadne", "Orla", "Zephyra", "Iskra", "Vayla", "Elowen", "Celestia", "Meira",
    "Rhea", "Selis", "Eirene", "Kalista", "Nyx", "Sylva", "Evania",

    // Newest additions
    "Seraphina", "Lunara", "Azura", "Kaelia", "Solara", "Lyanna", "Evania", "Artemis", "Theia", "Sienna",
    "Illyria", "Celestine", "Althaea", "Vaelora", "Xyra", "Nocturne", "Elira", "Damaris", "Selinia", "Ravena",
    "Ephyra", "Zyra", "Thessia", "Aeris", "Liora", "Zerina", "Calista", "Ilythia", "Mirabel", "Naeris",
    "Isilme", "Velora", "Tindra", "Astoria", "Cytherea", "Zanthe", "Aelith", "Quintessa", "Yliana", "Sybella",
    "Virelia", "Xanthea", "Tyrelia", "Caelith", "Nivara", "Zyphira", "Avaris", "Nymara", "Soliana", "Veralya",
    "Meliora", "Ithara", "Orlith", "Faelira", "Nytheria", "Zelara"
};




        public static List<string> pu_Names = new List<string> {
     "AirKris", "AndyP", "Brunks",
    "Dante", "Geda", "GeoDao", "Hokken", "Gollop", "Kal", "Millioneigher", "NoobCaptain",
    "Origami", "Ravenoid", "Bobby", "Stridtul", "Tyraenon", "Ikobot", "Valygar", "E.E.",
    "BFIG", "Sheepy", "Conductiv", "mad2342", "Pantolomin", "Etermes", "Mokushi",
    
    // Previous Additions
    "Axiom", "Cipher", "Zenith", "Mechatron", "Omicron", "Hyperion", "Dynamo", "Spectra",
    "Tesseract", "Mach", "Sigma", "Altron", "Nexus", "Vector", "Glitch", "Augur", "Xypher",
    "Cyberius", "Neuron", "Byte", "Exo", "Xeno", "Chronos", "Hex", "Modulo", "Quantum",
    "Parsec", "Synapse", "Metron", "Stratos", "Echelon", "Kyber", "Nano", "Oblivion",
    "Scion", "Lucid", "Technos", "V1per", "Drone", "Havok", "Magnetar", "Polaris",
    "Omen", "Zed", "Eidolon", "Infinity", "Rift", "Zero", "Circuit", "Pulse", "Cyberis",
    "Void", "Aegis", "Xypheron", "Nyx", "Override", "Horizon", "Relay", "Arc", "Phantom",
    "Cognis", "Silicor", "Echo", "Neutrino", "Titan", "Omega", "Xylon", "Synthetis", "Omnis",
    "Anomaly", "Perseus", "Vortex", "Solon", "Helix", "Ziron", "Aetherion", "Mechanoid",

    // New Additions
    "Nebulus", "Astrolith", "Xyber", "Aeternis", "Voidwalker", "Chronovore", "Cyberion",
    "Excaliber", "Kryo", "Opticon", "Zyron", "Icarus", "Override", "Subroutine",
    "OmniMind", "Vortexus", "Neon", "Sudo", "Kilon", "Gladius", "Plexus", "Infinitron",
    "Aetheris", "Mechanox", "Vega", "Quasix", "Voltaic", "Electra", "Neural", "Node",
    "Mechamancer", "Irongeist", "Synergy", "Kryon", "Quantumis", "Singularis", "Astrafire",
    "Zircon", "Datashade", "Technovore", "Netrix", "Parallax", "Xyberus", "Eon", "Omni",
    "EchoPrime", "Oraculum", "Lexar", "Vex", "Gigas", "Titanis", "Zykon", "Zenthesis",
    "Drift", "Paragon", "Voidcore", "Seraphon", "Mechavore", "OmegaX", "Helion", "Corteks",
    "Synexis", "Cyberforge", "Xerion", "Datalis", "Neurocore", "Cyberneticus", "GlitchX",
    "Corex", "Luminous", "Astralis", "Valkyris", "NexusX", "MechaPrime", "Orion", "Zenox",
    "Neuromancer", "Aetheron", "Pyrex", "Zypher", "Bitstream", "Overclock", "Metaflux"
};


        public static List<string> fo_Names = new List<string> {
    "Thriceborn", "Shai-Hulud", "Shorr Kan", "Yurtle", "Lorax", "Seer", "Belial", "Torinus",
    "Voland", "Yar-Shalak", "Ghul", "Gheist", "Melachot", "Xelot", "Nacht-Zur'acht", "Bane",
    "Oshazahul", "Slithering", "Azelot", "Ursuk", "Hottaku", "Weirdling", "Outsider", "Tleilaxu",
    "Tuek", "Whisperblade", "Bladehands", "Yokes", "Thoth", "Amon", "Belit", "Numedides", "Taramis",
    "Azarax", "Valyndor", "Ebonshade", "Zephyrial", "Drakarn", "Shadowweaver", "Nyxaris", "Thalorin",
    "Voxariel", "Malachai", "Caelinor", "Zylthar", "Serpentia", "Vaelus", "Myrkaal", "Xylerith",
    "Vortigan", "Morvain", "Sylveran", "Ashryn",

    // New Additions
    "Duskveil", "Omen", "Morghul", "Varzith", "Xirion", "Zha'kar", "Vorzhul", "Dreloth",
    "Khalzarak", "Nazareth", "Sablefang", "Vesper", "Erebus", "Duskwraith", "Hollowborn",
    "Azoth", "Nyxshade", "Zar'khan", "Sabbath", "Noc'tara", "Lurker", "Oblivion", "Wraithborn",
    "Karnyx", "Sableborn", "Malakar", "Voidfang", "Zherathis", "Xothul", "Dreadweaver",
    "Morgrim", "Vel'kar", "Shar'zul", "Xandor", "Azarik", "Eldrik", "Vaesh", "Thanis", "Krythos",
    "Zhovan", "Tzalik", "Shadovarn", "Veyzorith", "Skorn", "Ozrakar", "Tenebros", "Gloomfang",
    "Zha'thor", "Mal'azar", "Voidstalker", "Xy'zan", "Zhakaroth", "Velkaris", "Umbrazar", "Vaelkor",
    "Nychthemeron", "Ghavor", "Zyrrak", "Tharn", "Sorothis", "Velizar", "Duskborn", "Hollowfang",
    "Mournblade", "Vorakos", "Shadowfang", "Noctis", "Xerathis", "Vorkash", "Malzan", "Zyrix",
    "Necros", "Doomveil", "Varkon", "Kaelthas", "Ravynor", "Zal'vash", "Tharizdun", "Shadryn",
    "Morvael", "Nightpiercer", "Velmoran", "Exarion", "Kylaris", "Zaedryn", "Vorgrimm", "Necrotis",
    "Xalthor", "Grimwraith", "Vel'zaroth", "Zharthus", "Xolthar", "Voidreaper", "Sableveil",
    "Zharok", "Thanathel", "Ebonfang", "Nyxthar", "Xel'zor", "Gorathis", "Shadowthorn",
    "Velkael", "Zylaroth", "Varothis", "Kaldarax", "Vorithis", "Umbrax", "Omenborn",
    "Xaedros", "Vezarith", "Mournshade", "Zyphos", "Velthar", "Duskveil", "Xaeloth", "Varkros",
    "Zyphar", "Gloomveil", "Necrothar", "Tenebral", "Noctalis", "Kaelgrim", "Xaedrith", "Zal'zor",
    "Veyloris", "Oblivionborn", "Voidheart", "Xalthir", "Kyvoris", "Than'kor", "Sablethorn",
    "Mournthar", "Velzar", "Nycthos", "Zhakar", "Kaelzor", "Umbraith", "Zyvoris", "Vaelkoris",
    "Thal'zor", "Zyvalis", "Xaerthos", "Vorith", "Xorath", "Velmoris", "Shadowveil"
};


        public static List<string> ban_NameRanks = new List<string> {
 "HUMAN_ENEMIES_KEY_BOSS",
"HUMAN_ENEMIES_KEY_ENFORCER",
"HUMAN_ENEMIES_KEY_RAIDER",
"HUMAN_ENEMIES_KEY_CARRION"
        };
        public static List<string> nj_NameRanks = new List<string> {
            "HUMAN_ENEMIES_KEY_LEADER",
"HUMAN_ENEMIES_KEY_VETERAN",
"HUMAN_ENEMIES_KEY_JACKBOOT",
"HUMAN_ENEMIES_KEY_GREENHORN"};
        public static List<string> syn_NameRanks = new List<string> {
            "HUMAN_ENEMIES_KEY_WARDEN",
"HUMAN_ENEMIES_KEY_RANGER",
"HUMAN_ENEMIES_KEY_PEACEKEEPER",
"HUMAN_ENEMIES_KEY_CITIZEN"};
        public static List<string> anu_NameRanks = new List<string> {
            "HUMAN_ENEMIES_KEY_TAXIARCH",
"HUMAN_ENEMIES_KEY_ADEPT",
"HUMAN_ENEMIES_KEY_ACOLYTE",
"HUMAN_ENEMIES_KEY_NEOPHYTE"};
        public static List<string> pu_NameRanks = new List<string> {
            "HUMAN_ENEMIES_KEY_MACHINA",
"HUMAN_ENEMIES_KEY_METALHEART",
"HUMAN_ENEMIES_KEY_CLEANSED",
"HUMAN_ENEMIES_KEY_MEATHEAD"};
        public static List<string> fo_NameRanks = new List<string> {
            "HUMAN_ENEMIES_KEY_SCOURGE",
"HUMAN_ENEMIES_KEY_EVOLVED",
"HUMAN_ENEMIES_KEY_REBORN",
"HUMAN_ENEMIES_KEY_FLEDGLING"};

        public static string tier1description = "HUMAN_ENEMIES_KEY_TIER_1";//"Creates special effect on allies and/or enemies (Tactic). Allies lose 4 WP if character dies.";
        public static string tier2description = "HUMAN_ENEMIES_KEY_TIER_2";//"Allies who can see the character gain +1 WP per turn and lose 3WP if character dies.";
        public static string tier3description = "HUMAN_ENEMIES_KEY_TIER_3";//"Allies lose 2 WP if character dies.";
        public static string tier4description = "HUMAN_ENEMIES_KEY_TIER_4";//"Allies do not lose WP if character dies. Nobody expects them to live long anyway.";

        public static List<string> tierDescriptions = new List<string>();

        public static void CreateTierDescriptions()
        {
            try
            {
                tierDescriptions.Add(new LocalizedTextBind() { LocalizationKey = tier1description }.Localize());
                tierDescriptions.Add(new LocalizedTextBind() { LocalizationKey = tier2description }.Localize());
                tierDescriptions.Add(new LocalizedTextBind() { LocalizationKey = tier3description }.Localize());
                tierDescriptions.Add(new LocalizedTextBind() { LocalizationKey = tier4description }.Localize());

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

    }
}
