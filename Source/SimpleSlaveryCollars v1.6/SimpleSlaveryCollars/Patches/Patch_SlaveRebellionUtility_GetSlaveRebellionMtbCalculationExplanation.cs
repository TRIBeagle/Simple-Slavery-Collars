// SimpleSlaveryCollars | Patches | Patch_SlaveRebellionUtility_GetSlaveRebellionMtbCalculationExplanation.cs
// 목적   : SlaveRebellionUtility.GetSlaveRebellionMtbCalculationExplanation 실행 시, 노예 Stage별 반란 주기 보정 설명을 추가
// 용도   : Harmony Postfix 패치로 계산 설명 문자열에 Slavery Stage 보정값(×%) 라인을 삽입
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 요약 재작성
// 주의   : Stage5 = ( x ≥ SlaveStage4 ) && !Steadfast / Stage4 = (SlaveStage3 < x < SlaveStage4) 또는 ( x ≥ SlaveStage4 && Steadfast )
// 성능   : 단발 호출 시 StringBuilder 사용(할당 최소화), ToStringTicksToPeriod 포맷 비용만 존재
// 저장   : 설명 문자열은 UI 표시 전용으로, 세이브 데이터에는 영향 없음

using HarmonyLib;
using RimWorld;
using System;
using System.Text;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// SlaveRebellionUtility.GetSlaveRebellionMtbCalculationExplanation 후처리 패치.
    /// - Stage 구간(1~4/Steadfast 예외)에 따른 보정값을 설명 문자열에 추가한다.
    /// - “x{percent}” 접두 포맷은 원본 스타일을 그대로 유지한다.
    /// </summary>
    [HarmonyPatch(typeof(SlaveRebellionUtility), "GetSlaveRebellionMtbCalculationExplanation")]
    public static class Patch_SlaveRebellionUtility_GetSlaveRebellionMtbCalculationExplanation
    {
        /// <summary>
        /// Postfix: Slavery Stage 보정 설명을 기존 텍스트 끝에 덧붙인다.
        /// - SlaveryStageEnable && RebelCycleChangeEnable && Need_Suppression 존재 && 반란 참여 가능일 때만 동작
        /// - Stage5는 (x ≥ SlaveStage4 && !Steadfast), 그 외 x ≥ SlaveStage4 && Steadfast는 Stage4로 간주
        /// </summary>
        [HarmonyPostfix]
        public static void GetSlaveRebellionMtbCalculationExplanation_Patch(ref Pawn pawn, ref string __result)
        {
            Need_Suppression need = pawn.needs.TryGetNeed<Need_Suppression>();
            if (SimpleSlaveryCollarsSetting.SlavestageEnable == false || SimpleSlaveryCollarsSetting.RebelCycleChangeEnable == false || need == null || !SlaveRebellionUtility.CanParticipateInSlaveRebellion(pawn))
                return;

            StringBuilder stringBuilder = new StringBuilder();

            // Stage1: x < S1
            if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage1)
            {
                float f4 = 1f;
                stringBuilder.AppendLine(string.Format("\n{0}: x{1}", (object)"SuppressionSlavestageFactor".Translate(), (object)f4.ToStringPercent()));
            }
            // Stage2: S1 ≤ x < S2
            else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage2)
            {
                float f4 = 1.5f;
                stringBuilder.AppendLine(string.Format("\n{0}: x{1}", (object)"SuppressionSlavestageFactor".Translate(), (object)f4.ToStringPercent()));
            }
            // Stage3: S2 ≤ x ≤ S3 (경계 포함 여부는 유틸 정의에 따름)
            else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage3)
            {
                float f4 = 1.75f;
                stringBuilder.AppendLine(string.Format("\n{0}: x{1}", (object)"SuppressionSlavestageFactor".Translate(), (object)f4.ToStringPercent()));
            }
            // Stage4: (S3 < x < S4) 또는 (x ≥ S4 && Steadfast) — Steadfast 예외 포함
            else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage4 || (SlaveUtility.TimeAsSlave(pawn) >= SlaveUtility.SlaveStage3 && SlaveUtility.IsSteadfast(pawn)))
            {
                float f4 = 2f;
                stringBuilder.AppendLine(string.Format("\n{0}: x{1}", (object)"SuppressionSlavestageFactor".Translate(), (object)f4.ToStringPercent()));
            }

            // 최종 주기 표시(원본 포맷 유지)
            stringBuilder.AppendLine(string.Format("{0}: {1}", (object)"SuppressionFinalInterval".Translate(), (object)((int)((double)SlaveRebellionUtility.InitiateSlaveRebellionMtbDays(pawn) * 60000.0)).ToStringTicksToPeriod()));

            // 마지막 개행 제거 후 합치기(원본 로직 유지)
            __result = __result.Remove(__result.LastIndexOf(Environment.NewLine));
            __result += stringBuilder.ToString();
        }
    }
}
