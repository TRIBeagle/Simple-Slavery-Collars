// SimpleSlaveryCollars | Hediffs | Hediff_Enslaved.cs
// 목적   : [레거시] 기존 세이브 역직렬화 호환용 빈 껍데기. 다음 버전에서 삭제 예정.
// 용도   : 기존 세이브 로드 시 Hediff 데이터를 읽어 CompSlave.TryMigrateAndRemoveHediff()에서 Comp로 이전 후 자동 제거됨.
// 주의   : 클래스명/네임스페이스 변경 금지 (직렬화 호환). ExposeData의 Scribe 키 유지 필수.

using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// [레거시] 기존 세이브 역직렬화용 빈 껍데기.
    /// 모든 데이터는 CompSlave로 이동됨. CompTickRare에서 자동 마이그레이션 후 제거.
    /// </summary>
    public class Hediff_Enslaved : HediffWithComps
    {
        public bool shackledGoal = true;
        public bool shackled = true;
        public bool assimilatedAtStage4 = false;
        public bool uiRefreshedAtStage4 = false;

        /// <summary>
        /// [레거시] 기존 세이브 호환용. Scribe 키 유지 필수.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref shackledGoal, "shackledGoal", false);
            Scribe_Values.Look(ref shackled, "shackled", false);
            Scribe_Values.Look(ref assimilatedAtStage4, "assimilatedAtStage4", false);
            Scribe_Values.Look(ref uiRefreshedAtStage4, "uiRefreshedAtStage4", false);
        }

        /// <summary>UI에 표시되지 않도록 항상 false.</summary>
        public override bool Visible => false;
    }
}