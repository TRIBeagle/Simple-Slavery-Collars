// SimpleSlaveryCollars | Comps | CompRemoteSlaveCollar.Gizmos.cs
// 목적 : 원격 콘솔 UI/Gizmo 생성 및 FloatMenu 관리
// 용도 : CompRemoteSlaveCollar의 UI 로직 partial 분리
// 성능 : CompGetGizmosExtra에서 Pawn 전체 스캔(옵션 분기). 스캔은 1회로 최소화

using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars
{
    public partial class CompRemoteSlaveCollar
    {
        #region UI/플롯 메뉴
        /// <summary>그룹 선택 1차 메뉴(전체/노예/죄수/식민자/노예+죄수).</summary>
        private void OpenPawnGroupMenu(RemoteCollarAction actionType)
        {
            List<FloatMenuOption> groupOptions = new List<FloatMenuOption>
            {
                new FloatMenuOption("SSC_Remote_GroupAll".Translate(),           () => ShowPawnList(actionType, RemoteCollarPawnGroup.All)),
                new FloatMenuOption("SSC_Remote_GroupSlaves".Translate(),        () => ShowPawnList(actionType, RemoteCollarPawnGroup.Slaves)),
                new FloatMenuOption("SSC_Remote_GroupPrisoners".Translate(),     () => ShowPawnList(actionType, RemoteCollarPawnGroup.Prisoners)),
                new FloatMenuOption("SSC_Remote_GroupColonists".Translate(),     () => ShowPawnList(actionType, RemoteCollarPawnGroup.Colonists)),
                new FloatMenuOption("SSC_Remote_GroupSlavesAndPrisoners".Translate(), () => ShowPawnList(actionType, RemoteCollarPawnGroup.SlavesAndPrisoners))
            };
            Find.WindowStack.Add(new FloatMenu(groupOptions));
        }

        /// <summary>2차 메뉴: 대상 Pawn 리스트 + 그룹 실행/전체 취소/개별 예약.</summary>
        private void ShowPawnList(RemoteCollarAction actionType, RemoteCollarPawnGroup group)
        {
            var eligiblePawns = FindEligiblePawnsForAction(actionType, group);
            if (eligiblePawns.NullOrEmpty())
            {
                Messages.Message("SSC_Remote_NoEligiblePawn".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            var options = new List<FloatMenuOption>();

            // [그룹 실행]
            options.Add(new FloatMenuOption("SSC_Remote_ExecuteForGroup".Translate(), () =>
            {
                ReserveJobForGroup(eligiblePawns, actionType);
            }));

            // [그룹 전체 취소]
            options.Add(new FloatMenuOption("SSC_Remote_CancelAll".Translate(), () =>
            {
                CancelReservationsForGroup(eligiblePawns, actionType);
            }));

            // [개별 예약] — 이미 예약된 Pawn은 회색(비활성)
            foreach (var pawn in eligiblePawns)
            {
                string label = GetColoredPawnLabel(pawn);
                if (IsPawnReserved(pawn))
                {
                    label += " " + "SSC_Remote_AlreadyReservedShort".Translate();
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
            // LINQ 7회 ToList() → 단일 for 루프 + switch (GetSlaveCollar 1회/Pawn)
            var allPawns = this.parent.Map.mapPawns.AllPawnsSpawned;
            var result = new List<Pawn>();

            for (int i = 0; i < allPawns.Count; i++)
            {
                var p = allPawns[i];
                if (p.Dead || !p.Spawned) continue;

                // 그룹 필터
                switch (group)
                {
                    case RemoteCollarPawnGroup.Slaves:
                        if (!p.IsSlaveOfColony) continue; break;
                    case RemoteCollarPawnGroup.Prisoners:
                        if (!p.IsPrisonerOfColony) continue; break;
                    case RemoteCollarPawnGroup.Colonists:
                        if (!p.IsColonist) continue; break;
                    case RemoteCollarPawnGroup.SlavesAndPrisoners:
                        if (!p.IsSlaveOfColony && !p.IsPrisonerOfColony) continue; break;
                    // RemoteCollarPawnGroup.All — 필터 없음
                }

                var collar = SimpleSlaveryUtility.GetSlaveCollar(p);
                if (collar == null) continue;

                // 액션 타입별 칼라 종류 + armed 상태 매칭
                switch (actionType)
                {
                    case RemoteCollarAction.ArmExplosive:
                        if (collar is SlaveCollar_Explosive e1 && !e1.armed) result.Add(p); break;
                    case RemoteCollarAction.DisarmExplosive:
                    case RemoteCollarAction.DetonateExplosive:
                        if (collar is SlaveCollar_Explosive e2 && e2.armed) result.Add(p); break;
                    case RemoteCollarAction.ArmElectric:
                        if (collar is SlaveCollar_Electric el1 && !el1.armed) result.Add(p); break;
                    case RemoteCollarAction.DisarmElectric:
                        if (collar is SlaveCollar_Electric el2 && el2.armed) result.Add(p); break;
                    case RemoteCollarAction.ArmCrypto:
                        if (collar is SlaveCollar_Crypto c1 && !c1.armed) result.Add(p); break;
                    case RemoteCollarAction.DisarmCrypto:
                        if (collar is SlaveCollar_Crypto c2 && c2.armed) result.Add(p); break;
                }
            }

            return result;
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
                // [스캔] Pawn 전체 1회 스캔 — bool 플래그만 사용 (List 할당 제거)
                // 버튼 표시 여부만 판정. 클릭 시 FindEligiblePawnsForAction이 실제 필터링 수행
                bool hasArmExplosive = false, hasDisarmExplosive = false;
                bool hasArmElectric = false, hasDisarmElectric = false;
                bool hasArmCrypto = false, hasDisarmCrypto = false;

                var allPawns = this.parent.Map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < allPawns.Count; i++)
                {
                    var p = allPawns[i];
                    if (p.Dead || !p.Spawned) continue;

                    var collar = SimpleSlaveryUtility.GetSlaveCollar(p);
                    if (collar == null) continue;

                    if (collar is SlaveCollar_Explosive explosive)
                    {
                        if (!explosive.armed) hasArmExplosive = true;
                        else hasDisarmExplosive = true;
                    }
                    else if (collar is SlaveCollar_Electric electric)
                    {
                        if (!electric.armed) hasArmElectric = true;
                        else hasDisarmElectric = true;
                    }
                    else if (collar is SlaveCollar_Crypto crypto)
                    {
                        if (!crypto.armed) hasArmCrypto = true;
                        else hasDisarmCrypto = true;
                    }

                    // 모든 플래그가 true면 추가 스캔 불필요
                    if (hasArmExplosive && hasDisarmExplosive &&
                        hasArmElectric && hasDisarmElectric &&
                        hasArmCrypto && hasDisarmCrypto)
                        break;
                }

                if (hasArmExplosive)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "SSC_Console_ExplosiveArm".Translate(),
                        defaultDesc = "SSC_Console_ExplosiveArm_Desc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/ArmCollar_Explosive", true),
                        action = () => { OpenPawnGroupMenu(RemoteCollarAction.ArmExplosive); }
                    };
                }

                if (hasDisarmExplosive)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "SSC_Console_ExplosiveDisarm".Translate(),
                        defaultDesc = "SSC_Console_ExplosiveDisarm_Desc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/ArmCollar_Explosive", true),
                        action = () => { OpenPawnGroupMenu(RemoteCollarAction.DisarmExplosive); }
                    };

                    yield return new Command_Action
                    {
                        defaultLabel = "SSC_Console_ExplosiveDetonate".Translate(),
                        defaultDesc = "SSC_Console_ExplosiveDetonate_Desc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Explosive", true),
                        action = () => { OpenPawnGroupMenu(RemoteCollarAction.DetonateExplosive); }
                    };
                }

                if (hasArmElectric)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "SSC_Console_ElectricArm".Translate(),
                        defaultDesc = "SSC_Console_ElectricArm_Desc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Electric", true),
                        action = () => { OpenPawnGroupMenu(RemoteCollarAction.ArmElectric); }
                    };
                }

                if (hasDisarmElectric)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "SSC_Console_ElectricDisarm".Translate(),
                        defaultDesc = "SSC_Console_ElectricDisarm_Desc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Electric", true),
                        action = () => { OpenPawnGroupMenu(RemoteCollarAction.DisarmElectric); }
                    };
                }

                if (hasArmCrypto)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "SSC_Console_CryptoArm".Translate(),
                        defaultDesc = "SSC_Console_CryptoArm_Desc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/DetonateCollar_Crypto", true),
                        action = () => { OpenPawnGroupMenu(RemoteCollarAction.ArmCrypto); }
                    };
                }

                if (hasDisarmCrypto)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "SSC_Console_CryptoDisarm".Translate(),
                        defaultDesc = "SSC_Console_CryptoDisarm_Desc".Translate(),
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
            armCollarExplosive.defaultLabel = "SSC_Remote_ExplosiveArm".Translate();
            armCollarExplosive.defaultDesc = "SSC_Remote_ExplosiveArm_Desc".Translate();
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
                detonate.defaultLabel = "SSC_Remote_ExplosiveDetonate".Translate();
                detonate.defaultDesc = "SSC_Remote_ExplosiveDetonate_Desc".Translate();
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
            armCollarElectric.defaultLabel = "SSC_Remote_ElectricArm".Translate();
            armCollarElectric.defaultDesc = "SSC_Remote_ElectricArm_Desc".Translate();
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
            armCollarCrypto.defaultLabel = "SSC_Remote_CryptoArm".Translate();
            armCollarCrypto.defaultDesc = "SSC_Remote_CryptoArm_Desc".Translate();
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
