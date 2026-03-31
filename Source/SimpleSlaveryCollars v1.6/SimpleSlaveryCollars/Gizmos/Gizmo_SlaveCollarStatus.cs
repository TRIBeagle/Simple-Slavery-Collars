// SimpleSlaveryCollars | Gizmos | Gizmo_SlaveCollarStatus.cs
// 목적 : 노예 칼라 충전 상태 기즈모 — 충전 바 + 드래그 임계값 슬라이더
// 용도 : 충전 옵션 ON 시 기존 Arm/Detonate 기즈모와 함께 표시
// 주의 : EMP 비활성화 중에는 바 회색 + "EMP 비활성화" 텍스트
//        드래그 중 collar.rechargeThreshold 실시간 갱신 → 세이브에 반영

using RimWorld;
using UnityEngine;
using Verse;

namespace SimpleSlaveryCollars.Gizmos
{
    [StaticConstructorOnStartup]
    public class Gizmo_SlaveCollarStatus : Gizmo
    {
        /// <summary>대상 칼라.</summary>
        public SlaveApparel collar;

        // 바 텍스처 (static 캐시)
        // 충전량 구간별 색상 (75%+/50%+/25%+/25%-)
        private static readonly Texture2D BarFullTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.24f, 0.55f, 0.72f));   // 파랑
        private static readonly Texture2D BarMidTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.75f, 0.68f, 0.22f));   // 노랑
        private static readonly Texture2D BarLowTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.82f, 0.45f, 0.18f));   // 주황
        private static readonly Texture2D BarCriticalTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.75f, 0.25f, 0.20f));   // 빨강
        private static readonly Texture2D BarHighlightTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.65f, 0.82f));
        private static readonly Texture2D BarEmpTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.35f, 0.35f));
        private static readonly Texture2D BarEmptyTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));
        private static readonly Texture2D BarDragTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.74f, 0.97f, 0.8f));

        // 임계값 드래그 상태 (Gizmo_SetFuelLevel과 동일 패턴: static)
        private static bool draggingBar;

        // 고정 마커 없음
        private static readonly float[] ThresholdMarkers = { };

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

            // ── 상단: 칼라 이름(폰 이름) ──
            Rect headerRect = innerRect;
            headerRect.height = Text.LineHeightOf(GameFont.Small);

            // 칼라 타입 라벨 — 다중 선택 시에만 폰 이름 부기
            string collarLabel = collar.def.LabelCap.Resolve();
            string fullLabel = collarLabel;
            if (Find.Selector.NumSelected > 1 && collar.Wearer != null)
                fullLabel = $"{collarLabel}({collar.Wearer.LabelShort})";

            // 폭 초과 시 Tiny 폰트로 축소
            Text.Font = GameFont.Small;
            if (Text.CalcSize(fullLabel).x > headerRect.width)
                Text.Font = GameFont.Tiny;

            string truncated = fullLabel.Truncate(headerRect.width);
            Widgets.Label(headerRect, truncated);
            Text.Font = GameFont.Small;
            if (truncated != fullLabel && Mouse.IsOver(headerRect))
                TooltipHandler.TipRegion(headerRect, fullLabel);

            // ── 하단: 충전 바 ──
            Rect barRect = innerRect;
            barRect.yMin = headerRect.yMax + 4f;

            if (empDisabled)
            {
                // 전자기 교란(EMP/흑점) — 회색 바
                Widgets.FillableBar(barRect, 1f, BarEmpTex, BarEmptyTex, doBorder: true);
                DrawBarLabel(barRect, "SSC_Collar_Disrupted".Translate());
            }
            else
            {
                // 드래그 가능한 임계값 슬라이더 + 충전 바
                float threshold = collar.rechargeThreshold;
                Texture2D fillTex = GetChargeTex(collar.charge);

                Widgets.DraggableBar(barRect, fillTex, BarHighlightTex, BarEmptyTex, BarDragTex,
                    ref draggingBar, collar.charge, ref threshold,
                    ThresholdMarkers, 20, 0f, 1f);

                collar.rechargeThreshold = threshold;
                DrawBarLabel(barRect, $"{collar.ChargeWd:F0} / {collar.BatteryCapacityWd:F0}");
            }

            // ── 툴팁 ──
            if (Mouse.IsOver(outerRect))
            {
                Widgets.DrawHighlight(outerRect);
                TooltipHandler.TipRegion(outerRect, GetTooltip(empDisabled));
            }

            return new GizmoResult(GizmoState.Clear);
        }

        /// <summary>충전량 구간별 텍스처 반환.</summary>
        private static Texture2D GetChargeTex(float charge)
        {
            if (charge > 0.75f) return BarFullTex;      // 파랑
            if (charge > 0.50f) return BarMidTex;       // 노랑
            if (charge > 0.25f) return BarLowTex;       // 주황
            return BarCriticalTex;                       // 빨강
        }

        /// <summary>바 중앙에 라벨 표시.</summary>
        private static void DrawBarLabel(Rect barRect, string label)
        {
            GameFont prevFont = Text.Font;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = prevFont;
        }

        /// <summary>툴팁 생성.</summary>
        private string GetTooltip(bool empDisabled)
        {
            if (empDisabled)
            {
                // 흑점이면 잔여 시간 없음, EMP면 잔여 시간 표시
                if (collar.empDisabledTicks > 0)
                    return "SSC_Collar_DisruptedEmp_Tooltip".Translate(collar.empDisabledTicks.ToStringTicksToPeriod());
                return "SSC_Collar_DisruptedFlare_Tooltip".Translate();
            }

            float thresholdWd = collar.rechargeThreshold * collar.BatteryCapacityWd;
            int thresholdPct = Mathf.RoundToInt(collar.rechargeThreshold * 100f);
            string drainIdle = "SSC_Collar_DrainIdle".Translate(collar.IdleDrainPerDay.ToString("F0"));
            string drainActive = "SSC_Collar_DrainActive".Translate(collar.ActiveDrainPerDay.ToString("F0"));
            return $"{"SSC_Collar_Charge_Tooltip".Translate(collar.ChargeWd.ToString("F1"), collar.BatteryCapacityWd.ToString("F1"))}\n{drainIdle}\n{drainActive}\n{"SSC_Collar_RechargeThreshold".Translate()}: {thresholdWd:F1} Wd ({thresholdPct}%)";
        }
    }
}
