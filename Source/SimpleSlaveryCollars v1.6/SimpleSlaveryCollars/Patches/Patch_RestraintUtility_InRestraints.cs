// SimpleSlaveryCollars | Patches | Patch_RestraintUtility_InRestraints.cs
// 목적   : RestraintsUtility.InRestraints 실행 시 노예의 shackled 상태를 올바르게 반영
// 용도   : Harmony Postfix 패치로 Colony 노예 + Enslaved Hediff 보유 Pawn은 shackledGoal 대신 shackled 값 사용
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 요약 재작성
// 주의   : Colony 소속 노예 + Enslaved Hediff 보유 시에만 동작
// 저장   : shackled 값은 Hediff 필드에 저장되며 Pawn 세이브에 직접 반영됨

using HarmonyLib;
using RimWorld;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// RestraintsUtility.InRestraints 후처리 패치.
    /// - Colony 노예 + Enslaved Hediff 보유 Pawn이면 shackled 필드 값을 그대로 반환
    /// </summary>
    [HarmonyPatch(typeof(RestraintsUtility), "InRestraints")]
    public static class Patch_RestraintUtility_InRestraints
    {
        /// <summary>
        /// Postfix: Enslaved 노예 Pawn은 shackledGoal이 아닌 실제 shackled 여부로 반환.
        /// </summary>
        [HarmonyPostfix]
        public static void InRestraints_Patch(ref Pawn pawn, ref bool __result)
        {
            if (pawn.IsSlaveOfColony &&
                pawn.health.hediffSet.HasHediff(SimpleSlaveryDefOf.Enslaved))
            {
                __result = SimpleSlaveryUtility.GetEnslavedHediff(pawn).shackled;
            }
        }
    }
}
