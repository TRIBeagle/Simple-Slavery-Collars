// SimpleSlaveryCollars | Core | HarmonyInit.cs
// 목적   : 모드 초기화 시점에 Harmony 패치를 일괄 등록
// 용도   : [StaticConstructorOnStartup]을 통해 RimWorld 로딩 시 자동 실행
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용
// 주의   : Harmony ID는 고유 문자열("TRIBeagle.simpleslaverycollars")로 관리해야 함
// 저장   : 없음 (런타임 패치 전용)

using HarmonyLib;
using System.Reflection;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// 모드 로드 시 Harmony 패치를 일괄 등록하는 초기화 클래스.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            new Harmony("TRIBeagle.simpleslaverycollars")
                .PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
