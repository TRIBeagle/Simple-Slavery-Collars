// SimpleSlaveryCollars | Comps | CompRemoteSlaveCollar.cs
// 목적   : 원격 노예 칼라 제어(폭발/감전/크립토) 및 그룹/개별 예약·실행·UI 제공
// 용도   : Remote 콘솔(Thing)에 부착되어 Pawn 대상 제어, FloatMenu·Gizmo 노출
// 변경   : [FIX] PostExposeData() 추가 — remotearmed* 토글 상태 저장/로드. 기존 세이브에서는 기본값(false)으로 로드됨.
// 주의   : 그룹 진행 중 중복 예약·실행을 막기 위해 Busy 플래그 사용(IsGroupBusy)
// 성능   : CompGetGizmosExtra에서 Pawn 전체 스캔(옵션 분기). 스캔은 1회로 최소화

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// RemoteSlaveCollar의 핵심 로직 컴포넌트.
    /// - Pawn 예약/해제/조회 관리
    /// - 원격 액션(폭발/감전/정신동결) 일괄·개별 실행
    /// - 그룹 선택/개별 선택 FloatMenu와 콘솔용 Gizmo 버튼 제공
    /// </summary>
    public class CompRemoteSlaveCollar : ThingComp
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
        public bool remotearmedExplosive = false;
        public bool remotearmedElectric = false;
        public bool remotearmedCrypto = false;

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
            Scribe_Values.Look(ref remotearmedExplosive, "remotearmedExplosive", false);
            Scribe_Values.Look(ref remotearmedElectric, "remotearmedElectric", false);
            Scribe_Values.Look(ref remotearmedCrypto, "remotearmedCrypto", false);
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

        #region 실행/명령 (일괄)
        /// <summary>전 맵의 식민 Pawn들 중 폭발 칼라 상태를 콘솔 토글값에 맞춰 일괄 갱신.</summary>
        private void DoRemoteCollarExplosive()
        {
            // [FIX] armed 토글만 수행, 컬렉션 변경 없음 → ToList 불필요
            foreach (var pawn in this.parent.Map.mapPawns.AllPawnsSpawned)
            {
                if (!SimpleSlaveryUtility.IsColonyMember(pawn)) continue;
                var collar = SimpleSlaveryUtility.GetSlaveCollar(pawn) as SlaveCollar_Explosive;
                if (collar == null || collar.armed == remotearmedExplosive) continue;

                collar.armed = remotearmedExplosive;
                if (collar.armed && collar.arm_cooldown == 0)
                {
                    SimpleSlaveryUtility.TryInstantBreak(pawn, Rand.Range(0.25f, 0.33f));
                    collar.arm_cooldown = 2500;
                }
            }
        }

        /// <summary>전 맵 대상 감전 칼라 Armed 상태를 콘솔 토글값에 맞춰 일괄 갱신.</summary>
        private void DoRemoteCollarElectric()
        {
            // [FIX] armed 토글만 수행, 컬렉션 변경 없음 → ToList 불필요
            foreach (var pawn in this.parent.Map.mapPawns.AllPawnsSpawned)
            {
                if (!SimpleSlaveryUtility.IsColonyMember(pawn)) continue;
                var collar = SimpleSlaveryUtility.GetSlaveCollar(pawn) as SlaveCollar_Electric;
                if (collar == null || collar.armed == remotearmedElectric) continue;

                collar.armed = remotearmedElectric;
            }
        }

        /// <summary>전 맵 대상 크립토 칼라 Armed 상태를 콘솔 토글값에 맞춰 일괄 갱신(해제 시 정신상태 복원).</summary>
        private void DoRemoteCollarCrypto()
        {
            // [FIX] armed 토글 + RevertMentalState만 수행, AllPawnsSpawned 컬렉션 불변 → ToList 불필요
            foreach (var pawn in this.parent.Map.mapPawns.AllPawnsSpawned)
            {
                if (!SimpleSlaveryUtility.IsColonyMember(pawn)) continue;
                var collar = SimpleSlaveryUtility.GetSlaveCollar(pawn) as SlaveCollar_Crypto;
                if (collar == null || collar.armed == remotearmedCrypto) continue;

                collar.armed = remotearmedCrypto;
                if (!collar.armed)
                {
                    collar.RevertMentalState();
                }
            }
        }

        /// <summary>전 맵 대상 폭발 칼라가 Armed 상태인 Pawn을 폭발시킴.</summary>
        private void DoRemoteCollarGoBoom()
        {
            // [NOTE] GoBoom()에서 Pawn 사망 → AllPawnsSpawned 컬렉션 변경 가능 → 스냅샷 필수
            var allPawns = this.parent.Map.mapPawns.AllPawnsSpawned;
            var targets = new List<SlaveCollar_Explosive>(allPawns.Count);
            for (int i = 0; i < allPawns.Count; i++)
            {
                var pawn = allPawns[i];
                if (!SimpleSlaveryUtility.IsColonyMember(pawn)) continue;
                var collar = SimpleSlaveryUtility.GetSlaveCollar(pawn) as SlaveCollar_Explosive;
                if (collar != null && collar.armed)
                    targets.Add(collar);
            }
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].GoBoom();
            }
        }
        #endregion

        #region 실행/명령 (개별)
        /// <summary>지정 Pawn의 폭발 칼라 Armed 토글 및 초기 충격.</summary>
        public void DoRemoteCollarExplosive(bool active, Pawn targetPawn)
        {
            var collar = SimpleSlaveryUtility.GetSlaveCollar(targetPawn) as SlaveCollar_Explosive;
            if (collar == null) return;

            collar.armed = active;
            if (active && collar.arm_cooldown == 0)
            {
                SimpleSlaveryUtility.TryInstantBreak(targetPawn, Rand.Range(0.25f, 0.33f));
                collar.arm_cooldown = 2500;
            }
        }

        /// <summary>지정 Pawn의 감전 칼라 Armed 토글.</summary>
        public void DoRemoteCollarElectric(bool active, Pawn targetPawn)
        {
            var collar = SimpleSlaveryUtility.GetSlaveCollar(targetPawn) as SlaveCollar_Electric;
            if (collar == null) return;

            collar.armed = active;
        }

        /// <summary>지정 Pawn의 크립토 칼라 Armed 토글(해제 시 정신상태 복원).</summary>
        public void DoRemoteCollarCrypto(bool active, Pawn targetPawn)
        {
            var collar = SimpleSlaveryUtility.GetSlaveCollar(targetPawn) as SlaveCollar_Crypto;
            if (collar == null) return;

            collar.armed = active;
            if (!active)
            {
                collar.RevertMentalState();
            }
        }

        /// <summary>지정 Pawn의 폭발 칼라가 Armed면 폭발.</summary>
        public void DoRemoteCollarGoBoom(Pawn targetPawn)
        {
            var collar = SimpleSlaveryUtility.GetSlaveCollar(targetPawn) as SlaveCollar_Explosive;
            if (collar == null || !collar.armed) return;

            collar.GoBoom();
        }
        #endregion

        #region UI/플롯 메뉴
        /// <summary>그룹 선택 1차 메뉴(전체/노예/죄수/식민자/노예+죄수).</summary>
        private void OpenPawnGroupMenu(RemoteCollarAction actionType)
        {
            List<FloatMenuOption> groupOptions = new List<FloatMenuOption>
            {
                new FloatMenuOption("RemoteCollar_Group_All".Translate(),           () => ShowPawnList(actionType, RemoteCollarPawnGroup.All)),
                new FloatMenuOption("RemoteCollar_Group_Slaves".Translate(),        () => ShowPawnList(actionType, RemoteCollarPawnGroup.Slaves)),
                new FloatMenuOption("RemoteCollar_Group_Prisoners".Translate(),     () => ShowPawnList(actionType, RemoteCollarPawnGroup.Prisoners)),
                new FloatMenuOption("RemoteCollar_Group_Colonists".Translate(),     () => ShowPawnList(actionType, RemoteCollarPawnGroup.Colonists)),
                new FloatMenuOption("RemoteCollar_Group_SlavesAndPrisoners".Translate(), () => ShowPawnList(actionType, RemoteCollarPawnGroup.SlavesAndPrisoners))
            };
            Find.WindowStack.Add(new FloatMenu(groupOptions));
        }

        /// <summary>2차 메뉴: 대상 Pawn 리스트 + 그룹 실행/전체 취소/개별 예약.</summary>
        private void ShowPawnList(RemoteCollarAction actionType, RemoteCollarPawnGroup group)
        {
            var eligiblePawns = FindEligiblePawnsForAction(actionType, group);
            if (eligiblePawns.NullOrEmpty())
            {
                Messages.Message("RemoteCollar_NoEligiblePawn".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            var options = new List<FloatMenuOption>();

            // [그룹 실행]
            options.Add(new FloatMenuOption("RemoteCollar_ExecuteForGroup".Translate(), () =>
            {
                ReserveJobForGroup(eligiblePawns, actionType);
            }));

            // [그룹 전체 취소]
            options.Add(new FloatMenuOption("RemoteCollar_CancelAllReservations".Translate(), () =>
            {
                CancelReservationsForGroup(eligiblePawns, actionType);
            }));

            // [개별 예약] — 이미 예약된 Pawn은 회색(비활성)
            foreach (var pawn in eligiblePawns)
            {
                string label = GetColoredPawnLabel(pawn);
                if (IsPawnReserved(pawn))
                {
                    label += " " + "RemoteCollar_AlreadyReservedShort".Translate();
                    options.Add(new FloatMenuOption(label, null));
                }
                else
                {
                    options.Add(new FloatMenuOption(label, () =>
                    {
                        ReserveJobForPawn(pawn, actionType);
                    }));
                }
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        /// <summary>Pawn 라벨 컬러링(식민=하늘색/노예=노랑/죄수=빨강) + 타이틀 병기.</summary>
        private string GetColoredPawnLabel(Pawn pawn)
        {
            string name = pawn.LabelShort;
            string title = pawn.story?.Title;

            string coloredName;
            if (pawn.IsColonist)
                coloredName = $"<color=#a2c8ff>{name}</color>";
            else if (pawn.IsSlaveOfColony)
                coloredName = $"<color=#ffd700>{name}</color>";
            else if (pawn.IsPrisonerOfColony)
                coloredName = $"<color=#ff9090>{name}</color>";
            else
                coloredName = name;

            return title != null ? $"{coloredName}, {title}" : coloredName;
        }
        #endregion

        #region Pawn 필터
        /// <summary>액션/그룹 조건에 맞는 Pawn 필터링.</summary>
        private List<Pawn> FindEligiblePawnsForAction(RemoteCollarAction actionType, RemoteCollarPawnGroup group)
        {
            var pawns = this.parent.Map.mapPawns.AllPawnsSpawned
                .Where(p =>
                    !p.Dead &&
                    p.Spawned &&
                    (
                        (group == RemoteCollarPawnGroup.All) ||
                        (group == RemoteCollarPawnGroup.Slaves && p.IsSlaveOfColony) ||
                        (group == RemoteCollarPawnGroup.Prisoners && p.IsPrisonerOfColony) ||
                        (group == RemoteCollarPawnGroup.Colonists && p.IsColonist) ||
                        (group == RemoteCollarPawnGroup.SlavesAndPrisoners && (p.IsSlaveOfColony || p.IsPrisonerOfColony))
                    )
                ).ToList();

            switch (actionType)
            {
                case RemoteCollarAction.ArmExplosive:
                    return pawns.Where(p =>
                    {
                        var collar = SimpleSlaveryUtility.GetSlaveCollar(p) as SlaveCollar_Explosive;
                        return collar != null && !collar.armed;
                    }).ToList();
                case RemoteCollarAction.DisarmExplosive:
                    return pawns.Where(p =>
                    {
                        var collar = SimpleSlaveryUtility.GetSlaveCollar(p) as SlaveCollar_Explosive;
                        return collar != null && collar.armed;
                    }).ToList();
                case RemoteCollarAction.DetonateExplosive:
                    return pawns.Where(p =>
                    {
                        var collar = SimpleSlaveryUtility.GetSlaveCollar(p) as SlaveCollar_Explosive;
                        return collar != null && collar.armed;
                    }).ToList();
                case RemoteCollarAction.ArmElectric:
                    return pawns.Where(p =>
                    {
                        var collar = SimpleSlaveryUtility.GetSlaveCollar(p) as SlaveCollar_Electric;
                        return collar != null && !collar.armed;
                    }).ToList();
                case RemoteCollarAction.DisarmElectric:
                    return pawns.Where(p =>
                    {
                        var collar = SimpleSlaveryUtility.GetSlaveCollar(p) as SlaveCollar_Electric;
                        return collar != null && collar.armed;
                    }).ToList();
                case RemoteCollarAction.ArmCrypto:
                    return pawns.Where(p =>
                    {
                        var collar = SimpleSlaveryUtility.GetSlaveCollar(p) as SlaveCollar_Crypto;
                        return collar != null && !collar.armed;
                    }).ToList();
                case RemoteCollarAction.DisarmCrypto:
                    return pawns.Where(p =>
                    {
                        var collar = SimpleSlaveryUtility.GetSlaveCollar(p) as SlaveCollar_Crypto;
                        return collar != null && collar.armed;
                    }).ToList();
                default:
                    return new List<Pawn>();
            }
        }
        #endregion

        #region Gizmo
        /// <summary>
        /// 콘솔에 추가 Gizmo 제공.
        /// - 옵션 ON(RemoteOnlyOnConsoleEnable): Pawn 리스트 팝업 기반의 그룹 실행 버튼만 노출
        /// - 옵션 OFF: 기존 토글/일괄 버튼 전부 노출
        /// </summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!this.PowerOn)
                yield break;

            // 옵션 ON: Pawn 리스트 팝업 기반의 그룹 실행만 제공(토글/일괄 버튼 숨김)
            if (SimpleSlaveryCollarsSetting.RemoteOnlyOnConsoleEnable)
            {
                // [스캔] Pawn 전체 1회 스캔 후 액션별 대상 분류
                var pawns = this.parent.Map.mapPawns.AllPawnsSpawned
                    .Where(p => !p.Dead && p.Spawned)
                    .ToList();

                var eligibleArmExplosive = new List<Pawn>();
                var eligibleDisarmExplosive = new List<Pawn>();
                var eligibleDetonateExplosive = new List<Pawn>();
                var eligibleArmElectric = new List<Pawn>();
                var eligibleDisarmElectric = new List<Pawn>();
                var eligibleArmCrypto = new List<Pawn>();
                var eligibleDisarmCrypto = new List<Pawn>();

                // [FIX] GetSlaveCollar를 Pawn당 1회만 호출 (기존 3회 → 1회)
                foreach (var p in pawns)
                {
                    var collar = SimpleSlaveryUtility.GetSlaveCollar(p);
                    if (collar == null) continue;

                    if (collar is SlaveCollar_Explosive explosive)
                    {
                        if (!explosive.armed)
                            eligibleArmExplosive.Add(p);
                        else
                        {
                            eligibleDisarmExplosive.Add(p);
                            eligibleDetonateExplosive.Add(p);
                        }
                    }
                    else if (collar is SlaveCollar_Electric electric)
                    {
                        if (!electric.armed)
                            eligibleArmElectric.Add(p);
                        else
                            eligibleDisarmElectric.Add(p);
                    }
                    else if (collar is SlaveCollar_Crypto crypto)
                    {
                        if (!crypto.armed)
                            eligibleArmCrypto.Add(p);
                        else
                            eligibleDisarmCrypto.Add(p);
                    }
                }

                // [UI] 폭발 목걸이 장전
                if (eligibleArmExplosive.Any())
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Label_CollarExplosive_Arm_Console".Translate(),
                        defaultDesc = "Desc_CollarExplosive_Arm_Console".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/ArmCollar_Explosive", true),
                        action = () => { OpenPawnGroupMenu(RemoteCollarAction.ArmExplosive); }
                    };
                }

                // [UI] 폭발 목걸이 해제
                if (eligibleDisarmExplosive.Any())
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Label_CollarExplosive_Disarm_Console".Translate(),
                        defaultDesc = "Desc_CollarExplosive_Disarm_Console".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/ArmCollar_Explosive", true),
                        action = () => { OpenPawnGroupMenu(RemoteCollarAction.DisarmExplosive); }
                    };
                }

                // [UI] 폭발 목걸이 폭발
                if (eligibleDetonateExplosive.Any())
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Label_CollarExplosive_Detonate_Console".Translate(),
                        defaultDesc = "Desc_CollarExplosive_Detonate_Console".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Explosive", true),
                        action = () => { OpenPawnGroupMenu(RemoteCollarAction.DetonateExplosive); }
                    };
                }

                // [UI] 감전 목걸이 장전
                if (eligibleArmElectric.Any())
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Label_CollarElectric_Arm_Console".Translate(),
                        defaultDesc = "Desc_CollarElectric_Arm_Console".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Electric", true),
                        action = () => { OpenPawnGroupMenu(RemoteCollarAction.ArmElectric); }
                    };
                }

                // [UI] 감전 목걸이 해제
                if (eligibleDisarmElectric.Any())
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Label_CollarElectric_Disarm_Console".Translate(),
                        defaultDesc = "Desc_CollarElectric_Disarm_Console".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Electric", true),
                        action = () => { OpenPawnGroupMenu(RemoteCollarAction.DisarmElectric); }
                    };
                }

                // [UI] 크립토(동결) 목걸이 장전
                if (eligibleArmCrypto.Any())
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Label_CollarCrypto_Arm_Console".Translate(),
                        defaultDesc = "Desc_CollarCrypto_Arm_Console".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Crypto", true),
                        action = () => { OpenPawnGroupMenu(RemoteCollarAction.ArmCrypto); }
                    };
                }

                // [UI] 크립토(동결) 목걸이 해제
                if (eligibleDisarmCrypto.Any())
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Label_CollarCrypto_Disarm_Console".Translate(),
                        defaultDesc = "Desc_CollarCrypto_Disarm_Console".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Crypto", true),
                        action = () => { OpenPawnGroupMenu(RemoteCollarAction.DisarmCrypto); }
                    };
                }

                // 옵션 ON인 경우 여기서 종료(기존 토글/일괄 명령 숨김)
                yield break;
            }

            // 옵션 OFF: 기존 토글/일괄 버튼 전부 노출(원래 코드 유지)

            // [UI] 1) 폭발 목걸이 장전(토글)
            var armCollarExplosive = new Command_Toggle();
            Func<bool> isArmedExplosive = () => remotearmedExplosive;
            armCollarExplosive.isActive = isArmedExplosive;
            armCollarExplosive.defaultLabel = "Label_CollarExplosive_Arm_Remote".Translate();
            armCollarExplosive.defaultDesc = "Desc_CollarExplosive_Arm_Remote".Translate();
            armCollarExplosive.toggleAction = delegate
            {
                remotearmedExplosive = !remotearmedExplosive;
                DoRemoteCollarExplosive();
            };
            armCollarExplosive.activateSound = SoundDefOf.Click;
            armCollarExplosive.icon = ContentFinder<Texture2D>.Get("UI/Commands/ArmCollar_Explosive", true);
            yield return armCollarExplosive;

            // [UI] 2) 폭발 목걸이 폭발(Armed일 때만 노출)
            if (remotearmedExplosive)
            {
                var detonate = new Command_Action();
                detonate.defaultLabel = "Label_CollarExplosive_Detonate_Remote".Translate();
                detonate.defaultDesc = "Desc_CollarExplosive_Detonate_Remote".Translate();
                detonate.action = delegate
                {
                    DoRemoteCollarGoBoom();
                };
                detonate.activateSound = SoundDefOf.Click;
                detonate.icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Explosive", true);
                yield return detonate;
            }

            // [UI] 3) 감전 목걸이 장전(토글)
            var armCollarElectric = new Command_Toggle();
            Func<bool> isArmedElectric = () => remotearmedElectric;
            armCollarElectric.isActive = isArmedElectric;
            armCollarElectric.defaultLabel = "Label_CollarElectric_Arm_Remote".Translate();
            armCollarElectric.defaultDesc = "Desc_CollarElectric_Arm_Remote".Translate();
            armCollarElectric.toggleAction = delegate
            {
                remotearmedElectric = !remotearmedElectric;
                DoRemoteCollarElectric();
            };
            armCollarElectric.activateSound = SoundDefOf.Click;
            armCollarElectric.icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Electric", true);
            yield return armCollarElectric;

            // [UI] 4) 크립토(동결) 목걸이 장전(토글)
            var armCollarCrypto = new Command_Toggle();
            Func<bool> isArmedCrypto = () => remotearmedCrypto;
            armCollarCrypto.isActive = isArmedCrypto;
            armCollarCrypto.defaultLabel = "Label_CollarCrypto_Arm_Remote".Translate();
            armCollarCrypto.defaultDesc = "Desc_CollarCrypto_Arm_Remote".Translate();
            armCollarCrypto.toggleAction = delegate
            {
                remotearmedCrypto = !remotearmedCrypto;
                DoRemoteCollarCrypto();
            };
            armCollarCrypto.activateSound = SoundDefOf.Click;
            armCollarCrypto.icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Crypto", true);
            yield return armCollarCrypto;
        }
        #endregion
    }
}