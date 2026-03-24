// SimpleSlaveryCollars | Patches | Patch_RestraintUtility_ShouldShowRestraintsInfo.cs
// 목적   : RestraintsUtility.ShouldShowRestraintsInfo 실행 시 노예의 구속 정보 표시 여부 제어
// 용도   : Harmony Postfix 패치로 Colony 노예 + 구속 상태일 경우 바닐라의 "구속됨(느려짐)" 표시를 억제
// 주의   : SSC 노예의 구속 정보는 CompSlave.CompInspectStringExtra에서 노예 기간과 한 줄로 합쳐 표시됨
//          이동 속도 페널티는 Patch_RestraintUtility_InRestraints가 담당하므로 표시만 억제해도 안전
// 저장   : 표시 여부 자체는 저장과 무관

using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// RestraintsUtility.ShouldShowRestraintsInfo 후처리 패치.
    /// - Colony 노예 + InRestraints 조건 충족 시 바닐라 구속줄 억제 (SSC가 직접 표시)
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
                // SSC 노예의 구속 정보는 CompInspectStringExtra에서 직접 표시하므로
                // 바닐라의 "구속됨(느려짐)" 줄을 억제 (이동 속도 페널티는 InRestraints 패치가 담당)
                if (RestraintsUtility.InRestraints(pawn) && pawn.IsSlaveOfColony)
                {
                    __result = false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_RestraintUtility_ShouldShowRestraintsInfo.ShouldShowRestraintsInfo_Patch 오류: {ex}");
            }
        }
    }
}
