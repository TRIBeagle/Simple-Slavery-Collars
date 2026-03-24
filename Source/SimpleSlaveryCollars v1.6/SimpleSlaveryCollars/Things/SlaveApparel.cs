// SimpleSlaveryCollars | Things | SlaveApparel.cs
// 목적 : 모든 노예 칼라의 추상 기반 클래스
// 용도 : SlaveGizmos() 인터페이스 정의. Patch_Pawn_GetGizmos에서 호출

using System.Collections.Generic;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>모든 노예 칼라(Explosive/Electric/Crypto)의 추상 기반 클래스.</summary>
    public abstract class SlaveApparel : Apparel
    {
        /// <summary>칼라 전용 기즈모 반환. Pawn 선택 시 UI에 노출.</summary>
        public abstract IEnumerable<Gizmo> SlaveGizmos();
    }
}