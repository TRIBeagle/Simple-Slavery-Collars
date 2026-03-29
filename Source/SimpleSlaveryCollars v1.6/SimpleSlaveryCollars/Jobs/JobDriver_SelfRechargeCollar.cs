// SimpleSlaveryCollars | Jobs | JobDriver_SelfRechargeCollar.cs
// 목적 : 칼라 착용자가 직접 충전소로 이동하여 자기 칼라를 충전하는 작업
// 용도 : WorkGiver_SelfRechargeCollar에서 생성. TargetA = 충전소(건물)
// 주의 : Warden 충전과 달리 에스코트 없음 — 본인이 직접 이동/대기/충전

using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Jobs
{
    /// <summary>
    /// 자가충전 JobDriver.
    /// 1) 충전소로 이동 → 2) 대기(충전 연출) → 3) 충전 완료
    /// </summary>
    internal class JobDriver_SelfRechargeCollar : JobDriver
    {
        private const int RechargeDuration = 300;

        private Thing Station => job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Station, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            // 충전소 유효성: 콘솔이면 전원 ON 확인, 배터리면 저장량 확인
            this.FailOn(() =>
            {
                var s = Station;
                if (s == null) return true;

                var remoteComp = s.TryGetComp<CompRemoteSlaveCollar>();
                if (remoteComp != null)
                    return !remoteComp.PowerOn;

                var battery = s.TryGetComp<CompPowerBattery>();
                if (battery != null)
                    return battery.StoredEnergy < 1f;

                return true;
            });

            // 1) 충전소로 이동
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // 2) 충전 대기
            var waitToil = Toils_General.Wait(RechargeDuration, TargetIndex.A);
            waitToil.WithProgressBarToilDelay(TargetIndex.A);
            yield return waitToil;

            // 3) 충전 완료
            yield return new Toil
            {
                initAction = () =>
                {
                    var collar = SimpleSlaveryUtility.GetSlaveCollar(pawn) as SlaveApparel;
                    if (collar == null) return;

                    float neededWd = (1f - collar.charge) * collar.BatteryCapacityWd;
                    if (neededWd <= 0f) return;

                    // 배터리 우선, 없으면 전력망 배터리에서 순차 차감
                    var battery = Station.TryGetComp<CompPowerBattery>();
                    if (battery != null && battery.StoredEnergy > 0f)
                    {
                        float usedWd = collar.RechargeWd(battery.StoredEnergy);
                        battery.DrawPower(usedWd);
                    }
                    else
                    {
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
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
