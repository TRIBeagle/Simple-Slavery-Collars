// SimpleSlaveryCollars | Comps | CollarBatteryExtension.cs
// 목적 : 칼라별 배터리/소모량을 XML에서 지정하는 DefModExtension
// 용도 : ThingDef.modExtensions에 추가하여 칼라별 배터리 용량/소모량 커스텀
// 주의 : 다른 모드가 새 칼라 추가 시 이 Extension만 달면 충전 시스템 자동 호환

using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// 칼라 배터리 설정 DefModExtension.
    /// XML에서 칼라별 배터리 용량/소모량을 지정한다.
    /// </summary>
    public class CollarBatteryExtension : DefModExtension
    {
        /// <summary>배터리 용량 (Wd). 품질 배율이 곱해짐. 기본 50Wd.</summary>
        public float batteryCapacity = 50f;

        /// <summary>대기 소모량 (Wd/일). 착용 중 항상 소모. 기본 10Wd/일.</summary>
        public float idleDrain = 10f;

        /// <summary>작동(armed) 소모량 (Wd/일). armed 중에만 적용. 기본 30Wd/일.</summary>
        public float activeDrain = 30f;
    }
}
