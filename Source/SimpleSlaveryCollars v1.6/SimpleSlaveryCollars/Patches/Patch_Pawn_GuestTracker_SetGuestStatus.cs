// SimpleSlaveryCollars | Patches | Patch_Pawn_GuestTracker_SetGuestStatus.cs
// 목적   : SetGuestStatus 실행 시 Enslaved Hediff 부여/제거 및 Stage5 동화를 일괄 처리
// 용도   : Harmony Postfix 패치 (단일 통합)
// 변경   : [FIX] 기존 3개의 독립 Postfix를 1개로 통합하여 실행 순서 보장.
//           - EnsureEnslavedHediff: Slave + Player Host → Hediff 부여
//           - Assimilation: Stage5 노예 → SlaveFaction을 Player로 전환
//           - RemoveEnslavedHediff: Slave 해제 → Hediff 제거
// 주의   : 기존 파일 3개 삭제 필요:
//           - Patch_Pawn_GuestTracker_SetGuestStatus_EnsureEnslavedHediff.cs
//           - Patch_Pawn_GuestTracker_SetGuestStatus_RemoveEnslavedHediff.cs
//           (이 파일이 기존 Patch_Pawn_GuestTracker_SetGuestStatus.cs를 대체)

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
        [HarmonyPostfix]
        public static void Postfix(
            Pawn_GuestTracker __instance,
            ref Faction ___slaveFactionInt,
            Faction newHost,
            GuestStatus guestStatus,
            Pawn ___pawn)
        {
            if (___pawn == null) return;
            if (___pawn.Dead || ___pawn.DestroyedOrNull()) return;

            if (guestStatus == GuestStatus.Slave && newHost == Faction.OfPlayer)
            {
                // === 1) Enslaved Hediff 부여 (기존 EnsureEnslavedHediff) ===
                EnsureEnslavedHediff(___pawn);

                // === 2) Stage5 동화 (기존 Assimilation) ===
                TryAssimilation(__instance, ref ___slaveFactionInt, ___pawn);
            }
            else if (guestStatus != GuestStatus.Slave)
            {
                // === 3) Enslaved Hediff 제거 (기존 RemoveEnslavedHediff) ===
                TryRemoveEnslavedHediff(__instance, newHost, ___pawn);
            }
        }

        /// <summary>
        /// Slave + Player Host 시 Enslaved Hediff가 없으면 추가.
        /// DevMode AddGuest/AddSlave 경로도 커버.
        /// </summary>
        private static void EnsureEnslavedHediff(Pawn pawn)
        {
            if (!pawn.RaceProps.Humanlike) return;

            var hs = pawn.health?.hediffSet;
            if (hs == null) return;
            if (SimpleSlaveryDefOf.Enslaved == null) return;

            if (!hs.HasHediff(SimpleSlaveryDefOf.Enslaved))
            {
                pawn.health.AddHediff(SimpleSlaveryDefOf.Enslaved);
            }

            // ShacklesDefault 옵션 반영
            if (!SimpleSlaveryCollarsSetting.ShacklesDefault)
            {
                var enslaved = SimpleSlaveryUtility.GetEnslavedHediff(pawn);
                if (enslaved != null)
                    enslaved.shackledGoal = false;
            }
        }

        /// <summary>
        /// Stage5 노예(x ≥ SlaveStage4 && !Steadfast) + SlaveFaction != Player 시 동화.
        /// </summary>
        private static void TryAssimilation(
            Pawn_GuestTracker guest,
            ref Faction slaveFactionInt,
            Pawn pawn)
        {
            if (!SimpleSlaveryCollarsSetting.SlavestageEnable) return;
            if (!SimpleSlaveryCollarsSetting.AssimilationSlaveEnable) return;

            if (!pawn.IsSlaveOfColony) return;
            if (SimpleSlaveryUtility.TimeAsSlave(pawn) < SimpleSlaveryUtility.SlaveStage4) return;
            if (SimpleSlaveryUtility.IsSteadfast(pawn)) return;
            if (guest.SlaveFaction == Faction.OfPlayer) return;

            slaveFactionInt = Faction.OfPlayer;
            Messages.Message(
                "MessageAssimilationSlave".Translate().AdjustedFor(pawn),
                (LookTargets)pawn,
                MessageTypeDefOf.NeutralEvent);
        }

        /// <summary>
        /// Slave가 아닌 상태로 전환 시 Enslaved Hediff 제거.
        /// Player 관련 Pawn만 대상 (타 팩션/퀘스트 Pawn 오염 방지).
        /// </summary>
        private static void TryRemoveEnslavedHediff(
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

            var hs = pawn.health?.hediffSet;
            if (hs == null) return;
            if (SimpleSlaveryDefOf.Enslaved == null) return;

            var enslaved = hs.GetFirstHediffOfDef(SimpleSlaveryDefOf.Enslaved);
            if (enslaved != null)
                pawn.health.RemoveHediff(enslaved);
        }
    }
}