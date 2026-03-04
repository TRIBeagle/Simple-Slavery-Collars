// SimpleSlaveryCollars | Thoughts | ThoughtWorker_Enslaved.cs
// 목적   : Pawn이 Colony 소속 노예일 때 억제 단계(Slavery Stage)에 따른 정신 사상을 부여
// 용도   : RimWorld ThoughtWorker 확장
// 변경   : [FIX] 부작용 로직(SetGuestStatus, Notify_DisabledWorkTypesChanged, assimilatedAtStage4 등)
//           전부 CompSlave.CompTickRare()로 이동. ThoughtWorker는 순수 Stage 판정만 수행.
// 주의   : CurrentStateInternal()은 UI 계산용으로 비동기·무작위로 수십~수백 회 호출됨.
//           상태 변경(부작용)을 넣으면 틱 스파이크, 무한 루프, Harmony 패치 연쇄 발동 위험.

using RimWorld;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// 노예 Pawn의 억제 단계(Slavery Stage)에 따라 적절한 ThoughtState를 반환한다.
    /// Stage 정의:
    ///   Stage1 = x &lt; S1
    ///   Stage2 = S1 ≤ x &lt; S2
    ///   Stage3 = S2 ≤ x &lt; S3
    ///   Stage4 = (S3 ≤ x &lt; S4) 또는 (x ≥ S4 &amp;&amp; Steadfast)
    ///   Stage5 = x ≥ S4 &amp;&amp; !Steadfast
    /// </summary>
    public class ThoughtWorker_Enslaved : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            if (!SimpleSlaveryCollarsSetting.SlavestageEnable)
                return ThoughtState.Inactive;

            if (!pawn.IsSlaveOfColony)
                return ThoughtState.Inactive;

            float time = SlaveUtility.TimeAsSlave(pawn);

            if (time < SlaveUtility.SlaveStage1)
                return ThoughtState.ActiveAtStage(0);

            if (time < SlaveUtility.SlaveStage2)
                return ThoughtState.ActiveAtStage(1);

            if (time < SlaveUtility.SlaveStage3)
                return ThoughtState.ActiveAtStage(2);

            if (time < SlaveUtility.SlaveStage4
                || (time >= SlaveUtility.SlaveStage3 && SlaveUtility.IsSteadfast(pawn)))
                return ThoughtState.ActiveAtStage(3);

            // Stage5: x ≥ S4 && !Steadfast
            return ThoughtState.ActiveAtStage(4);
        }
    }
}