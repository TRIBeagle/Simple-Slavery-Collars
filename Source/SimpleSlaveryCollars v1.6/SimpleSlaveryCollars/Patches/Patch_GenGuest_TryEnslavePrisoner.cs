// SimpleSlaveryCollars | Patches | Patch_GenGuest_TryEnslavePrisoner.cs
// 목적 : 노예화 성공 시 족쇄 기본값 적용
// 용도 : ShacklesDefault 옵션이 false면 shackledGoal을 false로 초기화
// 주의 : Hediff 추가는 SetGuestStatus 패치에서 처리 — 여기서는 족쇄만 담당

using System;
using HarmonyLib;
using RimWorld;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// GenGuest.TryEnslavePrisoner 후처리.
    /// Hediff 부여는 SetGuestStatus 패치가 담당. 여기서는 족쇄 기본값만 처리.
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

                // 족쇄 기본값: 설정이 OFF면 shackledGoal을 false로
                if (!SimpleSlaveryCollarsSetting.ShacklesDefault)
                {
                    var enslaved = SimpleSlaveryUtility.GetEnslavedHediff(prisoner);
                    if (enslaved != null)
                        enslaved.shackledGoal = false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_GenGuest_TryEnslavePrisoner 오류: {ex}");
            }
        }
    }
}
