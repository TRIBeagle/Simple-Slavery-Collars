// SimpleSlaveryCollars | Gizmos | Gizmo_SlaveCollarStatus.cs
// 목적 : 노예 칼라 충전 상태 기즈모 — 프로그래스 바 + 자가충전 토글
// 용도 : 충전 옵션 ON 시 기존 Arm/Detonate 기즈모와 함께 표시
// 주의 : EMP 비활성화 중에는 바 회색 + "EMP 비활성화" 텍스트

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

            // ── 상단: 칼라 이름 + 자가충전 토글 ──
            Rect headerRect = innerRect;
            headerRect.height = Text.LineHeightOf(GameFont.Small);
            float headerBtnX = headerRect.xMax;
            bool mouseOverBtn = false;

            // 자가충전 토글 버튼 (충전 ON일 때만)
            if (chargeEnabled)
            {
                headerBtnX -= HeaderBtnSize;
                Rect toggleRect = new Rect(headerBtnX, headerRect.y, HeaderBtnSize, HeaderBtnSize);

                // 체크박스 스타일
                GUI.DrawTexture(toggleRect, collar.selfRechargeAllowed
                    ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex);

                if (Widgets.ButtonInvisible(toggleRect))
                {
                    collar.selfRechargeAllowed = !collar.selfRechargeAllowed;
                    if (collar.selfRechargeAllowed)
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    else
                        SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }

                if (Mouse.IsOver(toggleRect))
                {
                    Widgets.DrawHighlight(toggleRect);
                    TooltipHandler.TipRegion(toggleRect, "SSC_Collar_SelfRechargeToggle".Translate());
                    mouseOverBtn = true;
                }
            }

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

        /// <summary>툴팁 생성.</summary>
        private string GetTooltip(bool empDisabled, bool chargeEnabled)
        {
            if (empDisabled)
                return "SSC_Collar_EmpTooltip".Translate(collar.empDisabledTicks.ToStringTicksToPeriod());

            if (!chargeEnabled)
                return "SSC_Collar_UnlimitedTooltip".Translate();

            string selfStatus = collar.selfRechargeAllowed
                ? "SSC_Collar_SelfRechargeOn".Translate()
                : "SSC_Collar_SelfRechargeOff".Translate();
            return "SSC_Collar_ChargeTooltip".Translate(collar.ChargePercent) + "\n" + selfStatus;
        }
    }
}
