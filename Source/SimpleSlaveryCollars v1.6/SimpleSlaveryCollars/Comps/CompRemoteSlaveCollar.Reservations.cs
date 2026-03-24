// SimpleSlaveryCollars | Comps | CompRemoteSlaveCollar.Reservations.cs
// 목적 : 원격 콘솔 Pawn 예약/해제/조회 관리
// 용도 : CompRemoteSlaveCollar의 예약 관련 partial 분리

using RimWorld;
using System.Collections.Generic;
using Verse;

namespace SimpleSlaveryCollars
{
    public partial class CompRemoteSlaveCollar
    {
        #region 예약 관리
        /// <summary>개별 Pawn에 액션 예약.</summary>
        public void ReserveJobForPawn(Pawn targetPawn, RemoteCollarAction actionType)
        {
            if (!reservedPawns.ContainsKey(targetPawn))
            {
                reservedPawns[targetPawn] = actionType;
                string actionTypeLabel = ("RemoteCollarAction_" + actionType).Translate();
                Messages.Message(
                    "RemoteCollar_ReservedJob".Translate(targetPawn.LabelShort, actionTypeLabel),
                    MessageTypeDefOf.TaskCompletion
                );
            }
            else
            {
                Messages.Message(
                    "RemoteCollar_AlreadyReserved".Translate(targetPawn.LabelShort),
                    MessageTypeDefOf.RejectInput
                );
            }
        }

        /// <summary>그룹 대상에 동일 액션 예약(기존 예약 초기화 후 재설정).</summary>
        public void ReserveJobForGroup(List<Pawn> targetPawns, RemoteCollarAction actionType)
        {
            reservedPawns.Clear();
            foreach (var pawn in targetPawns)
            {
                reservedPawns[pawn] = actionType;
            }
            string actionTypeLabel = ("RemoteCollarAction_" + actionType).Translate();
            groupJobPending = true;
            groupJobActionType = actionType;

            Messages.Message(
                "RemoteCollar_GroupReserved".Translate(targetPawns.Count, actionTypeLabel),
                MessageTypeDefOf.TaskCompletion
            );
        }

        /// <summary>해당 Pawn이 예약되어 있는지.</summary>
        public bool IsPawnReserved(Pawn pawn)
        {
            return reservedPawns.ContainsKey(pawn);
        }

        /// <summary>개별 예약 해제.</summary>
        public void ReleaseReservation(Pawn pawn)
        {
            reservedPawns.Remove(pawn);
        }

        /// <summary>Pawn에 예약된 액션 타입 반환.</summary>
        public RemoteCollarAction GetReservedAction(Pawn pawn)
        {
            return reservedPawns.TryGetValue(pawn, out var action) ? action : default;
        }

        /// <summary>예약된 Pawn 전체 반환.</summary>
        public IEnumerable<Pawn> GetAllReservedPawns()
        {
            return reservedPawns.Keys;
        }

        /// <summary>그룹 예약 취소(해당 액션 타입과 일치할 때만 제거).</summary>
        public void CancelReservationsForGroup(List<Pawn> targetPawns, RemoteCollarAction actionType)
        {
            int cancelled = 0;
            foreach (var pawn in targetPawns)
            {
                if (reservedPawns.TryGetValue(pawn, out var reservedAction))
                {
                    if (reservedAction == actionType)
                    {
                        reservedPawns.Remove(pawn);
                        cancelled++;
                    }
                }
            }
            string actionTypeLabel = ("RemoteCollarAction_" + actionType).Translate();
            Messages.Message(
                "RemoteCollar_AllReservationsCancelled".Translate(cancelled, actionTypeLabel),
                MessageTypeDefOf.RejectInput
            );
        }
        #endregion
    }
}
