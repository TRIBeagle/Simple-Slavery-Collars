// SimpleSlaveryCollars | Patches | Patch_GenGuest_TryEnslavePrisoner.cs
// 목적   : RimWorld 기본 로직 GenGuest.TryEnslavePrisoner 성공 시 노예 헤디프(Enslaved)를 자동 부여
// 용도   : Harmony Postfix 패치로 노예화 성공 직후 Pawn 상태 초기화
// 변경   : 2026-01-27 1.6 대응 — EnslavePrisoner -> TryEnslavePrisoner로 타겟 변경 + 성공(__result) 체크 + 파라미터 바인딩 안정화
// 주의   : ShacklesDefault 옵션이 false일 경우, shackledGoal을 강제로 false로 초기화
// 저장   : Hediff 추가/속성 변경은 세이브 데이터에 직접 기록됨

using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// GenGuest.TryEnslavePrisoner 후처리 패치.
    /// - 노예화 성공 시 Enslaved 헤디프를 자동 부여
    /// - 모드 설정에 따라 shackledGoal 초기값 제어
    /// </summary>
    [HarmonyPatch(typeof(GenGuest), "TryEnslavePrisoner")]
    public static class Patch_GenGuest_TryEnslavePrisoner
    {
        /// <summary>
        /// Postfix: 노예화 성공(__result==true) 시 Enslaved 헤디프 추가 및 shackledGoal 초기화.
        /// </summary>
        [HarmonyPostfix]
        public static void TryEnslavePrisoner_Postfix(bool __result, Pawn warden, Pawn prisoner)
        {
            if (!__result) return;
            if (prisoner == null) return;

            var hs = prisoner.health?.hediffSet;
            if (hs == null) return;

            if (!hs.HasHediff(SSC_HediffDefOf.Enslaved))
                prisoner.health.AddHediff(SSC_HediffDefOf.Enslaved);

            if (SimpleSlaveryCollarsSetting.ShacklesDefault == false)
            {
                var enslaved = SlaveUtility.GetEnslavedHediff(prisoner);
                if (enslaved != null)
                    enslaved.shackledGoal = false;
            }
        }
    }
}
