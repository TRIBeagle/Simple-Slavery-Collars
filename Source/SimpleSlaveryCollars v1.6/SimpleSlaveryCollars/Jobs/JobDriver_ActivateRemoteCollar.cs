// SimpleSlaveryCollars | Jobs | JobDriver_ActivateRemoteCollar.cs
// 목적   : 콘솔 A에서 지정 Pawn B에게 원격 칼라 액션(단일 대상)을 실행
// 용도   : WorkGiver가 Pawn 단일 예약 시 자동 배정, 액션 수행 후 예약 해제
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 요약 재작성
// 주의   : 진행 중 경합·도달 불가 시 FailOn으로 즉시 중단
// 성능   : Toil 경로 단순, 대기 60~120틱. LINQ 미사용, null 가드 강화
// 비고   : TargetIndex.A=Console, TargetIndex.B=Pawn, job.count=RemoteCollarAction

using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SimpleSlaveryCollars.Jobs
{
    /// <summary>
    /// 단일 대상 원격 칼라 액션 JobDriver.
    /// - 콘솔 A를 예약·도달
    /// - 대상 Pawn B에 대해 job.count에 지정된 액션 실행
    /// - 실행 직후 Pawn 예약 해제
    /// </summary>
    public class JobDriver_ActivateRemoteCollar : JobDriver
    {
        private const TargetIndex ConsoleInd = TargetIndex.A;
        private const TargetIndex PawnInd = TargetIndex.B;

        private Building Console => (Building)job.GetTarget(ConsoleInd).Thing;   // [안전] null/파괴 가드
        private Pawn TargetPawn => (Pawn)job.GetTarget(PawnInd).Thing;           // [안전] null/사망 가드

        /// <summary>
        /// 콘솔·Pawn 예약 시도. 도달 불가/경합 시 false.
        /// </summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            var console = Console;
            var target = TargetPawn;

            if (console == null || console.Destroyed) return false;
            if (target == null || target.Dead) return false;

            // [Safety] 도달 불가/선점 방지
            if (!pawn.CanReserveAndReach(console, PathEndMode.InteractionCell, pawn.NormalMaxDanger(), 1, -1, null, false))
                return false;
            if (!pawn.CanReserve(target, 1, -1, null, false))
                return false;

            // 명시적 예약 (maxPawns=1, stackCount=-1)
            if (!pawn.Reserve(console, job, 1, -1, null, errorOnFailed)) return false;
            if (!pawn.Reserve(target, job, 1, -1, null, errorOnFailed)) return false;

            return true;
        }

        /// <summary>
        /// Toil 시퀀스:
        /// 1) 콘솔 이동 → 2) 짧은 대기 → 3) 액션 실행 및 예약 해제
        /// </summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            // [Safety] 기본 유효성/상태 가드
            this.FailOnDespawnedNullOrForbidden(ConsoleInd);
            this.FailOnDespawnedNullOrForbidden(PawnInd);
            this.FailOnBurningImmobile(ConsoleInd);
            this.FailOn(() => TargetPawn == null || TargetPawn.Dead);
            this.FailOn(() => !pawn.CanReserve(job.targetA.Thing));
            this.FailOn(() => !pawn.CanReserve(job.targetB.Thing));

            // [Toils] 1) 콘솔 상호작용 위치로 이동
            yield return Toils_Goto.GotoThing(ConsoleInd, PathEndMode.InteractionCell)
                                   .FailOn(() => !pawn.CanReserve(job.targetA.Thing));

            // [Toils] 2) 짧은 대기(연출용, 60~120틱)
            var shortWait = Toils_General.Wait(90, ConsoleInd);
            shortWait.WithProgressBarToilDelay(ConsoleInd);
            yield return shortWait;

            // [Toils] 3) 액션 실행 및 Pawn 예약 해제
            yield return new Toil
            {
                initAction = () =>
                {
                    var console = Console;
                    var target = TargetPawn;
                    if (console == null || console.Destroyed) return;
                    if (target == null || target.Dead) return;

                    var comp = console.TryGetComp<CompRemoteSlaveCollar>();
                    if (comp == null) return;

                    var actionType = (RemoteCollarAction)job.count;
                    switch (actionType)
                    {
                        case RemoteCollarAction.ArmExplosive:
                            comp.DoRemoteCollarExplosive(true, target);
                            break;
                        case RemoteCollarAction.DisarmExplosive:
                            comp.DoRemoteCollarExplosive(false, target);
                            break;
                        case RemoteCollarAction.DetonateExplosive:
                            comp.DoRemoteCollarGoBoom(target);
                            break;
                        case RemoteCollarAction.ArmElectric:
                            comp.DoRemoteCollarElectric(true, target);
                            break;
                        case RemoteCollarAction.DisarmElectric:
                            comp.DoRemoteCollarElectric(false, target);
                            break;
                        case RemoteCollarAction.ArmCrypto:
                            comp.DoRemoteCollarCrypto(true, target);
                            break;
                        case RemoteCollarAction.DisarmCrypto:
                            comp.DoRemoteCollarCrypto(false, target);
                            break;
                        default:
                            // [안전] 알 수 없는 액션은 무시
                            break;
                    }

                    // [Safety] 액션 직후 대상 Pawn 예약 해제 → 중복 Job 방지
                    comp.ReleaseReservation(target);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
