// SimpleSlaveryCollars | Patches | Patch_GenGuest_EmancipateSlave.cs
// 목적   : RimWorld 기본 로직 GenGuest.EmancipateSlave 실행 시 CompSlave 상태 리셋
// 용도   : Harmony Postfix 패치로 자유화 직후 Pawn 상태를 정상화
// 변경   : [리팩터] Hediff 제거 → CompSlave.ResetSlaveState()로 변경

using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// GenGuest.EmancipateSlave 후처리 패치.
    /// - EmancipateSlave 실행 시 CompSlave 상태를 리셋한다.
    /// </summary>
    [HarmonyPatch(typeof(GenGuest), "EmancipateSlave")]
    public static class Patch_GenGuest_EmancipateSlave
    {
        /// <summary>
        /// Postfix: 노예 해방 시 CompSlave 상태 리셋.
        /// </summary>
        [HarmonyPostfix]
        public static void EmancipateSlave_Postfix(Pawn warden, Pawn slave)
        {
            try
            {
                if (slave == null) return;

                var comp = slave.GetComp<CompSlave>();
                if (comp != null)
                    comp.ResetSlaveState();
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_GenGuest_EmancipateSlave.EmancipateSlave_Postfix error: {ex}");
            }
        }
    }
}
