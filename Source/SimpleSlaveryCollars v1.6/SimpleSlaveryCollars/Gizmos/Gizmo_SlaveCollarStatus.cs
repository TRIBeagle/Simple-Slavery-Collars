// SimpleSlaveryCollars | Gizmos | Gizmo_SlaveCollarStatus.cs
// 목적 : 노예 칼라 통합 기즈모 — 충전량 바 + Arm/Detonate 버튼을 하나의 기즈모로 표시
// 용도 : 충전 옵션 ON 시 기존 Command_Toggle/Action 대신 사용
// 주의 : EMP 비활성화 중에는 바 회색 + 버튼 비활성. 충전 부족 시 바 빨간색

using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SimpleSlaveryCollars.Gizmos
{
    [StaticConstructorOnStartup]
    public class Gizmo_SlaveCollarStatus : Gizmo
    {
        /// <summary>대상 칼라.</summary>
        public SlaveApparel collar;

        // 바 텍스처 (static 캐시)
        private static readonly Texture2D BarFilledTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.24f, 0.55f, 0.72f));
        private static readonly Texture2D BarLowTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.75f, 0.25f, 0.20f));
        private static readonly Texture2D BarEmpTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.35f, 0.35f));
        private static readonly Texture2D BarEmptyTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));

        // 버튼 크기
        private const float HeaderBtnSize = 24f;

        public Gizmo_SlaveCollarStatus()
        {
            Order = -99f;
        }

        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect outerRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect innerRect = outerRect.ContractedBy(6f);
            Widgets.DrawWindowBackground(outerRect);

            if (collar == null)
                return new GizmoResult(GizmoState.Clear);

            bool empDisabled = collar.IsEmpDisabled;
            bool chargeEnabled = SimpleSlaveryCollarsSetting.CollarChargeEnable;
            bool operational = collar.IsOperational;

            // ── 상단: 칼라 타입명 + 버튼 ──
            Rect headerRect = innerRect;
            headerRect.height = Text.LineHeightOf(GameFont.Small);
            float headerBtnX = headerRect.xMax;
            bool mouseOverBtn = false;

            // 버튼 배치 (우측부터)
            headerBtnX = DrawCollarButtons(headerRect, headerBtnX, operational, ref mouseOverBtn);

            headerRect.xMax = headerBtnX - 2f;

            // 칼라 타입 라벨
            Text.Font = GameFont.Small;
            string collarLabel = collar.def.LabelCap.Resolve();
            string truncated = collarLabel.Truncate(headerRect.width);
            Widgets.Label(headerRect, truncated);
            if (truncated != collarLabel && Mouse.IsOver(headerRect))
                TooltipHandler.TipRegion(headerRect, collarLabel);

            // ── 하단: 프로그래스 바 ──
            Rect barRect = innerRect;
            barRect.yMin = headerRect.yMax + 4f;

            float fillPct;
            string barLabel;
            Texture2D fillTex;

            if (empDisabled)
            {
                fillPct = 1f;
                barLabel = "SSC_Collar_EmpDisabled".Translate();
                fillTex = BarEmpTex;
            }
            else if (!chargeEnabled)
            {
                fillPct = 1f;
                barLabel = "SSC_Collar_Unlimited".Translate();
                fillTex = BarFilledTex;
            }
            else
            {
                fillPct = collar.charge;
                barLabel = collar.ChargePercent + "%";
                fillTex = collar.charge <= SlaveApparel.ChargeThreshold ? BarLowTex : BarFilledTex;
            }

            Widgets.FillableBar(barRect, fillPct, fillTex, BarEmptyTex, doBorder: true);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, barLabel);
            Text.Anchor = TextAnchor.UpperLeft;

            // ── 툴팁 ──
            if (Mouse.IsOver(outerRect) && !mouseOverBtn)
            {
                Widgets.DrawHighlight(outerRect);
                TooltipHandler.TipRegion(outerRect, GetTooltip(empDisabled, chargeEnabled));
            }

            return new GizmoResult(GizmoState.Clear);
        }

        /// <summary>칼라 타입별 버튼 배치. 반환값은 남은 x 좌표.</summary>
        private float DrawCollarButtons(Rect headerRect, float x, bool operational, ref bool mouseOver)
        {
            if (collar is SlaveCollar_Explosive explosive)
            {
                // 폭발 버튼 (armed 시에만)
                if (explosive.armed)
                {
                    x -= HeaderBtnSize;
                    Rect detonateRect = new Rect(x, headerRect.y, HeaderBtnSize, HeaderBtnSize);
                    var detonateIcon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Explosive", true);
                    GUI.DrawTexture(detonateRect, detonateIcon);
                    if (operational && Widgets.ButtonInvisible(detonateRect))
                    {
                        explosive.GoBoom();
                        SoundDefOf.Click.PlayOneShotOnCamera();
                    }
                    if (Mouse.IsOver(detonateRect))
                    {
                        Widgets.DrawHighlight(detonateRect);
                        TooltipHandler.TipRegion(detonateRect, "Desc_CollarExplosive_Detonate".Translate());
                        mouseOver = true;
                    }
                }

                // Arm 토글 버튼
                x -= HeaderBtnSize;
                Rect armRect = new Rect(x, headerRect.y, HeaderBtnSize, HeaderBtnSize);
                var armIcon = ContentFinder<Texture2D>.Get("UI/Commands/ArmCollar_Explosive", true);
                GUI.DrawTexture(armRect, armIcon);
                if (!operational) GUI.color = new Color(1f, 1f, 1f, 0.3f);
                if (Widgets.ButtonInvisible(armRect) && operational)
                {
                    explosive.armed = !explosive.armed;
                    if (explosive.armed && explosive.arm_cooldown == 0)
                    {
                        SimpleSlaveryCollars.Utilities.SimpleSlaveryUtility.TryInstantBreak(
                            collar.Wearer, Rand.Range(0.25f, 0.33f));
                        explosive.arm_cooldown = 2500;
                    }
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
                GUI.color = Color.white;
                if (Mouse.IsOver(armRect))
                {
                    Widgets.DrawHighlight(armRect);
                    string armTip = explosive.armed
                        ? "SSC_Collar_Disarm".Translate()
                        : "SSC_Collar_Arm".Translate();
                    TooltipHandler.TipRegion(armRect, armTip);
                    mouseOver = true;
                }
            }
            else if (collar is SlaveCollar_Electric electric)
            {
                x -= HeaderBtnSize;
                Rect armRect = new Rect(x, headerRect.y, HeaderBtnSize, HeaderBtnSize);
                var icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Electric", true);
                GUI.DrawTexture(armRect, icon);
                if (!operational) GUI.color = new Color(1f, 1f, 1f, 0.3f);
                if (Widgets.ButtonInvisible(armRect) && operational)
                {
                    electric.armed = !electric.armed;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
                GUI.color = Color.white;
                if (Mouse.IsOver(armRect))
                {
                    Widgets.DrawHighlight(armRect);
                    string tip = electric.armed
                        ? "SSC_Collar_Disarm".Translate()
                        : "SSC_Collar_Arm".Translate();
                    TooltipHandler.TipRegion(armRect, tip);
                    mouseOver = true;
                }
            }
            else if (collar is SlaveCollar_Crypto crypto)
            {
                x -= HeaderBtnSize;
                Rect armRect = new Rect(x, headerRect.y, HeaderBtnSize, HeaderBtnSize);
                var icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Crypto", true);
                GUI.DrawTexture(armRect, icon);
                if (!operational) GUI.color = new Color(1f, 1f, 1f, 0.3f);
                if (Widgets.ButtonInvisible(armRect) && operational)
                {
                    crypto.armed = !crypto.armed;
                    if (!crypto.armed)
                        crypto.RevertMentalState();
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
                GUI.color = Color.white;
                if (Mouse.IsOver(armRect))
                {
                    Widgets.DrawHighlight(armRect);
                    string tip = crypto.armed
                        ? "SSC_Collar_Disarm".Translate()
                        : "SSC_Collar_Arm".Translate();
                    TooltipHandler.TipRegion(armRect, tip);
                    mouseOver = true;
                }
            }

            return x;
        }

        /// <summary>툴팁 생성.</summary>
        private string GetTooltip(bool empDisabled, bool chargeEnabled)
        {
            if (empDisabled)
                return "SSC_Collar_EmpTooltip".Translate(collar.empDisabledTicks.ToStringTicksToPeriod());

            if (!chargeEnabled)
                return "SSC_Collar_UnlimitedTooltip".Translate();

            return "SSC_Collar_ChargeTooltip".Translate(collar.ChargePercent);
        }
    }
}
