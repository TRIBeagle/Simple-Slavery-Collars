// SimpleSlaveryCollars | Patches | Patch_GenGuest_SlaveRelease.cs
// 목적   : RimWorld 기본 로직 GenGuest.SlaveRelease 실행 시 노예 해방된 Pawn의 기억 처리 변경
// 용도   : Harmony Postfix 패치로 해방 직후 Pawn 상태를 정상화
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 요약 재작성
// 주의   : Stage5 = ( x ≥ SlaveStage4 ) && !Steadfast / Stage4 = (SlaveStage3 < x < SlaveStage4) 또는 ( x ≥ SlaveStage4 && Steadfast )
// 저장   : ThoughtMemories 변경은 Pawn 세이브에 직접 반영됨

using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// GenGuest.SlaveRelease 후처리 패치.
    /// - 해방 Pawn이 Stage5( x ≥ SlaveStage4 이면서 Steadfast 아님 )면
    ///   기존 "WasEnslaved" 기억을 제거하고 "WasEnslaved_Assimilation" 기억을 부여한다.
    /// </summary>
    [HarmonyPatch(typeof(GenGuest), "SlaveRelease")]
    public static class Patch_GenGuest_SlaveRelease
    {
        /// <summary>
        /// Postfix: 해방 Pawn이 Stage5 조건을 만족하면 동화 기억 부여.
        /// </summary>
        [HarmonyPostfix]
        public static void SlaveRelease_Patch(Pawn p)
        {
            if (SlaveUtility.TimeAsSlave(p) >= SlaveUtility.SlaveStage4 &&
                !SlaveUtility.IsSteadfast(p) &&
                p.Faction == Faction.OfPlayer &&
                p.needs.mood != null)
            {
                p.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.WasEnslaved);
                p.needs.mood.thoughts.memories.TryGainMemory(SSC_ThoughtDefOf.WasEnslaved_Assimilation);
            }
        }
    }
}
