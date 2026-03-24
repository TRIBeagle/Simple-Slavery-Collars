// SimpleSlaveryCollars | Jobs | JobDriver_RechargeCollar.cs
// 목적 : Warden이 노예를 콘솔로 데려가 칼라를 충전하는 작업
// 용도 : WorkGiver_Warden_RechargeCollar에서 생성
// 주의 : TargetA = 노예, TargetB = 콘솔. 콘솔 전원 OFF 시 실패

using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SimpleSlaveryCollars.Jobs
{
    /// <summary>
    /// 칼라 충전 JobDriver.
    /// 1) 노예 예약 → 2) 콘솔로 이동 → 3) 대기(충전 연출) → 4) 충전 완료
    /// </summary>
    internal class JobDriver_RechargeCollar : JobDriver
    {
        private const int RechargeDuration = 300; // 충전 시간 (틱). 족쇄 작업과 동일
        private const float RechargeAmount = 1f;  // 1회 충전으로 만충

        private Pawn Slave => (Pawn)job.GetTarget(TargetIndex.A).Thing;
        private Thing Console => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Slave, job, 1, -1, null, errorOnFailed) &&
                   pawn.Reserve(Console, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 유효성 가드
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            this.FailOn(() =>
            {
                var comp = Console.TryGetComp<CompRemoteSlaveCollar>();
                return comp == null || !comp.PowerOn;
            });

            // 1) 노예/콘솔 예약
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.B);

            // 2) 노예에게 이동
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // 3) 노예를 콘솔로 에스코트
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);

            // 4) 충전 대기
            var waitToil = Toils_General.Wait(RechargeDuration, TargetIndex.B);
            waitToil.WithProgressBarToilDelay(TargetIndex.B);
            yield return waitToil;

            // 5) 충전 완료
            yield return new Toil
            {
                initAction = () =>
                {
                    // 캐리 중인 노예 내려놓기
                    if (pawn.carryTracker.CarriedThing != null)
                        pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);

                    var collar = SimpleSlaveryCollars.Utilities.SimpleSlaveryUtility.GetSlaveCollar(Slave) as SlaveApparel;
                    if (collar != null)
                    {
                        collar.Recharge(RechargeAmount);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
