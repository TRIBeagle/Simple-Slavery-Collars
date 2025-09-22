// SimpleSlaveryCollars | Thoughts | ThoughtWorker_ExplosiveCollar.cs
// 목적   : 노예가 폭발 칼라(Explosive Collar)를 착용했을 때 정신 사상(Thought)을 부여
// 용도   : RimWorld ThoughtWorker 확장
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용
// 주의   : armed 여부와 소속(Faction)에 따라 Stage가 달라짐
// 저장   : ThoughtWorker 자체는 저장 영향 없음, 칼라 상태(armed)는 Comp 내부에서 관리됨

using RimWorld;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// 폭발 칼라를 착용한 Pawn에게 사상을 적용한다.
    /// - Stage0 : 무장되지 않은 폭발 칼라 + Colony 소속 노예
    /// - Stage1 : 무장(armed) 상태
    /// - Stage2 : 무장되지 않은 폭발 칼라 + Colony 소속 아님
    /// - Inactive : 폭발 칼라 없음
    /// </summary>
    public class ThoughtWorker_ExplosiveCollar : ThoughtWorker
    {
        /// <summary>
        /// Pawn의 칼라 상태(존재 여부, 타입, armed 여부)에 따라 ThoughtState를 반환한다.
        /// </summary>
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            if (SlaveUtility.HasSlaveCollar(pawn)
                && SlaveUtility.GetSlaveCollar(pawn).def.thingClass == typeof(SlaveCollar_Explosive))
            {
                var collar = SlaveUtility.GetSlaveCollar(pawn) as SlaveCollar_Explosive;
                if (collar.armed)
                    return ThoughtState.ActiveAtStage(1); // armed 상태
                return pawn.IsSlaveOfColony
                    ? ThoughtState.ActiveAtStage(0)        // Colony 소속 노예
                    : ThoughtState.ActiveAtStage(2);       // Colony 소속 아님
            }
            return ThoughtState.Inactive;
        }
    }
}
