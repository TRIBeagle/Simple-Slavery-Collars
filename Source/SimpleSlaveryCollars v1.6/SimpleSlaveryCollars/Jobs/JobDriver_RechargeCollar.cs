// SimpleSlaveryCollars | Jobs | JobDriver_RechargeCollar.cs
// 목적 : Warden이 노예/죄수를 들고 충전소로 이동하여 칼라를 충전하는 작업
// 용도 : WorkGiver_Warden_RechargeCollar에서 생성. TargetA = 대상 폰, TargetB = 충전소
// 주의 : 운반 중 TargetA는 디스폰 → FailOnDestroyedOrNull 사용. 배터리/콘솔 양쪽 지원

using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Jobs
{
    /// <summary>
    /// 칼라 충전 JobDriver.
    /// 1) 대상에게 이동 → 2) 들기 → 3) 충전소로 운반 → 4) 내려놓기 → 5) 충전 대기 → 6) 충전 완료
    /// </summary>
    internal class JobDriver_RechargeCollar : JobDriver
    {
        private const int RechargeDuration = 300; // 충전 시간 (틱)

        private Pawn Target => (Pawn)job.GetTarget(TargetIndex.A).Thing;
        private Thing Station => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Target, job, 1, -1, null, errorOnFailed) &&
                   pawn.Reserve(Station, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 운반 중 대상은 디스폰되므로 Destroyed 체크만
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            // 충전소 유효성: 콘솔이면 전원 ON, 배터리면 저장량 확인
            this.FailOn(() =>
            {
                var station = Station;
                if (station == null) return true;

                var remoteComp = station.TryGetComp<CompRemoteSlaveCollar>();
                if (remoteComp != null)
                    return !remoteComp.PowerOn;

                var battery = station.TryGetComp<CompPowerBattery>();
                if (battery != null)
                    return battery.StoredEnergy < 1f;

                return true;
            });

            // 1) 대상에게 이동
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

            // 2) 대상 들기
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);

            // 3) 충전소로 운반
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);

            // 4) 든 채로 충전 대기
            var waitToil = Toils_General.Wait(RechargeDuration, TargetIndex.B);
            waitToil.WithProgressBarToilDelay(TargetIndex.B);
            yield return waitToil;

            // 5) 충전 완료 + 내려놓기
            yield return new Toil
            {
                initAction = () =>
                {
                    var collar = SimpleSlaveryUtility.GetSlaveCollar(Target) as SlaveApparel;
                    if (collar != null)
                    {
                        float neededWd = (1f - collar.charge) * collar.BatteryCapacityWd;
                        if (neededWd > 0f)
                        {
                            // 배터리에서 전력 차감 (부분 충전 가능)
                            var battery = Station.TryGetComp<CompPowerBattery>();
                            if (battery != null && battery.StoredEnergy > 0f)
                            {
                                float usedWd = collar.RechargeWd(battery.StoredEnergy);
                                battery.DrawPower(usedWd);
                            }
                            else
                            {
                                // 전력망 배터리 전체에서 순차 차감
                                float usedWd = collar.RechargeWd(neededWd);
                                var net = Station.TryGetComp<CompPowerTrader>()?.PowerNet;
                                if (net != null)
                                {
                                    var batts = net.batteryComps;
                                    float remaining = usedWd;
                                    for (int i = 0; i < batts.Count && remaining > 0f; i++)
                                    {
                                        float draw = Mathf.Min(remaining, batts[i].StoredEnergy);
                                        if (draw <= 0f) continue;
                                        batts[i].DrawPower(draw);
                                        remaining -= draw;
                                    }
                                }
                            }
                        }
                    }

                    // 내려놓기
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing _);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            // 6) 죄수면 감방으로 되돌려놓기
            yield return new Toil
            {
                initAction = () =>
                {
                    if (Target == null || Target.Dead || !Target.Spawned) return;
                    if (!Target.IsPrisonerOfColony) return;

                    var bed = RestUtility.FindBedFor(Target, pawn, checkSocialProperness: true);
                    if (bed != null)
                    {
                        var escortJob = JobMaker.MakeJob(JobDefOf.EscortPrisonerToBed, Target, bed);
                        escortJob.count = 1;
                        pawn.jobs.jobQueue.EnqueueFirst(escortJob);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
