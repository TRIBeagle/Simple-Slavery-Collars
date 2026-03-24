// SimpleSlaveryCollars | Patches | Patch_Pawn_GetGizmos.cs
// 목적   : Pawn.GetGizmos 실행 시 노예 칼라(Apparel)의 추가 Gizmo 버튼을 병합
// 용도   : Harmony Postfix 패치로 Colony Pawn이 착용한 SlaveApparel의 Gizmo를 노출
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 요약 재작성
// 주의   : Colony Pawn만 적용, Colonist/Prisoner는 제외
// 성능   : Pawn.apparel.WornApparel 순회. 보통 5개 이하 아이템이라 부담 미미

using HarmonyLib;
using System.Collections.Generic;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// Pawn.GetGizmos 후처리 패치.
    /// - Colony Pawn이 착용 중인 SlaveApparel의 Gizmo를 추가 노출
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class Patch_Pawn_GetGizmos
    {
        /// <summary>
        /// Postfix: 원래 Gizmos + SlaveGizmos 병합. LINQ Concat 제거 → yield 체이닝.
        /// </summary>
        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            if (__result != null)
            {
                foreach (var g in __result)
                    yield return g;
            }

            if (!SimpleSlaveryUtility.IsColonyMember(__instance))
                yield break;

            if (__instance.apparel == null)
                yield break;

            var worn = __instance.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                if (worn[i] is SlaveApparel slaveApparel)
                {
                    foreach (var g in slaveApparel.SlaveGizmos())
                        yield return g;
                }
            }
        }
    }
}
