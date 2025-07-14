using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Apflu.VimPetrify
{
    [DefOf]
    public static class DefOfs
    {
        public static HediffDef StonePetrifiedHediff;
        public static ThingDef BuildingPetrifiedPawnStatue;

        static DefOfs()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefOfs));
            DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
        }
    }
}