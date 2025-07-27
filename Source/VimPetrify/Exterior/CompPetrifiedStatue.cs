using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Apflu.VimPetrify.Exterior
{
    public class CompPropertiesPetrifiedPawnStatue : CompProperties
    {
        public CompPropertiesPetrifiedPawnStatue()
        {
            compClass = typeof(CompPetrifiedPawnStatue);
        }
    }

    public class CompPetrifiedPawnStatue : ThingComp
    {
        public Pawn originalPawn;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref originalPawn, "originalPawn");
        }
    }
}