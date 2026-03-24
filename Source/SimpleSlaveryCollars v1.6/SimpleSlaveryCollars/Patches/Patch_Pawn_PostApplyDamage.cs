// SimpleSlaveryCollars | Patches | Patch_Pawn_PostApplyDamage.cs
// 목적 : Pawn이 EMP 데미지를 받으면 착용 중인 칼라를 일시 비활성화
// 용도 : Harmony Postfix 패치. EMP 옵션 ON일 때만 작동
// 주의 : DamageDef.isEMP 체크. 바닐라 EMP stun 기본 틱(180) 사용

using System;
using HarmonyLib;
using RimWorld;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// Pawn.PostApplyDamage Postfix.
    /// EMP 데미지 시 착용 칼라에 ApplyEmp() 호출.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "PostApplyDamage")]
    public static class Patch_Pawn_PostApplyDamage
    {
        private const int DefaultEmpDuration = 180; // 바닐라 EMP stun 기본 틱 (3초)

        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, DamageInfo dinfo)
        {
            try
            {
                if (!SimpleSlaveryCollarsSetting.CollarDisruptionEnable) return;
                if (dinfo.Def != DamageDefOf.EMP) return;

                var collar = SimpleSlaveryUtility.GetSlaveCollar(__instance) as SlaveApparel;
                if (collar == null) return;

                collar.ApplyEmp(DefaultEmpDuration);
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_Pawn_PostApplyDamage.Postfix 오류: {ex}");
            }
        }
    }
}
