using System;
using System.Reflection;
using HarmonyLib;

namespace XInvTweaksFork;

internal class ManualPatch
{
    internal static void PatchMethod(Harmony harmony, Type type, Type patch, string method)
    {
        if (harmony == null || type == null || patch == null || method == null) return;
        MethodInfo original;
        var baseType = type;
        do
        {
            original = baseType.GetMethod(method,
                           BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
                           BindingFlags.Public | BindingFlags.NonPublic) ??
                       baseType.GetMethod("get_" + method,
                           BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
                           BindingFlags.Public | BindingFlags.NonPublic);
            baseType = baseType.BaseType;
        } while (baseType != null && original == null);

        if (original == null) return;

        var prefix = patch.GetMethod(method + "Prefix",
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var postfix = patch.GetMethod(method + "Postfix",
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        var harmonyPrefix = prefix != null ? new HarmonyMethod(prefix) : null;
        var harmonyPostfix = postfix != null ? new HarmonyMethod(postfix) : null;

        harmony.Patch(original, harmonyPrefix, harmonyPostfix);
    }

    internal static void PatchConstructor(Harmony harmony, Type type, Type patch)
    {
        if (harmony == null || type == null) return;
        ConstructorInfo original;
        var baseType = type;
        do
        {
            original = baseType.GetConstructor(new Type[0]);
            var constructors = baseType.GetConstructors();

            if (original == null && constructors.Length > 0) original = constructors[0];

            baseType = baseType.BaseType;
        } while (baseType != null && original == null);

        if (original == null) return;

        var prefix = patch.GetMethod("ConstructorPrefix",
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var postfix = patch.GetMethod("ConstructorPostfix",
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        var harmonyPrefix = prefix != null ? new HarmonyMethod(prefix) : null;
        var harmonyPostfix = postfix != null ? new HarmonyMethod(postfix) : null;

        harmony.Patch(original, harmonyPrefix, harmonyPostfix);
    }
}