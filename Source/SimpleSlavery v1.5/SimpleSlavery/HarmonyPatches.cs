using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace SimpleSlaveryCollars
{
    // Mood buff when releasing slaves, the player's faction.
    [HarmonyPatch(typeof(GenGuest), "SlaveRelease")]
    public static class GenGuest_SlaveRelease_Patch
    {
        [HarmonyPostfix]
        public static void SlaveRelease_Patch(Pawn p)
        {
            if (SlaveUtility.TimeAsSlave(p) >= SlaveUtility.SlaveStage4 && !SlaveUtility.IsSteadfast(p) && p.Faction == Faction.OfPlayer && p.needs.mood != null)
            {
                p.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.WasEnslaved);
                p.needs.mood.thoughts.memories.TryGainMemory(SS_ThoughtDefOf.WasEnslaved_Assimilation);
            }
        }
    }
    // Change the slave's faction to the player's faction when the slave stage reaches 5.
    [HarmonyPatch(typeof(Pawn_GuestTracker), "SetGuestStatus")]
    public static class Pawn_GuestTracker_SetGuestStatus_Patch
    {
        [HarmonyPostfix]
        public static void SetGuestStatus_Patch(Pawn_GuestTracker __instance, ref Faction ___slaveFactionInt, Pawn ___pawn)
        {
            if (SimpleSlaveryCollarsSetting.SlavestageEnable == false || SimpleSlaveryCollarsSetting.AssimilationSlaveEnable == false)
                return;
            if (SlaveUtility.TimeAsSlave(___pawn) >= SlaveUtility.SlaveStage4 && !SlaveUtility.IsSteadfast(___pawn) && ___pawn.IsSlaveOfColony && __instance.SlaveFaction != Faction.OfPlayer)
            {
                ___slaveFactionInt = Faction.OfPlayer;
                Messages.Message((string)"MessageAssimilationSlave".Translate().AdjustedFor(___pawn), (LookTargets)(Thing)___pawn, MessageTypeDefOf.NeutralEvent);
            }
        }
    }
    // Remove global work speed debuff from slave of Slave Stage 5.
    [HarmonyPatch(typeof(StatPart_Slave), "ActiveFor")]
    public static class StatPartSlave_ActiveFor_Patch
    {
        [HarmonyPostfix]
        public static void ActiveFor_Patch(Thing t, ref bool __result)
        {
            if (SimpleSlaveryCollarsSetting.SlavestageEnable == false || SimpleSlaveryCollarsSetting.RebelCycleChangeEnable == false || SimpleSlaveryCollarsSetting.RemoveWorkspeedDebuffEnable == false)
                return;
            if (t is Pawn pawn && SlaveUtility.TimeAsSlave(pawn) >= SlaveUtility.SlaveStage4 && !SlaveUtility.IsSteadfast(pawn))
                __result = false;
        }
    }
    // Add role assignment button for slave pawns.
    // SocialCardUtility.DrawPawnRoleSelection 패치
    [HarmonyPatch(typeof(SocialCardUtility), "DrawPawnRoleSelection")]
    public static class SocialCardUtility_DrawPawnRoleSelection_Patch
    {
        // 1) 내부 private 필드 캐싱 via FieldInfo
        private static readonly FieldInfo _cachedRolesField =
            AccessTools.Field(typeof(SocialCardUtility), "cachedRoles");
        private static readonly FieldInfo _buttonSizeField =
            AccessTools.Field(typeof(SocialCardUtility), "RoleChangeButtonSize");

        // 2) FieldInfo를 통해 값 가져오기
        static List<Precept_Role> CachedRoles =>
            (List<Precept_Role>)_cachedRolesField.GetValue(null);
        static Vector2 RoleChangeButtonSize =>
            (Vector2)_buttonSizeField.GetValue(null);

        [HarmonyPostfix]
        public static void Postfix_DrawPawnRoleSelection(Pawn pawn, Rect rect)
        {
            // 설정 플래그 및 노예 여부 체크
            if (!SimpleSlaveryCollarsSetting.SlavestageEnable
                || !SimpleSlaveryCollarsSetting.AssignSlaveEnable
                || !(pawn.IsFreeColonist && pawn.IsSlave))
                return;

            var currentRole = pawn.Ideo?.GetRole(pawn);
            var primaryIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            bool active = CachedRoles.Any() && pawn.Ideo != null;

            // 버튼 위치 계산
            float y = rect.y + rect.height / 2f - 14f;
            Vector2 size = RoleChangeButtonSize;
            Rect buttonRect = new Rect(rect.width - 150f, y, size.x, size.y);
            buttonRect.xMax = rect.width - 26f - 4f;

            if (!active) GUI.color = Color.gray;

            if (Widgets.ButtonText(buttonRect, "ChooseRole".Translate() + "...",
                                   drawBackground: true, doMouseoverSound: true, active: active))
            {
                // 3) 알터·문양·행사장소 유효성 검사 (원본 흐름과 동일)
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

                // 4) 현재 역할 해임(취임식 흐름 사용)
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

                // 5) 가능한 새 역할 할당 (취임식 흐름 사용)
                foreach (var role in CachedRoles)
                {
                    bool canSelect = role != currentRole
                                     && role.Active
                                     && role.RequirementsMet(pawn)
                                     && (!role.def.leaderRole || pawn.Ideo == primaryIdeo);

                    string label2 = role.LabelForPawn(pawn) + (pawn.Ideo.classicMode ? "" : $" ({role.def.label})");

                    // 자유민 원본과 맞추기: 이미 누군가 차지 중이면 표시
                    if (role.ChosenPawnSingle() != null && role.ChosenPawnSingle() != pawn)
                    {
                        label2 += ": " + role.ChosenPawnSingle().LabelShort;
                        canSelect = false;
                    }
                    // 요구조건 불충족
                    else if (!role.RequirementsMet(pawn))
                    {
                        var unmet = role.GetFirstUnmetRequirement(pawn);
                        if (unmet != null)
                            label2 += ": " + unmet.GetLabel(role).CapitalizeFirst();
                        canSelect = false;
                    }
                    // 비활성(인원부족 등)
                    else if (!role.Active && role.def.activationBelieverCount > role.ideo.ColonistBelieverCountCached)
                    {
                        label2 += ": " + "InactiveRoleRequiresMoreBelievers".Translate(role.def.activationBelieverCount, role.ideo.memberName, role.ideo.ColonistBelieverCountCached).CapitalizeFirst();
                        canSelect = false;
                    }

                    // 옵션 추가
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
                        // 회색(선택 불가, action: null)
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
                    Messages.Message("SimpleSlaveryCollars_NoAssignableRole".Translate(), pawn, MessageTypeDefOf.RejectInput);
                    GUI.color = Color.white;
                    return;
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            GUI.color = Color.white;
        }
    }

    [HarmonyPatch(typeof(RitualRoleIdeoRoleChanger), nameof(RitualRoleIdeoRoleChanger.AppliesToPawn))]
    public static class RitualRoleIdeoRoleChanger_Patch
    {
        // AppliesIfChild 직접 재구현
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

            // 노예일 때만 별도 조건 체크
            if (p.IsSlaveOfColony)
            {
                // 재구현한 AppliesIfChild 호출
                if (!AppliesIfChild_Custom(__instance, p, out reason, skipReason))
                {
                    __result = false;
                    return false;
                }

                // 이념 존재 여부 체크 유지
                if (p.Ideo == null)
                {
                    __result = false;
                    return false;
                }

                // 역할 존재 여부 체크 유지
                if (p.Ideo.GetRole(p) == null && !RitualUtility.AllRolesForPawn(p).Any(r => r.RequirementsMet(p)))
                {
                    reason = "MessageRitualNoRolesAvailable".Translate(p);
                    __result = false;
                    return false;
                }

                // 플레이어 이념 체크 유지
                if (!Faction.OfPlayer.ideos.Has(p.Ideo))
                {
                    reason = "MessageRitualNotOfPlayerIdeo".Translate(p);
                    __result = false;
                    return false;
                }

                // 모든 조건을 통과했으므로 true로 설정 후 원본 메서드 생략
                __result = true;
                return false;
            }

            // 노예가 아니면 원본 메서드 그대로 실행
            return true;
        }
    }
    // Allow role assignment of a slave pawn.
    [HarmonyPatch(typeof(Precept_Role), "ValidatePawn")]
    public static class PreceptRole_ValidatePawn_Patch
    {
        [HarmonyPostfix]
        public static void ValidatePawn_Patch(ref bool __result, Pawn p, Precept_Role __instance)
        {
            if (SimpleSlaveryCollarsSetting.SlavestageEnable == false || SimpleSlaveryCollarsSetting.RebelCycleChangeEnable == false || SimpleSlaveryCollarsSetting.AssignSlaveEnable == false)
                return;
            if (__result == false && (p.Faction != null && (!p.Faction.IsPlayer || p.IsFreeColonist) && !p.Destroyed && !p.Dead && __instance.RequirementsMet(p)))
                __result = true;
        }
    }
    // Remove Rebellion Cycle according to Slave Stage 5.
    [HarmonyPatch(typeof(SlaveRebellionUtility), "CanParticipateInSlaveRebellion")]
    public static class SlaveRebellionUtility_CanParticipateInSlaveRebellion_Patch
    {
        [HarmonyPostfix]
        public static void CanParticipateInSlaveRebellion_Patch(ref Pawn pawn, ref bool __result)
        {
            if (SimpleSlaveryCollarsSetting.SlavestageEnable == false || SimpleSlaveryCollarsSetting.RebelCycleChangeEnable == false)
                return;
            if (SlaveUtility.TimeAsSlave(pawn) >= SlaveUtility.SlaveStage4 && !SlaveUtility.IsSteadfast(pawn))
                __result = false;
        }
    }
    // Added Rebellion Cycle Effect according to Slave Stage.
    [HarmonyPatch(typeof(SlaveRebellionUtility), "InitiateSlaveRebellionMtbDays")]
    public static class SlaveRebellionUtility_InitiateSlaveRebellionMtbDays_Patch
    {
        [HarmonyPostfix]
        public static void InitiateSlaveRebellionMtbDays_Patch(ref Pawn pawn, ref float __result)
        {
            if (SimpleSlaveryCollarsSetting.SlavestageEnable == false || SimpleSlaveryCollarsSetting.RebelCycleChangeEnable == false || __result == -1f)
                return;
            else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage1)
                __result *= 1f;
            else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage2)
                __result *= 1.5f;
            else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage3)
                __result *= 1.75f;
            else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage4 || (SlaveUtility.TimeAsSlave(pawn) >= SlaveUtility.SlaveStage3 && SlaveUtility.IsSteadfast(pawn)))
                __result *= 2f;
        }
    }
    // Added Rebellion Cycle Description according to Slave Stage.
    [HarmonyPatch(typeof(SlaveRebellionUtility), "GetSlaveRebellionMtbCalculationExplanation")]
    public static class SlaveRebellionUtility_GetSlaveRebellionMtbCalculationExplanation_Patch
    {
        [HarmonyPostfix]
        public static void GetSlaveRebellionMtbCalculationExplanation_Patch(ref Pawn pawn, ref string __result)
        {
            Need_Suppression need = pawn.needs.TryGetNeed<Need_Suppression>();
            if (SimpleSlaveryCollarsSetting.SlavestageEnable == false || SimpleSlaveryCollarsSetting.RebelCycleChangeEnable == false || need == null || !SlaveRebellionUtility.CanParticipateInSlaveRebellion(pawn))
                return;
            StringBuilder stringBuilder = new StringBuilder();
            if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage1)
            {
                float f4 = 1f;
                stringBuilder.AppendLine(string.Format("\n{0}: x{1}", (object)"SuppressionSlavestageFactor".Translate(), (object)f4.ToStringPercent()));
            }
            else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage2)
            {
                float f4 = 1.5f;
                stringBuilder.AppendLine(string.Format("\n{0}: x{1}", (object)"SuppressionSlavestageFactor".Translate(), (object)f4.ToStringPercent()));
            }
            else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage3)
            {
                float f4 = 1.75f;
                stringBuilder.AppendLine(string.Format("\n{0}: x{1}", (object)"SuppressionSlavestageFactor".Translate(), (object)f4.ToStringPercent()));
            }
            else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage4 || (SlaveUtility.TimeAsSlave(pawn) >= SlaveUtility.SlaveStage3 && SlaveUtility.IsSteadfast(pawn)))
            {
                float f4 = 2f;
                stringBuilder.AppendLine(string.Format("\n{0}: x{1}", (object)"SuppressionSlavestageFactor".Translate(), (object)f4.ToStringPercent()));
            }
            stringBuilder.AppendLine(string.Format("{0}: {1}", (object)"SuppressionFinalInterval".Translate(), (object)((int)((double)SlaveRebellionUtility.InitiateSlaveRebellionMtbDays(pawn) * 60000.0)).ToStringTicksToPeriod()));
            __result = __result.Remove(__result.LastIndexOf(Environment.NewLine));
            __result += stringBuilder.ToString();
        }
    }
    // Turn off pawn's collar when stripping pawns.
    [HarmonyPatch(typeof(Pawn), "Strip")]
    public static class Pawn_Strip_Patch
    {
        [HarmonyPrefix]
        public static void Strip_Patch(ref Pawn __instance)
        {
            if (SlaveUtility.HasSlaveCollar(__instance) && SlaveUtility.GetSlaveCollar(__instance).def.thingClass == typeof(SlaveCollar_Crypto))
            {
                (SlaveUtility.GetSlaveCollar(__instance) as SlaveCollar_Crypto).armed = false;
                if (!__instance.Dead)
                {
                    (SlaveUtility.GetSlaveCollar(__instance) as SlaveCollar_Crypto).RevertMentalState();
                }
            }
            if (SlaveUtility.HasSlaveCollar(__instance) && SlaveUtility.GetSlaveCollar(__instance).def.thingClass == typeof(SlaveCollar_Electric))
            {
                (SlaveUtility.GetSlaveCollar(__instance) as SlaveCollar_Electric).armed = false;
            }
        }
    }
    // Remove enslave hediff when slaves are freed.
    [HarmonyPatch(typeof(GenGuest), "EmancipateSlave")]
    public static class GenGuest_EmancipateSlave_Patch
    {
        [HarmonyPostfix]
        public static void EmancipateSlave_Patch(ref Pawn slave)
        {
            Hediff enslaved = slave.health.hediffSet.GetFirstHediffOfDef(SS_HediffDefOf.Enslaved);
            if (slave.health.hediffSet.HasHediff(SS_HediffDefOf.Enslaved))
                slave.health.RemoveHediff(enslaved);
        }
    }
    // Add enslave hediff when pawn become a slave.
    [HarmonyPatch(typeof(GenGuest), "EnslavePrisoner")]
    public static class GenGuest_EnslavePrisoner_Patch
    {
        [HarmonyPostfix]
        public static void EnslavePrisoner_Patch(ref Pawn prisoner)
        {
            if (!prisoner.health.hediffSet.HasHediff(SS_HediffDefOf.Enslaved))
                prisoner.health.AddHediff(SS_HediffDefOf.Enslaved);
            if (SimpleSlaveryCollarsSetting.ShacklesDefault == false)
                SlaveUtility.GetEnslavedHediff(prisoner).shackledGoal = false;
        }
    }
    // Patch for when a slave is sold
    [HarmonyPatch(typeof(Pawn), "PreTraded")]
    public static class Pawn_PreTraded_Patch
    {
        [HarmonyPostfix]
        public static void PreTraded_Patch(ref Pawn __instance, ref TradeAction action)
        {
            Hediff enslaved = __instance.health.hediffSet.GetFirstHediffOfDef(SS_HediffDefOf.Enslaved);
            if (action == TradeAction.PlayerBuys && __instance.RaceProps.Humanlike && !__instance.health.hediffSet.HasHediff(SS_HediffDefOf.Enslaved) && __instance.IsSlaveOfColony)
            {
                __instance.health.AddHediff(SS_HediffDefOf.Enslaved);
            }
            else if (action == TradeAction.PlayerSells && __instance.health.hediffSet.HasHediff(SS_HediffDefOf.Enslaved))
            {
                __instance.health.RemoveHediff(enslaved);
            }
        }
    }
    // Changes the behaviour of restraints to acknowledge shackled slaves
    [HarmonyPatch(typeof(RestraintsUtility), "InRestraints")]
    public static class RestraintUtility_Patch
    {
        [HarmonyPostfix]
        public static void InRestraints_Patch(ref Pawn pawn, ref bool __result)
        {
            // Pawn is a shackled slave
            if (pawn.IsSlaveOfColony && pawn.health.hediffSet.HasHediff(SS_HediffDefOf.Enslaved))
                __result = SlaveUtility.GetEnslavedHediff(pawn).shackled;
        }
    }
    [HarmonyPatch(typeof(RestraintsUtility), "ShouldShowRestraintsInfo")]
    public static class RestraintUtility_Show_Patch
    {
        [HarmonyPostfix]
        public static void ShouldShowRestraintsInfo_Patch(ref Pawn pawn, ref bool __result)
        {
            if (RestraintsUtility.InRestraints(pawn) && pawn.IsSlaveOfColony)
            {
                __result = true;
            }
        }
    }
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class Pawn_GetGizmos_Patch
    {
        static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = __result.Concat(SlaveGizmos(__instance));
        }

        internal static IEnumerable<Gizmo> SlaveGizmos(Pawn pawn)
        {
            if (!SlaveUtility.IsColonyMember(pawn))
            { // Only display the apparel gizmos if the pawn isn't a slave, not a colonist, not a prisoner.
                yield break;
            }
            if (pawn.apparel != null)
            {
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    var slaveApparel = apparel as SlaveApparel;
                    if (slaveApparel != null)
                    {
                        foreach (var g in slaveApparel.SlaveGizmos()) yield return g;
                    }
                }
            }
        }
    }
}

