using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

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

            if (PetrifiedComp != null && PetrifiedComp.originalPawn != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "CommandViewPawnInfo".Translate(),
                    defaultDesc = "CommandViewPawnInfoDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/Language"),
                    action = delegate
                    {
                        Find.WindowStack.Add(new Dialog_InfoCard(PetrifiedComp.originalPawn));
                    }
                };
            }
        }
    }
}