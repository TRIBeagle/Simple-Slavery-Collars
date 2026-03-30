// SimpleSlaveryCollars | Patches | Patch_SlaveRebellionUtility_GetSlaveRebellionMtbCalculationExplanation.cs
// 목적   : SlaveRebellionUtility.GetSlaveRebellionMtbCalculationExplanation 실행 시, 노예 Stage별 반란 주기 보정 설명을 추가
// 용도   : Harmony Postfix 패치로 계산 설명 문자열에 Slavery Stage 보정값(×%) 라인을 삽입
// 주의   : Stage5 = ( x ≥ SlaveStage4 ) && !Steadfast / Stage4 = (SlaveStage3 < x < SlaveStage4) 또는 ( x ≥ SlaveStage4 && Steadfast )
// 성능   : 단발 호출 시 StringBuilder 사용(할당 최소화), ToStringTicksToPeriod 포맷 비용만 존재

using HarmonyLib;
using RimWorld;
using System;
using System.Text;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// SlaveRebellionUtility.GetSlaveRebellionMtbCalculationExplanation 후처리 패치.
    /// - Stage 구간(1~4/Steadfast 예외)에 따른 보정값을 설명 문자열에 추가한다.
    /// </summary>
    [HarmonyPatch(typeof(SlaveRebellionUtility), "GetSlaveRebellionMtbCalculationExplanation")]
    public static class Patch_SlaveRebellionUtility_GetSlaveRebellionMtbCalculationExplanation
    {
        /// <summary>
        /// Postfix: Slavery Stage 보정 설명을 기존 텍스트 끝에 덧붙인다.
        /// </summary>
        [HarmonyPostfix]
        public static void GetSlaveRebellionMtbCalculationExplanation_Patch(ref Pawn pawn, ref string __result)
        {
            try
            {
                if (pawn?.needs == null) return;
                Need_Suppression need = pawn.needs.TryGetNeed<Need_Suppression>();
                if (!SimpleSlaveryCollarsSetting.SlavestageEnable
                    || !SimpleSlaveryCollarsSetting.RebelCycleChangeEnable
                    || need == null
                    || !SlaveRebellionUtility.CanParticipateInSlaveRebellion(pawn))
                    return;

                float time = SimpleSlaveryUtility.TimeAsSlave(pawn);
                string stageLabel = "SSC_Stage_SuppressionFactor".Translate();
                float stageFactor;

                // Stage1: x < S1
                if (time < SimpleSlaveryUtility.SlaveStage1)
                    stageFactor = 1f;
                // Stage2: S1 ≤ x < S2
                else if (time < SimpleSlaveryUtility.SlaveStage2)
                    stageFactor = 1.5f;
                // Stage3: S2 ≤ x < S3
                else if (time < SimpleSlaveryUtility.SlaveStage3)
                    stageFactor = 1.75f;
                // Stage4: (S3 ≤ x < S4) 또는 (x ≥ S4 && Steadfast)
                else if (time < SimpleSlaveryUtility.SlaveStage4 || SimpleSlaveryUtility.IsSteadfast(pawn))
                    stageFactor = 2f;
                else
                    return; // Stage5: 표시 없음

                int finalTicks = (int)(SlaveRebellionUtility.InitiateSlaveRebellionMtbDays(pawn) * 60000f);
                string finalLabel = "SuppressionFinalInterval".Translate();

                var sb = new StringBuilder();
                sb.AppendLine($"\n{stageLabel}: x{stageFactor.ToStringPercent()}");
                sb.AppendLine($"{finalLabel}: {finalTicks.ToStringTicksToPeriod()}");

                // 마지막 줄(원본의 SuppressionFinalInterval) 교체
                if (string.IsNullOrEmpty(__result))
                {
                    __result = sb.ToString();
                    return;
                }

                // Environment.NewLine 기준 마지막 줄 제거 — 바닐라 포맷 변경 시 원본 유지
                int lastNewLine = __result.LastIndexOf(Environment.NewLine);
                if (lastNewLine >= 0)
                    __result = __result.Remove(lastNewLine);

                __result += sb.ToString();
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_SlaveRebellionUtility.Postfix error: {ex}");
            }
        }
    }
}
