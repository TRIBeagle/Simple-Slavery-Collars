// SimpleSlaveryCollars | Comps | CompSlave.cs
// 목적   : Pawn의 "노예로 지낸 시간"을 Comp 단일 진실원천(SSOT)으로 관리하고, 저장/표시/마이그레이션/동기화를 담당
// 용도   : Pawn에 부착되어 CompTickRare로 누적, 인스펙트에 노출, 저장 직전에 Record로 1회 동기화
// 변경   : [FIX] ThoughtWorker_Enslaved에서 이동된 Stage5 동화(Assimilation)/UI갱신 로직을 CompTickRare에 추가.
//           CompTickRare()는 250틱마다 1회 호출되므로 부작용 로직을 안전하게 수행 가능.
// 저장   : IExposable 필드 3종(_timeAsSlaveTicks/_lastGameTick/_migratedOnce). 새 필드 추가 없음.
// 성능   : per-tick 없음(rare-tick만). 저장 시 리플렉션 멤버 탐색은 1회 캐시.

using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// Pawn의 노예 경과 시간을 Comp로 일원화(SSOT)하여 누적/표시/세이브 동기화를 처리.
    /// - 마이그레이션: 기존 Record 값을 1회만 Comp로 가져옴.
    /// - 저장 직전: Comp 값을 Record에 반영(DefMap set_Item 리플렉션).
    /// - [FIX] Stage5 동화/UI갱신 로직을 ThoughtWorker에서 이곳으로 이동.
    /// </summary>
    public class CompSlave : ThingComp
    {
        // [저장][센티널] -1f: 미설정(마이그레이션 전 상태를 의미)
        private float _timeAsSlaveTicks = -1f;

        // [저장] 누적 델타 계산 기준 Tick
        private int _lastGameTick = -1;

        // [저장] 마이그레이션 1회 완료 여부
        private bool _migratedOnce = false;

        /// <summary>[읽기전용] null: 미설정, 값 존재 시 누적 틱.</summary>
        public float? TimeAsSlaveTicksNullable => _timeAsSlaveTicks < 0f ? (float?)null : _timeAsSlaveTicks;

        /// <summary>[읽기전용] 미설정이면 0으로 간주한 누적 틱.</summary>
        public float TimeAsSlaveTicks => _timeAsSlaveTicks < 0f ? 0f : _timeAsSlaveTicks;

        /// <summary>외부에서 누적값을 직접 세팅(음수 방지).</summary>
        public void SetTimeAsSlaveTicks(float ticks)
        {
            _timeAsSlaveTicks = Mathf.Max(0f, ticks);
        }

        /// <summary>
        /// 저장/로드 훅. 저장 필드 노출 및 저장 직전(Mode=Saving) Record 동기화 수행.
        /// 저장: ssc_timeAsSlaveTicks, ssc_lastGameTick, ssc_migratedOnce
        /// </summary>
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref _timeAsSlaveTicks, "ssc_timeAsSlaveTicks", -1f);
            Scribe_Values.Look(ref _lastGameTick, "ssc_lastGameTick", -1);
            Scribe_Values.Look(ref _migratedOnce, "ssc_migratedOnce", false);

            // 저장 직전 1회만 Comp→Record 동기화
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                TrySyncRecordOnSave();
            }
        }

        /// <summary>
        /// Rare Tick(250틱)마다 호출.
        /// - 노예 상태일 때만 경과 시간 누적.
        /// - [FIX] Stage5 동화/UI갱신 처리 (ThoughtWorker에서 이동됨).
        /// </summary>
        public override void CompTickRare()
        {
            base.CompTickRare();

            var pawn = parent as Pawn;
            if (pawn == null || !pawn.Spawned) return;

            int current = Find.TickManager.TicksGame;

            // 최초 진입: 기준점 세팅 + 1회 마이그레이션
            if (_lastGameTick < 0)
            {
                _lastGameTick = current;
                TryMigrateOnceIfNeeded(pawn);  // 기존 세이브용 1회만
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

            // [FIX] Stage5 동화 및 UI 갱신 (ThoughtWorker_Enslaved에서 이동됨)
            TryProcessStage5Assimilation(pawn);
        }

        /// <summary>
        /// [FIX] Stage5 도달 시 동화(Assimilation) 및 UI 갱신 처리.
        /// 기존 ThoughtWorker_Enslaved.CurrentStateInternal()에 있던 부작용 로직을
        /// 안전한 CompTickRare(250틱 주기)로 이동.
        /// </summary>
        private void TryProcessStage5Assimilation(Pawn pawn)
        {
            if (!pawn.IsSlaveOfColony) return;
            if (_timeAsSlaveTicks < SimpleSlaveryUtility.SlaveStage4) return;
            if (SimpleSlaveryUtility.IsSteadfast(pawn)) return;

            // Hediff_Enslaved 안전 접근
            if (pawn.health == null || pawn.health.hediffSet == null) return;
            var enslavedHediff = pawn.health.hediffSet.GetFirstHediffOfDef(SimpleSlaveryDefOf.Enslaved) as Hediff_Enslaved;
            if (enslavedHediff == null) return;

            // 동화 처리: SlaveFaction을 플레이어로 전환
            if (!enslavedHediff.assimilatedAtStage4 && SimpleSlaveryCollarsSetting.AssimilationSlaveEnable)
            {
                if (pawn.guest != null && pawn.guest.SlaveFaction != Faction.OfPlayer)
                {
                    pawn.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);
                }
                enslavedHediff.assimilatedAtStage4 = true;
            }

            // UI refresh 처리: WorkType 변경 알림
            if (!enslavedHediff.uiRefreshedAtStage4)
            {
                pawn.Notify_DisabledWorkTypesChanged();
                enslavedHediff.uiRefreshedAtStage4 = true;
            }
        }

        /// <summary>인스펙트 문자열. 노예 상태일 때만 경과 시간(가독 포맷) 표기.</summary>
        public override string CompInspectStringExtra()
        {
            if (parent is Pawn pawn && pawn.IsSlaveOfColony)
            {
                return SimpleSlaveryUtility.FormatEnslaveDurationReadable(pawn, TimeAsSlaveTicks);
            }
            return null;
        }

        /// <summary>
        /// 기즈모: 족쇄 토글. 노예+Enslaved 헤디프가 있을 때만 노출.
        /// </summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent == null)
            {
                yield break;
            }
            var pawn = parent as Pawn;

            if (pawn.IsSlaveOfColony && pawn.health.hediffSet.HasHediff(SimpleSlaveryDefOf.Enslaved))
            {
                var hediff = SimpleSlaveryUtility.GetEnslavedHediff(pawn);

                var shackleSlave = new Command_Toggle();
                shackleSlave.isActive = () => hediff.shackledGoal;
                shackleSlave.defaultLabel = "LabelWordShackle".Translate();
                shackleSlave.defaultDesc = "CommandDescriptionShackle".Translate(pawn.Name.ToStringShort);
                shackleSlave.toggleAction = () => hediff.shackledGoal = !hediff.shackledGoal;
                shackleSlave.alsoClickIfOtherInGroupClicked = true;
                shackleSlave.activateSound = SoundDefOf.Click;
                shackleSlave.icon = ContentFinder<Texture2D>.Get("UI/Commands/Shackle", true);
                yield return shackleSlave;
            }
        }

        /// <summary>
        /// 기존 세이브 1회 마이그레이션: 과거 Record(TimeAsSlave)에서 Comp로 이전.
        /// 실제 이전이 발생한 경우에만 토스트 1회 메시지.
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
                Messages.Message("SimpleSlaveryCollars_MigrationDone".Translate(pawn.LabelShortCap),
                                 MessageTypeDefOf.TaskCompletion,
                                 historical: false);
            }
        }


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