﻿using Base.Defs;
using I2.Loc;
using System;
using System.IO;
using System.Linq;
using UnityEngine;



namespace TFTV
{
    internal class Helper
    {
        // Get config, definition repository (and shared data, not neccesary currently)

        private static readonly DefRepository Repo = TFTVMain.Repo;
        internal static string ModDirectory;
        internal static string LocalizationDirectory;


        public static readonly string AbilitiesLocalizationFileName = "TFTV_AbilitiesEffectsStatusesTactical_Localization.csv";
        public static readonly string CharactersLocalizationFileName = "TFTV_CharactersItemsFacilities_Localization.csv";
        public static readonly string EventsLocalizationFileName = "TFTV_Events_Localization.csv";
          
        public static readonly string GeoUIElementsLocalizationFileName = "TFTV_GeoUIElements_Localization.csv";      
        public static readonly string HintsGeoLocalizationFileName = "TFTV_HintsGeo_Localization.csv";
        public static readonly string HintsTacticalLocalizationFileName = "TFTV_HintsTactical_Localization.csv";

        public static readonly string LoreLocalizationFileName = "TFTV_LoreAndTips_Localization.csv";
        public static readonly string MissionObjectivesLocalizationFileName = "TFTV_MissionObjectives_Localization.csv";
        public static readonly string OptionsLocalizationFileName = "TFTV_Options_Localization.csv";
        public static readonly string ResearchLocalizationFileName = "TFTV_Research_Localization.csv";
        public static readonly string VoidOmensAndODIyLocalizationFileName = "TFTV_VoidOmensAndODI_Localization.csv";
        public static readonly string NJStoryLocalizationFileName = "TFTV_NJStoryMissions_Localization.csv";
        public static readonly string AircraftReworkLocalizationFileName = "TFTV_AircraftRework_Localization.csv";



        public static void Initialize()
        {
            try
            {
                ModDirectory = TFTVMain.ModDirectory;
                LocalizationDirectory = TFTVMain.LocalizationDirectory;
                if (File.Exists(Path.Combine(LocalizationDirectory, AbilitiesLocalizationFileName)))
                {
                    AddLocalizationFromCSV(AbilitiesLocalizationFileName, null);
                }
                if (File.Exists(Path.Combine(LocalizationDirectory, CharactersLocalizationFileName)))
                {
                    AddLocalizationFromCSV(CharactersLocalizationFileName, null);
                }
                if (File.Exists(Path.Combine(LocalizationDirectory, EventsLocalizationFileName)))
                {
                    AddLocalizationFromCSV(EventsLocalizationFileName, null);
                }

                if (File.Exists(Path.Combine(LocalizationDirectory, GeoUIElementsLocalizationFileName)))
                {
                    AddLocalizationFromCSV(GeoUIElementsLocalizationFileName, null);
                }
                if (File.Exists(Path.Combine(LocalizationDirectory, HintsGeoLocalizationFileName)))
                {
                    AddLocalizationFromCSV(HintsGeoLocalizationFileName, null);
                }
                if (File.Exists(Path.Combine(LocalizationDirectory, HintsTacticalLocalizationFileName)))
                {
                    AddLocalizationFromCSV(HintsTacticalLocalizationFileName, null);
                }

                if (File.Exists(Path.Combine(LocalizationDirectory, LoreLocalizationFileName)))
                {
                    AddLocalizationFromCSV(LoreLocalizationFileName, null);
                }
                if (File.Exists(Path.Combine(LocalizationDirectory, OptionsLocalizationFileName)))
                {
                    AddLocalizationFromCSV(OptionsLocalizationFileName, null);
                }
                if (File.Exists(Path.Combine(LocalizationDirectory, ResearchLocalizationFileName)))
                {
                    AddLocalizationFromCSV(ResearchLocalizationFileName, null);
                }
                if (File.Exists(Path.Combine(LocalizationDirectory, VoidOmensAndODIyLocalizationFileName)))
                {
                    AddLocalizationFromCSV(VoidOmensAndODIyLocalizationFileName, null);
                }
                if (File.Exists(Path.Combine(LocalizationDirectory, MissionObjectivesLocalizationFileName)))
                {
                    AddLocalizationFromCSV(MissionObjectivesLocalizationFileName, null);
                }
              /*  if (File.Exists(Path.Combine(LocalizationDirectory, NJStoryLocalizationFileName))) 
                { 
                    AddLocalizationFromCSV(NJStoryLocalizationFileName, null);       
                }*/
                if (TFTVAircraftRework.AircraftReworkOn && File.Exists(Path.Combine(LocalizationDirectory, AircraftReworkLocalizationFileName)))
                {
                    AddLocalizationFromCSV(AircraftReworkLocalizationFileName, null);
                }

            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        // Read localization from CSV file
        public static void AddLocalizationFromCSV(string LocalizationFileName, string Category = null)
        {
            try
            {
                string CSVstring = File.ReadAllText(Path.Combine(LocalizationDirectory, LocalizationFileName));
                if (!CSVstring.EndsWith("\n"))
                {
                    CSVstring += "\n";
                }
                LanguageSourceData SourceToChange = Category == null ? // if category is not given
                    LocalizationManager.Sources[0] :                   // use fist language source
                    LocalizationManager.Sources.First(source => source.GetCategories().Contains(Category));
                if (SourceToChange != null)
                {
                    int numBefore = SourceToChange.mTerms.Count;
                    _ = SourceToChange.Import_CSV(string.Empty, CSVstring, eSpreadsheetUpdateMode.AddNewTerms, ',');
                    LocalizationManager.LocalizeAll(true);    // Force localing all enabled labels/sprites with the new data
                    int numAfter = SourceToChange.mTerms.Count;
                    TFTVLogger.Always("----------------------------------------------------------------------------------------------------", false);
                    TFTVLogger.Always($"Added {numAfter - numBefore} terms from {LocalizationFileName} in localization source {SourceToChange}, category: {Category}");
                    TFTVLogger.Always("----------------------------------------------------------------------------------------------------", false);
                }
                else
                {
                    TFTVLogger.Always("----------------------------------------------------------------------------------------------------", false);
                    TFTVLogger.Always($"No language source with category {Category} found!");
                    TFTVLogger.Always("----------------------------------------------------------------------------------------------------", false);
                }
                TFTVLogger.Debug("----------------------------------------------------------------------------------------------------", false);
                TFTVLogger.Debug("CSV Data:" + Environment.NewLine + CSVstring);
                foreach (LanguageSourceData source in LocalizationManager.Sources)
                {
                    TFTVLogger.Debug($"Source owner {source.owner}{Environment.NewLine}Categories:{Environment.NewLine}{{source.GetCategories().Join()}}{Environment.NewLine}", false);
                }
                TFTVLogger.Debug("----------------------------------------------------------------------------------------------------", false);
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }
        // Creating new runtime def by cloning from existing def
        public static T CreateDefFromClone<T>(T source, string guid, string name) where T : BaseDef
        {
            try
            {
                TFTVLogger.Debug("CreateDefFromClone called ... ");
                TFTVLogger.Debug($"CreateDefFromClone, check if GUID <{guid}> already exist in Repo ...");
                if (Repo.GetDef(guid) != null)
                {
                    if (!(Repo.GetDef(guid) is T tmp))
                    {
                        throw new TypeAccessException($"An item with the GUID <{guid}> has already been added to the Repo, but the type <{Repo.GetDef(guid).GetType().Name}> does not match <{typeof(T).Name}>!");
                    }
                    else
                    {
                        if (tmp != null)
                        {
                            TFTVLogger.Debug($"CreateDefFromClone, <{guid}> already in Repo, <{tmp}> returned as result.");
                            return tmp;
                        }
                    }
                }
                TFTVLogger.Debug($"CreateDefFromClone, additional check if GUID <{guid}> already exist in Repo RuntimeDefs ...");
                T tmp2 = Repo.GetRuntimeDefs<T>(true).FirstOrDefault(rt => rt.Guid.Equals(guid));
                if (tmp2 != null)
                {
                    TFTVLogger.Debug($"CreateDefFromClone, <{guid}> already in Repo RunTimeDefs, <{tmp2}> returned as result.");
                    return tmp2;
                }
                TFTVLogger.Debug($"CreateDefFromClone, start name creation with parameter '{name}' ...");
                Type type = null;
                string resultName = "";
                if (source != null)
                {
                    int start = source.name.IndexOf('[') + 1;
                    int end = source.name.IndexOf(']');
                    string toReplace = !name.Contains("[") && start > 0 && end > start ? source.name.Substring(start, end - start) : source.name;
                    resultName = source.name.Replace(toReplace, name);
                    TFTVLogger.Debug($"CreateDefFromClone, name '{resultName}' created, start cloning from <{source.name}> with type <{source.GetType().Name}> ...");
                }
                else
                {
                    type = typeof(T);
                    resultName = name;
                    TFTVLogger.Debug($"CreateDefFromClone, name '{resultName}' created, start creating Def of type <{typeof(T).Name}> ...");
                }

                T result = (T)Repo.CreateDef(guid, source, type);

                
                result.name = resultName;
                TFTVMain.Main.DefCache.AddDef(result.name, result.Guid);
                TFTVLogger.Debug($"CreateDefFromClone, <{result.name}> of type <{result.GetType().Name}> sucessful created.");
                TFTVLogger.Debug("----------------------------------------------------", false);
                return result;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }

        }
        public static Sprite CreateSpriteFromImageFile(string imageFileName, int width = 128, int height = 128, TextureFormat textureFormat = TextureFormat.RGBA32, bool mipChain = true)
        {
            try
            {
                string filePath = Path.Combine(TFTVMain.TexturesDirectory, imageFileName);
                byte[] data = File.Exists(filePath) ? File.ReadAllBytes(filePath) : throw new FileNotFoundException("File not found: " + filePath);
                Texture2D texture = new Texture2D(width, height, textureFormat, mipChain);
                return ImageConversion.LoadImage(texture, data)
                    ? Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.0f, 0.0f))
                    : null;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }
        public static Sprite CreatePortraitFromImageFile(string imageFileName, int width = 128, int height = 128, TextureFormat textureFormat = TextureFormat.RGBA32, bool mipChain = true)
        {
            try
            {
                string filePath = Path.Combine(ModDirectory, "Assets", "Textures", "Portraits", imageFileName);
                byte[] data = File.Exists(filePath) ? File.ReadAllBytes(filePath) : throw new FileNotFoundException("File not found: " + filePath);
                Texture2D texture = new Texture2D(width, height, textureFormat, mipChain);
                return ImageConversion.LoadImage(texture, data)
                    ? Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.0f, 0.0f))
                    : null;
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
                return null;
            }
        }

    }

}