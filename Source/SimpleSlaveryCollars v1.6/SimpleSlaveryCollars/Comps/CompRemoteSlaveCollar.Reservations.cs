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
                string actionTypeLabel = ("SSC_Action_" + actionType).Translate();
                Messages.Message(
                    "SSC_Remote_Reserved".Translate(targetPawn.LabelShort, actionTypeLabel),
                    MessageTypeDefOf.TaskCompletion
                );
            }
            else
            {
                Messages.Message(
                    "SSC_Remote_AlreadyReserved".Translate(targetPawn.LabelShort),
                    MessageTypeDefOf.RejectInput
                );
            }
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

        /// <summary>예약이 하나라도 있는지(값싼 조기 탈락용).</summary>
        public bool HasAnyReservation => reservedPawns.Count > 0;

        /// <summary>
        /// null/사망/소멸된 예약 Pawn을 정리한다.
        /// 로드 직후(참조 해제 실패로 null 키 발생 가능) 및 런타임 stale 방지용.
        /// </summary>
        public void PruneInvalidReservations()
        {
            if (reservedPawns.Count == 0) return;

            List<Pawn> invalid = null;
            foreach (var kv in reservedPawns)
            {
                var p = kv.Key;
                if (p == null || p.Dead || p.Destroyed)
                    (invalid ?? (invalid = new List<Pawn>())).Add(p);
            }
            if (invalid != null)
            {
                for (int i = 0; i < invalid.Count; i++)
                    reservedPawns.Remove(invalid[i]);
            }
        }
        #endregion
    }
}
