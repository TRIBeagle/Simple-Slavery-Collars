// SimpleSlaveryCollars | Patches | Patch_SocialCardUtility_DrawPawnRoleSelection.cs
// 목적   : SocialCard 화면에서 Colony 노예 Pawn도 역할(Role) 버튼을 표시 가능하게 확장
// 용도   : Harmony Postfix 패치
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더 및 요약 주석 재작성
// 주의   : SlaveryStage/AssignSlave 옵션 활성화 시에만 적용됨

using System;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace SimpleSlaveryCollars.Patches
{
    /// <summary>
    /// SocialCardUtility.DrawPawnRoleSelection 후처리 패치.
    /// - 노예 Pawn도 조건 충족 시 역할 선택 버튼을 노출한다.
    /// - 버튼 클릭 시 Ritual 창을 통해 역할 부여/해임 절차를 진행한다.
    /// </summary>
    [HarmonyPatch(typeof(SocialCardUtility), "DrawPawnRoleSelection")]
    public static class Patch_SocialCardUtility_DrawPawnRoleSelection
    {
        private static readonly FieldInfo _cachedRolesField =
            AccessTools.Field(typeof(SocialCardUtility), "cachedRoles");
        private static readonly FieldInfo _buttonSizeField =
            AccessTools.Field(typeof(SocialCardUtility), "RoleChangeButtonSize");

        // [성능] RoleChangeButtonSize는 static readonly 상수이므로 1회만 읽어 캐시
        private static readonly Vector2 _buttonSizeCached =
            _buttonSizeField != null
                ? (Vector2)_buttonSizeField.GetValue(null)
                : new Vector2(200f, 25f);

        // cachedRoles는 RimWorld이 매 프레임 갱신하는 가변 리스트이므로 매번 읽어야 함
        static List<Precept_Role> CachedRoles =>
            (List<Precept_Role>)_cachedRolesField.GetValue(null);
        static Vector2 RoleChangeButtonSize => _buttonSizeCached;

        [HarmonyPostfix]
        public static void Postfix_DrawPawnRoleSelection(Pawn pawn, Rect rect)
        {
            try
            {
                // [Safety] 리플렉션 필드가 null이면 패치 무효화 (RimWorld 버전 변경 대응)
                if (_cachedRolesField == null || _buttonSizeField == null)
                    return;

                // [Safety] 모드 설정 및 노예 조건 확인
                if (!SimpleSlaveryCollarsSetting.SlavestageEnable
                    || !SimpleSlaveryCollarsSetting.AssignSlaveEnable
                    || !pawn.IsSlaveOfColony)
                    return;

                var currentRole = pawn.Ideo?.GetRole(pawn);
                var primaryIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
                bool active = CachedRoles.Count > 0 && pawn.Ideo != null;

                // 버튼 위치 계산
                float y = rect.y + rect.height / 2f - 14f;
                Vector2 size = RoleChangeButtonSize;
                Rect buttonRect = new Rect(rect.width - 150f, y, size.x, size.y);
                buttonRect.xMax = rect.width - 26f - 4f;

                if (!active) GUI.color = Color.gray;

                if (Widgets.ButtonText(buttonRect, "ChooseRole".Translate() + "...",
                                       drawBackground: true, doMouseoverSound: true, active: active))
                {
                    var ritual = (Precept_Ritual)pawn.Ideo.GetPrecept(PreceptDefOf.RoleChange);
                    TargetInfo ritualTarget = ritual?.targetFilter
                                              .BestTarget(pawn, TargetInfo.Invalid)
                                          ?? TargetInfo.Invalid;
                    if (!ritualTarget.IsValid)
                    {
                        Messages.Message(
                            (Find.IdeoManager.classicMode
                                ? "AbilityDisabledNoRitualSpot"
                                : "AbilityDisabledNoAltarIdeogramOrRitualsSpot"
                            ).Translate(),
                            pawn,
                            MessageTypeDefOf.RejectInput
                        );
                        GUI.color = Color.white;
                        return;
                    }

                    var options = new List<FloatMenuOption>();

                    // 현재 역할 해임
                    if (currentRole != null)
                    {
                        options.Add(new FloatMenuOption(
                            "RemoveCurrentRole".Translate(),
                            () =>
                            {
                                var dlg = (Dialog_BeginRitual)ritual.GetRitualBeginWindow(
                                    ritualTarget,
                                    null,
                                    null,
                                    pawn,
                                    new Dictionary<string, Pawn> { { "role_changer", pawn } }
                                );
                                dlg.SetRoleToChangeTo(null);
                                Find.WindowStack.Add(dlg);
                            },
                            Widgets.PlaceholderIconTex,
                            Color.white
                        ));
                    }

                    // 새 역할 할당
                    foreach (var role in CachedRoles)
                    {
                        bool canSelect = role != currentRole
                                         && role.Active
                                         && role.RequirementsMet(pawn)
                                         && (!role.def.leaderRole || pawn.Ideo == primaryIdeo);

                        string label2 = role.LabelForPawn(pawn) + (pawn.Ideo.classicMode ? "" : $" ({role.def.label})");

                        var chosenPawn = role.ChosenPawnSingle();
                        if (chosenPawn != null && chosenPawn != pawn)
                        {
                            label2 += ": " + chosenPawn.LabelShort;
                            canSelect = false;
                        }
                        else if (!role.RequirementsMet(pawn))
                        {
                            var unmet = role.GetFirstUnmetRequirement(pawn);
                            if (unmet != null)
                                label2 += ": " + unmet.GetLabel(role).CapitalizeFirst();
                            canSelect = false;
                        }
                        else if (!role.Active && role.def.activationBelieverCount > role.ideo.ColonistBelieverCountCached)
                        {
                            label2 += ": " + "InactiveRoleRequiresMoreBelievers".Translate(
                                role.def.activationBelieverCount,
                                role.ideo.memberName,
                                role.ideo.ColonistBelieverCountCached).CapitalizeFirst();
                            canSelect = false;
                        }

                        if (canSelect)
                        {
                            options.Add(new FloatMenuOption(
                                label2,
                                () =>
                                {
                                    var dlg = (Dialog_BeginRitual)ritual.GetRitualBeginWindow(
                                        ritualTarget,
                                        null,
                                        null,
                                        pawn,
                                        new Dictionary<string, Pawn> { { "role_changer", pawn } }
                                    );
                                    dlg.SetRoleToChangeTo(role);
                                    Find.WindowStack.Add(dlg);
                                },
                                role.Icon,
                                role.ideo.Color
                            ));
                        }
                        else
                        {
                            options.Add(new FloatMenuOption(
                                label2,
                                null,
                                role.Icon,
                                role.ideo.Color
                            ));
                        }
                    }

                    if (options.Count == 0)
                    {
                        Messages.Message("SimpleSlaveryCollars_NoAssignableRole".Translate(),
                                         pawn,
                                         MessageTypeDefOf.RejectInput);
                        GUI.color = Color.white;
                        return;
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }

                GUI.color = Color.white;
            }
            catch (Exception ex)
            {
                Log.Error($"[SSC] Patch_SocialCardUtility_DrawPawnRoleSelection.Postfix_DrawPawnRoleSelection 오류: {ex}");
            }
        }
    }
}
