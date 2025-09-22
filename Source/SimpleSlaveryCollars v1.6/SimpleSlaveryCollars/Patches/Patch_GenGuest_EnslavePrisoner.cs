// SimpleSlaveryCollars | Patches | Patch_GenGuest_EnslavePrisoner.cs
// 목적   : RimWorld 기본 로직 GenGuest.EnslavePrisoner 실행 시 노예 헤디프(Enslaved)를 자동 부여
// 용도   : Harmony Postfix 패치로 포획된 Pawn이 노예화 될 때 상태 초기화
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 요약 재작성
// 주의   : ShacklesDefault 옵션이 false일 경우, shackledGoal을 강제로 false로 초기화
// 저장   : Hediff 추가/속성 변경은 세이브 데이터에 직접 기록됨

using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// GenGuest.EnslavePrisoner 후처리 패치.
    /// - 노예화 시 Enslaved 헤디프를 자동 부여
    /// - 모드 설정에 따라 shackledGoal 초기값 제어
    /// </summary>
    [HarmonyPatch(typeof(GenGuest), "EnslavePrisoner")]
    public static class Patch_GenGuest_EnslavePrisoner
    {
        /// <summary>
        /// Postfix: 노예화 시 Enslaved 헤디프 추가 및 shackledGoal 초기화.
        /// </summary>
        [HarmonyPostfix]
        public static void EnslavePrisoner_Patch(ref Pawn prisoner)
        {
            if (!prisoner.health.hediffSet.HasHediff(SSC_HediffDefOf.Enslaved))
                prisoner.health.AddHediff(SSC_HediffDefOf.Enslaved);

            if (SimpleSlaveryCollarsSetting.ShacklesDefault == false)
                SlaveUtility.GetEnslavedHediff(prisoner).shackledGoal = false;
        }
    }
}
