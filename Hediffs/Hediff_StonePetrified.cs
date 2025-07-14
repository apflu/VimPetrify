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
    public class Hediff_StonePetrified : HediffWithComps
    {
        private BuildingPetrifiedPawnStatue associatedStatue;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            Log.Message($"[VimPetrify] Hediff_StonePetrified added to Pawn: {pawn?.Name.ToStringShort ?? "N/A"}");

            if (pawn == null || pawn.Dead) return; // 不检查 pawn.Map，因为我们就是要把它从地图上移除

            // 检查是否已经是雕像，避免重复处理
            if (pawn.ParentHolder is BuildingPetrifiedPawnStatue existingStatue)
            {
                Log.Message($"[VimPetrify] Pawn {pawn.Name.ToStringShort} already associated with a statue. Skipping PostAdd actions.");
                associatedStatue = existingStatue;
                return;
            }

            // 获取 Pawn 当前的位置和地图，用于生成雕像
            Map currentMap = pawn.Map;
            IntVec3 currentPosition = pawn.Position;
            Rot4 currentRotation = pawn.Rotation; // 保存 Pawn 的朝向

            // 关键步骤 1：将原始 Pawn 从地图上移除，但保留在内存中
            // 这会阻止其被游戏系统（如救援警报、AI）识别
            if (pawn.Spawned) // 只有当 Pawn 实际在地图上时才移除
            {
                pawn.DeSpawn(DestroyMode.Vanish); // 使用 Vanish 模式，Pawn 会被从地图上移除，但不销毁
                Log.Message($"[VimPetrify] Pawn {pawn.Name.ToStringShort} Despawned for petrification.");
            }
            else
            {
                Log.Message($"[VimPetrify] Pawn {pawn.Name.ToStringShort} was not spawned on map, skipping despawn.");
            }

            // 关键步骤 2：实例化并放置石化雕像
            BuildingPetrifiedPawnStatue statue = (BuildingPetrifiedPawnStatue)ThingMaker.MakeThing(DefOfs.BuildingPetrifiedPawnStatue);
            if (statue == null)
            {
                Log.Error($"VimPetrify: Could not create BuildingPetrifiedPawnStatue for pawn {pawn.Name.ToStringShort}.");
                return;
            }

            // 设置雕像的派系
            statue.SetFaction(pawn.Faction);

            // 获取 PawnsOnDisplay 纹理并保存
            Texture2D[] pawnTextures = PawnsOnDisplayTextureManager.GetTexturesFromPawn(pawn, true, false);
            string baseFileName = Statue_Util.SanitizeFilename(pawn.Name.ToStringShort);
            string saveFolder = Statue_Util.GetMetadataSaveLocation();
            PawnsOnDisplayTextureManager.SaveTextures(saveFolder, baseFileName, pawnTextures);

            // 关键步骤 3：将原始 Pawn 引用传递给雕像组件
            if (statue.PetrifiedComp != null)
            {
                statue.PetrifiedComp.originalPawn = pawn; // 将被 DeSpawn 的 Pawn 赋值给雕像组件
                Log.Message($"[VimPetrify] Assigned originalPawn {pawn.Name.ToStringShort} to statue comp.");
            }
            else
            {
                Log.Error($"VimPetrify: Statue {statue.LabelCap} is missing CompPetrifiedPawnStatue!");
            }

            // 生成雕像到之前 Pawn 的位置和朝向
            if (currentMap != null) // 确保有地图才能生成
            {
                GenSpawn.Spawn(statue, currentPosition, currentMap, currentRotation);
                associatedStatue = statue; // 存储对创建的雕像的引用
                Log.Message($"[VimPetrify] Spawned statue {statue.LabelCap} at {currentPosition} on map {currentMap.info.parent.Label}.");
            }
            else
            {
                Log.Error($"VimPetrify: Cannot spawn statue for {pawn.Name.ToStringShort} because currentMap is null. Pawn was likely not on a map.");
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();

            if (pawn == null) return;

            Log.Message($"[VimPetrify] Hediff_StonePetrified removed from Pawn: {pawn.Name.ToStringShort}.");

            // 关键步骤 1：如果雕像存在且 Pawn 仍在雕像中，则将 Pawn 重新生成到地图上
            if (associatedStatue != null && associatedStatue.PetrifiedComp?.originalPawn == pawn)
            {
                Map statueMap = associatedStatue.Map;
                IntVec3 statuePosition = associatedStatue.Position;
                Rot4 statueRotation = associatedStatue.Rotation;

                // 重新生成 Pawn 到地图上
                if (statueMap != null && !pawn.Spawned) // 只有 Pawn 没在地图上时才重新生成
                {
                    GenSpawn.Spawn(pawn, statuePosition, statueMap, statueRotation);
                    Log.Message($"[VimPetrify] Pawn {pawn.Name.ToStringShort} respawned from statue at {statuePosition} on map {statueMap.info.parent.Label}.");

                    // 强制 Pawn 渲染刷新，使其可见
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
                else
                {
                    Log.Warning($"[VimPetrify] Attempted to respawn Pawn {pawn.Name.ToStringShort}, but map is null or Pawn is already spawned.");
                }

                // 关键步骤 2：移除关联的石化雕像
                if (associatedStatue.Spawned)
                {
                    associatedStatue.Destroy();
                    Log.Message($"[VimPetrify] Statue {associatedStatue.LabelCap} destroyed upon pawn de-petrification.");
                }
                associatedStatue = null; // 清除引用
            }
            else // 如果 associatedStatue 引用丢失，或者 Pawn 不在雕像中
            {
                Log.Warning($"[VimPetrify] associatedStatue is null or originalPawn mismatch for {pawn.Name.ToStringShort}. Attempting fallback removal.");
                // 尝试在 Pawn 位置查找并移除（如果 Pawn 仍在雕像中，但引用丢失）
                // 仅在 Pawn 没有重新生成时才尝试此逻辑，以防万一
                if (!pawn.Spawned && pawn.Position.IsValid && pawn.Map != null) // 检查 Pawn 的位置和地图是否仍然有效
                {
                    foreach (Thing thing in pawn.Map.thingGrid.ThingsAt(pawn.Position))
                    {
                        if (thing is BuildingPetrifiedPawnStatue s && s.PetrifiedComp?.originalPawn == pawn)
                        {
                            s.Destroy();
                            Log.Message($"[VimPetrify] Fallback: Found and destroyed statue {s.LabelCap} at pawn's position.");
                            break;
                        }
                    }
                }

                // 如果Pawn因为某种原因没有被Spawn，确保它被重新激活可见
                if (pawn.Spawned)
                {
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
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