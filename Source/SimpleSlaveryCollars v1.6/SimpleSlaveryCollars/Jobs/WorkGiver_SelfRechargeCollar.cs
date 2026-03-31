// SimpleSlaveryCollars | Jobs | WorkGiver_SelfRechargeCollar.cs
// 목적 : Stage5 노예/정착민이 자기 칼라를 직접 충전소에서 충전하는 WorkGiver
// 용도 : Stage5 노예/정착민이 충전 부족 시 자동으로 충전소 이동 Job 생성
// 주의 : ShouldSkip에서 칼라 상태 사전 필터 → 불필요한 건물 스캔 방지

using RimWorld;
using Verse;
using Verse.AI;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Jobs
{
    /// <summary>
    /// 자가충전 WorkGiver — 칼라 착용자가 직접 충전소로 이동하여 충전.
    /// Stage5 노예 또는 정착민만 가능.
    /// </summary>
    public class WorkGiver_SelfRechargeCollar : WorkGiver_Scanner
    {
        /// <summary>최소 배터리 저장량.</summary>
        private const float MinBatteryEnergy = 1f;

        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        /// <summary>
        /// 칼라 조건 사전 필터. 충전 불필요하면 건물 스캔 자체를 건너뜀.
        /// </summary>
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (!SimpleSlaveryCollarsSetting.CollarChargeEnable) return true;

            var collar = SimpleSlaveryUtility.GetSlaveCollar(pawn) as SlaveApparel;
            if (collar == null) return true;
            if (collar.charge > collar.rechargeThreshold) return true;

            // Stage5 노예 또는 자유 정착민(칼라 착용)만 가능
            if (!pawn.IsFreeNonSlaveColonist && !SimpleSlaveryUtility.IsStage5Slave(pawn)) return true;

            return false;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!IsValidChargeStation(t)) return false;
            return pawn.CanReserveAndReach(t, PathEndMode.InteractionCell,
                pawn.NormalMaxDanger(), 1, -1, null, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!HasJobOnThing(pawn, t, forced)) return null;
            return JobMaker.MakeJob(SimpleSlaveryDefOf.SelfRechargeSlaveCollar, t);
        }

        /// <summary>콘솔(전원 ON) 또는 배터리(전력 보유)면 충전소로 인정.</summary>
        private static bool IsValidChargeStation(Thing t)
        {
            var remoteComp = t.TryGetComp<CompRemoteSlaveCollar>();
            if (remoteComp != null && remoteComp.PowerOn)
                return true;

            var battery = t.TryGetComp<CompPowerBattery>();
            if (battery != null && battery.StoredEnergy >= MinBatteryEnergy)
                return true;

            return false;
        }
    }
}
