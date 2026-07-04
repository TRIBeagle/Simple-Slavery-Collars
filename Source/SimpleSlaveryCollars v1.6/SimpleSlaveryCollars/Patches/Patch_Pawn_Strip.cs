// SimpleSlaveryCollars | Patches | Patch_Pawn_Strip.cs
// 목적   : Pawn.Strip 실행 시 착용 중인 SlaveCollar를 자동 해제/무력화
// 용도   : Harmony Prefix 패치로 스트립 직전 칼라를 안전 상태로 전환
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 요약 재작성
// 주의   : Crypto/Electric 칼라는 스트립 시 강제로 disarm. Crypto는 정신상태 복원도 수행
// 저장   : armed 상태 변경은 Pawn 세이브에 직접 반영됨

using System;
using HarmonyLib;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// Pawn.Strip Prefix 패치.
    /// - 스트립 시 SlaveCollar 무력화
    /// - CryptoCollar: armed=false + 정신상태 복원
    /// - ElectricCollar: armed=false
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "Strip")]
    public static class Patch_Pawn_Strip
    {
        /// <summary>
        /// Prefix: Pawn 스트립 시 착용 칼라 상태를 해제.
        /// </summary>
        [HarmonyPrefix]
        public static void Strip_Patch(ref Pawn __instance)
        {
            try
            {
                // 종류별 스트립 처리는 SlaveApparel.NotifyStripped 오버라이드로 위임
                // (폭발=유지, 감전=해제, 크립토=해제+정신상태 복원)
                var collar = SimpleSlaveryUtility.GetSlaveCollar(__instance) as SlaveApparel;
                collar?.NotifyStripped(__instance);
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_Pawn_Strip.Strip_Patch error: {ex}");
            }
        }
    }
}
