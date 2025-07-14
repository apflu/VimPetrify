using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Apflu.VimPetrify.Exterior
{
    // 这个是你的CompProperties，需要在ThingDef中引用
    public class CompPropertiesPetrifiedPawnStatue : CompProperties
    {
        public CompPropertiesPetrifiedPawnStatue()
        {
            compClass = typeof(CompPetrifiedPawnStatue);
        }
    }

    // 这个是实际的Comp类
    public class CompPetrifiedPawnStatue : ThingComp
    {
        public Pawn originalPawn; // Hediff_StonePetrified 需要访问这个字段

        // 这是 RimWorld 用于保存/加载数据的标准方法
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref originalPawn, "originalPawn");
        }
    }
}