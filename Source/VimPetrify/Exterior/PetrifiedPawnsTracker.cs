using Verse;
using System.Collections.Generic;

namespace Apflu.VimPetrify
{
    
    public static class PetrifiedPawnsTracker
    {
        
        public static HashSet<Pawn> PetrifiedPawns = new HashSet<Pawn>();

        public static void AddPetrifiedPawn(Pawn pawn)
        {
            if (pawn != null)
            {
                PetrifiedPawns.Add(pawn);
            }
        }

        public static void RemovePetrifiedPawn(Pawn pawn)
        {
            if (pawn != null)
            {
                PetrifiedPawns.Remove(pawn);
            }
        }

        public static bool IsPawnPetrified(Pawn pawn)
        {
            return pawn != null && PetrifiedPawns.Contains(pawn);
        }
    }
}