// SimpleSlaveryCollars | Patches | Patch_StatPartSlave_ActiveFor.cs
// 목적   : Stage5 노예( x ≥ SlaveStage4 && !Steadfast )일 경우 글로벌 작업속도 디버프를 제거
// 용도   : Harmony Postfix 패치
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — Stage5 조건 및 Steadfast 예외 명시
// 주의   : Stage5 = (x ≥ SlaveStage4 && !Steadfast), 그 외는 디버프 유지

using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// StatPart_Slave.ActiveFor 후처리 패치.
    /// - Stage5 노예는 작업 속도 디버프를 받지 않는다.
    /// </summary>
    [HarmonyPatch(typeof(StatPart_Slave), "ActiveFor")]
    public static class Patch_StatPartSlave_ActiveFor
    {
        [HarmonyPostfix]
        public static void ActiveFor_Patch(Thing t, ref bool __result)
        {
            if (!SimpleSlaveryCollarsSetting.SlavestageEnable
                || !SimpleSlaveryCollarsSetting.RebelCycleChangeEnable
                || !SimpleSlaveryCollarsSetting.RemoveWorkspeedDebuffEnable)
                return;

            if (t is Pawn pawn
                && SlaveUtility.TimeAsSlave(pawn) >= SlaveUtility.SlaveStage4
                && !SlaveUtility.IsSteadfast(pawn))
            {
                __result = false;
            }
        }
    }
}
