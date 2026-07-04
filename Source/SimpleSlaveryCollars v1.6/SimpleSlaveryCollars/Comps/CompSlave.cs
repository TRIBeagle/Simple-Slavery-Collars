// SimpleSlaveryCollars | Comps | CompSlave.cs
// 목적   : Pawn의 노예 상태를 Comp 단일 진실원천(SSOT)으로 관리 — 시간 누적, 족쇄, 동화 플래그, 저장/마이그레이션/동기화
// 용도   : Pawn에 부착되어 CompTickRare로 누적, 인스펙트에 노출, 저장 직전에 Record로 1회 동기화
// 변경   : [리팩터] Hediff_Enslaved의 4개 bool(shackledGoal/shackled/assimilatedAtStage4/uiRefreshedAtStage4)을 Comp로 이동.
//           Hediff_Enslaved는 빈 껍데기(세이브 역직렬화용)로 유지, 다음 버전에서 삭제 예정.
// 저장   : IExposable 필드 8종. 신규: ssc_shackledGoal/ssc_shackled/ssc_assimilatedAtStage4/ssc_uiRefreshedAtStage4/ssc_hediffDataMigrated
// 성능   : per-tick 없음(rare-tick만). 저장 시 리플렉션 멤버 탐색은 1회 캐시.

using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// Pawn의 노예 상태를 Comp로 일원화(SSOT).
    /// - 시간 누적, 족쇄 상태, 동화 플래그를 모두 관리.
    /// - 마이그레이션: Record→Comp (1회) + Hediff→Comp (안전망, 매 TickRare).
    /// - 저장 직전: Comp 값을 Record에 반영(리플렉션).
    /// </summary>
    public class CompSlave : ThingComp
    {
        // ── 시간 누적 필드 ──

        // [저장][센티널] -1f: 미설정(마이그레이션 전 상태를 의미)
        private float _timeAsSlaveTicks = -1f;

        // [저장] 누적 델타 계산 기준 Tick
        private int _lastGameTick = -1;

        // [저장] Record→Comp 마이그레이션 1회 완료 여부
        private bool _migratedOnce = false;

        // ── Hediff_Enslaved에서 이동한 필드 ──

        // [저장] 족쇄 목표 (Warden이 설정)
        private bool _shackledGoal = true;

        // [저장] 족쇄 실제 상태 (JobDriver가 적용)
        private bool _shackled = true;

        // [저장] Stage5 동화 처리 완료 여부
        private bool _assimilatedAtStage4 = false;

        // [저장] Stage5 UI 갱신 완료 여부
        private bool _uiRefreshedAtStage4 = false;

        // [저장] Hediff→Comp 마이그레이션 1회 완료 플래그
        private bool _hediffDataMigrated = false;

        // ── 프로퍼티: 시간 ──

        /// <summary>[읽기전용] 미설정이면 0으로 간주한 누적 틱.</summary>
        public float TimeAsSlaveTicks => _timeAsSlaveTicks < 0f ? 0f : _timeAsSlaveTicks;

        /// <summary>외부에서 누적값을 직접 세팅(음수 방지).</summary>
        public void SetTimeAsSlaveTicks(float ticks)
        {
            _timeAsSlaveTicks = Mathf.Max(0f, ticks);
        }

        // ── 프로퍼티: 족쇄/동화 (Hediff에서 이동) ──

        /// <summary>족쇄 목표 상태. Warden이 설정, 기즈모에서 토글.</summary>
        public bool ShackledGoal { get => _shackledGoal; set => _shackledGoal = value; }

        /// <summary>족쇄 실제 상태. JobDriver_ShackleSlave가 적용.</summary>
        public bool Shackled { get => _shackled; set => _shackled = value; }

        /// <summary>Stage5 동화 처리 완료 여부.</summary>
        public bool AssimilatedAtStage4 { get => _assimilatedAtStage4; set => _assimilatedAtStage4 = value; }

        /// <summary>Stage5 UI 갱신 완료 여부.</summary>
        public bool UiRefreshedAtStage4 { get => _uiRefreshedAtStage4; set => _uiRefreshedAtStage4 = value; }

        // ── 저장/로드 ──

        /// <summary>
        /// 저장/로드 훅. 모든 저장 필드 노출 및 저장 직전(Mode=Saving) Record 동기화 수행.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();

            // 시간 누적 필드
            Scribe_Values.Look(ref _timeAsSlaveTicks, "ssc_timeAsSlaveTicks", -1f);
            Scribe_Values.Look(ref _lastGameTick, "ssc_lastGameTick", -1);
            Scribe_Values.Look(ref _migratedOnce, "ssc_migratedOnce", false);

            // Hediff에서 이동한 필드
            Scribe_Values.Look(ref _shackledGoal, "ssc_shackledGoal", true);
            Scribe_Values.Look(ref _shackled, "ssc_shackled", true);
            Scribe_Values.Look(ref _assimilatedAtStage4, "ssc_assimilatedAtStage4", false);
            Scribe_Values.Look(ref _uiRefreshedAtStage4, "ssc_uiRefreshedAtStage4", false);
            Scribe_Values.Look(ref _hediffDataMigrated, "ssc_hediffDataMigrated", false);

            // 저장 직전 1회만 Comp→Record 동기화
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                TrySyncRecordOnSave();
            }
        }

        // ── CompTickRare ──

        /// <summary>
        /// Rare Tick(250틱)마다 호출.
        /// - 노예 상태일 때만 경과 시간 누적.
        /// - Hediff→Comp 마이그레이션 안전망.
        /// - Stage5 동화/UI갱신 처리.
        /// </summary>
        public override void CompTickRare()
        {
            base.CompTickRare();

            var pawn = parent as Pawn;
            if (pawn == null || !pawn.Spawned) return;

            int current = Find.TickManager.TicksGame;

            // 최초 진입: 기준점 세팅 + Record→Comp 1회 마이그레이션
            if (_lastGameTick < 0)
            {
                _lastGameTick = current;
                TryMigrateOnceIfNeeded(pawn);
                return;
            }

            // 노예 상태일 때만 Comp 원천값에 누적
            if (pawn.IsSlaveOfColony)
            {
                int delta = current - _lastGameTick;
                if (delta > 0)
                {
                    if (_timeAsSlaveTicks < 0f) _timeAsSlaveTicks = 0f; // 센티널→0 초기화
                    _timeAsSlaveTicks += delta;
                }
            }

            _lastGameTick = current;

            // [안전망] 고아 Hediff 마이그레이션 + 제거 (매 TickRare 체크)
            TryMigrateAndRemoveHediff(pawn);

            // Stage5 동화 및 UI 갱신
            TryProcessStage5Assimilation(pawn);
        }

        // ── Stage5 동화 ──

        /// <summary>
        /// Stage5 도달 시 동화(Assimilation) 및 UI 갱신 처리.
        /// Comp 필드를 직접 사용 (Hediff 접근 불필요).
        /// </summary>
        private void TryProcessStage5Assimilation(Pawn pawn)
        {
            if (!pawn.IsSlaveOfColony) return;
            if (_timeAsSlaveTicks < SimpleSlaveryUtility.SlaveStage4) return;
            if (SimpleSlaveryUtility.IsSteadfast(pawn)) return;

            // 동화 처리: SlaveFaction을 플레이어로 전환
            if (!_assimilatedAtStage4 && SimpleSlaveryCollarsSetting.AssimilationSlaveEnable)
            {
                if (pawn.guest != null && pawn.guest.SlaveFaction != Faction.OfPlayer)
                {
                    pawn.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);
                }
                // 확고한 충성심 무시 옵션 ON → 동화 시 충성심 해제
                if (SimpleSlaveryCollarsSetting.IgnoreUnwaveringLoyalty
                    && pawn.guest != null && !pawn.guest.Recruitable)
                {
                    pawn.guest.Recruitable = true;
                }
                _assimilatedAtStage4 = true;
            }

            // UI refresh 처리: WorkType 변경 알림
            if (!_uiRefreshedAtStage4)
            {
                pawn.Notify_DisabledWorkTypesChanged();
                _uiRefreshedAtStage4 = true;
            }
        }

        // ── 인스펙트 문자열 ──

        /// <summary>인스펙트 문자열. 노예 상태일 때만 경과 시간(가독 포맷) 표기.</summary>
        public override string CompInspectStringExtra()
        {
            if (parent is Pawn pawn && pawn.IsSlaveOfColony)
            {
                return SimpleSlaveryUtility.FormatEnslaveDurationReadable(pawn, TimeAsSlaveTicks);
            }
            return null;
        }

        // ── 기즈모 ──

        /// <summary>
        /// 기즈모: 족쇄 토글. 노예일 때만 노출.
        /// </summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!(parent is Pawn pawn))
            {
                yield break;
            }

            if (pawn.IsSlaveOfColony)
            {
                var shackleSlave = new Command_Toggle();
                shackleSlave.isActive = () => _shackledGoal;
                shackleSlave.defaultLabel = "SSC_Shackle_Label".Translate();
                shackleSlave.defaultDesc = "SSC_Shackle_Desc".Translate(pawn.Name.ToStringShort);
                shackleSlave.toggleAction = () => _shackledGoal = !_shackledGoal;
                shackleSlave.alsoClickIfOtherInGroupClicked = true;
                shackleSlave.activateSound = SoundDefOf.Click;
                shackleSlave.icon = ContentFinder<Texture2D>.Get("UI/Commands/Shackle", true);
                yield return shackleSlave;
            }
        }

        // ── 상태 리셋 ──

        /// <summary>
        /// 해방/매도 시 호출. 족쇄/동화 상태를 기본값으로 초기화.
        /// Comp는 Pawn에 상주하므로 Hediff처럼 제거되지 않음 — 대신 리셋.
        /// </summary>
        public void ResetSlaveState()
        {
            _shackledGoal = true;
            _shackled = true;
            _assimilatedAtStage4 = false;
            _uiRefreshedAtStage4 = false;
        }

        // ── 마이그레이션 ──

        /// <summary>
        /// [레거시] Record→Comp 1회 마이그레이션. 다음 버전에서 삭제 예정.
        /// </summary>
        private void TryMigrateOnceIfNeeded(Pawn pawn)
        {
            if (_migratedOnce) return;

            float legacy = pawn.records?.GetValue(SimpleSlaveryDefOf.TimeAsSlave) ?? 0f;
            bool migrated = false;

            if (_timeAsSlaveTicks < 0f && legacy > 0f)
            {
                _timeAsSlaveTicks = legacy;
                migrated = true;
            }

            _migratedOnce = true; // 저장됨 → 재로드 시 재마이그레이션 없음

            if (migrated)
            {
                Messages.Message("SSC_Migration_Done".Translate(pawn.LabelShortCap),
                                 MessageTypeDefOf.TaskCompletion,
                                 historical: false);
            }
        }

        /// <summary>
        /// [안전망] Hediff_Enslaved가 남아있으면 값을 Comp로 이전 후 제거.
        /// _migratedOnce와 무관하게 매 TickRare 체크 (기존 마이그레이션 완료 유저 대응).
        /// Hediff 없으면 즉시 return. 다음 버전에서 삭제 예정.
        /// </summary>
        private void TryMigrateAndRemoveHediff(Pawn pawn)
        {
            var hs = pawn.health?.hediffSet;
            if (hs == null) return;

            var hediff = hs.GetFirstHediffOfDef(SimpleSlaveryDefOf.Enslaved) as Hediff_Enslaved;
            if (hediff == null) return;

            // Comp에 아직 마이그레이션된 적 없을 때만 값 이전
            // (DevMode로 Hediff 수동 추가 시 기존 Comp 값 보호)
            if (!_hediffDataMigrated)
            {
                _shackledGoal = hediff.shackledGoal;
                _shackled = hediff.shackled;
                _assimilatedAtStage4 = hediff.assimilatedAtStage4;
                _uiRefreshedAtStage4 = hediff.uiRefreshedAtStage4;
                _hediffDataMigrated = true;
                Log.Message($"[SSC] {pawn.LabelShort}: Hediff_Enslaved migrated to CompSlave.");
            }

            // Hediff는 항상 제거 (빈 껍데기이므로 남아있을 이유 없음)
            pawn.health.RemoveHediff(hediff);
        }

        // ── 저장 시 동기화 ──

        /// <summary>
        /// 저장 직전 Comp→Record 동기화. SSC_ReflectionCache 사용.
        /// 실패 시 경고 로그만 남기고 안전하게 무시.
        /// </summary>
        private void TrySyncRecordOnSave()
        {
            var pawn = parent as Pawn;
            if (pawn?.records == null) return;

            if (!SimpleSlaveryReflectionUtility.IsAvailable)
            {
                Log.Warning("[SSC] Pawn_RecordsTracker DefMap not found; skipping save-time sync.");
                return;
            }

            SimpleSlaveryReflectionUtility.TrySetRecord(pawn.records, SimpleSlaveryDefOf.TimeAsSlave, TimeAsSlaveTicks);
        }
    }
}
