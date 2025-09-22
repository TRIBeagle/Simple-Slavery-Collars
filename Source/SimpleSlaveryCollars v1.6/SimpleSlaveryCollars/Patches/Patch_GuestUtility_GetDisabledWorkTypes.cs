// SimpleSlaveryCollars | Patches | Patch_GuestUtility_GetDisabledWorkTypes.cs
// 목적   : GuestUtility.GetDisabledWorkTypes 실행 시 Stage5 노예의 작업 제한 해제
// 용도   : Harmony Postfix 패치로 Stage5 노예를 Colonist와 동일하게 취급
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 요약 재작성
// 주의   : Slavery Stage 관련 모드 설정이 모두 활성화된 경우에만 적용
// 저장   : WorkType 제한 해제 여부는 Pawn 세이브에 간접 반영됨

using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// GuestUtility.GetDisabledWorkTypes 후처리 패치.
    /// - Stage5 노예(Colonist 취급)일 경우 노예 전용 작업 제한 해제
    /// </summary>
    [HarmonyPatch(typeof(GuestUtility), "GetDisabledWorkTypes")]
    public static class Patch_GuestUtility_GetDisabledWorkTypes
    {
        /// <summary>
        /// Postfix: Stage5 노예면 disabledForSlaves 작업 제한을 제거.
        /// </summary>
        static void Postfix(Pawn_GuestTracker guest, ref List<WorkTypeDef> __result)
        {
            if (!SimpleSlaveryCollarsSetting.SlavestageEnable ||
                !SimpleSlaveryCollarsSetting.RebelCycleChangeEnable ||
                !SimpleSlaveryCollarsSetting.Stage5SlaveWorkUnlockEnable)
                return;

            Pawn pawn = Traverse.Create(guest).Field("pawn").GetValue<Pawn>();
            if (pawn == null || !SlaveUtility.IsStage5Slave(pawn))
                return;

            __result.RemoveAll(wt => wt.disabledForSlaves);
        }
    }
}
