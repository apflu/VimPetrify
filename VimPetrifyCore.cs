using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Apflu.VimPetrify
{
    [StaticConstructorOnStartup]
    public class VimPetrifyCore
    {
        static VimPetrifyCore()
        {
            var harmony = new HarmonyLib.Harmony("Apflu.VimPetrify");
            harmony.PatchAll();
        }
    }
}
