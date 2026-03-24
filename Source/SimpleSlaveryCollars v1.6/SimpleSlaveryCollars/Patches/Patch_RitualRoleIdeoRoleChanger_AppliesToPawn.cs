// SimpleSlaveryCollars | Patches | Patch_RitualRoleIdeoRoleChanger_AppliesToPawn.cs
// 목적 : RitualRoleIdeoRoleChanger.AppliesToPawn 실행 시 Colony 노예도 의식 역할 배정 가능하도록 확장
// 용도 : Harmony Transpiler로 IsFreeNonSlaveColonist 가드를 커스텀 판정으로 교체
// 주의 : Child/Ideo/Role/PlayerIdeo 등 나머지 바닐라 조건은 원본 로직 그대로 유지

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// RitualRoleIdeoRoleChanger.AppliesToPawn Transpiler 패치.
    /// - IsFreeNonSlaveColonist 호출을 IsEligibleForRitualRole로 교체
    /// - 바닐라의 Child/Ideo/Role 조건은 원본 그대로 실행
    /// </summary>
    [HarmonyPatch(typeof(RitualRoleIdeoRoleChanger), nameof(RitualRoleIdeoRoleChanger.AppliesToPawn))]
    public static class Patch_RitualRoleIdeoRoleChanger_AppliesToPawn
    {
        /// <summary>
        /// IsFreeNonSlaveColonist 호출을 커스텀 판정으로 교체.
        /// </summary>
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsFreeNonSlaveColonist));
            var replacement = AccessTools.Method(
                typeof(Patch_RitualRoleIdeoRoleChanger_AppliesToPawn),
                nameof(IsEligibleForRitualRole));

            if (target == null)
            {
                Log.Warning("[SSC] Transpiler: Pawn.IsFreeNonSlaveColonist getter를 찾을 수 없음. 패치 건너뜀.");
                foreach (var inst in instructions)
                    yield return inst;
                yield break;
            }

            bool patched = false;
            foreach (var inst in instructions)
            {
                if (inst.Calls(target))
                {
                    yield return new CodeInstruction(OpCodes.Call, replacement);
                    patched = true;
                }
                else
                {
                    yield return inst;
                }
            }

            if (!patched)
                Log.Warning("[SSC] Transpiler: AppliesToPawn에서 IsFreeNonSlaveColonist 호출을 찾지 못함.");
        }

        /// <summary>
        /// FreeNonSlaveColonist 또는 Colony 노예면 true.
        /// </summary>
        public static bool IsEligibleForRitualRole(Pawn pawn)
        {
            if (pawn.IsFreeNonSlaveColonist) return true;
            return pawn.IsSlaveOfColony;
        }
    }
}
