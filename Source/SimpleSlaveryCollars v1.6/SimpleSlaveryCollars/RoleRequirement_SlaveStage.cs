// SimpleSlaveryCollars | Roles | RoleRequirement_SlaveStage.cs
// 목적   : Precept_Role 조건에서 Pawn의 Slavery Stage에 따라 역할 허용/차단
// 용도   : RimWorld RoleRequirement 확장
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용 — Stage4/5 조건 반영
// 주의   : Stage5 = (x ≥ SlaveStage4 && !Steadfast), 그 외는 Stage4 이하 → 역할 허용 제한

using RimWorld;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// Pawn의 Slavery Stage를 검사하여 역할 배정을 제약한다.
    /// Stage4 이하(또는 Steadfast 보유)는 제한 없음, Stage5면 차단.
    /// </summary>
    public class RoleRequirement_SlaveStage : RoleRequirement
    {
        public override string GetLabel(Precept_Role role) =>
            labelKey.Translate(Find.ActiveLanguageWorker.WithIndefiniteArticle(role.ideo.memberName, Gender.None));

        public override bool Met(Pawn pawn, Precept_Role role)
        {
            if (pawn.IsSlaveOfColony &&
                (SlaveUtility.TimeAsSlave(pawn) < SlaveUtility.SlaveStage4 || SlaveUtility.IsSteadfast(pawn)))
                return false;
            return true;
        }
    }
}
