// SimpleSlaveryCollars | Comps | CompTarget_SetSlaveCollar.cs
// 목적   : 플레이어가 식민자/죄수/노예 Pawn을 직접 지정해 'SetSlaveCollar' 작업을 배정하는 대상 지정/효과 컴포넌트
// 용도   : 아이템/장치에 부착되어 사용 시 대상 선택 UI 노출 → 선택 Pawn에게 작업(Job) 부여
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더·클래스/메서드 요약 추가
// 주의   : 예약/도달 불가 시 즉시 반환하여 안전하게 작업 배정 실패 처리
// 성능   : 단발성 상호작용(틱 핫패스 아님)

using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// 플레이어가 직접 대상을 고르는 Targetable 컴포넌트.
    /// - Pawn만 선택 가능, 범위는 식민자/죄수/노예로 제한.
    /// </summary>
    public class CompTargetable_ColonyPawn : CompTargetable
    {
        /// <summary>
        /// 플레이어가 수동으로 대상을 고르는지 여부.
        /// </summary>
        protected override bool PlayerChoosesTarget => true;

        /// <summary>
        /// 대상 선택 파라미터 설정(파온 한정, 식민자/죄수/노예만, 건물 제외).
        /// </summary>
        protected override TargetingParameters GetTargetingParameters()
        {
            return new TargetingParameters()
            {
                canTargetPawns = true,
                onlyTargetColonistsOrPrisonersOrSlaves = true,
                canTargetBuildings = false,
            };
        }

        /// <summary>
        /// 플레이어가 지정한 Thing만 그대로 타겟으로 사용.
        /// </summary>
        public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
        {
            yield return targetChosenByPlayer;
        }
    }

    /// <summary>
    /// 지정 대상에게 'SetSlaveCollar' 작업을 배정하는 효과 컴포넌트.
    /// - 가드: 유저 Pawn이 플레이어 통제 + 예약/도달 가능일 때만 작업 큐에 삽입.
    /// </summary>
    public class CompTargetEffect_SetSlaveCollar : CompTargetEffect
    {
        /// <summary>
        /// 대상 Pawn에게 SetSlaveCollar 잡을 생성하여 순서대로 수행하게 함.
        /// </summary>
        /// <param name="user">작업을 수행할 Pawn(플레이어 통제)</param>
        /// <param name="target">플레이어가 선택한 대상 Pawn</param>
        public override void DoEffectOn(Pawn user, Thing target)
        {
            // 안전: 플레이어 통제/예약·도달 가능 여부 사전 확인
            if (!user.IsColonistPlayerControlled || !user.CanReserveAndReach((LocalTargetInfo)target, PathEndMode.Touch, Danger.Deadly))
                return;

            // Job(A=대상 Pawn, B=이 컴프가 붙은 아이템/장치) 1회 실행
            Job job = JobMaker.MakeJob(SimpleSlaveryDefOf.SetSlaveCollar, (LocalTargetInfo)target, (LocalTargetInfo)(Thing)this.parent);
            job.count = 1;
            user.jobs.TryTakeOrderedJob(job);
        }
    }
}
