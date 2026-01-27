// SimpleSlaveryCollars | Patches | Patch_Pawn_GuestTracker_SetGuestStatus_EnsureEnslavedHediff.cs
// 목적   : DevMode(AddGuest/AddSlave) 등 SetGuestStatus로 Slave 상태만 박는 경로에서 Enslaved Hediff 누락 방지
// 용도   : Harmony Postfix 패치
// 변경   : 2026-01-28 디버그/특수경로 보강 — "플레이어가 호스트로 설정된 Slave"면 Enslaved Hediff 보장
// 주의   : 기존 GenGuest.TryEnslavePrisoner 패치(정상 노예화)와 중복 호출되어도 HasHediff로 안전

using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    [HarmonyPatch(typeof(Pawn_GuestTracker), "SetGuestStatus")]
    public static class Patch_Pawn_GuestTracker_SetGuestStatus_EnsureEnslavedHediff
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn_GuestTracker __instance, Faction newHost, GuestStatus guestStatus, Pawn ___pawn)
        {
            if (guestStatus != GuestStatus.Slave) return;
            if (newHost != Faction.OfPlayer) return;           // 디버그 AddGuest/AddSlave 커버 핵심
            if (___pawn == null) return;
            if (___pawn.Dead || ___pawn.DestroyedOrNull()) return;
            if (!___pawn.RaceProps.Humanlike) return;

            var hs = ___pawn.health?.hediffSet;
            if (hs == null) return;

            if (SSC_HediffDefOf.Enslaved == null) return;      // DefOf 초기화 안전
            if (hs.HasHediff(SSC_HediffDefOf.Enslaved)) return;

            ___pawn.health.AddHediff(SSC_HediffDefOf.Enslaved);

            // ShacklesDefault 옵션 반영(GenGuest 패치와 동일 정책 유지)
            if (SimpleSlaveryCollarsSetting.ShacklesDefault == false)
            {
                var enslaved = SlaveUtility.GetEnslavedHediff(___pawn);
                if (enslaved != null)
                    enslaved.shackledGoal = false;
            }
        }
    }
}
