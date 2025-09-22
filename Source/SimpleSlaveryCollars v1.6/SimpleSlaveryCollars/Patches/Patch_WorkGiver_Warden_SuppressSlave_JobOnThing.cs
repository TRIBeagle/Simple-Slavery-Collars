// SimpleSlaveryCollars | Patches | Patch_WorkGiver_Warden_SuppressSlave_JobOnThing.cs
// 목적   : Warden이 자기 자신(Pawn == TargetPawn)을 억압 대상으로 잘못 선택하는 경우를 차단
// 용도   : Harmony Prefix 패치
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/요약 주석 재작성
// 주의   : 자기 자신 대상일 때는 항상 Job=null 반환, 원본 로직은 실행되지 않음

using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// WorkGiver_Warden_SuppressSlave.JobOnThing Prefix 패치.
    /// - Warden이 자기 자신을 억압 대상으로 잡는 경우를 무효화한다.
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_Warden_SuppressSlave), "JobOnThing")]
    public static class Patch_WorkGiver_Warden_SuppressSlave_JobOnThing
    {
        static bool Prefix(Pawn pawn, Thing t, ref Job __result)
        {
            if (pawn.IsSlaveOfColony && t is Pawn targetPawn && pawn == targetPawn)
            {
                __result = null;
                return false; // 원본 실행 차단
            }
            return true;
        }
    }
}
