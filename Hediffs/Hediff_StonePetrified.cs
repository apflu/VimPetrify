using Apflu.VimPetrify.Exterior;
using PawnsOnDisplay;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Apflu.VimPetrify.Hediffs
{
    public class Hediff_StonePetrified: HediffWithComps
    {
        private BuildingPetrifiedPawnStatue associatedStatue;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            Log.Message($"[VimPetrify] Hediff_StonePetrified added to Pawn: {pawn?.Name.ToStringShort ?? "N/A"}");

            if (pawn == null || pawn.Map == null || pawn.Dead) return;

            // 将 Pawn 添加到追踪器中
            PetrifiedPawnsTracker.AddPetrifiedPawn(pawn);

            // 1. 强制 Pawn 渲染刷新，使其在下一个绘制周期中被隐藏
            pawn.Drawer.renderer.SetAllGraphicsDirty();

            // 2. 检查是否已经存在雕像，避免重复创建（例如，如果 Hediff 被多次添加）
            // 简单检查当前位置是否有我们的雕像
            foreach (Thing thing in pawn.Map.thingGrid.ThingsAt(pawn.Position))
            {
                if (thing is BuildingPetrifiedPawnStatue existingStatue && existingStatue.PetrifiedComp.originalPawn == pawn)
                {
                    associatedStatue = existingStatue;
                    return; // 雕像已存在，无需创建
                }
            }

            // 3. 实例化并放置石化雕像

            // 实例化并放置石化雕像
            BuildingPetrifiedPawnStatue statue = (BuildingPetrifiedPawnStatue)ThingMaker.MakeThing(DefOfs.BuildingPetrifiedPawnStatue);
            if (statue == null)
            {
                Log.Error($"VimPetrify: Could not create BuildingPetrifiedPawnStatue for pawn {pawn.Name.ToStringShort}.");
                return;
            }

            if (statue != null)
            {
                statue.SetFaction(pawn.Faction); // 雕像与 Pawn 拥有相同派系

                // 手动调用 PawnsOnDisplay Mod 的纹理生成和保存方法
                Texture2D[] pawnTextures = PawnsOnDisplayTextureManager.GetTexturesFromPawn(pawn, true, false); // 获取带头盔和不带头盔的纹理（如果需要，可以调整第二个参数）

                // PawnsOnDisplayTextureManager.SaveTextures 会在内部调用 TransformGrayscale，所以纹理会被保存为灰度
                string baseFileName = Statue_Util.SanitizeFilename(pawn.Name.ToStringShort);
                string saveFolder = Statue_Util.GetMetadataSaveLocation();

                // 这将保存灰度化的纹理到指定位置
                PawnsOnDisplayTextureManager.SaveTextures(saveFolder, baseFileName, pawnTextures);

                // 设置雕像组件中的原始 Pawn 引用
                if (statue.PetrifiedComp != null)
                {
                    statue.PetrifiedComp.originalPawn = pawn;
                }

                // 获取 Pawn 当前的朝向，让雕像朝向一致
                Rot4 pawnRotation = pawn.Rotation;
                GenSpawn.Spawn(statue, pawn.Position, pawn.Map, pawnRotation);
                associatedStatue = statue; // 存储对创建的雕像的引用
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();

            if (pawn == null) return;

            // 将 Pawn 从追踪器中移除
            PetrifiedPawnsTracker.RemovePetrifiedPawn(pawn);

            // 1. 强制 Pawn 渲染刷新，使其在下一个绘制周期中再次可见
            pawn.Drawer.renderer.SetAllGraphicsDirty();

            // 2. 移除关联的石化雕像
            if (associatedStatue != null && associatedStatue.Spawned)
            {
                associatedStatue.Destroy();
                associatedStatue = null;
            }
            else // 如果 associatedStatue 引用丢失，尝试在 Pawn 位置查找并移除
            {
                foreach (Thing thing in pawn.Map.thingGrid.ThingsAt(pawn.Position))
                {
                    if (thing is BuildingPetrifiedPawnStatue s && s.PetrifiedComp.originalPawn == pawn)
                    {
                        s.Destroy();
                        break;
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref associatedStatue, "associatedStatue");
        }
    }
}
