// SimpleSlaveryCollars | Gizmos | Gizmo_SlaveCollarStatus.cs
// 목적 : 노예 칼라 충전 상태 기즈모 — 충전 바 + 드래그 임계값 슬라이더 + 자가충전 토글
// 용도 : 충전 옵션 ON 시 기존 Arm/Detonate 기즈모와 함께 표시
// 주의 : EMP 비활성화 중에는 바 회색 + "EMP 비활성화" 텍스트
//        드래그 중 collar.rechargeThreshold 실시간 갱신 → 세이브에 반영

using System.Collections.Generic;
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
        private static readonly Texture2D BarHighlightTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.65f, 0.82f));
        private static readonly Texture2D BarLowTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.75f, 0.25f, 0.20f));
        private static readonly Texture2D BarEmpTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.35f, 0.35f));
        private static readonly Texture2D BarEmptyTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));
        private static readonly Texture2D BarDragTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.74f, 0.97f, 0.8f));

        private const float HeaderBtnSize = 24f;

        // 임계값 드래그 상태 (Gizmo_SetFuelLevel과 동일 패턴: static)
        private static bool draggingBar;

        // ChargeThreshold(armed 최소) 위치를 고정 마커로 표시
        private static readonly float[] ThresholdMarkers = { SlaveApparel.ChargeThreshold };

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

            // ── 하단: 충전 바 ──
            Rect barRect = innerRect;
            barRect.yMin = headerRect.yMax + 4f;

            if (empDisabled)
            {
                // EMP 비활성화 — 회색 바
                Widgets.FillableBar(barRect, 1f, BarEmpTex, BarEmptyTex, doBorder: true);
                DrawBarLabel(barRect, "SSC_Collar_EmpDisabled".Translate());
            }
            else if (!chargeEnabled)
            {
                // 충전 비활성화 — 무한
                Widgets.FillableBar(barRect, 1f, BarFilledTex, BarEmptyTex, doBorder: true);
                DrawBarLabel(barRect, "SSC_Collar_Unlimited".Translate());
            }
            else
            {
                // 드래그 가능한 임계값 슬라이더 + 충전 바
                float threshold = collar.rechargeThreshold;
                Texture2D fillTex = collar.charge <= SlaveApparel.ChargeThreshold ? BarLowTex : BarFilledTex;

                Widgets.DraggableBar(barRect, fillTex, BarHighlightTex, BarEmptyTex, BarDragTex,
                    ref draggingBar, collar.charge, ref threshold,
                    ThresholdMarkers, 20, 0.1f, 0.95f);

                collar.rechargeThreshold = threshold;
                DrawBarLabel(barRect, $"{collar.ChargePercent}%");
            }

            // ── 툴팁 ──
            if (Mouse.IsOver(outerRect) && !mouseOverBtn)
            {
                Widgets.DrawHighlight(outerRect);
                TooltipHandler.TipRegion(outerRect, GetTooltip(empDisabled, chargeEnabled));
            }

            return new GizmoResult(GizmoState.Clear);
        }

        /// <summary>바 중앙에 라벨 표시.</summary>
        private static void DrawBarLabel(Rect barRect, string label)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, label);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        /// <summary>툴팁 생성.</summary>
        private string GetTooltip(bool empDisabled, bool chargeEnabled)
        {
            if (empDisabled)
                return "SSC_Collar_EmpTooltip".Translate(collar.empDisabledTicks.ToStringTicksToPeriod());

            if (!chargeEnabled)
                return "SSC_Collar_UnlimitedTooltip".Translate();

            int thresholdPct = Mathf.RoundToInt(collar.rechargeThreshold * 100f);
            string selfStatus = collar.selfRechargeAllowed
                ? "SSC_Collar_SelfRechargeOn".Translate()
                : "SSC_Collar_SelfRechargeOff".Translate();
            return $"{"SSC_Collar_ChargeTooltip".Translate(collar.ChargePercent)}\n{"SSC_Collar_RechargeThreshold".Translate()}: {thresholdPct}%\n{selfStatus}";
        }
    }
}
