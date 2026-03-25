// SimpleSlaveryCollars | Patches | Patch_RestraintUtility_InRestraints.cs
// 목적   : RestraintsUtility.InRestraints 실행 시 노예의 shackled 상태를 올바르게 반영
// 용도   : Harmony Postfix 패치로 Colony 노예 Pawn은 CompSlave.Shackled 값을 사용
// 주의   : Colony 소속 노예 + CompSlave 보유 시에만 동작

using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// RestraintsUtility.InRestraints 후처리 패치.
    /// - Colony 노예 Pawn이면 CompSlave.Shackled 값을 반환
    /// </summary>
    [HarmonyPatch(typeof(RestraintsUtility), "InRestraints")]
    public static class Patch_RestraintUtility_InRestraints
    {
        /// <summary>
        /// Postfix: 노예 Pawn은 CompSlave.Shackled 여부로 반환.
        /// </summary>
        [HarmonyPostfix]
        public static void InRestraints_Patch(ref Pawn pawn, ref bool __result)
        {
            try
            {
                if (pawn.IsSlaveOfColony)
                {
                    var comp = pawn.GetComp<CompSlave>();
                    if (comp != null)
                        __result = comp.Shackled;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_RestraintUtility_InRestraints.InRestraints_Patch error: {ex}");
            }
        }
    }
}
