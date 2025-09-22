// SimpleSlaveryCollars | Patches | Patch_SlaveRebellionUtility_CanParticipateInSlaveRebellion.cs
// 목적   : SlaveRebellionUtility.CanParticipateInSlaveRebellion 실행 시 Stage에 따른 반란 참여 여부 제어
// 용도   : Harmony Postfix 패치로 Stage5 노예( x ≥ SlaveStage4 && !Steadfast )는 반란에서 제외
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 요약 재작성
// 주의   : Stage5 = ( x ≥ SlaveStage4 ) && !Steadfast / Stage4 = (SlaveStage3 < x < SlaveStage4) 또는 ( x ≥ SlaveStage4 && Steadfast )
// 저장   : 반란 참여 여부는 Pawn 세이브에 간접 반영됨

using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// SlaveRebellionUtility.CanParticipateInSlaveRebellion 후처리 패치.
    /// - Stage5( x ≥ SlaveStage4 && !Steadfast )이면 반란 참여 불가 처리
    /// - Steadfast 예외: x ≥ SlaveStage4 여도 Stage4로 간주(참여 가능성 원본 로직 유지)
    /// </summary>
    [HarmonyPatch(typeof(SlaveRebellionUtility), "CanParticipateInSlaveRebellion")]
    public static class Patch_SlaveRebellionUtility_CanParticipateInSlaveRebellion
    {
        /// <summary>
        /// Postfix: Stage5 조건 만족 시 __result=false로 강제.
        /// </summary>
        [HarmonyPostfix]
        public static void CanParticipateInSlaveRebellion_Patch(ref Pawn pawn, ref bool __result)
        {
            if (!SimpleSlaveryCollarsSetting.SlavestageEnable ||
                !SimpleSlaveryCollarsSetting.RebelCycleChangeEnable)
                return;

            if (SlaveUtility.TimeAsSlave(pawn) >= SlaveUtility.SlaveStage4 &&
                !SlaveUtility.IsSteadfast(pawn))
            {
                __result = false;
            }
        }
    }
}
