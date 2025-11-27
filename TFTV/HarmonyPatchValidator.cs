using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace TFTV
{
    internal static class HarmonyPatchValidator
    {
        public static void ValidatePatchTargets()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                foreach (var type in asm.GetTypes())
                {
                    // Nested patch containers
                    foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        var nestedClassLevel = FindNearestClassLevelPatch(nested);

                        foreach (var method in nested.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                        {
                            var attrs = method.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Cast<HarmonyPatch>();
                            foreach (var a in attrs)
                            {
                                TryResolveAndLogIfNull(a, nested, nestedClassLevel);
                            }
                        }

                        // Pure class-level patch (no method attrs)
                        var nestedClassAttr = nested.GetCustomAttributes(typeof(HarmonyPatch), inherit: true).Cast<HarmonyPatch>().FirstOrDefault();
                        if (nestedClassAttr != null)
                        {
                            TryResolveAndLogIfNull(nestedClassAttr, nested, nestedClassAttr);
                        }
                    }

                    // Methods directly on type
                    var classLevel = FindNearestClassLevelPatch(type);
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        var attrs = method.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Cast<HarmonyPatch>();
                        foreach (var a in attrs)
                        {
                            TryResolveAndLogIfNull(a, type, classLevel);
                        }
                    }

                    // Class-level-only
                    var classAttr = type.GetCustomAttributes(typeof(HarmonyPatch), inherit: true).Cast<HarmonyPatch>().FirstOrDefault();
                    if (classAttr != null)
                    {
                        TryResolveAndLogIfNull(classAttr, type, classAttr);
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }
        }

        private static HarmonyPatch FindNearestClassLevelPatch(Type patchType)
        {
            Type t = patchType;
            while (t != null)
            {
                var cls = t.GetCustomAttributes(typeof(HarmonyPatch), inherit: true).Cast<HarmonyPatch>().FirstOrDefault();
                if (cls != null && cls.info?.declaringType != null)
                {
                    return cls;
                }
                t = t.DeclaringType;
            }
            return null;
        }

        private static void TryResolveAndLogIfNull(HarmonyPatch methodLevelPatch, Type declaringPatchType, HarmonyPatch classLevelPatch)
        {
            try
            {
                var info = methodLevelPatch?.info;
                if (info == null) return;

                var targetType = info.declaringType ?? classLevelPatch?.info?.declaringType;
                var methodName = info.methodName;
                var argumentTypes = info.argumentTypes;
                var methodType = info.methodType;

                if (targetType == null)
                {
                    TFTVLogger.Always($"[HarmonyValidator] UNRESOLVED target type for patch in {declaringPatchType.FullName}");
                    return;
                }

                MethodBase resolved = null;

                // Constructors
                if (methodType == MethodType.Constructor || (string.IsNullOrEmpty(methodName) && methodType == MethodType.Constructor))
                {
                    resolved = AccessTools.Constructor(targetType, argumentTypes ?? Type.EmptyTypes);
                    LogIfNull(resolved, targetType, ".ctor", argumentTypes, declaringPatchType);
                    return;
                }

                // Property accessors
                if (methodType == MethodType.Getter && !string.IsNullOrEmpty(methodName))
                {
                    resolved = AccessTools.PropertyGetter(targetType, methodName);
                    LogIfNull(resolved, targetType, $"get_{methodName}", argumentTypes, declaringPatchType);
                    return;
                }
                if (methodType == MethodType.Setter && !string.IsNullOrEmpty(methodName))
                {
                    resolved = AccessTools.PropertySetter(targetType, methodName);
                    LogIfNull(resolved, targetType, $"set_{methodName}", argumentTypes, declaringPatchType);
                    return;
                }

                // Event add/remove
                if (!string.IsNullOrEmpty(methodName) && (methodName.StartsWith("add_") || methodName.StartsWith("remove_")))
                {
                    resolved = targetType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                                         .FirstOrDefault(m => m.Name == methodName && SignatureMatches(m, argumentTypes));
                    LogIfNull(resolved, targetType, methodName, argumentTypes, declaringPatchType);
                    return;
                }

                // Normal methods: try explicit signature first
                if (!string.IsNullOrEmpty(methodName))
                {
                    resolved = AccessTools.Method(targetType, methodName, argumentTypes);
                    if (resolved == null)
                    {
                        // Search across all methods including non-public instance/static
                        var all = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        resolved = all.FirstOrDefault(m => m.Name == methodName && SignatureMatches(m, argumentTypes))
                               ?? all.FirstOrDefault(m => m.Name == methodName); // last resort: name only
                    }

                    LogIfNull(resolved, targetType, methodName, argumentTypes, declaringPatchType);
                }
                // else: no methodName, nothing to resolve (some class-level patches only define type)
            }
            catch
            {
                TFTVLogger.Always($"[HarmonyValidator] UNRESOLVED (exception) for patch in {declaringPatchType.FullName}");
            }
        }

        private static bool SignatureMatches(MethodBase method, Type[] signature)
        {
            if (signature == null || signature.Length == 0) return true;
            var parms = method.GetParameters();
            if (parms.Length != signature.Length) return false;
            for (int i = 0; i < parms.Length; i++)
            {
                var pt = parms[i].ParameterType;
                var st = signature[i];
                if (st == null) continue;
                if (pt != st && !pt.IsAssignableFrom(st)) return false;
            }
            return true;
        }

        private static void LogIfNull(MethodBase resolved, Type targetType, string name, Type[] args, Type declaringPatchType)
        {
            if (resolved != null) return;
            var sig = args != null ? $"({string.Join(", ", args.Select(t => t?.Name ?? "null"))})" : "";
            TFTVLogger.Always($"[HarmonyValidator] UNRESOLVED target: {targetType.FullName}.{name}{sig} for patch in {declaringPatchType.FullName}");
        }
    }
}