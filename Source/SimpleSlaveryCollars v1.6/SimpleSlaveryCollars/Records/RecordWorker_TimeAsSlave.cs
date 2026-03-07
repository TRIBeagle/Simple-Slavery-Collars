// SimpleSlaveryCollars | Records | RecordWorker_TimeAsSlave.cs
// 목적   : Pawn의 노예화 시간 기록을 관리
// 용도   : RimWorld RecordWorker 확장
// 변경   : 1.6 구조 개편 — CompSlave(TimeAsSlaveTicks)를 진실원천으로 사용, Record는 세이브 직전 1회 동기화만 수행
// 주의   : Record(TimeAsSlave)는 외부 모드 호환용, 내부 누적은 중단됨
// 저장   : Record 값은 Pawn Save 시 CompSlave에서 동기화

using RimWorld;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// TimeAsSlave 기록 담당 RecordWorker.
    /// - CompSlave(TimeAsSlaveTicks)를 기준으로 동작
    /// - 자체 누적은 하지 않고 Save 시 동기화만 유지
    /// </summary>
    public class RecordWorker_TimeAsSlave : RecordWorker
    {
        /// <summary>
        /// 항상 false 반환 — Record 자체는 누적하지 않음.
        /// </summary>
        public override bool ShouldMeasureTimeNow(Pawn pawn) => false;
    }
}
