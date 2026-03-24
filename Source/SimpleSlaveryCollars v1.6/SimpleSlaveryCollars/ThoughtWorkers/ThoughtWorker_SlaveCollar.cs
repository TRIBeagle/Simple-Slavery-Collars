// SimpleSlaveryCollars | Thoughts | ThoughtWorker_SlaveCollar.cs
// 목적   : Pawn이 일반 노예 칼라(Explosive Collar 제외)를 착용했을 때 정신 사상(Thought)을 부여
// 용도   : RimWorld ThoughtWorker 확장
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용
// 주의   : Masochist 특성, Colony/Slave 여부, 노예 경과 시간에 따라 Stage 달라짐
// 저장   : ThoughtWorker 자체는 저장 영향 없음, 칼라 착용 여부는 Pawn 장비/Thing 상태에 따라 관리됨

using RimWorld;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// 일반 노예 칼라를 착용한 Pawn에게 사상을 적용한다. (폭발 칼라 제외)
    /// - Stage0 : 노예 경과 시간 < (Stage1+2+3 합산 기간)
    /// - Stage1 : 노예 경과 시간 ≥ (Stage1+2+3 합산 기간)
    /// - Stage2 : Masochist 특성이 있는 경우
    /// - Stage3 : Colony 소속 Pawn인데 SlaveOfColony 아님 (예외적 상황)
    /// - Inactive : 칼라 미착용 또는 폭발 칼라 착용
    /// </summary>
    public class ThoughtWorker_SlaveCollar : ThoughtWorker
    {
        /// <summary>
        /// Pawn의 특성/소속/노예 경과시간 및 칼라 종류에 따라 ThoughtState를 반환한다.
        /// </summary>
        // TraitDef.Named 반복 호출 방지 — 1회 캐싱
        private static TraitDef _masochistTraitDef;
        private static TraitDef MasochistTrait => _masochistTraitDef ?? (_masochistTraitDef = TraitDef.Named("Masochist"));

        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            // GetSlaveCollar 1회 호출 + 폭발 칼라 제외
            var collar = SimpleSlaveryUtility.GetSlaveCollar(pawn);
            if (collar == null || collar is SlaveCollar_Explosive)
                return ThoughtState.Inactive;

            // Masochist 특성 보유 → Stage2
            if (pawn.story.traits.HasTrait(MasochistTrait))
                return ThoughtState.ActiveAtStage(2);

            // Colonist인데 SlaveOfColony 아님 → Stage3 (예외 처리)
            if (pawn.IsColonist && !pawn.IsSlaveOfColony)
                return ThoughtState.ActiveAtStage(3);

            // 노예 경과시간 기준 Stage0/1 — SlaveStage3(누적 경계값) 활용
            float time = SimpleSlaveryUtility.TimeAsSlave(pawn);
            return time < SimpleSlaveryUtility.SlaveStage3
                ? ThoughtState.ActiveAtStage(0)
                : ThoughtState.ActiveAtStage(1);
        }
    }
}
