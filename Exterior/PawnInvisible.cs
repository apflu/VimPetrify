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
        public static bool Prefix(PawnRenderer __instance, Pawn ___pawn) // Harmony 注入私有字段的语法
        {
            // ___pawn 现在直接引用了 PawnRenderer 实例所对应的那个 Pawn
            Pawn pawnToRender = ___pawn;

            // 检查 Pawn 是否处于石化状态
            if (pawnToRender != null && pawnToRender.health != null && pawnToRender.health.hediffSet.HasHediff(DefOfs.StonePetrifiedHediff))
            {
                // 如果是石化状态，则阻止原始 RenderPawnAt 方法的执行
                return false;
            }

            return true;
        }
    }
}
