# Simple Slavery Collars — RimWorld Mod

RimWorld 1.6 노예 목걸이 모드. C# (.NET Framework 4.8) + Harmony 패치.
노예/죄수에게 폭발·감전·동결 목걸이를 장착하여 관리하는 시스템.

## 빌드

```bash
cd "Source/SimpleSlaveryCollars v1.6/SimpleSlaveryCollars"
dotnet build SimpleSlaveryCollars.csproj
```

빌드 결과물은 `1.6/Assemblies/SimpleSlaveryCollars.dll`로 출력됨.

## 핵심 구조

- `Source/SimpleSlaveryCollars v1.6/` — C# 소스 (1.6 전용, 활성 개발 대상)
- `Source/SimpleSlavery v1.5/` — 1.5 레거시 소스 (유지보수만)
- `1.6/Defs/` — XML Def 정의 (ThingDef, JobDef, WorkGiverDef 등)
- `Common/Languages/` — 5개 언어 번역 (English, Korean, ChineseSimplified, PortugueseBrazilian, Russian)
- `Common/Textures/` — 텍스처 에셋
- Harmony ID: `TRIBeagle.simpleslaverycollars`

## 절대 규칙

1. **세이브 호환성 최우선** — `ExposeData()`의 Scribe 키 변경/삭제 금지. 직렬화 클래스(Hediff, ThingComp, Apparel)의 네임스페이스·클래스명 변경 금지. 새 필드는 안전한 기본값 필수. 변경 전 세이브 영향 분석.
2. **주석은 한국어** — 파일 헤더, XML doc, 인라인 전부 한국어. 코드 내 RimWorld 용어(Pawn, Hediff 등)는 영어 그대로.
3. **번역 동기화** — 번역 키 추가/수정 시 5개 언어 전부 갱신. English 먼저, 나머지 언어는 해당 언어로 번역.
4. **csproj 동기화** — .cs 파일 추가/삭제 시 `SimpleSlaveryCollars.csproj`의 `<Compile Include>` 갱신.
5. **v1.6만 작업** — `Source/SimpleSlaveryCollars v1.6/`만 수정 대상. 구버전 소스(`v1.5/`, `v1.4/` 등)는 직접 수정 금지. `Common/Textures/`는 전 버전 공유(LoadFolders.xml 참조)이므로 변경 시 구버전 호환성 확인 필수. 코드 검색·감사 시에도 v1.6 소스만 대상으로 할 것.

## 세부 규칙 (필요시 참조)

작업 내용에 따라 아래 문서를 읽고 따를 것:

- `agent_docs/code_conventions.md` — C# 코딩 컨벤션 (파일 헤더 형식, Harmony 패치 규칙)
- `agent_docs/performance.md` — Tick 최적화, LINQ 금지, 리플렉션 캐싱 등 성능 규칙
- `agent_docs/save_compatibility.md` — 세이브 호환성 상세 가이드 (Scribe, 마이그레이션, ExposeData 패턴)
- `agent_docs/xml_and_translations.md` — XML Def 작성법, 번역 파일 구조, 언어별 주의사항
