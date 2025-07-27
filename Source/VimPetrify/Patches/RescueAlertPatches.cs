using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Apflu.VimPetrify.Patches
{
    // stop alerts about rescuing petrified pawns
    [HarmonyPatch(typeof(Alert_ColonistNeedsRescuing), "NeedsRescue")]
    public static class RescueAlertPatches
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn p, ref bool __result)
        {
            if (p != null && p.health != null && p.health.hediffSet.HasHediff(DefOfs.PetrifiedFull))
            {
                __result = false;
                return false;   
            }

            return true; 
        }
    }
}