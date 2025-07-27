using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Apflu.VimPetrify.Exterior
{
    [HarmonyPatch(typeof(PawnRenderer), "RenderPawnAt")]
    public static class PawnRenderer_RenderPawnAt_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(PawnRenderer __instance, Pawn ___pawn)
        {
            //Log.Message($"[VimPetrify] RenderPawnAt Prefix triggered for Pawn: {___pawn?.LabelCap ?? "N/A"}");

           
            if (PetrifiedPawnsTracker.IsPawnPetrified(___pawn))
            {
                return false;
            }

            return true;
        }
    }
}
