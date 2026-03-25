// SimpleSlaveryCollars | Utilities | SlaveCollarRegistry.cs
// 목적 : SlaveApparel 착용 Pawn의 O(1) 조회를 위한 정적 레지스트리
// 용도 : GetSlaveCollar 호출을 Dictionary 기반으로 가속. 폴백은 WornApparel 순회
// 주의 : 게임 로드/맵 전환 시 Clear() 필수. key는 Pawn.thingIDNumber(int)

using System.Collections.Generic;
using Verse;

namespace SimpleSlaveryCollars.Utilities
{
    /// <summary>
    /// SlaveApparel 착용 Pawn을 O(1)로 조회하기 위한 정적 레지스트리.
    /// key = Pawn.thingIDNumber (int) — GC 압력 없음, Pawn 참조 유지 없음.
    /// 폴백: 레지스트리 미스 시 SimpleSlaveryUtility.GetSlaveCollar()가 WornApparel 순회.
    /// </summary>
    public static class SlaveCollarRegistry
    {
        private static readonly Dictionary<int, SlaveApparel> _registry
            = new Dictionary<int, SlaveApparel>();

        /// <summary>칼라 착용 시 등록.</summary>
        public static void Register(Pawn pawn, SlaveApparel collar)
        {
            if (pawn == null || collar == null) return;
            _registry[pawn.thingIDNumber] = collar;
        }

        /// <summary>칼라 해제 시 등록 해제.</summary>
        public static void Unregister(Pawn pawn)
        {
            if (pawn == null) return;
            _registry.Remove(pawn.thingIDNumber);
        }

        /// <summary>
        /// 캐시에서 칼라 조회. 유효성 검증 포함 (Wearer == pawn).
        /// 무효 엔트리는 자동 제거. 미스 시 null 반환 (폴백 필요).
        /// </summary>
        public static SlaveApparel GetCached(Pawn pawn)
        {
            if (pawn == null) return null;
            if (_registry.TryGetValue(pawn.thingIDNumber, out var collar))
            {
                // 유효성 검증: 칼라가 여전히 착용 중인지
                if (collar != null && !collar.Destroyed && collar.Wearer == pawn)
                    return collar;
                // 무효 항목 자동 정리
                _registry.Remove(pawn.thingIDNumber);
            }
            return null;
        }

        /// <summary>게임 로드/맵 전환 시 전체 초기화.</summary>
        public static void Clear() => _registry.Clear();
    }
}
