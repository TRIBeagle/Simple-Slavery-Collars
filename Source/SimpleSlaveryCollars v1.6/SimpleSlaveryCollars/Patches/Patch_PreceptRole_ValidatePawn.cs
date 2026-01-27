// SimpleSlaveryCollars | Patches | Patch_PreceptRole_ValidatePawn.cs
// 목적   : Precept_Role.ValidatePawn에서 Stage5 노예를 예외 허용하여 역할/작업 제한을 해제
// 용도   : Harmony Postfix 패치로 바닐라의 "플레이어 노예 배제" 로직을 Stage5에 한해 우회
// 변경   : 2026-01-27 Stage5 조건 명시 — IsFreeColonist 기반(우연 통과) 제거, SlaveUtility.IsStage5Slave(p)로 고정
// 주의   : __result가 false인 경우에만 true로 뒤집음(기존 성공 케이스는 건드리지 않음)

using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// Precept_Role.ValidatePawn 후처리 패치.
    /// - 모드 옵션이 활성화되고 Pawn이 Stage5 노예일 경우, 바닐라의 제한을 우회하여 역할 후보로 인정한다.
    /// </summary>
    [HarmonyPatch(typeof(Precept_Role), "ValidatePawn")]
    public static class Patch_PreceptRole_ValidatePawn
    {
        /// <summary>
        /// Postfix: 바닐라에서 거부(__result==false)된 Pawn이라도 Stage5 노예면 RequirementsMet을 만족하는 경우 true로 승인.
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(Precept_Role __instance, Pawn p, ref bool __result)
        {
            if (__result) return; // 이미 통과면 손대지 않음

            if (!SimpleSlaveryCollarsSetting.SlavestageEnable ||
                !SimpleSlaveryCollarsSetting.RebelCycleChangeEnable ||
                !SimpleSlaveryCollarsSetting.Stage5SlaveWorkUnlockEnable)
                return;

            if (p == null) return;
            if (p.DestroyedOrNull()) return;
            if (p.Dead) return;

            if (!SlaveUtility.IsStage5Slave(p)) return;

            if (!__instance.RequirementsMet(p)) return;

            __result = true;
        }
    }
}
