// SimpleSlaveryCollars | Patches | Patch_SlaveRebellionUtility_InitiateSlaveRebellionMtbDays.cs
// 목적   : 노예 반란 발생 주기(MTB days) 계산식에 Stage별 보정값을 반영
// 용도   : Harmony Postfix 패치
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — Stage4/5 조건 및 Steadfast 예외 처리 명시
// 주의   : Stage5 = ( x ≥ SlaveStage4 ) && !Steadfast / Stage4 = (SlaveStage3 < x < SlaveStage4) 또는 ( x ≥ SlaveStage4 && Steadfast )

using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// 반란 발생 주기 계산 시 노예 Stage별 보정값을 적용한다.
    /// Steadfast 특성이 있으면 Stage5로 승급하지 않고 Stage4로 유지된다.
    /// </summary>
    [HarmonyPatch(typeof(SlaveRebellionUtility), "InitiateSlaveRebellionMtbDays")]
    public static class Patch_SlaveRebellionUtility_InitiateSlaveRebellionMtbDays
    {
        [HarmonyPostfix]
        public static void InitiateSlaveRebellionMtbDays_Patch(ref Pawn pawn, ref float __result)
        {
            if (SimpleSlaveryCollarsSetting.SlavestageEnable == false
                || SimpleSlaveryCollarsSetting.RebelCycleChangeEnable == false
                || __result == -1f)
                return;

            if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage1)
            {
                __result *= 1f;
            }
            else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage2)
            {
                __result *= 1.5f;
            }
            else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage3)
            {
                __result *= 1.75f;
            }
            else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage4
                  || (SlaveUtility.TimeAsSlave(pawn) >= SlaveUtility.SlaveStage3 && SlaveUtility.IsSteadfast(pawn)))
            {
                __result *= 2f;
            }
        }
    }
}
