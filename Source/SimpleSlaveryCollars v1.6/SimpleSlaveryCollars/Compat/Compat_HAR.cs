// SimpleSlaveryCollars | Compat | Compat_HAR.cs
// 목적 : Humanoid Alien Races(HAR) 호환 — 에일리언 종족에 CompSlave 추가 + 칼라 화이트리스트
// 용도 : HAR 설치 시 리플렉션으로 AlienRace 타입 접근, 모든 에일리언 ThingDef에 칼라 착용 허용
// 주의 : AlienRace.dll 직접 참조 없음 — 리플렉션 1회 캐시. HAR 미설치 시 실행되지 않음

using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace SimpleSlaveryCollars.Compat
{
    /// <summary>HAR 호환 패치. 리플렉션으로 AlienRace 타입에 접근.</summary>
    internal static class Compat_HAR
    {
        // 리플렉션 캐시
        private static Type _alienRaceType;          // AlienRace.ThingDef_AlienRace
        private static FieldInfo _alienRaceField;     // ThingDef_AlienRace.alienRace
        private static FieldInfo _raceRestrictField;  // AlienRaceSettings.raceRestriction
        private static FieldInfo _whiteApparelField;  // RaceRestrictionSettings.whiteApparelList
        private static FieldInfo _onlyUseRestrictField; // RaceRestrictionSettings.onlyUseRaceRestrictedApparel
        private static bool _reflectionSearched;
        private static bool _reflectionOk;

        /// <summary>리플렉션 1회 탐색. 실패 시 재탐색 없음.</summary>
        private static bool EnsureReflection()
        {
            if (_reflectionSearched) return _reflectionOk;
            _reflectionSearched = true;

            try
            {
                // AlienRace.ThingDef_AlienRace 타입 탐색
                _alienRaceType = GenTypes.GetTypeInAnyAssembly("AlienRace.ThingDef_AlienRace");
                if (_alienRaceType == null)
                {
                    CompatManager.HAR.Failed("Reflection", "ThingDef_AlienRace type not found");
                    return false;
                }

                // ThingDef_AlienRace.alienRace (AlienRaceSettings)
                _alienRaceField = _alienRaceType.GetField("alienRace",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (_alienRaceField == null)
                {
                    CompatManager.HAR.Failed("Reflection", "alienRace field not found");
                    return false;
                }

                // AlienRaceSettings.raceRestriction
                var settingsType = _alienRaceField.FieldType;
                _raceRestrictField = settingsType.GetField("raceRestriction",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (_raceRestrictField == null)
                {
                    CompatManager.HAR.Failed("Reflection", "raceRestriction field not found");
                    return false;
                }

                // RaceRestrictionSettings.whiteApparelList
                var restrictType = _raceRestrictField.FieldType;
                _whiteApparelField = restrictType.GetField("whiteApparelList",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _onlyUseRestrictField = restrictType.GetField("onlyUseRaceRestrictedApparel",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (_whiteApparelField == null)
                {
                    CompatManager.HAR.Failed("Reflection", "whiteApparelList field not found");
                    return false;
                }

                _reflectionOk = true;
                CompatManager.HAR.Patched("Reflection");
                return true;
            }
            catch (Exception e)
            {
                CompatManager.HAR.Failed("Reflection", e.Message);
                return false;
            }
        }

        /// <summary>HAR 패칭 실행: CompSlave 추가 + 칼라 화이트리스트.</summary>
        internal static void RunPatching()
        {
            if (!EnsureReflection()) return;

            // SlaveCollar 태그가 있는 의상 수집
            var slaveCollarDefs = new List<ThingDef>();
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.apparel?.defaultOutfitTags != null
                    && def.apparel.defaultOutfitTags.Contains("SlaveCollar"))
                    slaveCollarDefs.Add(def);
            }
            var collarDefNames = new HashSet<string>();
            for (int i = 0; i < slaveCollarDefs.Count; i++)
                collarDefNames.Add(slaveCollarDefs[i].defName);

            int raceCount = 0;
            int compAdded = 0;
            int whitelistAdded = 0;

            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.defName == "Human") continue;
                if (!_alienRaceType.IsInstanceOfType(def)) continue;

                raceCount++;

                // CompSlave 추가
                if (def.comps == null)
                    def.comps = new List<CompProperties>();

                bool hasComp = false;
                for (int i = 0; i < def.comps.Count; i++)
                {
                    if (def.comps[i] is CompProperties_Slave)
                    {
                        hasComp = true;
                        break;
                    }
                }
                if (!hasComp)
                {
                    def.comps.Add(new CompProperties_Slave());
                    compAdded++;
                }

                // 화이트리스트 패치
                try
                {
                    var alienRaceObj = _alienRaceField.GetValue(def);
                    if (alienRaceObj == null) continue;

                    var restrictObj = _raceRestrictField.GetValue(alienRaceObj);
                    if (restrictObj == null) continue;

                    // onlyUseRaceRestrictedApparel 체크
                    if (_onlyUseRestrictField != null)
                    {
                        bool onlyRestricted = (bool)_onlyUseRestrictField.GetValue(restrictObj);
                        if (!onlyRestricted) continue; // 제한 없음 → 화이트리스트 불필요
                    }

                    var whiteList = _whiteApparelField.GetValue(restrictObj) as List<ThingDef>;
                    if (whiteList == null) continue;

                    // 기존 목록 해시
                    var existing = new HashSet<string>();
                    for (int i = 0; i < whiteList.Count; i++)
                        existing.Add(whiteList[i].defName);

                    for (int i = 0; i < slaveCollarDefs.Count; i++)
                    {
                        if (!existing.Contains(slaveCollarDefs[i].defName))
                        {
                            whiteList.Add(slaveCollarDefs[i]);
                            whitelistAdded++;
                        }
                    }
                }
                catch (Exception e)
                {
                    CompatManager.HAR.Failed($"Whitelist_{def.defName}", e.Message);
                }
            }

            if (raceCount > 0)
            {
                CompatManager.HAR.Patched("AddComp");
                CompatManager.HAR.Patched("Whitelist");
                Log.Message($"{CompatManager.LOG_HAR} Patched {raceCount} alien races: " +
                    $"CompSlave added={compAdded}, whitelist entries={whitelistAdded}");
            }
        }
    }
}
