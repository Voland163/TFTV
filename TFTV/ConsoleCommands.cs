using Base.Core;
using Base.Utils.GameConsole;
using Epic.OnlineServices;
using PhoenixPoint.Common.Entities.Items;
using PhoenixPoint.Modding;
using PhoenixPoint.Tactical.Entities;
using PhoenixPoint.Tactical.Entities.Equipments;
using PhoenixPoint.Tactical.Entities.Statuses;
using PhoenixPoint.Tactical.Levels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TFTV;
using UnityEngine.Profiling.Memory.Experimental;

namespace MadSkunkyTweaks.Tools
{
    public class ConsoleCommands
    {
        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;

        [ConsoleCommand(Command = "checkcrates", Description = "tell me what's inside the crates")]
        public static void SayHello(IConsole console)
        {
           

            TacticalLevelController tacticalLevelController = GameUtl.CurrentLevel().GetComponent<TacticalLevelController>();

            foreach (TacticalActorBase actor in tacticalLevelController.Map.GetActors<TacticalActorBase>())
            {
                TFTVLogger.Always($"{actor?.name}");

                if(actor is CrateItemContainer crate) 
                { 
                foreach(Item item in crate.Inventory.Items) 
                    {
                        TFTVLogger.Always($"item in crate is {item.ItemDef.name}");
                    }
                
                }
            }
        }

        /// Injcecting the mods console commands to the base game console handler
        public static void InjectConsoleCommands()
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                foreach (MethodInfo methodInfo in types[i].GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    if (Attribute.GetCustomAttribute(methodInfo, typeof(ConsoleCommandAttribute)) is ConsoleCommandAttribute consoleCommandAttribute)
                    {
                        if (!methodInfo.IsPublic)
                        {
                            throw new InvalidOperationException(string.Concat(new string[]
                            {
                                "ConsoleCommandAttribute is defined on method ",
                                methodInfo.DeclaringType.FullName,
                                ".",
                                methodInfo.Name,
                                " that is not public."
                            }));
                        }
                        if (!methodInfo.IsStatic)
                        {
                            throw new InvalidOperationException(string.Concat(new string[]
                            {
                                "ConsoleCommandAttribute is defined on method ",
                                methodInfo.DeclaringType.FullName,
                                ".",
                                methodInfo.Name,
                                " that is not static."
                            }));
                        }
                        ParameterInfo[] parameters = methodInfo.GetParameters();
                        if (parameters.Length == 0 || !typeof(IConsole).IsAssignableFrom(parameters[0].ParameterType))
                        {
                            throw new InvalidOperationException(string.Concat(new string[]
                            {
                                "ConsoleCommandAttribute is defined on method ",
                                methodInfo.DeclaringType.FullName,
                                ".",
                                methodInfo.Name,
                                " that does not have something implementing IConsole as first argument."
                            }));
                        }
                        int k = 1;
                        int num = parameters.Length;
                        while (k < num)
                        {
                            ParameterInfo parameterInfo = parameters[k];
                            if (k == parameters.Length - 1 && parameterInfo.ParameterType.IsArray && parameterInfo.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length != 0 && parameterInfo.ParameterType.GetElementType() == typeof(string))
                            {
                                typeof(ConsoleCommandAttribute).GetField("_variableArguments", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(consoleCommandAttribute, true);
                            }
                            else if (!TypeToConvertFunc.ContainsKey(parameterInfo.ParameterType))
                            {
                                throw new InvalidOperationException(string.Concat(new string[]
                                {
                                    "ConsoleCommandAttribute is defined on method ",
                                    methodInfo.DeclaringType.FullName,
                                    ".",
                                    methodInfo.Name,
                                    " that has a parameter ",
                                    parameterInfo.Name,
                                    " that is of unsupported type."
                                }));
                            }
                            k++;
                        }
                        typeof(ConsoleCommandAttribute).GetField("_methodInfo", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(consoleCommandAttribute, methodInfo);
                        string key = consoleCommandAttribute.Command ?? methodInfo.Name;

                        // get access to the base game private static field of the console command handler to inject all commands from this mod
                        // Original: ConsoleCommandAttribute.CommandToInfo[key] = consoleCommandAttribute;
                        SortedList<string, ConsoleCommandAttribute> BaseCommandToInfo = (SortedList<string, ConsoleCommandAttribute>)typeof(ConsoleCommandAttribute).GetField("CommandToInfo", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                        BaseCommandToInfo[key] = consoleCommandAttribute;
                    }
                }
            }
        }
        public static readonly Dictionary<Type, Func<string, object>> TypeToConvertFunc = new Dictionary<Type, Func<string, object>>
        {
            {
                typeof(sbyte),
                (string v) => sbyte.Parse(v)
            },
            {
                typeof(short),
                (string v) => short.Parse(v)
            },
            {
                typeof(int),
                (string v) => int.Parse(v)
            },
            {
                typeof(long),
                (string v) => long.Parse(v)
            },
            {
                typeof(byte),
                (string v) => byte.Parse(v)
            },
            {
                typeof(ushort),
                (string v) => ushort.Parse(v)
            },
            {
                typeof(uint),
                (string v) => uint.Parse(v)
            },
            {
                typeof(ulong),
                (string v) => ulong.Parse(v)
            },
            {
                typeof(float),
                (string v) => float.Parse(v)
            },
            {
                typeof(double),
                (string v) => double.Parse(v)
            },
            {
                typeof(string),
                (string v) => v
            },
            {
                typeof(bool),
                delegate(string v)
                {
                    float num;
                    if (float.TryParse(v, out num))
                    {
                        return num != 0f;
                    }
                    return bool.Parse(v);
                }
            }
        };
    }
}
