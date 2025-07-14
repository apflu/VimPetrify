using Apflu.VimPetrify.Exterior;
using PawnsOnDisplay; // Still used for texture management, keep it.
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
        // 移除 private BuildingPetrifiedPawnStatue associatedStatue;
        // 因为在打包场景下，这个引用可能变得不可靠。
        // 我们改为在解除时动态查找。

        // --- Public Overrides ---
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            Log.Message($"[VimPetrify] Hediff_StonePetrified added to Pawn: {pawn?.Name.ToStringShort ?? "N/A"}");

            if (pawn == null || pawn.Dead) return;

            // Check if already petrified - this still uses pawn.ParentHolder for immediate check
            if (CheckExistingStatueAssociation()) // This method might need slight adjustment, see below
            {
                return;
            }

            // Perform petrification
            PerformPetrification();
        }

        public override void PostRemoved()
        {
            base.PostRemoved();

            if (pawn == null) return;

            Log.Message($"[VimPetrify] Hediff_StonePetrified removed from Pawn: {pawn.Name.ToStringShort}.");

            // Perform de-petrification
            PerformDePetrification();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // associatedStatue 不再需要持久化，因为我们是动态查找的
            // 如果你想保留它作为一种优化或调试信息，可以保留 Scribe_References.Look(ref associatedStatue, "associatedStatue");
            // 但为了支持打包移动，它不再是解除石化时的主要查找方式。
        }

        // --- Private Helper Methods for PostAdd ---

        /// <summary>
        /// Checks if the pawn is already associated with a statue (either deployed or minified) and should not be re-petrified.
        /// </summary>
        /// <returns>True if already associated and actions should be skipped, false otherwise.</returns>
        private bool CheckExistingStatueAssociation()
        {
            // If the pawn is already stored inside a BuildingPetrifiedPawnStatue
            if (pawn.ParentHolder is BuildingPetrifiedPawnStatue existingBuildingStatue)
            {
                Log.Message($"[VimPetrify] Pawn {pawn.Name.ToStringShort} is already inside a deployed statue.");
                // We'll trust that the existing statue has the correct pawn reference.
                return true;
            }
            // If the pawn is already stored inside a MinifiedPetrifiedPawnStatue (i.e., packed)
            if (pawn.ParentHolder is MinifiedThing minifiedStatue && minifiedStatue.InnerThing is BuildingPetrifiedPawnStatue innerBuildingStatue)
            {
                // Check if the minified thing is actually OUR statue with this pawn
                if (innerBuildingStatue.PetrifiedComp?.originalPawn == pawn)
                {
                    Log.Message($"[VimPetrify] Pawn {pawn.Name.ToStringShort} is already inside a minified statue.");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Handles the main logic for petrifying the pawn and creating the statue.
        /// </summary>
        private void PerformPetrification()
        {
            Map currentMap = pawn.Map;
            IntVec3 currentPosition = pawn.Position;
            Rot4 currentRotation = pawn.Rotation;

            DespawnOriginalPawn(); // Pawn is despawned, but still in memory

            BuildingPetrifiedPawnStatue newStatue = CreateAndSetupStatue();
            if (newStatue == null) return;

            GenerateAndSavePawnTexture(pawn);

            // IMPORTANT: Assign original pawn to statue component FIRST
            if (newStatue.PetrifiedComp != null)
            {
                newStatue.PetrifiedComp.originalPawn = pawn;
                Log.Message($"[VimPetrify] Assigned originalPawn {pawn.Name.ToStringShort} to statue comp.");
            }
            else
            {
                Log.Error($"VimPetrify: Statue {newStatue.LabelCap} is missing CompPetrifiedPawnStatue!");
            }

            // Then spawn the statue
            SpawnStatue(newStatue, currentMap, currentPosition, currentRotation);
        }

        private void DespawnOriginalPawn()
        {
            if (pawn.Spawned)
            {
                pawn.DeSpawn(DestroyMode.Vanish);
                Log.Message($"[VimPetrify] Pawn {pawn.Name.ToStringShort} Despawned for petrification.");
            }
            else
            {
                Log.Message($"[VimPetrify] Pawn {pawn.Name.ToStringShort} was not spawned on map, skipping despawn.");
            }
        }

        private BuildingPetrifiedPawnStatue CreateAndSetupStatue()
        {
            BuildingPetrifiedPawnStatue statue = (BuildingPetrifiedPawnStatue)ThingMaker.MakeThing(DefOfs.BuildingPetrifiedPawnStatue);
            if (statue == null)
            {
                Log.Error($"VimPetrify: Could not create BuildingPetrifiedPawnStatue for pawn {pawn.Name.ToStringShort}.");
            }
            else
            {
                statue.SetFaction(pawn.Faction);
            }
            return statue;
        }

        private void GenerateAndSavePawnTexture(Pawn targetPawn)
        {
            try
            {
                Texture2D[] pawnTextures = PawnsOnDisplayTextureManager.GetTexturesFromPawn(targetPawn, true, false);
                string baseFileName = Statue_Util.SanitizeFilename(targetPawn.Name.ToStringShort);
                string saveFolder = Statue_Util.GetMetadataSaveLocation();
                PawnsOnDisplayTextureManager.SaveTextures(saveFolder, baseFileName, pawnTextures);
            }
            catch (Exception ex)
            {
                Log.Error($"[VimPetrify] Failed to generate/save pawn texture for {targetPawn.Name.ToStringShort}: {ex.Message}");
            }
        }

        private void SpawnStatue(BuildingPetrifiedPawnStatue statue, Map map, IntVec3 position, Rot4 rotation)
        {
            if (map != null)
            {
                GenSpawn.Spawn(statue, position, map, rotation);
                // associatedStatue = statue; // This field is no longer strictly needed for de-petrification if we find dynamically
                Log.Message($"[VimPetrify] Spawned statue {statue.LabelCap} at {position} on map {map.info.parent.Label}.");
            }
            else
            {
                Log.Error($"VimPetrify: Cannot spawn statue for {pawn.Name.ToStringShort} because map is null. Pawn was likely not on a map.");
            }
        }

        // --- Private Helper Methods for PostRemoved ---

        /// <summary>
        /// Handles the main logic for de-petrifying the pawn and removing the statue.
        /// </summary>
        private void PerformDePetrification()
        {
            // Step 1: Find the associated statue (either deployed or minified)
            Thing associatedThing = FindAssociatedStatue();

            if (associatedThing != null)
            {
                RespawnPawnAndDestroyStatue(associatedThing);
            }
            else
            {
                // If no statue found, try to respawn pawn at its last known position (if valid)
                HandleNoStatueFound();
            }
        }

        /// <summary>
        /// Finds the BuildingPetrifiedPawnStatue or MinifiedPetrifiedPawnStatue associated with this pawn.
        /// </summary>
        /// <returns>The found statue (Building or MinifiedThing), or null if not found.</returns>
        private Thing FindAssociatedStatue()
        {
            // First, check if the pawn is currently held by a statue (should be despawned and held by the statue)
            // This is the most reliable way if the pawn is still tied to a specific statue instance in memory.
            if (pawn.ParentHolder is BuildingPetrifiedPawnStatue directBuildingStatue && directBuildingStatue.PetrifiedComp?.originalPawn == pawn)
            {
                Log.Message($"[VimPetrify] Found associated building statue directly for {pawn.Name.ToStringShort}.");
                return directBuildingStatue;
            }
            if (pawn.ParentHolder is MinifiedThing directMinifiedStatue && directMinifiedStatue.InnerThing is BuildingPetrifiedPawnStatue innerBuilding && innerBuilding.PetrifiedComp?.originalPawn == pawn)
            {
                Log.Message($"[VimPetrify] Found associated minified statue directly for {pawn.Name.ToStringShort}.");
                return directMinifiedStatue;
            }


            // Fallback: Iterate through all spawned BuildingPetrifiedPawnStatue and MinifiedPetrifiedPawnStatue
            // across all maps and in storage (if any). This is more resource-intensive but robust.

            // Search for deployed buildings on all maps
            foreach (Map map in Find.Maps)
            {
                foreach (Thing thing in map.listerThings.ThingsOfDef(DefOfs.BuildingPetrifiedPawnStatue))
                {
                    if (thing is BuildingPetrifiedPawnStatue deployedStatue && deployedStatue.PetrifiedComp?.originalPawn == pawn)
                    {
                        Log.Message($"[VimPetrify] Found associated deployed statue {deployedStatue.LabelCap} on map {map.info.parent.Label} for {pawn.Name.ToStringShort}.");
                        return deployedStatue;
                    }
                }
            }

            // Search for minified statues across all maps (in storage, inventory, etc.)
            foreach (Map map in Find.Maps)
            {
                foreach (Thing thing in map.listerThings.ThingsOfDef(DefOfs.MinifiedPetrifiedPawnStatue))
                {
                    if (thing is MinifiedThing minifiedThing)
                    {
                        if (minifiedThing.InnerThing is BuildingPetrifiedPawnStatue innerStatue && innerStatue.PetrifiedComp?.originalPawn == pawn)
                        {
                            Log.Message($"[VimPetrify] Found associated minified statue {minifiedThing.LabelCap} on map {map.info.parent.Label} for {pawn.Name.ToStringShort}.");
                            return minifiedThing;
                        }
                    }
                }
            }

            // Finally, search through all existing Things in the game (more expensive, last resort)
            // Not usually necessary if Pawn.ParentHolder is properly managed or map searches cover it.
            // foreach (Thing thing in Find.World.GetComponent<WorldPawns>().AllPawnsAliveOrDead().OfType<MinifiedThing>()) { ... } // Example if pawns are in minified things in world
            // Consider if the statue could be in pawn's inventory (unlikely for buildings) or somewhere else.

            Log.Warning($"[VimPetrify] Could not find any associated statue for pawn {pawn.Name.ToStringShort}.");
            return null;
        }


        /// <summary>
        /// Respawns the pawn from the given associated statue (either Building or MinifiedThing) and destroys the statue.
        /// </summary>
        /// <param name="foundStatue">The statue (Building or MinifiedThing) that was found.</param>
        private void RespawnPawnAndDestroyStatue(Thing foundStatue)
        {
            Map respawnMap = null;
            IntVec3 respawnPosition = IntVec3.Invalid;
            Rot4 respawnRotation = Rot4.North;

            // 用于存储被销毁的部署雕像实例
            BuildingPetrifiedPawnStatue deployedStatueToDestroy = null;

            if (foundStatue is BuildingPetrifiedPawnStatue deployedStatue) // 这里声明的 deployedStatue 只在此if块内有效
            {
                // 将内部声明的 deployedStatue 赋值给外部变量，以便后续销毁
                deployedStatueToDestroy = deployedStatue;
                respawnMap = deployedStatue.Map;
                respawnPosition = deployedStatue.Position;
                respawnRotation = deployedStatue.Rotation;
                Log.Message($"[VimPetrify] De-petrifying from DEPLOYED statue at {respawnPosition} on map {respawnMap?.info.parent.Label}.");
            }
            else if (foundStatue is MinifiedThing minifiedStatue)
            {
                // 如果是打包物品，我们假设它内部的 BuildingPetrifiedPawnStatue 实例是有效的。
                // 此时，pawn应该是在minifiedStatue.InnerThing中。
                BuildingPetrifiedPawnStatue innerStatue = minifiedStatue.InnerThing as BuildingPetrifiedPawnStatue;

                if (innerStatue == null)
                {
                    Log.Error($"[VimPetrify] Minified statue {minifiedStatue.LabelCap} does not contain a BuildingPetrifiedPawnStatue inner thing.");
                    return; // 内部物品类型不符，中止操作
                }

                if (minifiedStatue.Map != null)
                {
                    respawnMap = minifiedStatue.Map;
                    respawnPosition = minifiedStatue.Position; // Respawn at minified item's position
                    respawnRotation = Rot4.North; // Default rotation for respawned pawn
                    Log.Message($"[VimPetrify] De-petrifying from MINIFIED statue at {respawnPosition} on map {respawnMap.info.parent.Label}.");

                    // 销毁 MinifiedThing 本身，它会包含内部的 BuildingPetrifiedPawnStatue
                    minifiedStatue.Destroy();
                    Log.Message($"[VimPetrify] Minified statue {minifiedStatue.LabelCap} destroyed upon pawn de-petrification.");
                }
                else // If minified statue is in inventory, or not on a map (e.g. caravan)
                {
                    Log.Error($"[VimPetrify] Minified statue {minifiedStatue.LabelCap} is not on a map. Cannot respawn pawn {pawn.Name.ToStringShort}. Pawn is lost!");
                    // 在此情况下，由于无法确定安全的生成位置，我们选择直接返回
                    // 你可以在此处添加更复杂的逻辑，例如在最近的殖民地地图生成，或弹出警告让玩家手动处理。
                    return;
                }
            }
            else
            {
                Log.Error($"[VimPetrify] Unknown statue type ({foundStatue.GetType().Name}) found for de-petrification of {pawn.Name.ToStringShort}.");
                return;
            }

            // Respawn Pawn if not already spawned
            if (respawnMap != null && !pawn.Spawned)
            {
                IntVec3 spawnCell = CellFinder.StandableCellNear(respawnPosition, respawnMap, 5);
                if (spawnCell.IsValid)
                {
                    GenSpawn.Spawn(pawn, spawnCell, respawnMap, respawnRotation);
                    Log.Message($"[VimPetrify] Pawn {pawn.Name.ToStringShort} respawned from statue at {spawnCell} (near {respawnPosition}) on map {respawnMap.info.parent.Label}.");
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
                else
                {
                    Log.Error($"[VimPetrify] Could not find a standable cell near {respawnPosition} on map {respawnMap.info.parent.Label} to respawn pawn {pawn.Name.ToStringShort}. Pawn is lost!");
                }
            }
            else
            {
                Log.Warning($"[VimPetrify] Attempted to respawn Pawn {pawn.Name.ToStringShort}, but map is null or Pawn is already spawned.");
            }

            // 在这里统一销毁部署状态的雕像
            if (deployedStatueToDestroy != null && deployedStatueToDestroy.Spawned)
            {
                deployedStatueToDestroy.Destroy();
                Log.Message($"[VimPetrify] Deployed statue {deployedStatueToDestroy.LabelCap} destroyed upon pawn de-petrification.");
            }
        }


        /// <summary>
        /// Handles the scenario where no associated statue is found during de-petrification.
        /// </summary>
        private void HandleNoStatueFound()
        {
            Log.Warning($"[VimPetrify] No associated statue found for {pawn.Name.ToStringShort} during de-petrification. Attempting to respawn at pawn's last known position.");

            // Try to respawn the pawn at its last known valid position (might be where it was despawned)
            if (!pawn.Spawned && pawn.Position.IsValid && pawn.Map != null) // Check if pawn is still despawned and has a valid map/position
            {
                IntVec3 respawnPos = pawn.Position;
                Map respawnMap = pawn.Map;

                // Try to find a standable cell. If original position is invalid, this will help.
                IntVec3 spawnCell = CellFinder.StandableCellNear(respawnPos, respawnMap, 5);
                if (spawnCell.IsValid)
                {
                    GenSpawn.Spawn(pawn, spawnCell, respawnMap, Rot4.North); // Default rotation
                    Log.Message($"[VimPetrify] Fallback: Pawn {pawn.Name.ToStringShort} respawned at {spawnCell} (last valid position) after statue not found.");
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
                else
                {
                    Log.Error($"[VimPetrify] Fallback: Could not find standable cell for {pawn.Name.ToStringShort} at {respawnPos} on map {respawnMap.info.parent.Label}. Pawn is LOST!");
                }
            }
            else if (pawn.Spawned) // If pawn somehow became spawned already (e.g. from a bug or another mod)
            {
                pawn.Drawer.renderer.SetAllGraphicsDirty();
                Log.Message($"[VimPetrify] Pawn {pawn.Name.ToStringShort} was already spawned. Forcing graphics refresh.");
            }
            else
            {
                Log.Error($"[VimPetrify] Pawn {pawn.Name.ToStringShort} is neither spawned nor associated with a statue, and has no valid last position to respawn. Pawn is LOST!");
            }
        }
    }
}