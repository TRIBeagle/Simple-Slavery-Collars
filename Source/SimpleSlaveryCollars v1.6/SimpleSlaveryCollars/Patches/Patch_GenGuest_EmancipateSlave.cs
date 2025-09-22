// SimpleSlaveryCollars | Patches | Patch_GenGuest_EmancipateSlave.cs
// 목적   : RimWorld 기본 로직 GenGuest.EmancipateSlave 실행 시 노예 헤디프(Enslaved)를 자동 제거
// 용도   : Harmony Postfix 패치로 자유화 직후 Pawn 상태를 정상화
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 요약 재작성
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
        public static void EmancipateSlave_Patch(ref Pawn slave)
        {
            var enslaved = slave.health.hediffSet.GetFirstHediffOfDef(SSC_HediffDefOf.Enslaved);
            if (enslaved != null)
                slave.health.RemoveHediff(enslaved);
        }
    }
}
