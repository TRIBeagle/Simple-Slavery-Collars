// SimpleSlaveryCollars | Jobs | JobDriver_SelfRechargeCollar.cs
// 목적 : 칼라 착용자가 직접 충전소로 이동하여 자기 칼라를 충전하는 작업
// 용도 : WorkGiver_SelfRechargeCollar에서 생성. TargetA = 충전소(건물)
// 주의 : Warden 충전과 달리 에스코트 없음 — 본인이 직접 이동/대기/충전

using RimWorld;
using System.Collections.Generic;
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
            // 콘솔 전원 OFF 시 실패
            this.FailOn(() =>
            {
                var comp = Station.TryGetComp<CompRemoteSlaveCollar>();
                return comp != null && !comp.PowerOn;
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

                    // 배터리 우선, 없으면 전력망에서 소모
                    var battery = Station.TryGetComp<CompPowerBattery>();
                    if (battery != null && battery.StoredEnergy > 0f)
                    {
                        float usedWd = collar.RechargeWd(battery.StoredEnergy);
                        battery.DrawPower(usedWd);
                    }
                    else
                    {
                        collar.RechargeWd(neededWd);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
