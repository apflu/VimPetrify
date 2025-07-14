using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace Apflu.VimPetrify.Exterior
{
    public class BuildingPetrifiedPawnStatue : Building
    {
        public CompPetrifiedPawnStatue PetrifiedComp
        {
            get
            {
                return GetComp<CompPetrifiedPawnStatue>();
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            // 添加一个用于测试的移除按钮
            yield return new Command_Action
            {
                defaultLabel = "DEBUG: Remove Statue",
                defaultDesc = "Removes this placeholder statue. For testing purposes only.",
                icon = ContentFinder<Texture2D>.Get("UI/Designators/Deconstruct", true), // 随便找个图标
                action = delegate
                {
                    Destroy();
                }
            };
        }
    }
}