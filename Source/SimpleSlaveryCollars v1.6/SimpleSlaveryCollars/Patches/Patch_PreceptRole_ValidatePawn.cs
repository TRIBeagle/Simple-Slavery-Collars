// SimpleSlaveryCollars | Patches | Patch_PreceptRole_ValidatePawn.cs
// 목적   : Precept_Role.ValidatePawn 실행 시 노예 Pawn도 역할(Precept Role) 부여 가능하도록 허용
// 용도   : Harmony Postfix 패치로 조건 충족 시 __result 강제 true 반환
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 요약 재작성
// 주의   : SlaveryStage/RebelCycle/AssignSlave 옵션 모두 ON일 때만 발동
// 저장   : 역할 부여 결과는 Pawn 세이브에 간접 반영됨

using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// Precept_Role.ValidatePawn 후처리 패치.
    /// - 옵션 활성화 시 노예 Pawn도 역할 배정 가능
    /// - RequirementsMet 조건 만족 시 __result 강제 true
    /// </summary>
    [HarmonyPatch(typeof(Precept_Role), "ValidatePawn")]
    public static class Patch_PreceptRole_ValidatePawn
    {
        /// <summary>
        /// Postfix: 노예 Pawn도 조건 만족 시 역할 부여 허용.
        /// </summary>
        [HarmonyPostfix]
        public static void ValidatePawn_Patch(ref bool __result, Pawn p, Precept_Role __instance)
        {
            if (SimpleSlaveryCollarsSetting.SlavestageEnable == false ||
                SimpleSlaveryCollarsSetting.RebelCycleChangeEnable == false ||
                SimpleSlaveryCollarsSetting.AssignSlaveEnable == false)
                return;

            if (!__result &&
                p.Faction != null &&
                (!p.Faction.IsPlayer || p.IsFreeColonist) &&
                !p.Destroyed &&
                !p.Dead &&
                __instance.RequirementsMet(p))
            {
                __result = true;
            }
        }
    }
}
