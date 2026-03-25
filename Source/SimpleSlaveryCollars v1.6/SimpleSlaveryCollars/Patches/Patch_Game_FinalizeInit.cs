// SimpleSlaveryCollars | Patches | Patch_Game_FinalizeInit.cs
// 목적   : 게임 로드/새 게임 시 SlaveCollarRegistry를 초기화하는 Harmony 패치
// 용도   : Game.FinalizeInit Postfix — 이전 세션의 레지스트리 잔여 항목 일괄 제거
// 주의   : 이후 GetSlaveCollar() 폴백에서 자동 재등록됨

using HarmonyLib;
using SimpleSlaveryCollars.Utilities;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// 게임 로드/새 게임 시 SlaveCollarRegistry 초기화.
    /// </summary>
    [HarmonyPatch(typeof(Game), "FinalizeInit")]
    public static class Patch_Game_FinalizeInit
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            SlaveCollarRegistry.Clear();
        }
    }
}
