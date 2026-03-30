// SimpleSlaveryCollars | Core | SimpleSlaveryCollars_Setting.cs
// 목적   : 모드 옵션 저장/로드 및 설정 UI 제공
// 용도   : RimWorld ModSettings 확장
// 주의   : Reset 버튼 클릭 시 모든 값 기본값으로 복원
// 저장   : Scribe_Values 통해 모든 옵션 직렬화

using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// 모드 설정 관리 클래스.
    /// UI는 카테고리별로 그룹화: 노예 관리 → 칼라 시스템 → Stage 기간.
    /// </summary>
    public class SimpleSlaveryCollarsSetting : ModSettings
    {
        // ── 스크롤 (UI 전용) ──
        private Vector2 _scrollPos;

        // ── 노예 관리 ──
        public static bool ShacklesDefault = true;
        public static bool RemoteOnlyOnConsoleEnable = true;

        // ── Stage 시스템 ──
        public static bool SlavestageEnable = true;
        public static bool RebelCycleChangeEnable = true;
        public static bool RemoveWorkspeedDebuffEnable = true;
        public static bool AssignSlaveEnable = true;
        public static bool Stage5SlaveWorkUnlockEnable = true;
        public static bool AssimilationSlaveEnable = true;

        public static float Slavestage1Period = 15f;
        public static float Slavestage2Period = 15f;
        public static float Slavestage3Period = 15f;
        public static float Slavestage4Period = 15f;

        // ── 칼라 충전/교란 ──
        public static bool CollarChargeEnable = true;
        public static bool CollarDisruptionEnable = true;
        public static float CollarDrainMultiplier = 1f;
        public static float DefaultRechargeThreshold = 0.5f;

        // ── 입력 버퍼 (UI 전용, 저장 안 함) ──
        private string _drainMultiplierBuffer;
        private string _stage1PeriodBuffer;
        private string _stage2PeriodBuffer;
        private string _stage3PeriodBuffer;
        private string _stage4PeriodBuffer;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ShacklesDefault, "ShacklesDefault", true);
            Scribe_Values.Look(ref SlavestageEnable, "SlavestageEnable", true);
            Scribe_Values.Look(ref RebelCycleChangeEnable, "RebelCycleChangeEnable", true);
            Scribe_Values.Look(ref RemoveWorkspeedDebuffEnable, "RemoveWorkspeedDebuffEnable", true);
            Scribe_Values.Look(ref AssignSlaveEnable, "AssignSlaveEnable", true);
            Scribe_Values.Look(ref Stage5SlaveWorkUnlockEnable, "Stage5SlaveWorkUnlockEnable", true);
            Scribe_Values.Look(ref AssimilationSlaveEnable, "AssimilationSlaveEnable", true);
            Scribe_Values.Look(ref RemoteOnlyOnConsoleEnable, "RemoteOnlyOnConsoleEnable", true);
            Scribe_Values.Look(ref CollarChargeEnable, "CollarChargeEnable", true);
            Scribe_Values.Look(ref CollarDisruptionEnable, "CollarDisruptionEnable", true);
            Scribe_Values.Look(ref CollarDrainMultiplier, "CollarDrainMultiplier", 1f);
            Scribe_Values.Look(ref DefaultRechargeThreshold, "DefaultRechargeThreshold", 0.5f);
            Scribe_Values.Look(ref Slavestage1Period, "Slavestage1Period", 15f);
            Scribe_Values.Look(ref Slavestage2Period, "Slavestage2Period", 15f);
            Scribe_Values.Look(ref Slavestage3Period, "Slavestage3Period", 15f);
            Scribe_Values.Look(ref Slavestage4Period, "Slavestage4Period", 15f);

            // 기존 세이브에서 0 이하 값 방어 — 최소 1일
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Slavestage1Period = Math.Max(Slavestage1Period, 1f);
                Slavestage2Period = Math.Max(Slavestage2Period, 1f);
                Slavestage3Period = Math.Max(Slavestage3Period, 1f);
                Slavestage4Period = Math.Max(Slavestage4Period, 1f);
            }
        }

        /// <summary>
        /// 모드 설정 UI. 카테고리별 그룹: 노예 관리 / Stage / 칼라.
        /// </summary>
        public void DoSettingsWindowContents(Rect inRect)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 800f);
            Widgets.BeginScrollView(inRect, ref _scrollPos, viewRect);

            Listing_Standard ls = new Listing_Standard();
            ls.Begin(viewRect);

            // ═══════ 노예 관리 ═══════
            ls.Label("SSC_Setting_Header_SlaveManagement".Translate());
            ls.GapLine();

            ls.CheckboxLabeled("SSC_Setting_ShacklesDefault_Title".Translate(), ref ShacklesDefault, "SSC_Setting_ShacklesDefault_Desc".Translate());
            ls.CheckboxLabeled("SSC_Setting_RemoteOnlyConsole_Title".Translate(), ref RemoteOnlyOnConsoleEnable, "SSC_Setting_RemoteOnlyConsole_Desc".Translate());

            ls.Gap();

            // ═══════ Stage 시스템 ═══════
            ls.Label("SSC_Setting_Header_StageSystem".Translate());
            ls.GapLine();

            ls.CheckboxLabeled("SSC_Setting_SlaveStage_Title".Translate(), ref SlavestageEnable, "SSC_Setting_SlaveStage_Desc".Translate());

            if (SlavestageEnable)
            {
                ls.CheckboxLabeled("SSC_Setting_RebelCycle_Title".Translate(), ref RebelCycleChangeEnable, "SSC_Setting_RebelCycle_Desc".Translate());
                ls.CheckboxLabeled("SSC_Setting_RemoveWorkDebuff_Title".Translate(), ref RemoveWorkspeedDebuffEnable, "SSC_Setting_RemoveWorkDebuff_Desc".Translate());
                ls.CheckboxLabeled("SSC_Setting_AssignSlave_Title".Translate(), ref AssignSlaveEnable, "SSC_Setting_AssignSlave_Desc".Translate());
                ls.CheckboxLabeled("SSC_Setting_Stage5WorkUnlock_Title".Translate(), ref Stage5SlaveWorkUnlockEnable, "SSC_Setting_Stage5WorkUnlock_Desc".Translate());
                ls.CheckboxLabeled("SSC_Setting_Assimilation_Title".Translate(), ref AssimilationSlaveEnable, "SSC_Setting_Assimilation_Desc".Translate());

                ls.Gap(4f);
                ls.Label("SSC_Setting_Stage1Period_Title".Translate(), -1f, "SSC_Setting_Stage1Period_Desc".Translate());
                ls.TextFieldNumeric(ref Slavestage1Period, ref _stage1PeriodBuffer, 1f, 999f);
                ls.Label("SSC_Setting_Stage2Period_Title".Translate(), -1f, "SSC_Setting_Stage2Period_Desc".Translate());
                ls.TextFieldNumeric(ref Slavestage2Period, ref _stage2PeriodBuffer, 1f, 999f);
                ls.Label("SSC_Setting_Stage3Period_Title".Translate(), -1f, "SSC_Setting_Stage3Period_Desc".Translate());
                ls.TextFieldNumeric(ref Slavestage3Period, ref _stage3PeriodBuffer, 1f, 999f);
                ls.Label("SSC_Setting_Stage4Period_Title".Translate(), -1f, "SSC_Setting_Stage4Period_Desc".Translate());
                ls.TextFieldNumeric(ref Slavestage4Period, ref _stage4PeriodBuffer, 1f, 999f);
            }

            ls.Gap();

            // ═══════ 칼라 시스템 ═══════
            ls.Label("SSC_Setting_Header_CollarSystem".Translate());
            ls.GapLine();

            ls.CheckboxLabeled("SSC_Setting_CollarCharge_Title".Translate(), ref CollarChargeEnable, "SSC_Setting_CollarCharge_Desc".Translate());

            if (CollarChargeEnable)
            {
                ls.Label("SSC_Setting_DrainMultiplier_Title".Translate(), -1f, "SSC_Setting_DrainMultiplier_Desc".Translate());
                ls.TextFieldNumeric(ref CollarDrainMultiplier, ref _drainMultiplierBuffer, 0.1f, 10f);

                int thresholdPct = Mathf.RoundToInt(DefaultRechargeThreshold * 100f);
                ls.Label("SSC_Setting_DefaultRechargeThreshold_Title".Translate(thresholdPct), -1f, "SSC_Setting_DefaultRechargeThreshold_Desc".Translate());
                DefaultRechargeThreshold = ls.Slider(DefaultRechargeThreshold, 0f, 1f);
            }

            ls.CheckboxLabeled("SSC_Setting_CollarDisruption_Title".Translate(), ref CollarDisruptionEnable, "SSC_Setting_CollarDisruption_Desc".Translate());

            ls.Gap();

            // ═══════ 리셋 ═══════
            if (ls.ButtonText("SSC_Setting_ResetAll_Title".Translate()))
            {
                ShacklesDefault = true;
                RemoteOnlyOnConsoleEnable = true;

                SlavestageEnable = true;
                RebelCycleChangeEnable = true;
                RemoveWorkspeedDebuffEnable = true;
                AssignSlaveEnable = true;
                Stage5SlaveWorkUnlockEnable = true;
                AssimilationSlaveEnable = true;

                Slavestage1Period = 15f;
                Slavestage2Period = 15f;
                Slavestage3Period = 15f;
                Slavestage4Period = 15f;
                _stage1PeriodBuffer = "15";
                _stage2PeriodBuffer = "15";
                _stage3PeriodBuffer = "15";
                _stage4PeriodBuffer = "15";

                CollarChargeEnable = true;
                CollarDisruptionEnable = true;
                CollarDrainMultiplier = 1f;
                DefaultRechargeThreshold = 0.5f;
                _drainMultiplierBuffer = "1";
            }

            ls.End();
            Widgets.EndScrollView();
        }
    }
}
