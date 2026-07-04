// SimpleSlaveryCollars | Comps | CompRemoteSlaveCollar.cs
// 목적   : 원격 노예 칼라 제어(폭발/감전/크립토) — 핵심 필드 및 상태 관리
// 용도   : Remote 콘솔(Thing)에 부착되어 Pawn 대상 제어
// 주의   : partial class — Reservations, Actions, Gizmos 파일로 분리

using RimWorld;
using System.Collections.Generic;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// RemoteSlaveCollar의 핵심 로직 컴포넌트.
    /// partial class로 분할: Core / Reservations / Actions / Gizmos
    /// </summary>
    public partial class CompRemoteSlaveCollar : ThingComp
    {
        #region 필드/상태 변수
        public bool remoteArmedExplosive = false;
        public bool remoteArmedElectric = false;
        public bool remoteArmedCrypto = false;

        private Dictionary<Pawn, RemoteCollarAction> reservedPawns = new Dictionary<Pawn, RemoteCollarAction>();

        // Scribe_Collections 임시 작업 리스트 (직렬화 전용, 저장 안 됨)
        private List<Pawn> _reservedPawnsKeysWork;
        private List<RemoteCollarAction> _reservedPawnsValuesWork;
        #endregion

        #region [FIX] 저장/로드
        /// <summary>
        /// [FIX] remotearmed* 토글 상태 + 예약 목록(reservedPawns)을 저장/로드.
        /// 기존 세이브에서는 해당 키가 없으므로 기본값(false/빈 목록)으로 로드됨 — 기존과 동일한 동작.
        /// Pawn 키는 참조(LookMode.Reference)로 저장하며, 로드 후 소멸/사망 Pawn 키는
        /// PostSpawnSetup에서 정리한다.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref remoteArmedExplosive, "ssc_remoteArmedExplosive", false);
            Scribe_Values.Look(ref remoteArmedElectric, "ssc_remoteArmedElectric", false);
            Scribe_Values.Look(ref remoteArmedCrypto, "ssc_remoteArmedCrypto", false);

            Scribe_Collections.Look(ref reservedPawns, "ssc_reservedPawns",
                LookMode.Reference, LookMode.Value,
                ref _reservedPawnsKeysWork, ref _reservedPawnsValuesWork);

            // 구세이브(노드 없음) 로드 시 null 방어
            if (Scribe.mode == LoadSaveMode.PostLoadInit && reservedPawns == null)
                reservedPawns = new Dictionary<Pawn, RemoteCollarAction>();
        }
        #endregion

        #region 전원 확인
        // [성능] CompPowerTrader를 스폰 시 1회 캐시 — PowerOn은 JobDriver FailOn 등에서 매 틱 호출됨
        private CompPowerTrader _powerTraderCache;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            _powerTraderCache = parent.GetComp<CompPowerTrader>();

            // 로드 직후: 참조 해제 실패(null)·사망 Pawn 예약 정리
            if (respawningAfterLoad)
                PruneInvalidReservations();
        }

        /// <summary>전원 연결/ON 여부.</summary>
        public bool PowerOn
        {
            get
            {
                // 캐시 미스(스폰 전 조기 호출 등) 시 폴백 탐색
                var comp = _powerTraderCache ?? (_powerTraderCache = parent.GetComp<CompPowerTrader>());
                return comp != null && comp.PowerOn;
            }
        }
        #endregion
    }
}
