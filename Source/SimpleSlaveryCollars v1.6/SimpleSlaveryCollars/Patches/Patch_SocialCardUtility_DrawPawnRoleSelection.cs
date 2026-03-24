// SimpleSlaveryCollars | Patches | Patch_SocialCardUtility_DrawPawnRoleSelection.cs
// 목적 : SocialCard 화면에서 Colony 노예 Pawn도 역할(Role) 버튼을 사용 가능하게 확장
// 용도 : Harmony Transpiler로 IsFreeNonSlaveColonist 가드를 커스텀 판정으로 교체
// 주의 : SSC 설정(SlavestageEnable + AssignSlaveEnable) 활성화 시에만 노예 허용

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// SocialCardUtility.DrawPawnRoleSelection Transpiler 패치.
    /// - IsFreeNonSlaveColonist 호출을 IsEligibleForRoleSelection으로 교체
    /// - 바닐라 UI 로직은 그대로 유지하며, 진입 조건만 완화
    /// </summary>
    [HarmonyPatch(typeof(SocialCardUtility), "DrawPawnRoleSelection")]
    public static class Patch_SocialCardUtility_DrawPawnRoleSelection
    {
        /// <summary>
        /// IsFreeNonSlaveColonist 호출을 커스텀 판정으로 교체.
        /// </summary>
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsFreeNonSlaveColonist));
            var replacement = AccessTools.Method(
                typeof(Patch_SocialCardUtility_DrawPawnRoleSelection),
                nameof(IsEligibleForRoleSelection));

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
                Log.Warning("[SSC] Transpiler: DrawPawnRoleSelection에서 IsFreeNonSlaveColonist 호출을 찾지 못함.");
        }

        /// <summary>
        /// FreeNonSlaveColonist 또는 SSC 설정 ON인 Colony 노예면 true.
        /// </summary>
        public static bool IsEligibleForRoleSelection(Pawn pawn)
        {
            if (pawn.IsFreeNonSlaveColonist) return true;
            return SimpleSlaveryCollarsSetting.SlavestageEnable
                && SimpleSlaveryCollarsSetting.AssignSlaveEnable
                && pawn.IsSlaveOfColony;
        }
    }
}
