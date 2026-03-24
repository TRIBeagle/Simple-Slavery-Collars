// SimpleSlaveryCollars | Patches | Patch_Pawn_PreTraded.cs
// 목적 : 노예 매도 시 Enslaved 헤디프 제거
// 용도 : PlayerSells 시 Hediff 정리. 매수 시 Hediff 부여는 SetGuestStatus 패치가 담당
// 주의 : 매수(PlayerBuys) 경로에서는 바닐라가 SetGuestStatus를 호출 → 중복 방지

using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// Pawn.PreTraded 후처리. 매도 시 Enslaved Hediff 제거만 담당.
    /// 매수 시 Hediff 부여는 SetGuestStatus 패치 + CompTickRare 안전망이 처리.
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

                var hs = __instance.health?.hediffSet;
                if (hs == null) return;

                var enslaved = hs.GetFirstHediffOfDef(SimpleSlaveryDefOf.Enslaved);
                if (enslaved != null)
                    __instance.health.RemoveHediff(enslaved);
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_Pawn_PreTraded 오류: {ex}");
            }
        }
    }
}
