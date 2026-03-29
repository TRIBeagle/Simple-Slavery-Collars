// SimpleSlaveryCollars | Comps | CompRemoteSlaveCollar.cs
// 목적   : 원격 노예 칼라 제어(폭발/감전/크립토) — 핵심 필드 및 상태 관리
// 용도   : Remote 콘솔(Thing)에 부착되어 Pawn 대상 제어
// 주의   : partial class — Reservations, Actions, Gizmos 파일로 분리

using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// RemoteSlaveCollar의 핵심 로직 컴포넌트.
    /// partial class로 분할: Core / Reservations / Actions / Gizmos
    /// </summary>
    public partial class CompRemoteSlaveCollar : ThingComp
    {
        #region 상태/Busy 관리
        /// <summary>[레이스] 그룹 작업 중 Busy 종료 Tick. 현재 Tick보다 크면 Busy.</summary>
        public int groupBusyUntilTick = 0;

        /// <summary>그룹 작업이 Busy 상태인지.</summary>
        public bool IsGroupBusy => Find.TickManager.TicksGame < groupBusyUntilTick;

        /// <summary>그룹 Busy 시작(지속 ticks).</summary>
        public void BeginGroupBusy(int ticks)
        {
            groupBusyUntilTick = Find.TickManager.TicksGame + ticks;
        }

        /// <summary>그룹 Busy 종료.</summary>
        public void EndGroupBusy()
        {
            groupBusyUntilTick = 0;
        }
        #endregion

        #region 필드/상태 변수
        public bool remoteArmedExplosive = false;
        public bool remoteArmedElectric = false;
        public bool remoteArmedCrypto = false;

        private Dictionary<Pawn, RemoteCollarAction> reservedPawns = new Dictionary<Pawn, RemoteCollarAction>();
        public bool groupJobPending = false;
        public RemoteCollarAction groupJobActionType;
        #endregion

        #region [FIX] 저장/로드
        /// <summary>
        /// [FIX] remotearmed* 토글 상태를 저장/로드.
        /// 기존 세이브에서는 이 키가 없으므로 기본값(false)으로 로드됨 — 기존과 동일한 동작.
        /// reservedPawns/groupJobPending은 런타임 전용이므로 저장하지 않음.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref remoteArmedExplosive, "ssc_remoteArmedExplosive", false);
            Scribe_Values.Look(ref remoteArmedElectric, "ssc_remoteArmedElectric", false);
            Scribe_Values.Look(ref remoteArmedCrypto, "ssc_remoteArmedCrypto", false);
        }
        #endregion

        #region 전원 확인
        /// <summary>전원 연결/ON 여부.</summary>
        public bool PowerOn
        {
            get
            {
                CompPowerTrader comp = this.parent.GetComp<CompPowerTrader>();
                return comp != null && comp.PowerOn;
            }
        }
        #endregion
    }
}
