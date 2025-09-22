// SimpleSlaveryCollars | Core | SimpleSlaveryCollars_Mod.cs
// 목적   : 모드 엔트리포인트 정의 및 설정(SimpleSlaveryCollarsSetting) 초기화
// 용도   : RimWorld Mod 클래스 확장
// 변경   : 2025-09-22 주석 규칙(v4.2) 적용
// 주의   : SettingsCategory/DoSettingsWindowContents는 RimWorld 기본 Mod UI 연동용

using UnityEngine;
using Verse;

namespace SimpleSlaveryCollars
{
    /// <summary>
    /// 모드 엔트리포인트. 설정 객체를 생성/등록한다.
    /// </summary>
    class SimpleSlaveryCollarsMod : Mod
    {
        public static SimpleSlaveryCollarsSetting settings;

        public SimpleSlaveryCollarsMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<SimpleSlaveryCollarsSetting>();
        }

        public override string SettingsCategory() => "Simple Slavery Collars";

        public override void DoSettingsWindowContents(Rect inRect) =>
            settings.DoSettingsWindowContents(inRect);
    }
}
