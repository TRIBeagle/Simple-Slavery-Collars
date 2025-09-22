// SimpleSlaveryCollars | Patches | Patch_RitualRoleIdeoRoleChanger_AppliesToPawn.cs
// 목적   : RitualRoleIdeoRoleChanger.AppliesToPawn 실행 시 Colony 노예도 의식 역할 배정 가능하도록 확장
// 용도   : Harmony Prefix 패치로 노예 Pawn일 경우 조건 재검증 및 허용
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 요약 재작성
// 주의   : Juvenile/Ideo/Role/Player Ideo 조건은 그대로 유지. 노예만 예외 처리
// 저장   : 역할 배정 여부는 Pawn 세이브에 간접 반영됨

using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// RitualRoleIdeoRoleChanger.AppliesToPawn Prefix 패치.
    /// - Colony 노예 Pawn도 조건을 충족하면 Ritual Role 배정 허용
    /// - Child/Ideo/Role/Player Ideo 조건은 원본 로직을 재검증하여 유지
    /// </summary>
    [HarmonyPatch(typeof(RitualRoleIdeoRoleChanger), nameof(RitualRoleIdeoRoleChanger.AppliesToPawn))]
    public static class Patch_RitualRoleIdeoRoleChanger_AppliesToPawn
    {
        /// <summary>
        /// Juvenile 예외 처리: Biotech 활성화 시 아동 금지 조건 재검증.
        /// </summary>
        private static bool AppliesIfChild_Custom(RitualRole instance, Pawn p, out string reason, bool skipReason = false)
        {
            reason = null;
            if (ModsConfig.BiotechActive && !instance.allowChild && p.DevelopmentalStage.Juvenile())
            {
                if (!skipReason)
                {
                    reason = instance.customChildDisallowMessage ??
                             "MessageRitualRoleCannotBeChild".Translate(instance.Label).CapitalizeFirst();
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Prefix: 노예 Pawn일 경우 커스텀 조건 검증 후 허용/차단 결정.
        /// - Juvenile 여부, Ideo 존재, Role 존재, Player Ideo 여부를 재확인
        /// - 모든 조건을 통과하면 __result=true 후 원본 실행 스킵
        /// - 노예가 아니면 원본 메서드 그대로 실행
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(
            RitualRoleIdeoRoleChanger __instance,
            Pawn p,
            out string reason,
            TargetInfo selectedTarget,
            LordJob_Ritual ritual,
            RitualRoleAssignments assignments,
            Precept_Ritual precept,
            bool skipReason,
            ref bool __result)
        {
            reason = null;

            if (p.IsSlaveOfColony)
            {
                if (!AppliesIfChild_Custom(__instance, p, out reason, skipReason))
                {
                    __result = false;
                    return false;
                }

                if (p.Ideo == null)
                {
                    __result = false;
                    return false;
                }

                if (p.Ideo.GetRole(p) == null &&
                    !RitualUtility.AllRolesForPawn(p).Any(r => r.RequirementsMet(p)))
                {
                    reason = "MessageRitualNoRolesAvailable".Translate(p);
                    __result = false;
                    return false;
                }

                if (!Faction.OfPlayer.ideos.Has(p.Ideo))
                {
                    reason = "MessageRitualNotOfPlayerIdeo".Translate(p);
                    __result = false;
                    return false;
                }

                __result = true;
                return false;
            }

            return true; // 노예가 아닐 경우 원본 실행
        }
    }
}
