// SimpleSlaveryCollars | Jobs | JobDriver_ActivateRemoteCollarGroup.cs
// 목적   : 콘솔 A에서 그룹 대상 Pawn들에 대해 원격 칼라 액션을 브로드캐스트 실행
// 용도   : WorkGiver가 Pawn 그룹 예약 시 자동 배정, 그룹 액션 실행 후 Pawn별 예약 해제
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 주석 재작성
// 주의   : 진행 중 경합·도달 불가 시 FailOn으로 즉시 중단, 그룹 Busy로 중복 실행 방지
// 성능   : Toil 경로 단순, 대기 60틱. for 루프 사용으로 할당 최소화

using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SimpleSlaveryCollars.Jobs
{
    /// <summary>
    /// 그룹 대상에 RemoteCollarAction을 브로드캐스트 실행하는 JobDriver.
    /// - 콘솔 A 예약 및 도달
    /// - Pawn 대상 큐를 순회하며 액션 실행
    /// - 실행 직후 대상 Pawn 예약 해제, 마지막에 그룹 Busy 해제
    /// </summary>
    public class JobDriver_ActivateRemoteCollarGroup : JobDriver
    {
        private const TargetIndex ConsoleInd = TargetIndex.A;

        private Building Console => (Building)job.GetTarget(ConsoleInd).Thing; // [안전] null 가드 아래 처리

        /// <summary>
        /// 콘솔 예약 시도. 도달 불가/경합 시 false.
        /// </summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            var console = Console;
            if (console == null || console.Destroyed) return false;

            // [Safety] 상호작용 셀까지 도달 가능 + 선점 충돌 방지
            if (!pawn.CanReserveAndReach(console, PathEndMode.InteractionCell, pawn.NormalMaxDanger(), 1, -1, null, false))
                return false;

            return pawn.Reserve(console, job, 1, -1, null, errorOnFailed);
        }

        /// <summary>
        /// Toil 시퀀스:
        /// 1) 콘솔 이동 → 2) 짧은 대기 → 3) 그룹 전체 액션 실행 및 Pawn 예약 해제
        /// </summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            // [Safety] 기본 유효성/상태 가드
            this.FailOnDespawnedNullOrForbidden(ConsoleInd);
            this.FailOnBurningImmobile(ConsoleInd);
            this.FailOn(() => !pawn.CanReserve(job.targetA.Thing));

            // [Toils] 1) 콘솔 상호작용 지점으로 이동
            yield return Toils_Goto.GotoThing(ConsoleInd, PathEndMode.InteractionCell)
                                   .FailOn(() => !pawn.CanReserve(job.targetA.Thing));

            // [Toils] 2) 짧은 대기(연출/선점 유지) — 프로그레스바 표시
            var shortWait = Toils_General.Wait(60, ConsoleInd);
            shortWait.WithProgressBarToilDelay(ConsoleInd);
            yield return shortWait;

            // [Toils] 3) 그룹 전체 작업 실행
            yield return new Toil
            {
                initAction = () =>
                {
                    var console = Console;
                    if (console == null || console.Destroyed) return;

                    var comp = console.TryGetComp<CompRemoteSlaveCollar>();
                    if (comp == null) return;

                    var actionType = (RemoteCollarAction)job.count;
                    var targets = job.targetQueueA;
                    if (targets == null || targets.Count == 0) return;

                    // 그룹 Busy 시작(중복 실행 방지)
                    comp.BeginGroupBusy(180);
                    try
                    {
                        for (int i = 0; i < targets.Count; i++)
                        {
                            var pawnTarget = targets[i].Thing as Pawn;
                            if (pawnTarget == null || pawnTarget.Dead) continue;

                            switch (actionType)
                            {
                                case RemoteCollarAction.ArmExplosive:
                                    comp.DoRemoteCollarExplosive(true, pawnTarget);
                                    break;
                                case RemoteCollarAction.DisarmExplosive:
                                    comp.DoRemoteCollarExplosive(false, pawnTarget);
                                    break;
                                case RemoteCollarAction.DetonateExplosive:
                                    comp.DoRemoteCollarGoBoom(pawnTarget);
                                    break;
                                case RemoteCollarAction.ArmElectric:
                                    comp.DoRemoteCollarElectric(true, pawnTarget);
                                    break;
                                case RemoteCollarAction.DisarmElectric:
                                    comp.DoRemoteCollarElectric(false, pawnTarget);
                                    break;
                                case RemoteCollarAction.ArmCrypto:
                                    comp.DoRemoteCollarCrypto(true, pawnTarget);
                                    break;
                                case RemoteCollarAction.DisarmCrypto:
                                    comp.DoRemoteCollarCrypto(false, pawnTarget);
                                    break;
                                default:
                                    break;
                            }

                            // [Safety] 실행 직후 대상 Pawn 예약 해제 → 중복 Job 방지
                            comp.ReleaseReservation(pawnTarget);
                        }
                    }
                    finally
                    {
                        // 혹시 남은 대상이 있으면 일괄 정리
                        var left = job.targetQueueA;
                        if (left != null)
                        {
                            for (int i = 0; i < left.Count; i++)
                            {
                                var p2 = left[i].Thing as Pawn;
                                if (p2 != null) comp.ReleaseReservation(p2);
                            }
                        }

                        // 그룹 Busy 해제
                        comp.EndGroupBusy();
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
