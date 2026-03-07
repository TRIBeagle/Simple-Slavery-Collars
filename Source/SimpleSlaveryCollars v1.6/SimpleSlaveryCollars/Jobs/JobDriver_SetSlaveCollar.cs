// SimpleSlaveryCollars | Jobs | JobDriver_SetSlaveCollar.cs
// 목적   : Pawn A에게 노예 칼라(Apparel)를 강제로 장착시키는 JobDriver
// 용도   : CompTargetEffect_SetSlaveCollar 등에 의해 할당된 작업 실행
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — 헤더/클래스/메서드 주석 재작성
// 주의   : Victim 상태(정신/다운/특성)에 따라 성공/실패 → 실패 시 Berserk 발생
// 성능   : Toil 경로 단순, 대기 300틱. null 가드 및 조건 분기 최소화

using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars.Jobs
{
    /// <summary>
    /// Victim Pawn에게 SlaveCollar(Apparel)를 장착시키는 JobDriver.
    /// - 칼라 회수→운반→Victim 대기→착용 시도
    /// - 성공 시 칼라 장착, 실패 시 Berserk MentalState
    /// </summary>
    internal class JobDriver_SetSlaveCollar : JobDriver
    {
        private const int EnslaveDuration = 300; // 착용 대기 시간(틱)

        private Pawn Victim => (Pawn)job.GetTarget(TargetIndex.A).Thing;
        private Apparel SlaveCollar => (Apparel)job.GetTarget(TargetIndex.B).Thing;

        /// <summary>
        /// Victim/칼라 예약 시도.
        /// </summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Victim, job, 1, -1, null) &&
                   pawn.Reserve(SlaveCollar, job, 1, -1, null);
        }

        /// <summary>
        /// Toil 시퀀스:
        /// 1) Victim/칼라 예약 → 2) 칼라로 이동 → 3) 집어들기
        /// 4) Victim으로 이동 → 5) 함께 대기(300틱) → 6) 착용 시도
        /// </summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnForbidden(TargetIndex.B);

            // [Toils] 1) 대상 예약
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.B);

            // [Toils] 2) 칼라 위치로 이동
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);

            // [Toils] 3) 집기 준비(수량=1)
            yield return new Toil
            {
                initAction = () => pawn.jobs.curJob.count = 1
            };

            // [Toils] 4) 칼라 집어들기
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);

            // [Toils] 5) Victim 위치로 이동
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // [Toils] 6) Victim과 함께 대기(착용 시도 준비)
            yield return Toils_General.WaitWith(TargetIndex.A, EnslaveDuration, true);

            // [Toils] 7) 착용 시도(성공→장착/메시지, 실패→Berserk)
            yield return new Toil
            {
                initAction = () =>
                {
                    if (!pawn.carryTracker.TryDropCarriedThing(pawn.PositionHeld, ThingPlaceMode.Direct, out Thing dropped, null))
                    {
                        AddEndCondition(() => JobCondition.Incompletable);
                        return;
                    }

                    var collar = dropped as Apparel;
                    if (collar == null)
                    {
                        AddEndCondition(() => JobCondition.Incompletable);
                        return;
                    }

                    bool success = true;

                    // [조건] Victim이 깨어있고, Wimp 아님, 정신이상/다운 아님일 때만 저항 발생
                    if (!Victim.jobs.curDriver.asleep &&
                        !Victim.story.traits.HasTrait(TraitDef.Named("Wimp")) &&
                        !Victim.InMentalState &&
                        !Victim.Downed)
                    {
                        if ((Victim.story.traits.HasTrait(SimpleSlaveryDefOf.Nerves) &&
                             Victim.story.traits.GetTrait(SimpleSlaveryDefOf.Nerves).Degree == -2 &&
                             Rand.Value > 0.66f)
                            || Victim.needs.mood.CurInstantLevelPercentage < Rand.Range(0f, 0.33f))
                        {
                            success = false;
                        }
                    }

                    if (success)
                    {
                        SimpleSlaveryUtility.GiveSlaveCollar(Victim, collar);
                        Messages.Message(
                            "TargetSetSlaveCollar".Translate(pawn.Name.ToStringShort, Victim.Name.ToStringShort),
                            MessageTypeDefOf.PositiveEvent);
                        AddEndCondition(() => JobCondition.Succeeded);
                    }
                    else
                    {
                        Victim.mindState.mentalStateHandler.TryStartMentalState(
                            MentalStateDefOf.Berserk,
                            "ReasonFailedSetSlaveCollar".Translate(pawn.Name.ToStringShort, Victim.Name.ToStringShort));
                        AddEndCondition(() => JobCondition.Incompletable);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
