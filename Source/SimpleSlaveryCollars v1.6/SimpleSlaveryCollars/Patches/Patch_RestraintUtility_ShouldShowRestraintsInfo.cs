// SimpleSlaveryCollars | Patches | Patch_RestraintUtility_ShouldShowRestraintsInfo.cs
// 목적   : RestraintsUtility.ShouldShowRestraintsInfo 실행 시 노예의 구속 정보 표시 여부 제어
// 용도   : Harmony Postfix 패치로 Colony 노예 + 구속 상태일 경우 강제 true 반환
// 주의   : Colony 소속 노예 + InRestraints 조건일 때만 발동
// 저장   : 표시 여부 자체는 저장과 무관

using System;
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
            try
            {
                // [성능] 값싼 IsSlaveOfColony를 먼저 검사해 비싼 InRestraints 호출을 회피
                if (pawn.IsSlaveOfColony && RestraintsUtility.InRestraints(pawn))
                {
                    __result = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_RestraintUtility_ShouldShowRestraintsInfo.ShouldShowRestraintsInfo_Patch error: {ex}");
            }
        }
    }
}
