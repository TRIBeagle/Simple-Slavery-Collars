// SimpleSlaveryCollars | Jobs | WorkGiver_ActivateRemoteCollar.cs
// 목적   : Pawn이 RemoteSlaveCollar 콘솔에서 예약된 개별 작업을 수행하도록 WorkGiver 제공
// 용도   : AI Pawn이 WorkGiver 스캔 → 콘솔 접근 가능 시 개별 JobDriver 생성
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 주석 재작성
// 주의   : 콘솔 접근/예약 불가 시 Job 미할당
// 성능   : colonist 건물 목록만 스캔, 대상 Pawn 리스트는 캐싱 없이 즉시 평가

using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Jobs
{
    /// <summary>
    /// RemoteSlaveCollar 콘솔에 대한 WorkGiver.
    /// - 개별 예약이 있으면 개별 Job 생성
    /// - 콘솔 접근/예약 불가 시 false
    /// </summary>
    public class WorkGiver_ActivateRemoteCollar : WorkGiver_Scanner
    {
        /// <summary>스캔 대상: 인공 건물(Colonist 건물 전체)</summary>
        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        /// <summary>상호작용 위치까지 이동</summary>
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        /// <summary>
        /// [성능] 수행 자격이 없는 Pawn(정착민/Stage5 노예 외)은 건물 스캔 자체를 건너뜀.
        /// </summary>
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !pawn.IsColonist && !SimpleSlaveryUtility.IsStage5Slave(pawn);
        }

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
        /// - [Safety] 전원 꺼짐/예약 불가일 때 false
        /// - Pawn은 Colonist 또는 Stage5 노예여야 함
        /// - 개별 예약이 있으면 Pawn/콘솔 둘 다 예약 가능해야 true
        /// </summary>
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var comp = t.TryGetComp<CompRemoteSlaveCollar>();
            if (comp == null || !comp.PowerOn) return false;

            // [조건] 수행 Pawn 자격: Colonist 또는 Stage5 노예
            if (!pawn.IsColonist && !SimpleSlaveryUtility.IsStage5Slave(pawn))
                return false;

            // [성능] 예약이 하나도 없으면 비싼 도달성 계산 전에 조기 탈락
            if (!comp.HasAnyReservation)
                return false;

            // [Safety] 콘솔 접근/예약 불가 시 false
            if (!pawn.CanReserveAndReach(t, PathEndMode.InteractionCell, pawn.NormalMaxDanger(), 1, -1, null, forced))
                return false;

            // [Job] 개별 예약 확인: 자기 자신 제외, 대상 Pawn도 예약 가능해야 함
            // 콘솔 예약 가능 여부는 이미 위 CanReserveAndReach에서 검증됨
            foreach (var targetPawn in comp.GetAllReservedPawns())
            {
                if (targetPawn == pawn) continue;
                // 사망/디스폰/소멸 대상은 무효 — stale 예약이 true를 반환하지 않도록 방어
                if (targetPawn == null || targetPawn.Dead || !targetPawn.Spawned) continue;
                if (!pawn.CanReserve(targetPawn, 1, -1, null, forced)) continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 실제 Job 생성.
        /// - 개별 예약 존재 → ActivateRemoteCollar
        /// - 예약 불가/조건 불일치 → null
        /// </summary>
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var comp = t.TryGetComp<CompRemoteSlaveCollar>();
            if (comp == null || !comp.PowerOn) return null;

            // 무효(사망/소멸) 예약을 이 시점에 실제로 제거해 stale 누적 방지
            comp.PruneInvalidReservations();

            // [Job] 개별 Job 생성 — 자기 자신 제외, 생존+스폰 중인 첫 Pawn
            Pawn targetPawn = null;
            foreach (var rp in comp.GetAllReservedPawns())
            {
                if (rp != pawn && !rp.Dead && rp.Spawned)
                {
                    targetPawn = rp;
                    break;
                }
            }
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
