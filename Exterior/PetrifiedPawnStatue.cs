using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI; // 可能需要引用 JobGiverDefOf，如果你的按钮与 Job 相关

namespace Apflu.VimPetrify.Exterior
{
    public class BuildingPetrifiedPawnStatue : Building
    {
        public CompPetrifiedPawnStatue PetrifiedComp => GetComp<CompPetrifiedPawnStatue>();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            Log.Message($"[VimPetrify] BuildingPetrifiedPawnStatue.SpawnSetup called. Respawning: {respawningAfterLoad}");

            if (Graphic is Graphic_PetrifiedPawn petrifiedGraphic)
            {
                if (PetrifiedComp?.originalPawn != null)
                {
                    // 在这里我们不再使用原始 Pawn 渲染自身，而是由 Graphic_PetrifiedPawn 根据原始 Pawn 的纹理数据渲染
                    // 确保 PetrifiedComp.originalPawn 在这里是有效的引用
                    petrifiedGraphic.SetOriginalPawn(PetrifiedComp.originalPawn, this.DrawColor);
                    Log.Message($"[VimPetrify] BuildingPetrifiedPawnStatue.SpawnSetup: Calling SetOriginalPawn for {PetrifiedComp.originalPawn.Name.ToStringShort}.");
                }
                else
                {
                    Log.Warning($"[VimPetrify] BuildingPetrifiedPawnStatue.SpawnSetup: PetrifiedComp or originalPawn is null, cannot initialize custom graphic for statue {this.LabelCap}.");
                }
            }
            else
            {
                Log.Warning($"[VimPetrify] BuildingPetrifiedPawnStatue.SpawnSetup: Graphic is not Graphic_PetrifiedPawn type. Actual type: {Graphic?.GetType().Name ?? "NULL"}.");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            // 添加一个按钮来显示原始 Pawn 的信息面板
            if (PetrifiedComp != null && PetrifiedComp.originalPawn != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "CommandViewPawnInfo".Translate(), // 使用翻译键更通用
                    defaultDesc = "CommandViewPawnInfoDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Language"),
                    action = delegate
                    {
                        // 核心：直接调用 RimWorld 的信息卡片系统
                        // 这将显示 Pawn 的标准信息面板
                        Find.WindowStack.Add(new Dialog_InfoCard(PetrifiedComp.originalPawn));
                    }
                };
            }
        }
    }
}