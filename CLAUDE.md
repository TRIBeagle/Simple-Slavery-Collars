# Simple Slavery Collars v1.6 — Claude Code 프로젝트 규칙

## 프로젝트 개요

RimWorld 1.6 모드. C# (.NET Framework 4.8) + Harmony 패치.
네임스페이스: `SimpleSlaveryCollars` (하위: `.Utilities`, `.Patches`, `.Jobs`)
소스 경로: `Source/SimpleSlaveryCollars v1.6/SimpleSlaveryCollars/`
Harmony ID: `TRIBeagle.simpleslaverycollars`

## 코드 작성 규칙

### 주석 스타일

- **파일 헤더** (필수, 모든 .cs 파일):
  ```
  // SimpleSlaveryCollars | [폴더명] | [파일명].cs
  // 목적 : [한국어로 이 파일의 핵심 역할 1-2문장]
  // 용도 : [한국어로 구체적 사용 맥락/동작 설명]
  // 주의 : [한국어로 주의사항] (해당 시에만)
  ```
- **XML doc comments**: 한국어 `/// <summary>설명</summary>`
- **인라인 주석**: 한국어 `// 설명`
- **영어 사용 금지**: 주석은 전부 한국어. 코드 내 영어 용어(Pawn, Verb, Hediff, Apparel 등)는 그대로 사용.

### csproj 동기화

파일 추가/삭제 시 반드시 `SimpleSlaveryCollars.csproj`의 `<Compile Include="...">` 항목을 갱신할 것.
기존 항목의 폴더/알파벳 정렬 순서를 따를 것.

### 세이브 호환성 (최우선 원칙)

- `ExposeData()`의 Scribe 키 문자열은 **절대 변경/삭제 금지**.
- 새 필드 추가 시 반드시 기존 세이브에서 안전한 기본값(default) 설정.
- 직렬화 대상 클래스(Hediff, ThingComp, Apparel 등)의 **네임스페이스와 클래스명 변경 금지**.
- 변경 전 세이브 영향 분석 필수.

### 성능 원칙

- **Tick() / TickInterval()** 내 코드는 최소한으로. 무거운 로직은 TickRare(250틱) 이상 간격으로.
- **리플렉션**: 1회 탐색 후 static 캐시. 실패 시 재탐색 없음 (`SimpleSlaveryReflectionUtility` 패턴 참고).
- **LINQ 지양**: 핫패스(Tick, Gizmo, ThoughtWorker)에서는 for 루프 사용.
- **컬렉션 순회 중 수정**: `AllPawnsSpawned` 등 순회 중 폰 사망 가능 시 스냅샷(`.ToList()` 또는 별도 리스트) 필수.
- **GetSlaveCollar() 등 유틸 호출**: 같은 폰에 대해 반복 호출 금지. 로컬 변수에 캐시.

### Null 안전성

- `pawn.jobs?.curDriver?.asleep` 패턴 사용 (jobs/curDriver는 null 가능).
- `pawn.mindState?.mentalStateHandler` null 체크 후 접근.
- `AccessTools.Field()` 결과는 반드시 null 체크.
- `as` 캐스팅 후 null 체크 없이 멤버 접근 금지.

### Harmony 패치

- Patch 클래스는 `Patches/` 폴더에 파일 1개 = 패치 1개.
- `[HarmonyPatch]` 어트리뷰트 사용. `PatchAll()` 자동 등록.
- ThoughtWorker에서 게임 상태 변경(부작용) 금지. CompTickRare 등 안전한 위치에서 수행.

## 빌드

```bash
cd "Source/SimpleSlaveryCollars v1.6/SimpleSlaveryCollars"
dotnet build SimpleSlaveryCollars.csproj
```

## 폴더 구조

```
SimpleSlaveryCollars/
├── Comps/           # ThingComp 구현 (CompSlave, CompRemoteSlaveCollar 등)
├── Debugs/          # 디버그 액션
├── Enums/           # 열거형
├── Hediffs/         # Hediff 클래스 (Enslaved, CryptoStasis)
├── Jobs/            # JobDriver, WorkGiver
├── Patches/         # Harmony 패치 (파일 1개 = 패치 1개)
├── Precepts/        # 이데올로기 역할 조건
├── Properties/      # AssemblyInfo
├── Records/         # RecordWorker
├── Things/          # Apparel 서브클래스 (SlaveCollar 3종)
├── ThoughtWorkers/  # ThoughtWorker (순수 판정, 부작용 없음)
├── Utilities/       # 유틸리티 (SimpleSlaveryUtility, ReflectionUtility)
├── HarmonyInit.cs
├── SimpleSlaveryCollars_Mod.cs
├── SimpleSlaveryCollars_Setting.cs
└── SimpleSlaveryDefOf.cs
```
