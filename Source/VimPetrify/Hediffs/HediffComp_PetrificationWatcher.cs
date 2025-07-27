using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Apflu.VimPetrify.Hediffs
{
    public class HediffComp_PetrificationWatcher: HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            
        }

        private bool CheckAllCriticalPartsPetrified(Pawn pawn)
        {
            BodyPartRecord torso = pawn.RaceProps.body.corePart;
            BodyPartRecord head = pawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.ConsciousnessSource).FirstOrDefault();
            return false; // TODO
        }
    }
}
