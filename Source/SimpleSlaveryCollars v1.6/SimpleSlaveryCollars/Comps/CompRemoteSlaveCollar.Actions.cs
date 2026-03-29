// SimpleSlaveryCollars | Comps | CompRemoteSlaveCollar.Actions.cs
// 목적 : 일괄/개별 원격 칼라 명령(폭발/감전/크립토) 실행
// 용도 : CompRemoteSlaveCollar의 실행 로직 partial 분리
// 주의 : 일괄 실행 시 per-pawn try-catch로 한 Pawn 실패가 전체를 중단시키지 않음

using System;
using System.Collections.Generic;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars
{
    public partial class CompRemoteSlaveCollar
    {
        #region 실행/명령 (일괄)
        /// <summary>전 맵의 식민 Pawn들 중 폭발 칼라 상태를 콘솔 토글값에 맞춰 일괄 갱신.</summary>
        private void DoRemoteCollarExplosive()
        {
            // [FIX] armed 토글만 수행, 컬렉션 변경 없음 → ToList 불필요
            foreach (var pawn in this.parent.Map.mapPawns.AllPawnsSpawned)
            {
                try
                {
                    if (!SimpleSlaveryUtility.IsColonyMember(pawn)) continue;
                    var collar = SimpleSlaveryUtility.GetSlaveCollar(pawn) as SlaveCollar_Explosive;
                    if (collar == null || collar.armed == remoteArmedExplosive) continue;

                    collar.armed = remoteArmedExplosive;
                    if (collar.armed && collar.armCooldown == 0)
                    {
                        SimpleSlaveryUtility.TryInstantBreak(pawn, Rand.Range(0.25f, 0.33f));
                        collar.armCooldown = 2500;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[SSC] DoRemoteCollarExplosive error ({pawn?.LabelShort}): {ex}");
                }
            }
        }

        /// <summary>전 맵 대상 감전 칼라 Armed 상태를 콘솔 토글값에 맞춰 일괄 갱신.</summary>
        private void DoRemoteCollarElectric()
        {
            // [FIX] armed 토글만 수행, 컬렉션 변경 없음 → ToList 불필요
            foreach (var pawn in this.parent.Map.mapPawns.AllPawnsSpawned)
            {
                try
                {
                    if (!SimpleSlaveryUtility.IsColonyMember(pawn)) continue;
                    var collar = SimpleSlaveryUtility.GetSlaveCollar(pawn) as SlaveCollar_Electric;
                    if (collar == null || collar.armed == remoteArmedElectric) continue;

                    collar.armed = remoteArmedElectric;
                }
                catch (Exception ex)
                {
                    Log.Error($"[SSC] DoRemoteCollarElectric error ({pawn?.LabelShort}): {ex}");
                }
            }
        }

        /// <summary>전 맵 대상 크립토 칼라 Armed 상태를 콘솔 토글값에 맞춰 일괄 갱신(해제 시 정신상태 복원).</summary>
        private void DoRemoteCollarCrypto()
        {
            // [FIX] armed 토글 + RevertMentalState만 수행, AllPawnsSpawned 컬렉션 불변 → ToList 불필요
            foreach (var pawn in this.parent.Map.mapPawns.AllPawnsSpawned)
            {
                try
                {
                    if (!SimpleSlaveryUtility.IsColonyMember(pawn)) continue;
                    var collar = SimpleSlaveryUtility.GetSlaveCollar(pawn) as SlaveCollar_Crypto;
                    if (collar == null || collar.armed == remoteArmedCrypto) continue;

                    collar.armed = remoteArmedCrypto;
                    if (!collar.armed)
                    {
                        collar.RevertMentalState();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[SSC] DoRemoteCollarCrypto error ({pawn?.LabelShort}): {ex}");
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
                try
                {
                    targets[i].GoBoom();
                }
                catch (Exception ex)
                {
                    Log.Error($"[SSC] DoRemoteCollarGoBoom error: {ex}");
                }
            }
        }
        #endregion

        #region 실행/명령 (디스패치)
        /// <summary>RemoteCollarAction에 따라 적절한 개별 명령을 디스패치. JobDriver에서 공용 호출.</summary>
        public void ExecuteAction(RemoteCollarAction actionType, Pawn targetPawn)
        {
            switch (actionType)
            {
                case RemoteCollarAction.ArmExplosive:
                    DoRemoteCollarExplosive(true, targetPawn);
                    break;
                case RemoteCollarAction.DisarmExplosive:
                    DoRemoteCollarExplosive(false, targetPawn);
                    break;
                case RemoteCollarAction.DetonateExplosive:
                    DoRemoteCollarGoBoom(targetPawn);
                    break;
                case RemoteCollarAction.ArmElectric:
                    DoRemoteCollarElectric(true, targetPawn);
                    break;
                case RemoteCollarAction.DisarmElectric:
                    DoRemoteCollarElectric(false, targetPawn);
                    break;
                case RemoteCollarAction.ArmCrypto:
                    DoRemoteCollarCrypto(true, targetPawn);
                    break;
                case RemoteCollarAction.DisarmCrypto:
                    DoRemoteCollarCrypto(false, targetPawn);
                    break;
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
            if (active && collar.armCooldown == 0)
            {
                SimpleSlaveryUtility.TryInstantBreak(targetPawn, Rand.Range(0.25f, 0.33f));
                collar.armCooldown = 2500;
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
    }
}
