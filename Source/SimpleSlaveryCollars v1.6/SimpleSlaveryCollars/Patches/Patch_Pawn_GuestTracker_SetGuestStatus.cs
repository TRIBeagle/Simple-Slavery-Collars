// SimpleSlaveryCollars | Patches | Patch_Pawn_GuestTracker_SetGuestStatus.cs
// 목적   : Pawn_GuestTracker.SetGuestStatus 실행 시 Stage5 노예를 자동 동화(플레이어 팩션 전환)
// 용도   : Harmony Postfix 패치로 Stage5 노예가 일정 조건 만족 시 Colony Pawn으로 전환
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 요약 재작성
// 주의   : SlaveryStage/Assimilation 옵션 활성화 필요, Steadfast 예외 처리
// 저장   : Faction 변경은 Pawn 세이브에 직접 반영됨

using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// Pawn_GuestTracker.SetGuestStatus 후처리 패치.
    /// - Stage5 노예(조건 충족)일 경우 자동으로 플레이어 팩션에 동화
    /// - AssimilationSlaveEnable 설정 시에만 발동
    /// </summary>
    [HarmonyPatch(typeof(Pawn_GuestTracker), "SetGuestStatus")]
    public static class Patch_Pawn_GuestTracker_SetGuestStatus
    {
        /// <summary>
        /// Postfix: Stage5 노예 + Steadfast 아님 + Colony 노예 → Faction 전환.
        /// </summary>
        [HarmonyPostfix]
        public static void SetGuestStatus_Patch(Pawn_GuestTracker __instance, ref Faction ___slaveFactionInt, Pawn ___pawn)
        {
            if (!SimpleSlaveryCollarsSetting.SlavestageEnable ||
                !SimpleSlaveryCollarsSetting.AssimilationSlaveEnable)
                return;

            if (SlaveUtility.TimeAsSlave(___pawn) >= SlaveUtility.SlaveStage4 &&
                !SlaveUtility.IsSteadfast(___pawn) &&
                ___pawn.IsSlaveOfColony &&
                __instance.SlaveFaction != Faction.OfPlayer)
            {
                ___slaveFactionInt = Faction.OfPlayer;
                Messages.Message("MessageAssimilationSlave".Translate().AdjustedFor(___pawn),
                                 (LookTargets)___pawn,
                                 MessageTypeDefOf.NeutralEvent);
            }
        }
    }
}
