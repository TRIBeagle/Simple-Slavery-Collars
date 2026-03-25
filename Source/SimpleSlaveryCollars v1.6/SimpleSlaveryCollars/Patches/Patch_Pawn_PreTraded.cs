// SimpleSlaveryCollars | Patches | Patch_Pawn_PreTraded.cs
// 목적 : 노예 매도 시 CompSlave 상태 리셋
// 용도 : PlayerSells 시 CompSlave 정리. 매수 시는 SetGuestStatus 패치가 담당
// 변경 : [리팩터] Hediff 제거 → CompSlave.ResetSlaveState()로 변경

using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// Pawn.PreTraded 후처리. 매도 시 CompSlave 상태 리셋만 담당.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "PreTraded")]
    public static class Patch_Pawn_PreTraded
    {
        [HarmonyPostfix]
        public static void PreTraded_Patch(ref Pawn __instance, ref TradeAction action)
        {
            try
            {
                if (action != TradeAction.PlayerSells) return;

                var comp = __instance.GetComp<CompSlave>();
                if (comp != null)
                    comp.ResetSlaveState();
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_Pawn_PreTraded error: {ex}");
            }
        }
    }
}
