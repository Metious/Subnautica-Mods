﻿using HarmonyLib;

namespace WarpersNoWarping
{
    [HarmonyPatch(typeof(WarperInspectPlayer), nameof(WarperInspectPlayer.Perform))]
    public class WarperInspectPlayer_Perform
    {
        [HarmonyPrefix]
        public static void Prefix(WarperInspectPlayer __instance)
        {
            __instance.warpOutDistance = 0;
        }
    }
}