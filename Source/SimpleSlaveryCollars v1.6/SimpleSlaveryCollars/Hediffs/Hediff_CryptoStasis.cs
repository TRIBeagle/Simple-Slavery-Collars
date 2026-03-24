// SimpleSlaveryCollars | Hediffs | Hediff_CryptoStasis.cs
// 목적   : 크립토 스테이시스(동결) 상태를 표현하는 Hediff
// 용도   : CryptoStasis 칼라 해제 시 이전 정신상태 복원용 메모리 관리
// 변경   : [FIX] SaveMemory()에 pawn.mindState null 체크 추가
// 저장   : revertMentalStateDef (해제 시 복원할 정신상태)

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