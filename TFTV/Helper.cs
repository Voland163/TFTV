using Base.Defs;
using HarmonyLib;
using I2.Loc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TFTV;
using UnityEngine;

namespace PRMBetterClasses
{
    internal class Helper
    {
        internal static string ModDirectory;
        internal static string ManagedDirectory;
        internal static string TexturesDirectory;
 

        
        //public static readonly string FsStoryLocalizationFileName = "PR_FS_Story_Localization.csv";

        // SP cost for main specialisation skills per level
        public static readonly int[] SPperLevel = new int[] { 0, 10, 15, 0, 20, 25, 30 };

        // Desearialize dictionary from Json to map ability names to Defs
        public static readonly string AbilitiesJsonFileName = "AbilityDefToNameDict.json";
        public static Dictionary<string, string> AbilityNameToDefMap;

        // Desearialize dictionary from Json to map non localized texts to ViewDefs
        public static readonly string TextMapFileName = "NotLocalizedTextMap.json";
        public static Dictionary<string, Dictionary<string, string>> NotLocalizedTextMap;


        public static void Initialize()
        {
            try
            {
                ModDirectory = TFTVMain.Main.Instance.Entry.Directory; ;
                ManagedDirectory = Path.Combine(ModDirectory, "Assets", "Presets");
                TexturesDirectory = Path.Combine(ModDirectory, "Assets", "Textures");
              
                AbilityNameToDefMap = ReadJson<Dictionary<string, string>>(AbilitiesJsonFileName);
                NotLocalizedTextMap = ReadJson<Dictionary<string, Dictionary<string, string>>>(TextMapFileName);
               
            }
            catch (Exception e)
            {
                PRMLogger.Error(e);
            }
        }

      

        /// <summary>
        /// Copy fields of two objects of the same or derived classes by using reflection
        /// </summary>
        /// <param name="src">The source object</param>
        /// <param name="dst">The destination object, can be an instance of a derived class of the source, all additional fields are skipped</param>
        /// <param name="bindFlags"></param>
        public static void CopyFieldsByReflection(object src, object dst, BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            Type srcType = src.GetType();
            foreach (FieldInfo dstFieldInfo in dst.GetType().GetFields(bindFlags))
            {
                FieldInfo srcField = srcType.GetField(dstFieldInfo.Name, bindFlags);
                if (srcField != null && srcField.Name != "Guid")
                {
                    dstFieldInfo.SetValue(dst, srcField.GetValue(src));
                }
            }
        }

        /// <summary>
        /// Generating a pair of independent standard normal random variables.
        /// Implements the Marsaglia polar method (https://en.wikipedia.org/wiki/Marsaglia_polar_method), a modification of the Box–Muller method which does not require computation of the sine and cosine functions.
        /// </summary>
        /// <param name="mean">Mean</param>
        /// <param name="stdDev">Standard deviation</param>
        /// <returns>A pair of independent standard normal random variables</returns>
        public static (float, float) GenerateRandomGaussianPair(float mean = 0.0f, float stdDev = 1.0f)
        {
            float u, v, S;

            do
            {
                u = 2.0f * UnityEngine.Random.value - 1.0f;
                v = 2.0f * UnityEngine.Random.value - 1.0f;
                S = (u * u) + (v * v);
            }
            while (S >= 1.0f || S == 0.0f);

            S = Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

            float gauss1 = mean + (stdDev * u * S);
            float gauss2 = mean + (stdDev * v * S);
            return (gauss1, gauss2);
        }

        
        public static T CreateDefFromClone<T>(T source, string guid, string name) where T : BaseDef
        {
            try
            {
                PRMLogger.Debug("CreateDefFromClone called ... ");
                PRMLogger.Debug($"CreateDefFromClone, check if GUID <{guid}> already exist in Repo ...");
                DefRepository Repo = TFTVMain.Repo;
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
                            PRMLogger.Debug($"CreateDefFromClone, <{guid}> already in Repo, <{tmp}> returned as result.");
                            return tmp;
                        }
                    }
                }
                PRMLogger.Debug($"CreateDefFromClone, additional check if GUID <{guid}> already exist in Repo RuntimeDefs ...");
                T tmp2 = Repo.GetRuntimeDefs<T>(true).FirstOrDefault(rt => rt.Guid.Equals(guid));
                if (tmp2 != null)
                {
                    PRMLogger.Debug($"CreateDefFromClone, <{guid}> already in Repo RunTimeDefs, <{tmp2}> returned as result.");
                    return tmp2;
                }
                PRMLogger.Debug($"CreateDefFromClone, start name creation with parameter '{name}' ...");
                Type type = null;
                string resultName = "";
                if (source != null)
                {
                    int start = source.name.IndexOf('[') + 1;
                    int end = source.name.IndexOf(']');
                    string toReplace = !name.Contains("[") && start > 0 && end > start ? source.name.Substring(start, end - start) : source.name;
                    resultName = source.name.Replace(toReplace, name);
                    PRMLogger.Debug($"CreateDefFromClone, name '{resultName}' created, start cloning from <{source.name}> with type <{source.GetType().Name}> ...");
                }
                else
                {
                    type = typeof(T);
                    resultName = name;
                    PRMLogger.Debug($"CreateDefFromClone, name '{resultName}' created, start creating Def of type <{typeof(T).Name}> ...");
                }
                T result = (T)Repo.CreateDef(
    guid,
    source,
    type);

                result.name = resultName;
                TFTVMain.Main.DefCache.AddDef(result.name, result.Guid);
                PRMLogger.Debug($"CreateDefFromClone, <{result.name}> of type <{result.GetType().Name}> sucessful created.");
                PRMLogger.Debug("----------------------------------------------------", false);
                return result;
            }
            catch (Exception e)
            {
                PRMLogger.Error(e);
                return null;
            }
        }

        public static Sprite CreateSpriteFromImageFile(string imageFileName, int width = 128, int height = 128, TextureFormat textureFormat = TextureFormat.RGBA32, bool mipChain = true)
        {
            try
            {
                string filePath = Path.Combine(TexturesDirectory, imageFileName);
                byte[] data = File.Exists(filePath) ? File.ReadAllBytes(filePath) : throw new FileNotFoundException("File not found: " + filePath);
                Texture2D texture = new Texture2D(width, height, textureFormat, mipChain);
                return ImageConversion.LoadImage(texture, data)
                    ? Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.0f, 0.0f))
                    : null;
            }
            catch (Exception e)
            {
                PRMLogger.Error(e);
                return null;
            }
        }

        // Read embedded or external json file
        public static T ReadJson<T>(string fileName)
        {
            try
            {
                string json = null;
                //Assembly assembly = Assembly.GetExecutingAssembly();
                //string source = assembly.GetManifestResourceNames().Single(str => str.EndsWith(fileName));
                string filePath = Path.Combine(ManagedDirectory, fileName);
                //DateTime fileLastChanged = File.GetLastWriteTime(filePath);
                //DateTime assemblyLastChanged = File.GetLastWriteTime(assembly.Location);
                //if (source != null && source != "" && fileLastChanged < assemblyLastChanged)
                //{
                //    PRMLogger.Always("----------------------------------------------------------------------------------------------------", false);
                //    PRMLogger.Always("Read JSON from assembly: " + source);
                //    PRMLogger.Always("----------------------------------------------------------------------------------------------------", false);
                //    using (Stream stream = assembly.GetManifestResourceStream(source))
                //    using (StreamReader reader = new StreamReader(stream))
                //    {
                //        json = reader.ReadToEnd();
                //    }
                //}
                if (json == null || json == "")
                {
                    PRMLogger.Always("----------------------------------------------------------------------------------------------------", false);
                    PRMLogger.Always("Read JSON from file: " + filePath);
                    PRMLogger.Always("----------------------------------------------------------------------------------------------------", false);
                    json = File.Exists(filePath) ? File.ReadAllText(filePath) : throw new FileNotFoundException(filePath);
                }
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                PRMLogger.Error(e);
                return default;
            }
        }

        // Write to external json file
        public static void WriteJson(string fileName, object obj, bool toFile = true)
        {
            try
            {
                string jsonString = JsonConvert.SerializeObject(obj, Formatting.Indented);
                if (toFile)
                {
                    //string ModDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string filePath = Path.Combine(ManagedDirectory, fileName);
                    if (File.Exists(filePath))
                    {
                        File.WriteAllText(Path.Combine(ManagedDirectory, fileName), jsonString);
                        PRMLogger.Always("----------------------------------------------------------------------------------------------------", false);
                        PRMLogger.Always("Write JSON to file: " + filePath);
                        PRMLogger.Always("----------------------------------------------------------------------------------------------------", false);
                    }
                    else
                    {
                        throw new FileNotFoundException(filePath);
                    }
                }
                // Writing in running assembly -- TODO: if really needed -> figure out to make it possible
                //Assembly assembly = Assembly.GetExecutingAssembly();
                //string source = assembly.GetManifestResourceNames().Single(str => str.EndsWith(fileName));
                //if (source != null || source != "")
                //{
                //    using (Stream stream = assembly.GetManifestResourceStream(source))
                //    using (StreamWriter writer = new StreamWriter(stream))
                //    {
                //        writer.Write(jsonString);
                //    }
                //}
            }
            catch (Exception e)
            {
                PRMLogger.Error(e);
            }
        }
    }
}
