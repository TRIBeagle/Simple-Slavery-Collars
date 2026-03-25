// SimpleSlaveryCollars | Jobs | WorkGiver_Warden_ShackleSlave.cs
// 목적   : Warden Pawn이 노예의 ShackledGoal과 Shackled 상태 불일치 시 'ShackleSlave' 작업을 수행하게 함
// 용도   : WorkGiver 스캔 시 조건 충족 노예 Pawn에게 JobDriver_ShackleSlave 부여
// 변경   : [리팩터] Hediff_Enslaved 참조를 CompSlave 참조로 변경
// 주의   : 자기 자신/비노예/정신 이상/예약 불가/이미 일치 상태일 경우 Job 미할당
// 성능   : 단순 조건 분기와 Toil 시퀀스, 성능 영향 미미

using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SimpleSlaveryCollars.Jobs
{
    /// <summary>
    /// Warden 전용 WorkGiver.
    /// - Colony 노예 Pawn만 대상
    /// - CompSlave 보유 + ShackledGoal≠Shackled 시 Job 생성
    /// </summary>
    public class WorkGiver_Warden_ShackleSlave : WorkGiver_Warden
    {
        /// <summary>Warden은 Pawn에 직접 접근해 상호작용 → Touch 모드.</summary>
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        /// <summary>
        /// Pawn t(노예)에 대해 'ShackleSlave' 작업을 부여할지 여부 판단.
        /// - [Safety] 자기 자신, 비노예, CompSlave 없음, 예약 불가, 목표=현상태 동일, 정신 이상 시 null
        /// - 조건 충족 시 JobDriver_ShackleSlave를 반환
        /// </summary>
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var slave = t as Pawn;
            if (pawn == slave) return null;

            if (slave == null ||
                !slave.IsSlaveOfColony ||
                !pawn.CanReserve(slave) ||
                slave.InAggroMentalState)
            {
                return null;
            }

            // CompSlave 1회만 조회
            var comp = slave.GetComp<CompSlave>();
            if (comp == null || comp.ShackledGoal == comp.Shackled)
                return null;

            return JobMaker.MakeJob(SimpleSlaveryDefOf.ShackleSlave, slave);
        }
    }

    /// <summary>
    /// 노예 Pawn의 ShackledGoal을 실제 Shackled 상태로 동기화하는 JobDriver.
    /// - Victim 예약→이동→300틱 대기→Shackled 상태 업데이트
    /// </summary>
    internal class JobDriver_ShackleSlave : JobDriver
    {
        private const int ShackleDuration = 300;

        private Pawn Victim => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        /// <summary>Victim 예약 시도.</summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Victim, job, 1, -1, null);
        }

        /// <summary>
        /// Toil 시퀀스:
        /// 1) Victim 예약 → 2) Victim 위치 이동 → 3) 대기(300틱) → 4) ShackledGoal 적용
        /// </summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            // [Safety] Victim 유효성/노예 여부 보장
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !Victim.IsSlaveOfColony);

            // [Toils] 1) Victim 예약
            yield return Toils_Reserve.Reserve(TargetIndex.A);

            // [Toils] 2) Victim 위치로 이동
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // [Toils] 3) Victim과 함께 대기(연출/확정)
            yield return Toils_General.WaitWith(TargetIndex.A, ShackleDuration, true);

            // [Toils] 4) ShackledGoal → Shackled 반영
            yield return new Toil
            {
                initAction = () =>
                {
                    var comp = Victim.GetComp<CompSlave>();
                    if (comp != null)
                        comp.Shackled = comp.ShackledGoal;
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
