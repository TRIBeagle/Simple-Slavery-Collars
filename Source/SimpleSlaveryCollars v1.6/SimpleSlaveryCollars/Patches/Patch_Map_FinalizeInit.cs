// SimpleSlaveryCollars | Patches | Patch_Map_FinalizeInit.cs
// 목적   : 맵 로드 완료 시 해당 맵의 Pawn 칼라를 SlaveCollarRegistry에 일괄 재등록
// 용도   : 맵 전환(새 맵 생성, 방문자 맵 등) 후 레지스트리 일관성 유지
// 주의   : GetSlaveCollar() 폴백이 개별 재등록하므로 이 패치는 성능 보조(일괄 사전 등록)

using HarmonyLib;
using SimpleSlaveryCollars.Utilities;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// 맵 FinalizeInit 시 해당 맵의 Pawn 칼라를 SlaveCollarRegistry에 재등록.
    /// </summary>
    [HarmonyPatch(typeof(Map), "FinalizeInit")]
    public static class Patch_Map_FinalizeInit
    {
        [HarmonyPostfix]
        public static void Postfix(Map __instance)
        {
            var pawns = __instance.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                var pawn = pawns[i];
                if (pawn?.apparel == null) continue;
                var worn = pawn.apparel.WornApparel;
                for (int j = 0; j < worn.Count; j++)
                {
                    if (worn[j] is SlaveApparel sa && SimpleSlaveryUtility.IsSlaveCollar(sa))
                    {
                        SlaveCollarRegistry.Register(pawn, sa);
                        break;
                    }
                }
            }
        }
    }
}
