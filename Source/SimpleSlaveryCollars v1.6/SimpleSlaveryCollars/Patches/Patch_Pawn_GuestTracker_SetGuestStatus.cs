// SimpleSlaveryCollars | Patches | Patch_Pawn_GuestTracker_SetGuestStatus.cs
// 목적   : SetGuestStatus 실행 시 노예 상태 전환에 따른 CompSlave 조작 및 Stage5 동화 처리
// 용도   : Harmony Postfix 패치 (단일 통합)
// 변경   : [리팩터] Hediff_Enslaved 추가/제거 로직 제거. CompSlave 직접 조작으로 변경.
//           - 노예화 시: ShacklesDefault 옵션 반영
//           - 비노예 전환 시: CompSlave.ResetSlaveState() 호출
//           - Stage5 동화: CompSlave 필드 직접 참조

using System;
using HarmonyLib;
using RimWorld;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// Pawn_GuestTracker.SetGuestStatus 통합 Postfix.
    /// 단일 Postfix에서 조건 분기하여 실행 순서를 확정적으로 보장한다.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_GuestTracker), "SetGuestStatus")]
    public static class Patch_Pawn_GuestTracker_SetGuestStatus
    {
        // ___slaveFactionInt 대신 캐시된 FieldRef 사용 — 필드명 변경 시에도 패치 자체는 유지됨
        private static readonly AccessTools.FieldRef<Pawn_GuestTracker, Faction> slaveFactionRef =
            AccessTools.FieldRefAccess<Pawn_GuestTracker, Faction>("slaveFactionInt");

        [HarmonyPostfix]
        public static void Postfix(
            Pawn_GuestTracker __instance,
            Faction newHost,
            GuestStatus guestStatus,
            Pawn ___pawn)
        {
            try
            {
                if (___pawn == null) return;
                if (___pawn.Dead || ___pawn.DestroyedOrNull()) return;

                if (guestStatus == GuestStatus.Slave && newHost == Faction.OfPlayer)
                {
                    // === 1) 노예화 시 ShacklesDefault 반영 ===
                    try { ApplyShacklesDefault(___pawn); }
                    catch (Exception ex) { Log.Error($"[SSC] ApplyShacklesDefault error: {ex}"); }

                    // === 2) Stage5 동화 ===
                    try { TryAssimilation(__instance, ___pawn); }
                    catch (Exception ex) { Log.Error($"[SSC] TryAssimilation error: {ex}"); }
                }
                else if (guestStatus != GuestStatus.Slave)
                {
                    // === 3) 비노예 전환 시 CompSlave 리셋 ===
                    try { TryResetSlaveState(__instance, newHost, ___pawn); }
                    catch (Exception ex) { Log.Error($"[SSC] TryResetSlaveState error: {ex}"); }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_Pawn_GuestTracker_SetGuestStatus.Postfix error: {ex}");
            }
        }

        /// <summary>
        /// 노예화 시 ShacklesDefault 옵션 반영.
        /// </summary>
        private static void ApplyShacklesDefault(Pawn pawn)
        {
            if (!pawn.RaceProps.Humanlike) return;
            if (SimpleSlaveryCollarsSetting.ShacklesDefault) return;

            var comp = pawn.GetComp<CompSlave>();
            if (comp != null)
                comp.ShackledGoal = false;
        }

        /// <summary>
        /// Stage5 노예(x ≥ SlaveStage4 && !Steadfast) + SlaveFaction != Player 시 동화.
        /// slaveFactionRef 캐시 필드로 접근 — 시그니처에서 분리하여 필드명 변경에도 패치 유지.
        /// </summary>
        private static void TryAssimilation(
            Pawn_GuestTracker guest,
            Pawn pawn)
        {
            if (!SimpleSlaveryCollarsSetting.SlavestageEnable) return;
            if (!SimpleSlaveryCollarsSetting.AssimilationSlaveEnable) return;

            if (!pawn.IsSlaveOfColony) return;
            float time = SimpleSlaveryUtility.TimeAsSlave(pawn);
            if (time < SimpleSlaveryUtility.SlaveStage4) return;
            if (SimpleSlaveryUtility.IsSteadfast(pawn)) return;
            if (guest.SlaveFaction == Faction.OfPlayer) return;

            slaveFactionRef(guest) = Faction.OfPlayer;
            Messages.Message(
                "SSC_Message_Assimilation".Translate().AdjustedFor(pawn),
                (LookTargets)pawn,
                MessageTypeDefOf.NeutralEvent);
        }

        /// <summary>
        /// 비노예 전환 시 CompSlave 상태 리셋.
        /// 플레이어가 관여한 노예만 대상 (타 팩션/퀘스트 Pawn 오염 방지).
        /// </summary>
        private static void TryResetSlaveState(
            Pawn_GuestTracker guest,
            Faction newHost,
            Pawn pawn)
        {
            // 플레이어가 관여한 노예만 처리
            bool playerContext =
                newHost == Faction.OfPlayer
                || (guest != null && guest.SlaveFaction == Faction.OfPlayer)
                || (pawn.Faction != null && pawn.Faction.IsPlayer);

            if (!playerContext) return;

            var comp = pawn.GetComp<CompSlave>();
            if (comp != null)
                comp.ResetSlaveState();
        }
    }
}
