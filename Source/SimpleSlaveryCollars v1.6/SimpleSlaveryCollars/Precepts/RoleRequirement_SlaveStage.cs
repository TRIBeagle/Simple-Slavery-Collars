// SimpleSlaveryCollars | Precepts | RoleRequirement_SlaveStage.cs
// 목적   : Precept_Role 조건에서 Pawn의 Slavery Stage에 따라 역할 허용/차단
// 용도   : RimWorld RoleRequirement 확장
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — Stage4/5 조건 반영
// 주의   : Stage5 = (x ≥ SlaveStage4 && !Steadfast), 그 외는 Stage4 이하 → 역할 허용 제한

using RimWorld;
using Verse;
using SimpleSlaveryCollars.Utilities;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// Pawn의 Slavery Stage를 검사하여 역할 배정을 제약한다.
    /// 비노예 → 무조건 통과. Stage5(동화 완료) 노예만 허용, 그 외 노예는 차단.
    /// </summary>
    public class RoleRequirement_SlaveStage : RoleRequirement
    {
        public override string GetLabel(Precept_Role role) =>
            labelKey.Translate(Find.ActiveLanguageWorker.WithIndefiniteArticle(role.ideo.memberName, Gender.None));

        public override bool Met(Pawn pawn, Precept_Role role)
        {
            // 노예가 아니면 이 조건은 해당 없음 → 통과
            if (!pawn.IsSlaveOfColony)
                return true;

            // Stage5(동화 완료) 노예만 역할 허용. Stage4 이하 or Steadfast → 차단
            float time = SimpleSlaveryUtility.TimeAsSlave(pawn);
            if (time < SimpleSlaveryUtility.SlaveStage4 || SimpleSlaveryUtility.IsSteadfast(pawn))
                return false;

            return true;
        }
    }
}
