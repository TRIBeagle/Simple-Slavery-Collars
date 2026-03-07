// SimpleSlaveryCollars | Jobs | WorkGiver_ActivateRemoteCollar.cs
// 목적   : Pawn이 RemoteSlaveCollar 콘솔에서 예약된 그룹/개별 작업을 수행하도록 WorkGiver 제공
// 용도   : AI Pawn이 WorkGiver 스캔 → 콘솔 접근 가능 시 JobDriver(그룹/개별) 생성
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 주석 재작성
// 주의   : 콘솔 접근/예약 불가, 그룹 Busy 상태일 때는 Job 미할당
// 성능   : colonist 건물 목록만 스캔, 대상 Pawn 리스트는 캐싱 없이 즉시 평가

using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Jobs
{
    /// <summary>
    /// RemoteSlaveCollar 콘솔에 대한 WorkGiver.
    /// - 그룹 예약이 있으면 그룹 Job 우선 할당
    /// - 개별 예약이 있으면 개별 Job 생성
    /// - 콘솔 접근/예약 불가 또는 그룹 Busy 시 false
    /// </summary>
    public class WorkGiver_ActivateRemoteCollar : WorkGiver_Scanner
    {
        /// <summary>스캔 대상: 인공 건물(Colonist 건물 전체)</summary>
        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        /// <summary>상호작용 위치까지 이동</summary>
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        /// <summary>
        /// 맵의 Colonist 건물 중 CompRemoteSlaveCollar가 있는 대상만 후보로 반환.
        /// </summary>
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (var thing in pawn.Map.listerBuildings.allBuildingsColonist)
                if (thing.TryGetComp<CompRemoteSlaveCollar>() != null)
                    yield return thing;
        }

        /// <summary>
        /// Pawn이 주어진 콘솔에 대해 Job 수행 가능 여부 판단.
        /// - [Safety] 그룹 Busy/전원 꺼짐/예약 불가일 때 false
        /// - Pawn은 Colonist 또는 Stage5 노예여야 함
        /// - 그룹 예약이 있으면 자신 제외한 예약 리스트 확인
        /// - 개별 예약이 있으면 Pawn/콘솔 둘 다 예약 가능해야 true
        /// </summary>
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var comp = t.TryGetComp<CompRemoteSlaveCollar>();
            if (comp != null && comp.IsGroupBusy) return false;
            if (comp == null || !comp.PowerOn) return false;

            // [조건] 수행 Pawn 자격: Colonist 또는 Stage5 노예
            if (!pawn.IsColonist && !SimpleSlaveryUtility.IsStage5Slave(pawn))
                return false;

            // [Safety] 콘솔 접근/예약 불가 시 false
            if (!pawn.CanReserveAndReach(t, PathEndMode.InteractionCell, pawn.NormalMaxDanger(), 1, -1, null, forced))
                return false;

            // [Job] 그룹 예약이 있으면 자기 자신 제외 시 true
            if (comp.groupJobPending)
            {
                var reservedList = comp.GetAllReservedPawns().ToList();
                if (reservedList.Contains(pawn))
                    return false;
                if (reservedList.Count > 0)
                    return true;
            }

            // [Job] 개별 예약 확인: 자기 자신 제외, Pawn/콘솔 둘 다 예약 가능해야 함
            foreach (var targetPawn in comp.GetAllReservedPawns().ToList())
            {
                if (targetPawn == pawn)
                    continue;
                if (!pawn.CanReserve(t, 1, -1, null, forced))
                    continue;
                if (!pawn.CanReserve(targetPawn, 1, -1, null, forced))
                    continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 실제 Job 생성.
        /// - 그룹 예약 존재 → ActivateRemoteCollarGroup
        /// - 개별 예약 존재 → ActivateRemoteCollar
        /// - 예약 불가/조건 불일치 → null
        /// </summary>
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var comp = t.TryGetComp<CompRemoteSlaveCollar>();
            if (comp == null || !comp.PowerOn) return null;

            // [Job] 그룹 Job 생성
            if (comp.groupJobPending)
            {
                var reservedList = comp.GetAllReservedPawns().ToList();
                if (reservedList.Contains(pawn))
                    return null;
                if (reservedList.Count > 0)
                {
                    var groupJob = JobMaker.MakeJob(SimpleSlaveryDefOf.ActivateRemoteCollarGroup, t);
                    groupJob.targetQueueA = new List<LocalTargetInfo>(reservedList.Select(p => new LocalTargetInfo(p)));
                    groupJob.count = (int)comp.groupJobActionType;

                    comp.groupJobPending = false;
                    comp.BeginGroupBusy(180); // 약 3초(60틱=1초)

                    return groupJob;
                }
            }

            // [Job] 개별 Job 생성
            var targetPawn = comp.GetAllReservedPawns().FirstOrDefault(p => p != pawn);
            if (targetPawn == null) return null;

            if (!pawn.CanReserveAndReach(t, PathEndMode.InteractionCell, pawn.NormalMaxDanger(), 1, -1, null, forced))
                return null;
            if (!pawn.CanReserve(targetPawn, 1, -1, null, forced))
                return null;

            int actionType = (int)comp.GetReservedAction(targetPawn);
            var job = JobMaker.MakeJob(SimpleSlaveryDefOf.ActivateRemoteCollar, t, targetPawn);
            job.count = actionType;
            job.expiryInterval = 3000;
            job.checkOverrideOnExpire = true;
            return job;
        }
    }
}
