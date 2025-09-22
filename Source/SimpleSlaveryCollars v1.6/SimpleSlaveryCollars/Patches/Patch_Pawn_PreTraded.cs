// SimpleSlaveryCollars | Patches | Patch_Pawn_PreTraded.cs
// 목적   : Pawn.PreTraded 실행 시 매매 상황에 따라 Enslaved 헤디프를 자동 부여/제거
// 용도   : Harmony Postfix 패치로 거래 직후 Pawn의 노예 상태를 최신화
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 요약 재작성
// 주의   : PlayerBuys 시 Colony 노예면 헤디프 부여, PlayerSells 시 제거
// 저장   : Hediff 추가/삭제는 Pawn 세이브에 직접 반영됨

using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// Pawn.PreTraded 후처리 패치.
    /// - PlayerBuys 시 Colony 노예 → Enslaved Hediff 부여
    /// - PlayerSells 시 Enslaved Hediff 제거
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "PreTraded")]
    public static class Patch_Pawn_PreTraded
    {
        /// <summary>
        /// Postfix: 거래 상황별로 Enslaved Hediff 부여/제거.
        /// </summary>
        [HarmonyPostfix]
        public static void PreTraded_Patch(ref Pawn __instance, ref TradeAction action)
        {
            Hediff enslaved = __instance.health.hediffSet.GetFirstHediffOfDef(SSC_HediffDefOf.Enslaved);

            if (action == TradeAction.PlayerBuys &&
                __instance.RaceProps.Humanlike &&
                !__instance.health.hediffSet.HasHediff(SSC_HediffDefOf.Enslaved) &&
                __instance.IsSlaveOfColony)
            {
                __instance.health.AddHediff(SSC_HediffDefOf.Enslaved);
            }
            else if (action == TradeAction.PlayerSells &&
                     __instance.health.hediffSet.HasHediff(SSC_HediffDefOf.Enslaved))
            {
                __instance.health.RemoveHediff(enslaved);
            }
        }
    }
}
