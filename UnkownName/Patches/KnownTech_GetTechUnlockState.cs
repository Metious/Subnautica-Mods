﻿namespace UnknownName.Patches
{
    using HarmonyLib;
    using System;
    using UnKnownName;

    [HarmonyPatch(typeof(KnownTech), nameof(KnownTech.GetTechUnlockState), new Type[] { typeof(TechType), typeof(int), typeof(int) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out })]
    public static class KnownTech_GetTechUnlockState
    {
        [HarmonyPostfix]
        public static void Postfix(TechType techType, ref TechUnlockState __result)
        {
            if(Main.Config.Hardcore && (__result != TechUnlockState.Available || !CrafterLogic.IsCraftRecipeUnlocked(techType)))
            {
                __result = TechUnlockState.Hidden;
            }
        }
    }
}
