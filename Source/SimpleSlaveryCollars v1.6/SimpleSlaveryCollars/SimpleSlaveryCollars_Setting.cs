// SimpleSlaveryCollars | Core | SimpleSlaveryCollars_Setting.cs
// 목적   : 모드 옵션 저장/로드 및 설정 UI 제공
// 용도   : RimWorld ModSettings 확장
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 옵션 필드 정리
// 주의   : Reset 버튼 클릭 시 모든 값 기본값으로 복원
// 저장   : Scribe_Values 통해 모든 옵션 직렬화

using RimWorld;
using UnityEngine;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// 모드 설정 관리 클래스.
    /// - Shackles, Stage 시스템, RebelCycle, WorkDebuff, Assign, Assimilation, Remote 제어
    /// - Stage1~4 기간 조정 가능
    /// </summary>
    public class SimpleSlaveryCollarsSetting : ModSettings
    {
        public static bool ShacklesDefault = true;
        public static bool SlavestageEnable = true;
        public static bool RebelCycleChangeEnable = true;
        public static bool RemoveWorkspeedDebuffEnable = true;
        public static bool AssignSlaveEnable = true;
        public static bool Stage5SlaveWorkUnlockEnable = true;
        public static bool AssimilationSlaveEnable = true;
        public static bool RemoteOnlyOnConsoleEnable = true;

        public static float Slavestage1Period = 15f;
        public static float Slavestage2Period = 15f;
        public static float Slavestage3Period = 15f;
        public static float Slavestage4Period = 15f;

        private string Slavestage1PeriodBuffer;
        private string Slavestage2PeriodBuffer;
        private string Slavestage3PeriodBuffer;
        private string Slavestage4PeriodBuffer;

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
            Scribe_Values.Look(ref Slavestage1Period, "Slavestage1Period", 15f);
            Scribe_Values.Look(ref Slavestage2Period, "Slavestage2Period", 15f);
            Scribe_Values.Look(ref Slavestage3Period, "Slavestage3Period", 15f);
            Scribe_Values.Look(ref Slavestage4Period, "Slavestage4Period", 15f);
        }

        /// <summary>
        /// RimWorld ModSettings UI를 출력한다.
        /// </summary>
        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.CheckboxLabeled("shacklesDefaultSetting_title".Translate(), ref ShacklesDefault, "shacklesDefaultSetting_desc".Translate());
            listingStandard.CheckboxLabeled("slavestageEnableSetting_title".Translate(), ref SlavestageEnable, "slavestageEnableSetting_desc".Translate());
            listingStandard.CheckboxLabeled("rebelcyclechangeEnableSetting_title".Translate(), ref RebelCycleChangeEnable, "rebelcyclechangeEnableSetting_desc".Translate());
            listingStandard.CheckboxLabeled("removeworkspeeddebuffEnableSetting_title".Translate(), ref RemoveWorkspeedDebuffEnable, "removeworkspeeddebuffEnableSetting_desc".Translate());
            listingStandard.CheckboxLabeled("assignslaveEnableSetting_title".Translate(), ref AssignSlaveEnable, "assignslaveEnableSetting_desc".Translate());
            listingStandard.CheckboxLabeled("stage5SlaveWorkUnlockEnableSetting_title".Translate(), ref Stage5SlaveWorkUnlockEnable, "stage5SlaveWorkUnlockEnableSetting_desc".Translate());
            listingStandard.CheckboxLabeled("assimilationslaveEnableSetting_title".Translate(), ref AssimilationSlaveEnable, "assimilationslaveEnableSetting_desc".Translate());
            listingStandard.CheckboxLabeled("remoteOnlyOnConsoleEnableSetting_title".Translate(), ref RemoteOnlyOnConsoleEnable, "remoteOnlyOnConsoleEnableSetting_desc".Translate());

            listingStandard.Label("slavestage1Period_title".Translate(), -1f, "slavestage1Period_desc".Translate());
            listingStandard.TextFieldNumeric(ref Slavestage1Period, ref Slavestage1PeriodBuffer);

            listingStandard.Label("slavestage2Period_title".Translate(), -1f, "slavestage2Period_desc".Translate());
            listingStandard.TextFieldNumeric(ref Slavestage2Period, ref Slavestage2PeriodBuffer);

            listingStandard.Label("slavestage3Period_title".Translate(), -1f, "slavestage3Period_desc".Translate());
            listingStandard.TextFieldNumeric(ref Slavestage3Period, ref Slavestage3PeriodBuffer);

            listingStandard.Label("slavestage4Period_title".Translate(), -1f, "slavestage4Period_desc".Translate());
            listingStandard.TextFieldNumeric(ref Slavestage4Period, ref Slavestage4PeriodBuffer);

            if (listingStandard.ButtonText("resetAllSetting_title".Translate()))
            {
                ShacklesDefault = true;
                SlavestageEnable = true;
                RebelCycleChangeEnable = true;
                RemoveWorkspeedDebuffEnable = true;
                AssignSlaveEnable = true;
                Stage5SlaveWorkUnlockEnable = true;
                AssimilationSlaveEnable = true;
                RemoteOnlyOnConsoleEnable = true;

                Slavestage1Period = 15f;
                Slavestage2Period = 15f;
                Slavestage3Period = 15f;
                Slavestage4Period = 15f;

                Slavestage1PeriodBuffer = "15";
                Slavestage2PeriodBuffer = "15";
                Slavestage3PeriodBuffer = "15";
                Slavestage4PeriodBuffer = "15";
            }

            listingStandard.End();
        }
    }
}
