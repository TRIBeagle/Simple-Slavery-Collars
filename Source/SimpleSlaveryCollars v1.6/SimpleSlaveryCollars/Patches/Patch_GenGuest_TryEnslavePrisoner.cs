// SimpleSlaveryCollars | Patches | Patch_GenGuest_TryEnslavePrisoner.cs
// 목적 : 노예화 성공 시 족쇄 기본값 적용
// 용도 : ShacklesDefault 옵션이 false면 CompSlave.ShackledGoal을 false로 초기화
// 주의 : Hediff 추가는 더 이상 필요하지 않음 — CompSlave가 상시 존재

using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// GenGuest.TryEnslavePrisoner 후처리.
    /// 족쇄 기본값만 처리. CompSlave 직접 접근.
    /// </summary>
    [HarmonyPatch(typeof(GenGuest), "TryEnslavePrisoner")]
    public static class Patch_GenGuest_TryEnslavePrisoner
    {
        [HarmonyPostfix]
        public static void TryEnslavePrisoner_Postfix(bool __result, Pawn warden, Pawn prisoner)
        {
            try
            {
                if (!__result || prisoner == null) return;

                // 족쇄 기본값: 설정이 OFF면 ShackledGoal을 false로
                if (!SimpleSlaveryCollarsSetting.ShacklesDefault)
                {
                    var comp = prisoner.GetComp<CompSlave>();
                    if (comp != null)
                        comp.ShackledGoal = false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_GenGuest_TryEnslavePrisoner error: {ex}");
            }
        }
    }
}
