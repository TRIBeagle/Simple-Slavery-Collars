// SimpleSlaveryCollars | Comps | CompProperties_RemoteSlaveCollar.cs
// 목적   : RemoteSlaveCollar 컴포넌트의 속성 정의를 제공
// 용도   : ThingDef XML에서 compClass를 지정할 때 사용
// 변경   : 2025-09-22 주석 규칙에 맞게 헤더/클래스 주석 재작성

using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// RemoteSlaveCollar용 컴포넌트 속성 정의.
    /// compClass를 CompRemoteSlaveCollar로 고정한다.
    /// </summary>
    public class CompProperties_RemoteSlaveCollar : CompProperties
    {
        public CompProperties_RemoteSlaveCollar()
        {
            this.compClass = typeof(CompRemoteSlaveCollar);
        }
    }
}
