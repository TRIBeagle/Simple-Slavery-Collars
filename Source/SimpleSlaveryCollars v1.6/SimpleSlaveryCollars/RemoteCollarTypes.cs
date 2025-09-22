// SimpleSlaveryCollars | Core | RemoteCollarTypes.cs
// 목적   : 원격 칼라(Remote Collar) 제어에 사용되는 명령 및 Pawn 그룹 열거형 정의
// 용도   : RemoteCollar 관련 JobDriver, WorkGiver, Gizmo 등에서 참조
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용
// 주의   : enum 값은 저장/로드 시 문자열 기반 직렬화됨

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// 원격 칼라(Remote Collar)에 적용할 수 있는 명령 종류.
    /// - Explosive : Arm/Disarm/Detonate
    /// - Electric  : Arm/Disarm
    /// - Crypto    : Arm/Disarm
    /// </summary>
    public enum RemoteCollarAction
    {
        ArmExplosive,
        DisarmExplosive,
        DetonateExplosive,
        ArmElectric,
        DisarmElectric,
        ArmCrypto,
        DisarmCrypto,
    }

    /// <summary>
    /// 원격 칼라 명령의 대상 Pawn 그룹.
    /// - All : 전원
    /// - Slaves : 노예
    /// - Prisoners : 죄수
    /// - Colonists : 정착민
    /// - SlavesAndPrisoners : 노예 + 죄수
    /// </summary>
    public enum RemoteCollarPawnGroup
    {
        All,
        Slaves,
        Prisoners,
        Colonists,
        SlavesAndPrisoners
    }
}
