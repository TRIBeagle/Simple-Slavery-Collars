// SimpleSlaveryCollars | Jobs | WorkGiver_Warden_RechargeCollar.cs
// 목적 : 충전 부족 칼라를 감지하여 Warden에게 충전 작업을 부여
// 용도 : WorkGiver 스캔 → 콘솔로 이동 → 충전 Job 생성
// 주의 : Warden만 가능, 자기 자신 제외. Stage5 노예는 다른 노예 충전 가능

using RimWorld;
using Verse;
using Verse.AI;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Jobs
{
    /// <summary>
    /// Warden 전용 WorkGiver — 칼라 충전 부족 노예를 콘솔로 데려가 충전.
    /// </summary>
    public class WorkGiver_Warden_RechargeCollar : WorkGiver_Warden
    {
        /// <summary>충전 경고 임계값. 이 이하면 충전 작업 발생.</summary>
        private const float RechargeThreshold = 0.5f;

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!SimpleSlaveryCollarsSetting.CollarChargeEnable) return null;

            var slave = t as Pawn;
            if (slave == null || pawn == slave) return null;
            if (!slave.IsSlaveOfColony) return null;
            if (slave.InAggroMentalState) return null;

            // 칼라 확인
            var collar = SimpleSlaveryUtility.GetSlaveCollar(slave) as SlaveApparel;
            if (collar == null) return null;
            if (collar.charge > RechargeThreshold) return null;

            // 예약 가능 확인
            if (!pawn.CanReserve(slave)) return null;

            // 콘솔 찾기
            var console = FindNearestConsole(pawn);
            if (console == null) return null;
            if (!pawn.CanReserveAndReach(console, PathEndMode.InteractionCell, pawn.NormalMaxDanger()))
                return null;

            var job = JobMaker.MakeJob(SimpleSlaveryDefOf.RechargeSlaveCollar, slave, console);
            return job;
        }

        /// <summary>맵에서 전원 켜진 가장 가까운 콘솔을 찾는다.</summary>
        private Building FindNearestConsole(Pawn pawn)
        {
            Building closest = null;
            float closestDist = float.MaxValue;

            var buildings = pawn.Map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < buildings.Count; i++)
            {
                var b = buildings[i];
                var comp = b.TryGetComp<CompRemoteSlaveCollar>();
                if (comp == null || !comp.PowerOn) continue;

                float dist = b.Position.DistanceToSquared(pawn.Position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = b;
                }
            }

            return closest;
        }
    }
}
