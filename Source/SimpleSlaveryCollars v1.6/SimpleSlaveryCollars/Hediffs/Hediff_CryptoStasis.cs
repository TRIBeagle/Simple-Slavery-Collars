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

            if (pawn.mindState.mentalStateHandler.CurStateDef == SimpleSlaveryDefOf.CryptoStasis)
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