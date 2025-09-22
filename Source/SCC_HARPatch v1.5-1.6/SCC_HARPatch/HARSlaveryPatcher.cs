using AlienRace;
using SimpleSlaveryCollars;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SCC_HARPatch
{
    [StaticConstructorOnStartup]
    public static class HARSlaveryPatcher
    {
        static HARSlaveryPatcher()
        {
            LongEventHandler.ExecuteWhenFinished(RunPatching);
        }

        private static void RunPatching()
        {
            var slaveCollarDefNames = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.apparel != null && d.apparel.defaultOutfitTags?.Contains("SlaveCollar") == true)
                .Select(d => d.defName)
                .ToHashSet();

            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.defName == "Human")
                    continue;

                var alienDef = def as ThingDef_AlienRace;
                if (alienDef == null)
                    continue;

                var raceRestrict = alienDef.alienRace?.raceRestriction;
                if (raceRestrict == null || raceRestrict.whiteApparelList == null)
                    continue;

                if (alienDef.comps == null)
                    alienDef.comps = new List<CompProperties>();

                bool alreadyHasComp = alienDef.comps.Any(c => c is CompProperties_Slave);
                if (!alreadyHasComp)
                {
                    alienDef.comps.Add(new CompProperties_Slave());
                }

                if (!raceRestrict.onlyUseRaceRestrictedApparel)
                {
                    Log.Message($"[SSC] {def.defName} - CompSlave {(alreadyHasComp ? "already present via XML" : "added")}, whitelist unused (restriction off)");
                    continue;
                }

                int added = 0;
                var whiteList = raceRestrict.whiteApparelList;
                var existing = new HashSet<string>(whiteList.Select(d => d.defName));

                foreach (var defName in slaveCollarDefNames)
                {
                    if (!existing.Contains(defName))
                    {
                        var collarDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                        if (collarDef != null)
                        {
                            whiteList.Add(collarDef);
                            added++;
                        }
                    }
                }

                Log.Message($"[SSC] {def.defName} - CompSlave {(alreadyHasComp ? "already present via XML" : "added")}, whitelist added {added}");
            }
        }
    }
}
