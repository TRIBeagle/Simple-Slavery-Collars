// SimpleSlaveryCollars | Roles | RoleRequirement_NotSlave.cs
// 목적   : Precept_Role 조건에서 Pawn이 노예가 아닌 경우만 허용
// 용도   : RimWorld RoleRequirement 확장
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용
// 주의   : Pawn.IsFreeNonSlaveColonist 조건 기반

using RimWorld;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// Pawn이 자유 식민자(노예 아님)일 때만 역할 부여 가능.
    /// </summary>
    public class RoleRequirement_NotSlave : RoleRequirement
    {
        public override string GetLabel(Precept_Role role) =>
            labelKey.Translate(Find.ActiveLanguageWorker.WithIndefiniteArticle(role.ideo.memberName, Gender.None));

        public override bool Met(Pawn pawn, Precept_Role role) =>
            pawn.IsFreeNonSlaveColonist;
    }
}
