using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Apflu.VimPetrify.Patches
{
    [HarmonyPatch(typeof(Alert_ColonistNeedsRescuing), "NeedsRescue")]
    public static class RescueAlertPatches
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn p, ref bool __result)
        {
            // 检查 Pawn 是否有你的石化 Hediff
            if (p != null && p.health != null && p.health.hediffSet.HasHediff(DefOfs.StonePetrifiedHediff))
            {
                __result = false; // 如果被石化，则不认为需要救援
                return false;     // 跳过原始 NeedsRescue 方法的执行
            }

            return true; // 如果 Pawn 没有石化 Hediff，则执行原始 NeedsRescue 方法
        }
    }
}