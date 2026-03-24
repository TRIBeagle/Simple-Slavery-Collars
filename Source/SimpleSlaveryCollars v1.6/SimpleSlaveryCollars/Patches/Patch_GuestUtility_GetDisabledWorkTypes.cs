// SimpleSlaveryCollars | Patches | Patch_GuestUtility_GetDisabledWorkTypes.cs
// 목적 : Stage5 노예의 작업 제한 해제. disabledForSlaves 목록에서 해당 항목 제거
// 용도 : Harmony Postfix 패치. SlavestageEnable + Stage5SlaveWorkUnlockEnable 옵션 활성화 시 적용

using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Patches
{
    [HarmonyPatch(typeof(GuestUtility), "GetDisabledWorkTypes")]
    public static class Patch_GuestUtility_GetDisabledWorkTypes
    {
        // 클래스 레벨에 고속 필드 접근자(Delegate) 캐싱
        private static readonly AccessTools.FieldRef<Pawn_GuestTracker, Pawn> pawnRef = 
            AccessTools.FieldRefAccess<Pawn_GuestTracker, Pawn>("pawn");

        static void Postfix(Pawn_GuestTracker guest, ref List<WorkTypeDef> __result)
        {
            if (!SimpleSlaveryCollarsSetting.SlavestageEnable ||
                !SimpleSlaveryCollarsSetting.RebelCycleChangeEnable ||
                !SimpleSlaveryCollarsSetting.Stage5SlaveWorkUnlockEnable)
                return;

            // Traverse 대신 캐싱된 델리게이트 사용 (성능 대폭 향상)
            Pawn pawn = pawnRef(guest);
            
            if (pawn == null || !SimpleSlaveryUtility.IsStage5Slave(pawn))
                return;

            __result.RemoveAll(wt => wt.disabledForSlaves);
        }
    }
}