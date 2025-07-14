using RimWorld;
using Verse;
using UnityEngine;

namespace Apflu.VimPetrify.Exterior
{
    public class BuildingPetrifiedPawnStatue : Building
    {
        public CompPetrifiedPawnStatue PetrifiedComp => GetComp<CompPetrifiedPawnStatue>();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            Log.Message($"[VimPetrify] BuildingPetrifiedPawnStatue.SpawnSetup called. Respawning: {respawningAfterLoad}");

            if (this.Graphic != null)
            {
                Log.Message($"[VimPetrify] Statue graphic type in SpawnSetup: {this.Graphic.GetType().Name}.");
            }
            else
            {
                Log.Error($"[VimPetrify] Statue graphic is NULL in SpawnSetup!");
            }

            if (Graphic is Graphic_PetrifiedPawn petrifiedGraphic)
            {
                if (PetrifiedComp?.originalPawn != null)
                {
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
                // 如果这里显示的是Graphic_Single或者其他类型，那么问题就在于XML的配置或类的加载
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }
    }
}