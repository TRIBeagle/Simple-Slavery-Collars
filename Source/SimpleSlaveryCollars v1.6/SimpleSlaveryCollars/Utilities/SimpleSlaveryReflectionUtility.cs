// SimpleSlaveryCollars | Utilities | SSC_ReflectionCache.cs
// 목적   : Pawn_RecordsTracker의 내부 DefMap<RecordDef, float>에 접근하기 위한 리플렉션 캐시
// 용도   : CompSlave.TrySyncRecordOnSave() 및 DebugActions에서 공용으로 사용
// 변경   : [FIX] 기존 CompSlave + DebugActions에 복사되어 있던 동일 코드를 단일 캐시로 통합
// 성능   : 1회 탐색 후 static 캐시. 실패 시 재탐색 없음.

using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace SimpleSlaveryCollars.Utilities
{
    /// <summary>
    /// Pawn_RecordsTracker 내부 DefMap에 대한 리플렉션 접근 캐시.
    /// - DefMap 필드(FieldInfo)와 set_Item 메서드(MethodInfo)를 1회 탐색 후 캐시.
    /// - 탐색 실패 시 경고 로그 1회, 이후 재탐색 없음.
    /// </summary>
    internal static class SimpleSlaveryReflectionUtility
    {
        private static FieldInfo _defMapField;
        private static MethodInfo _defMapSetItem;
        private static bool _searched;

        /// <summary>
        /// DefMap 필드와 set_Item이 사용 가능한지 여부.
        /// 최초 호출 시 탐색을 수행한다.
        /// </summary>
        internal static bool IsAvailable
        {
            get
            {
                EnsureSearched();
                return _defMapField != null && _defMapSetItem != null;
            }
        }

        /// <summary>
        /// records에서 DefMap set_Item을 호출하여 레코드 값을 설정한다.
        /// 실패 시 false 반환, 예외를 삼킨다.
        /// </summary>
        internal static bool TrySetRecord(Pawn_RecordsTracker records, RecordDef def, float value)
        {
            if (records == null || def == null) return false;
            EnsureSearched();
            if (_defMapField == null || _defMapSetItem == null) return false;

            try
            {
                var defMap = _defMapField.GetValue(records);
                if (defMap == null) return false;
                _defMapSetItem.Invoke(defMap, new object[] { def, value });
                return true;
            }
            catch (System.Exception e)
            {
                Log.Warning($"[SSC] DefMap set_Item failed: {e}");
                return false;
            }
        }

        /// <summary>
        /// 1회 리플렉션 탐색. 이미 수행했으면 즉시 반환.
        /// </summary>
        private static void EnsureSearched()
        {
            if (_searched) return;
            _searched = true;

            // 1차: 이름 "records"로 검색
            _defMapField = typeof(Pawn_RecordsTracker).GetField(
                "records",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            // 2차: 타입 매칭 폴백
            if (_defMapField == null)
            {
                var dmType = typeof(DefMap<RecordDef, float>);
                _defMapField = typeof(Pawn_RecordsTracker)
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .FirstOrDefault(f => f.FieldType == dmType);
            }

            if (_defMapField == null)
            {
                Log.Warning("[SSC] Pawn_RecordsTracker DefMap field not found.");
                return;
            }

            // set_Item 탐색
            var mapType = _defMapField.FieldType;
            _defMapSetItem = mapType.GetMethod(
                "set_Item",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(RecordDef), typeof(float) },
                modifiers: null);

            // 폴백: 시그니처 매칭
            if (_defMapSetItem == null)
            {
                _defMapSetItem = mapType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "set_Item") return false;
                        var ps = m.GetParameters();
                        return ps.Length == 2
                               && typeof(RecordDef).IsAssignableFrom(ps[0].ParameterType)
                               && ps[1].ParameterType == typeof(float);
                    });
            }

            if (_defMapSetItem == null)
            {
                Log.Warning("[SSC] DefMap<RecordDef,float>.set_Item not found.");
            }
        }
    }
}