using Verse;
using System.Collections.Generic;

namespace Apflu.VimPetrify
{
    // 这个类用于追踪所有当前处于石化状态的 Pawn
    public static class PetrifiedPawnsTracker
    {
        // 使用 HashSet 提供 O(1) 的平均查找时间
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