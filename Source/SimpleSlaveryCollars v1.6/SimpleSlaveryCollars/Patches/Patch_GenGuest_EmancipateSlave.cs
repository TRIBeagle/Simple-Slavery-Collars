// SimpleSlaveryCollars | Patches | Patch_GenGuest_EmancipateSlave.cs
// 목적   : RimWorld 기본 로직 GenGuest.EmancipateSlave 실행 시 노예 헤디프(Enslaved)를 자동 제거
// 용도   : Harmony Postfix 패치로 자유화 직후 Pawn 상태를 정상화
// 변경   : 2026-01-27 버그 수정 — [HarmonyPostfix] 누락 추가 + 파라미터 바인딩 안정화(ref 제거)
// 주의   : Pawn에 Enslaved Hediff가 존재할 경우에만 제거 호출

using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// GenGuest.EmancipateSlave 후처리 패치.
    /// - EmancipateSlave 실행 시 Enslaved 헤디프가 있으면 제거한다.
    /// </summary>
    [HarmonyPatch(typeof(GenGuest), "EmancipateSlave")]
    public static class Patch_GenGuest_EmancipateSlave
    {
        /// <summary>
        /// Postfix: 노예 해방 시 Enslaved Hediff 제거.
        /// </summary>
        [HarmonyPostfix]
        public static void EmancipateSlave_Postfix(Pawn warden, Pawn slave)
        {
            if (slave == null) return;

            var hs = slave.health?.hediffSet;
            if (hs == null) return;

            var enslaved = hs.GetFirstHediffOfDef(SimpleSlaveryDefOf.Enslaved);
            if (enslaved != null)
                slave.health.RemoveHediff(enslaved);
        }
    }
}
