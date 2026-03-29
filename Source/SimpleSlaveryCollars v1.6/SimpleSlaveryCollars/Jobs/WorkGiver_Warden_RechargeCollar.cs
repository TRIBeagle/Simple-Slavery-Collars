// SimpleSlaveryCollars | Jobs | WorkGiver_Warden_RechargeCollar.cs
// 목적 : 충전 부족 칼라를 감지하여 Warden에게 충전 작업을 부여
// 용도 : 노예/죄수 대상. Warden이 들고 충전소로 이동하여 충전
// 주의 : 소집/병상/정신붕괴 상태는 제외

using RimWorld;
using Verse;
using Verse.AI;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Jobs
{
    /// <summary>
    /// Warden 전용 WorkGiver — 칼라 충전 부족 노예/죄수를 충전소로 데려가 충전.
    /// </summary>
    public class WorkGiver_Warden_RechargeCollar : WorkGiver_Warden
    {
        /// <summary>최소 배터리 저장량. 이 미만이면 충전소로 인정 안함.</summary>
        private const float MinBatteryEnergy = 1f;

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!SimpleSlaveryCollarsSetting.CollarChargeEnable) return null;

            var target = t as Pawn;
            if (target == null || pawn == target) return null;
            if (!target.IsSlaveOfColony && !target.IsPrisonerOfColony) return null;
            if (target.InAggroMentalState) return null;
            if (target.Drafted) return null;
            if (target.Downed) return null;
            if (target.InBed()) return null;

            // 칼라 확인
            var collar = SimpleSlaveryUtility.GetSlaveCollar(target) as SlaveApparel;
            if (collar == null) return null;
            if (collar.charge > collar.rechargeThreshold) return null;

            // 예약 가능 확인
            if (!pawn.CanReserve(target)) return null;

            // 충전소 찾기 (콘솔 또는 배터리)
            var station = FindNearestChargeStation(pawn);
            if (station == null) return null;
            if (!pawn.CanReserveAndReach(station, PathEndMode.InteractionCell, pawn.NormalMaxDanger()))
                return null;

            var job = JobMaker.MakeJob(SimpleSlaveryDefOf.RechargeSlaveCollar, target, station);
            job.count = 1;
            return job;
        }

        /// <summary>맵에서 가장 가까운 충전소를 찾는다 (콘솔 또는 전력 보유 배터리).</summary>
        internal static Building FindNearestChargeStation(Pawn pawn)
        {
            Building closest = null;
            float closestDist = float.MaxValue;

            var buildings = pawn.Map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                var b = buildings[i];

                // 콘솔 (전원 ON)
                var remoteComp = b.TryGetComp<CompRemoteSlaveCollar>();
                if (remoteComp != null && remoteComp.PowerOn)
                {
                    float dist = b.Position.DistanceToSquared(pawn.Position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = b;
                    }
                    continue;
                }

                // 배터리 (저장 전력 있음)
                var battery = b.TryGetComp<CompPowerBattery>();
                if (battery != null && battery.StoredEnergy >= MinBatteryEnergy)
                {
                    float dist = b.Position.DistanceToSquared(pawn.Position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = b;
                    }
                }
            }

            return closest;
        }
    }
}
