// SimpleSlaveryCollars | Thoughts | ThoughtWorker_Enslaved.cs
// 목적   : Pawn이 Colony 소속 노예일 때 억제 단계(Slavery Stage)에 따른 정신 사상을 부여
// 용도   : RimWorld ThoughtWorker 확장
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — Stage4/5 및 Steadfast 예외 명시
// 주의   : Stage5 = (x ≥ SlaveStage4 && !Steadfast), Stage4 = (SlaveStage3 < x < SlaveStage4) 또는 (x ≥ SlaveStage4 && Steadfast)
// 저장   : Stage4 동화(assimilation) 여부와 UI refresh 여부는 Hediff_Enslaved 내부 플래그에 저장됨

using RimWorld;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// 노예 Pawn의 억제 단계(Slavery Stage)에 따라 적절한 ThoughtState를 반환한다.
    /// - Stage1~3: 단계별 사상 부여
    /// - Stage4: (x ≥ S3 && Steadfast) 또는 (S3 < x < S4) → Assimilation 처리 및 UI refresh
    /// - Stage5: (x ≥ S4 && !Steadfast) → 최종 사상 단계
    /// </summary>
    public class ThoughtWorker_Enslaved : ThoughtWorker
    {
        /// <summary>
        /// Pawn의 현재 억제 단계 및 상태를 바탕으로 활성화할 사상을 결정한다.
        /// </summary>
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            if (SimpleSlaveryCollarsSetting.SlavestageEnable == false)
                return ThoughtState.Inactive;

            if (pawn.IsSlaveOfColony)
            {
                if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage1)
                {
                    return ThoughtState.ActiveAtStage(0);
                }
                else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage2)
                {
                    return ThoughtState.ActiveAtStage(1);
                }
                else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage3)
                {
                    return ThoughtState.ActiveAtStage(2);
                }
                else if (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage4
                      || (SlaveUtility.TimeAsSlave(pawn) >= SlaveUtility.SlaveStage3 && SlaveUtility.IsSteadfast(pawn)))
                {
                    return ThoughtState.ActiveAtStage(3);
                }
                else if (SlaveUtility.TimeAsSlave(pawn) >= SlaveUtility.SlaveStage4)
                {
                    var enslavedHediff = pawn.health.hediffSet.GetFirstHediffOfDef(SSC_HediffDefOf.Enslaved) as Hediff_Enslaved;
                    if (enslavedHediff != null)
                    {
                        // Stage4 동화 처리
                        if (!enslavedHediff.assimilatedAtStage4 && SimpleSlaveryCollarsSetting.AssimilationSlaveEnable)
                        {
                            if (pawn.guest.SlaveFaction != Faction.OfPlayer)
                                pawn.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);

                            enslavedHediff.assimilatedAtStage4 = true;
                        }

                        // UI refresh 처리
                        if (!enslavedHediff.uiRefreshedAtStage4)
                        {
                            pawn.Notify_DisabledWorkTypesChanged();
                            enslavedHediff.uiRefreshedAtStage4 = true;
                        }
                    }

                    return ThoughtState.ActiveAtStage(4);
                }
            }
            return ThoughtState.Inactive;
        }
    }
}
