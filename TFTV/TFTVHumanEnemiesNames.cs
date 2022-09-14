using System;
using System.Collections.Generic;

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

        public static Dictionary<string, List<string>> names = new Dictionary<string, List<string>>();
        public static Dictionary<string, List<string>> ranks = new Dictionary<string, List<string>>();
        
        public static void CreateNamesDictionary()
        {
            try 
            { 
                names.Add("ban", ban_Names);
                names.Add("nj", nj_Names);
                names.Add("anu", anu_Names);
                names.Add("syn", syn_Names);
                names.Add("Purists", pu_Names);
                names.Add("FallenOnes", fo_Names);

                ranks.Add("ban", ban_NameRanks);
                ranks.Add("nj", nj_NameRanks);
                ranks.Add("anu", anu_NameRanks);
                ranks.Add("syn", syn_NameRanks);
                ranks.Add("Purists", pu_NameRanks);
                ranks.Add("FallenOnes", fo_NameRanks);

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

        }


        public static List <string> ban_Names = new List <string> {"Nuka-Cola","Kessar","Viper","Rictus","Nux","Dag","Ace","Barry","Mohawk","Bearclaw","Clunk",
            "Deepdog","Fuk-Ushima","Fifi Macaffe","Sol","Gutgash","Ironbar","Morsov","Mudguts","Papagallo","Sarse","Sav","Roop","Blackfinger","Scrooloose",
            "Scuttle","Starbuck","Slit","Slake","Tenderloin","Toadie","Toecutter","Toast","Kane","Splitear","Brainrot","Maddog","Coyote","Birdsheet",
            "Gearhead","Yo-yo","Madskunky","Walker","Charlie","Wez","Ziggy","Alex","Max","Miller","Kenzo","Karal","Katoa","Okoye","Fagan","Avery",
            "Parker","Joyce","Kai","Angel","Jesse","Riley","Ash","Finley","Shaw","Vickers","Marno","Jikkola","Kris","Strid","Showalter","Grimsrud"};

        public static List<string> nj_Names = new List<string> {"Rockatansky","Bryant","Richter",
            "Ripley","Amos" ,"Draper","Caleb" ,"Hunter","Tempest","Kruger","Sinclair","Morgan","Musk","Jackson","Hicks","Vasquez","Hudson","Ferro","Spunkmeyer",
            "Dietrich","Frost","Drake","Wierzbowski","Payne","Ventura","Dutch","Dillon","Hawkins","Mac","Poncho","Walker","Charlie","Wez","Ziggy",
            "Alex","Max","Miller","Kenzo","Karal","Katoa","Okoye","Fagan","Avery","Parker","Joyce","Kai","Angel","Jesse","Riley","Ash",
            "Finley","Shaw","Vickers","Marno","Jikkola","Kris","Strid","Showalter","Grimsrud"};
        public static List<string> anu_Names = new List<string> {"Walker","Charlie","Wez","Ziggy","Alex","Max","Miller","Kenzo","Karal","Katoa",
            "Okoye","Fagan","Avery","Parker","Joyce","Kai","Angel","Jesse","Riley","Ash","Finley","Shaw","Vickers","Marno","Jikkola",
            "Kris","Strid","Showalter","Grimsrud"};

        public static List<string> syn_Names = new List<string> {"Nagata","Inaros","Avasarala","Liberty","Fraternity","Equality","Lenin","Tenet",
            "Campion","Meseeks","Squanchy","Nimbus","Mojo","Nostromo","Odyssey","Bono","Eli","Naru","Taabe","Sanchez","Walker","Charlie",
            "Wez","Ziggy","Alex","Max","Miller","Kenzo","Karal","Katoa","Okoye","Fagan","Avery","Parker","Joyce","Kai","Angel","Jesse",
            "Riley","Ash","Finley","Shaw","Vickers","Marno","Jikkola","Kris","Strid","Showalter","Grimsrud"};

        public static List<string> pu_Names = new List<string> {"Walker","Charlie","Wez","Ziggy","Alex","Max","Miller","Kenzo","Karal","Katoa",
            "Okoye","Fagan","Avery","Parker","Joyce","Kai","Angel","Jesse","Riley","Ash","Finley","Shaw","Vickers", "Showalter","Grimsrud", "AirKris",
"AndyP","Brunks","Dante","Geda","GeoDao","Hokken","Gollop","Kal","Millioneigher","NoobCaptain","Origami","Ravenoid","Bobby","Stridtul",
"Tyraenon","Ikobot","Valygar","E.E.","BFIG","Sheepy","Conductiv","mad2342","Pantolomin"
};
        public static List<string> fo_Names = new List<string> {"Thriceborn","Shai-Hulud","Shorr Kan","Yurtle","Lorax","Seer","Belial","Torinus",
            "Voland","Yar-Shalak","Ghul","Gheist","Melachot","Xelot","Nacht-Zur'acht","Bane","Oshazahul","Slithering","Azelot",
            "Ursuk","Hottaku","Weirdling","Outsider","Tleilaxu","Tuek","Whisperblade","Bladehands", "Yokes"};

        public static List<string> ban_NameRanks = new List<string> { "Boss", "Enforcer", "Raider", "Carrion" };
        public static List<string> nj_NameRanks = new List<string> { "Leader", "Veteran", "Jackboot", "Greenhorn"};
        public static List<string> syn_NameRanks = new List<string> { "Warden", "Ranger", "Peacekeeper", "Citizen" };
        public static List<string> anu_NameRanks = new List<string> { "Taxiarch", "Adept", "Acolyte", "Neophyte"};
        public static List<string> pu_NameRanks = new List<string> { "Machina", "Metalheart", "Cleansed", "Meathead"};
        public static List<string> fo_NameRanks = new List<string> { "Scourge", "Evolved", "Reborn", "Fledgling" };

        public static string tier1description = "While alive, creates special effect on allies and/or enemies (Tactics). Allies lose 4 WP if character dies.";
        public static string tier2description = "Allies who can see the character gain +1 WP per turn and lose 3WP if character dies.";
        public static string tier3description = "Allies lose 2 WP if character dies.";
        public static string tier4description = "Allies do not lose WP if character dies. Nobody expects them to live long anyway.";

        public static List<string>tierDescriptions = new List<string> { tier1description, tier2description, tier3description, tier4description };
        
    }
}
