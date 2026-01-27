// SimpleSlaveryCollars | Patches | Patch_Pawn_GuestTracker_SetGuestStatus_RemoveEnslavedHediff.cs
// 목적   : DevMode/특수 경로로 Slave 상태가 해제될 때 Enslaved Hediff가 남는 문제 방지
// 용도   : Harmony Postfix 패치
// 정책   : "플레이어가 호스트였던 Slave"가 "Slave가 아닌 상태"로 바뀌면 Enslaved 제거
// 주의   : GenGuest.EmancipateSlave 패치와 중복되어도 안전(없으면 아무 것도 안 함)

using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    [HarmonyPatch(typeof(Pawn_GuestTracker), "SetGuestStatus")]
    public static class Patch_Pawn_GuestTracker_SetGuestStatus_RemoveEnslavedHediff
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn_GuestTracker __instance, Faction newHost, GuestStatus guestStatus, Pawn ___pawn)
        {
            if (___pawn == null) return;
            if (___pawn.Dead || ___pawn.DestroyedOrNull()) return;

            // "Slave가 아닌 상태"로 바뀌는 경우에만
            if (guestStatus == GuestStatus.Slave) return;

            // 플레이어가 관여한 노예만 건드리기 (타 팩션/퀘스트 pawn 오염 방지)
            // - 디버그 AddGuest/AddSlave는 newHost가 플레이어로 들어오는 편
            // - 일반 해방은 GenGuest 패치가 처리하지만, 여기서도 안전하게 커버
            bool playerContext =
                newHost == Faction.OfPlayer ||
                (__instance != null && __instance.SlaveFaction == Faction.OfPlayer) ||
                (___pawn.Faction != null && ___pawn.Faction.IsPlayer);

            if (!playerContext) return;

            var hs = ___pawn.health?.hediffSet;
            if (hs == null) return;
            if (SSC_HediffDefOf.Enslaved == null) return;

            var enslaved = hs.GetFirstHediffOfDef(SSC_HediffDefOf.Enslaved);
            if (enslaved != null)
                ___pawn.health.RemoveHediff(enslaved);
        }
    }
}
