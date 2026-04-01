// SimpleSlaveryCollars | Patches | Patch_SlaveRebellionUtility_InitiateSlaveRebellionMtbDays.cs
// 목적   : 노예 반란 발생 주기(MTB days) 계산식에 Stage별 보정값을 반영
// 용도   : Harmony Postfix 패치 — private Helper 메서드를 직접 패치하여 바닐라 캐시에도 보정값 반영
// 주의   : 바닐라의 InitiateSlaveRebellionMtbDays는 결과를 per-tick 캐시함.
//           public 메서드를 패치하면 캐시 후 보정이 적용되어 같은 틱 후속 호출에서 미보정 값이 반환됨.
//           Helper를 직접 패치하면 캐시 이전에 보정이 적용되어 모든 호출에서 일관된 값 보장.

using System;
using HarmonyLib;
using RimWorld;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// InitiateSlaveRebellionMtbDaysHelper(private) Postfix.
    /// 캐시 쓰기 전에 실행되므로 바닐라 캐시에도 보정된 값이 저장된다.
    /// </summary>
    [HarmonyPatch(typeof(SlaveRebellionUtility), "InitiateSlaveRebellionMtbDaysHelper")]
    public static class Patch_SlaveRebellionUtility_InitiateSlaveRebellionMtbDays
    {
        [HarmonyPostfix]
        public static void Postfix(ref Pawn pawn, ref float __result)
        {
            try
            {
                if (!SimpleSlaveryCollarsSetting.SlavestageEnable
                    || !SimpleSlaveryCollarsSetting.RebelCycleChangeEnable
                    || __result == -1f)
                    return;

                float time = SimpleSlaveryUtility.TimeAsSlave(pawn);

                if (time < SimpleSlaveryUtility.SlaveStage1)
                {
                    // Stage1: 바닐라 MTB 그대로 유지
                }
                else if (time < SimpleSlaveryUtility.SlaveStage2)
                {
                    __result *= 1.5f;
                }
                else if (time < SimpleSlaveryUtility.SlaveStage3)
                {
                    __result *= 1.75f;
                }
                else if (time < SimpleSlaveryUtility.SlaveStage4
                      || SimpleSlaveryUtility.IsSteadfast(pawn))
                {
                    __result *= 2f;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_SlaveRebellionUtility_InitiateSlaveRebellionMtbDays.Postfix error: {ex}");
            }
        }
    }
}
