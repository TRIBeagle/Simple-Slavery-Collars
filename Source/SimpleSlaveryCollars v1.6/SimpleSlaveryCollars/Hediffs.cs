// SimpleSlaveryCollars | Core | Hediffs.cs
// 목적   : 노예 칼라 및 상태에 연계된 전용 Hediff 정의
// 용도   : Enslaved(노예화), CryptoStasis(구속 정신상태) 관리
// 변경   : [FIX] Hediff_CryptoStasis.SaveMemory()에 pawn.mindState null 체크 추가
// 저장   : assimilatedAtStage4, uiRefreshedAtStage4 등 Stage4 전환 관련 상태 포함

using RimWorld;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// 노예화 상태를 표현하는 Hediff.
    /// - shackledGoal : 기본 구속 목표 여부
    /// - shackled     : 실제 구속 상태
    /// - assimilatedAtStage4 : Stage4 동화 처리 여부
    /// - uiRefreshedAtStage4 : Stage4 전환 시 UI 갱신 여부
    /// </summary>
    public class Hediff_Enslaved : HediffWithComps
    {
        public bool shackledGoal = true;
        public bool shackled = true;
        public bool assimilatedAtStage4 = false;
        public bool uiRefreshedAtStage4 = false;

        /// <summary>
        /// 저장/로드 처리.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref shackledGoal, "shackledGoal", false);
            Scribe_Values.Look(ref shackled, "shackled", false);
            Scribe_Values.Look(ref assimilatedAtStage4, "assimilatedAtStage4", false);
            Scribe_Values.Look(ref uiRefreshedAtStage4, "uiRefreshedAtStage4", false);
        }

        /// <summary>
        /// UI에 표시되지 않도록 항상 false.
        /// </summary>
        public override bool Visible => false;
    }

    /// <summary>
    /// 크립토 스테이시스(Crypto Stasis) 상태를 표현하는 Hediff.
    /// - revertMentalStateDef : 해제 시 복원할 정신상태 저장
    /// </summary>
    public class Hediff_CryptoStasis : HediffWithComps
    {
        public MentalStateDef revertMentalStateDef;

        /// <summary>
        /// 현재 정신상태를 메모리에 저장.
        /// [FIX] pawn.mindState/mentalStateHandler null 체크 추가
        ///       — 로딩 중, 사망 직후 등에서 NRE 방지
        /// </summary>
        public void SaveMemory()
        {
            if (pawn?.mindState?.mentalStateHandler == null)
                return;

            if (pawn.mindState.mentalStateHandler.CurStateDef == SSC_MentalStateDefOf.CryptoStasis)
                revertMentalStateDef = MentalStateDefOf.Berserk;
            else
                revertMentalStateDef = pawn.mindState.mentalStateHandler.CurStateDef;
        }

        /// <summary>
        /// 저장/로드 처리.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look<MentalStateDef>(ref revertMentalStateDef, "revertMentalStateDef");
        }
    }
}