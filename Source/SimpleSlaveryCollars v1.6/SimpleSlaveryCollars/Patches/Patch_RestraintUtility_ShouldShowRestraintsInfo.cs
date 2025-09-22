// SimpleSlaveryCollars | Patches | Patch_RestraintUtility_ShouldShowRestraintsInfo.cs
// 목적   : RestraintsUtility.ShouldShowRestraintsInfo 실행 시 노예의 구속 정보 표시 여부 제어
// 용도   : Harmony Postfix 패치로 Colony 노예 + 구속 상태일 경우 강제 true 반환
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 요약 재작성
// 주의   : Colony 소속 노예 + InRestraints 조건일 때만 발동
// 저장   : 표시 여부 자체는 저장과 무관

using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// RestraintsUtility.ShouldShowRestraintsInfo 후처리 패치.
    /// - Colony 노예 + InRestraints 조건 충족 시 강제로 true 반환
    /// </summary>
    [HarmonyPatch(typeof(RestraintsUtility), "ShouldShowRestraintsInfo")]
    public static class Patch_RestraintUtility_ShouldShowRestraintsInfo
    {
        /// <summary>
        /// Postfix: 구속 상태인 Colony 노예는 정보 표시를 강제 허용.
        /// </summary>
        [HarmonyPostfix]
        public static void ShouldShowRestraintsInfo_Patch(ref Pawn pawn, ref bool __result)
        {
            if (RestraintsUtility.InRestraints(pawn) && pawn.IsSlaveOfColony)
            {
                __result = true;
            }
        }
    }
}
