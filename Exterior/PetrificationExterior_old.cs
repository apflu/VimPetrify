using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Apflu.VimPetrify
{
    //[HarmonyPatch(typeof(PawnRenderNode), "GraphicsFor")]
    public static class PawnRenderNode_GraphicsFor_StoneColor_Patch
    {
        //[HarmonyPostfix]
        public static void Postfix(PawnRenderNode __instance, Pawn pawn, ref IEnumerable<Graphic> __result)
        {
            Log.Message($"[VimPetrify] GraphicsFor Postfix triggered for Pawn: {pawn?.LabelCap ?? "N/A"} - Node Type: {__instance.GetType().Name}");

            if (pawn == null || !pawn.health.hediffSet.HasHediff(DefOfs.StonePetrifiedHediff))
            {
                return;
            }

            Log.Message($"[VimPetrify] Pawn {pawn.LabelCap} IS petrified. Attempting to modify graphics...");

            var graphicsToModify = __result.ToList();

            foreach (Graphic originalGraphic in graphicsToModify)
            {
                if (originalGraphic == null)
                {
                    Log.Message($"[VimPetrify]   Encountered null graphic. Skipping.");
                    continue;
                }

                Material mat = originalGraphic.MatSingle;

                if (mat != null)
                {
                    Log.Message($"[VimPetrify]   Modifying material for graphic path: {originalGraphic.path}. Original color: {mat.color}");
                    SetMaterialToGray(mat);
                    Log.Message($"[VimPetrify]   Material color set to: {mat.color}");
                }
                // else
                // {
                //     Log.Message($"[VimPetrify]   Material is null for graphic path: {originalGraphic.path}.");
                // }
            }
            __result = graphicsToModify.AsEnumerable(); // 推荐保留此行，确保修改被返回
        }

        private static void SetMaterialToGray(Material mat)
        {
            if (mat == null) return;

            mat.SetColor("_Color", Color.gray);

            if (mat.HasProperty("_ColorTwo"))
            {
                mat.SetColor("_ColorTwo", Color.gray);
            }
            if (mat.HasProperty("_GlowColor"))
            {
                mat.SetColor("_GlowColor", Color.black);
            }
            if (mat.HasProperty("_SpecColor"))
            {
                mat.SetColor("_SpecColor", Color.gray);
            }
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}