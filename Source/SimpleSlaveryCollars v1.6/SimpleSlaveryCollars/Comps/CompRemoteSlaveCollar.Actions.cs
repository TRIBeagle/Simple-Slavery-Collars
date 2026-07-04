// SimpleSlaveryCollars | Comps | CompRemoteSlaveCollar.Actions.cs
// 목적 : 일괄/개별 원격 칼라 명령(폭발/감전/크립토) 실행
// 용도 : CompRemoteSlaveCollar의 실행 로직 partial 분리
// 주의 : 일괄 실행 시 per-pawn try-catch로 한 Pawn 실패가 전체를 중단시키지 않음
//        칼라 종류별 동작은 SlaveApparel.SetArmed/Detonate 가상 메서드로 위임

using System;
using System.Collections.Generic;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars
{
    public partial class CompRemoteSlaveCollar
    {
        #region 실행/명령 (일괄 — 레거시 토글 gizmo용)
        /// <summary>전 맵의 식민 Pawn들 중 폭발 칼라 상태를 콘솔 토글값에 맞춰 일괄 갱신.</summary>
        private void DoRemoteCollarExplosive() => DoRemoteCollarBatchArm(RemoteCollarAction.ArmExplosive, remoteArmedExplosive);

        /// <summary>전 맵 대상 감전 칼라 Armed 상태를 콘솔 토글값에 맞춰 일괄 갱신.</summary>
        private void DoRemoteCollarElectric() => DoRemoteCollarBatchArm(RemoteCollarAction.ArmElectric, remoteArmedElectric);

        /// <summary>전 맵 대상 크립토 칼라 Armed 상태를 콘솔 토글값에 맞춰 일괄 갱신(해제 시 정신상태 복원은 SetArmed 내부).</summary>
        private void DoRemoteCollarCrypto() => DoRemoteCollarBatchArm(RemoteCollarAction.ArmCrypto, remoteArmedCrypto);

        /// <summary>지정 ArmAction 종류의 칼라를 전 맵에서 target 상태로 일괄 토글.</summary>
        private void DoRemoteCollarBatchArm(RemoteCollarAction armAction, bool target)
        {
            // [FIX] armed 토글만 수행(크립토 해제 시 RevertMentalState 포함), 컬렉션 불변 → 스냅샷 불필요
            foreach (var pawn in this.parent.Map.mapPawns.AllPawnsSpawned)
            {
                try
                {
                    if (!SimpleSlaveryUtility.IsColonyMember(pawn)) continue;
                    var collar = SimpleSlaveryUtility.GetSlaveCollar(pawn) as SlaveApparel;
                    if (collar == null || collar.ArmAction != armAction) continue;
                    if (collar.IsArmed == target) continue;

                    collar.SetArmed(target, pawn);
                }
                catch (Exception ex)
                {
                    Log.Error($"[SSC] DoRemoteCollarBatchArm({armAction}) error ({pawn?.LabelShort}): {ex}");
                }
            }
        }

        /// <summary>전 맵 대상 폭발 칼라가 Armed 상태인 Pawn을 폭발시킴.</summary>
        private void DoRemoteCollarGoBoom()
        {
            // [NOTE] Detonate에서 Pawn 사망 → AllPawnsSpawned 컬렉션 변경 가능 → 스냅샷 필수
            var allPawns = this.parent.Map.mapPawns.AllPawnsSpawned;
            var targets = new List<SlaveApparel>(allPawns.Count);
            for (int i = 0; i < allPawns.Count; i++)
            {
                var pawn = allPawns[i];
                if (!SimpleSlaveryUtility.IsColonyMember(pawn)) continue;
                var collar = SimpleSlaveryUtility.GetSlaveCollar(pawn) as SlaveApparel;
                if (collar != null && collar.DetonateAction == RemoteCollarAction.DetonateExplosive && collar.IsArmed)
                    targets.Add(collar);
            }
            for (int i = 0; i < targets.Count; i++)
            {
                try
                {
                    targets[i].Detonate(targets[i].Wearer);
                }
                catch (Exception ex)
                {
                    Log.Error($"[SSC] DoRemoteCollarGoBoom error: {ex}");
                }
            }
        }
        #endregion

        #region 실행/명령 (디스패치 — 개별 예약 실행)
        /// <summary>RemoteCollarAction에 따라 대상 Pawn의 칼라에 개별 명령 실행. JobDriver에서 공용 호출.</summary>
        public void ExecuteAction(RemoteCollarAction actionType, Pawn targetPawn)
        {
            var collar = SimpleSlaveryUtility.GetSlaveCollar(targetPawn) as SlaveApparel;
            if (collar == null) return;

            // 예약 시점과 실행 시점의 칼라 종류가 다르면(교체 등) 무시 — 기존 no-op 동작 보존
            bool owns = collar.ArmAction == actionType
                     || collar.DisarmAction == actionType
                     || collar.DetonateAction == actionType;
            if (!owns) return;

            switch (actionType)
            {
                case RemoteCollarAction.ArmExplosive:
                case RemoteCollarAction.ArmElectric:
                case RemoteCollarAction.ArmCrypto:
                    collar.SetArmed(true, targetPawn);
                    break;
                case RemoteCollarAction.DisarmExplosive:
                case RemoteCollarAction.DisarmElectric:
                case RemoteCollarAction.DisarmCrypto:
                    collar.SetArmed(false, targetPawn);
                    break;
                case RemoteCollarAction.DetonateExplosive:
                    collar.Detonate(targetPawn);
                    break;
            }
        }
        #endregion
    }
}
