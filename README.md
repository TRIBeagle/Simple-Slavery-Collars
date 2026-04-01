# Simple Slavery Collars (RimWorld 1.6)

---

## Description (ENG)

An updated version of Thirite's **Simple Slavery**, reworked for RimWorld 1.6 and the official Ideology DLC slavery system.
The original standalone slavery mechanics have been removed (DLC covers this). This mod adds **Slave Collars** with remote control capabilities, a **collar charge/battery system**, and various tweaks to expand and improve the DLC slave experience.

### Features

**Slave Collars (3 types)**
- **Explosive Collar** -- arm and remotely detonate to execute a slave
- **Electric Collar** -- arm and deliver electric shocks
- **Crypto Collar** -- put a slave into cryptosleep stasis; restores previous mental state on release

**Collar Charge System**
- Collars have a battery that drains over time (faster when armed)
- Battery capacity scales with collar quality
- Charge bar gizmo with draggable recharge threshold slider
- Self-recharge: assimilated (Stage 5) slaves can recharge their own collar at a console
- Warden recharge: wardens escort slaves to recharge stations when charge drops below threshold
- Configurable drain multiplier

**EMP / Solar Flare Disruption**
- EMP hits temporarily disable collars (grey bar + disrupted status)
- Solar flares disable all collars globally
- Armed collars are forcibly disarmed during disruption

**Remote Collar Console**
- Building that provides centralized collar management
- Management UI with sortable columns: name, type, collar, status, action
- Filters by pawn type (colonist/slave/prisoner) and collar type
- Individual arm/disarm/detonate buttons per pawn
- Bulk arm/disarm/detonate with dropdown by collar type
- Option to restrict collar control to console only (no direct gizmo toggle)

**Slave Stage System (5 stages)**
- Slaves progress through 5 stages over time (configurable period per stage)
- Each stage affects mood, rebellion chance, and work restrictions
- Stage 5 (Assimilation): slave faction switches to player, work type restrictions lifted
- Slaves with vanilla "unwavering loyalty" are blocked from Stage 5 (configurable)
- Shackle system with warden jobs to apply/remove restraints

**DLC Slavery Tweaks**
- Rebellion cycle adjustment
- Work speed debuff removal option
- Slave role assignment in Ideology rituals
- Configurable shackle defaults for new slaves

**Compatibility**
- Humanoid Alien Races (HAR) support via reflection (graceful fallback if not installed)
- Compatible with Small/Tiny/Micro Comms Console and Vanilla Gravship Expanded comms terminal
- Safe to add mid-game. Do NOT remove mid-game (save data corruption)
- Safe save/load: all new fields have safe defaults for existing saves
- Fully configurable: every feature can be toggled on/off in mod settings

### Mod Settings

| Setting | Default | Description |
|---------|---------|-------------|
| Shackles Default | ON | New slaves start shackled |
| Slave Stage | ON | Enable 5-stage progression |
| Rebel Cycle Change | ON | Adjust rebellion timing |
| Remove Work Debuff | ON | Remove slave work speed penalty |
| Assign Slave | ON | Allow slave role assignment in rituals |
| Stage 5 Work Unlock | ON | Unlock all work types at Stage 5 |
| Assimilation | ON | Stage 5 slaves join player faction |
| Ignore Unwavering Loyalty | OFF | Allow unwavering slaves to reach Stage 5 |
| Remote Only on Console | ON | Restrict collar control to console |
| Collar Charge | ON | Enable battery/charge system |
| Collar Disruption | ON | Enable EMP/flare vulnerability |
| Drain Multiplier | 1.0x | Global charge drain speed |
| Stage 1-4 Period | 15 days each | Days per stage |

### Installation

- Download from [GitHub Releases](https://github.com/) or [Steam Workshop](https://steamcommunity.com/) and place in your `Mods` folder.
- Requires RimWorld 1.6 or higher.
- Requires [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077).
- No other dependencies.

### Credits

- **Original Author**: Thirite -- [Forum link](https://ludeon.com/forums/index.php?topic=33631.0)
- **Updates**:
  - Ziehn -- [Steam link](https://steamcommunity.com/sharedfiles/filedetails/?id=1635565299)
  - MarkSill (1.1-1.2) -- [Steam link (archived)](https://steamcommunity.com/sharedfiles/filedetails/?id=2144935009)
- **Collars version & RimWorld 1.3-1.6 Porting**: TRIBeagle
- **Translations**:
  - Proxyer (Japanese) -- [Translation link](https://steamcommunity.com/sharedfiles/filedetails/?id=1636672484)
  - Aramati (Portuguese-BR)
  - xRg (Russian)
  - lzw-723 (Chinese Simplified)

### License

This mod is licensed under CC-BY-NC-SA 4.0.
See [LICENSE](./LICENSE) for details.

---

## 소개 (KOR)

Thirite의 **Simple Slavery** 모드를 기반으로, RimWorld 1.6 및 이데올로기 DLC 노예 시스템과 호환되도록 업데이트한 버전입니다.
기존 독자적인 노예 시스템은 DLC에 통합되어 제거되었으며, **노예 목걸이** 3종 + **원격 제어 콘솔** + **충전/배터리 시스템** 등 다양한 기능이 추가되었습니다.

### 주요 기능

**노예 목걸이 (3종)**
- **폭발 목걸이** -- 무장 후 원격 기폭으로 착용자 처형
- **전기 목걸이** -- 무장 후 전기충격 가능
- **크립토 목걸이** -- 노예를 동면 상태로 전환; 해제 시 이전 정신상태 복원

**충전 시스템**
- 목걸이에 배터리 내장, 시간 경과에 따라 소모 (무장 시 가속)
- 품질에 따른 배터리 용량 차이
- 드래그 가능한 충전 임계값 슬라이더가 있는 충전 바 기즈모
- 자가충전: 동화(Stage 5) 노예가 콘솔에서 자가 충전 가능
- 감시원 충전: 감시원이 충전량 부족 시 노예를 콘솔로 에스코트
- 글로벌 소모 배율 조절 가능

**EMP / 태양 흑점 교란**
- EMP 피격 시 목걸이 일시 비활성화 (회색 바 + 교란 상태 표시)
- 태양 흑점 시 모든 목걸이 전역 비활성화
- 교란 중 무장된 목걸이 강제 해제

**원격 목걸이 콘솔**
- 중앙 집중식 목걸이 관리 건물
- 정렬 가능한 컬럼이 있는 관리 UI: 이름, 신분, 목걸이, 상태, 제어
- 폰 유형(식민자/노예/죄수) 및 목걸이 유형별 필터
- 개별 무장/해제/기폭 버튼
- 일괄 무장/해제/기폭 드롭다운 (목걸이 유형별)
- 콘솔 전용 제어 모드 옵션 (직접 기즈모 토글 제한)

**노예 Stage 시스템 (5단계)**
- 노예가 시간에 따라 5단계를 거쳐 진행 (단계별 기간 설정 가능)
- 각 단계별 기분, 반란 확률, 작업 제한 영향
- Stage 5 (동화): 노예 소속이 플레이어 팩션으로 변경, 작업 제한 해제
- 바닐라 "확고한 충성심" 상태의 노예는 Stage 5 도달 불가 (옵션으로 무시 가능)
- 족쇄 시스템 + 감시원 족쇄 적용/해제 Job

**DLC 노예 시스템 트윅**
- 반란 주기 조정
- 작업 속도 디버프 제거 옵션
- 이데올로기 의식에서 노예 역할 배정 가능
- 신규 노예 족쇄 기본값 설정

**호환성**
- Humanoid Alien Races (HAR) 리플렉션 지원 (미설치 시 정상 폴백)
- Small/Tiny/Micro Comms Console 및 Vanilla Gravship Expanded 통신기 호환
- 게임 도중 모드 추가: OK. 게임 도중 모드 제거: 권장하지 않음 (세이브 손상 가능)
- 세이브 호환: 모든 신규 필드에 안전한 기본값 적용
- 전체 설정 가능: 모든 기능을 모드 설정에서 개별 ON/OFF 가능

### 모드 설정

| 설정 | 기본값 | 설명 |
|------|--------|------|
| 족쇄 기본값 | ON | 신규 노예에 족쇄 적용 |
| 노예 Stage | ON | 5단계 진행 시스템 활성화 |
| 반란 주기 변경 | ON | 반란 타이밍 조정 |
| 작업 디버프 제거 | ON | 노예 작업 속도 페널티 제거 |
| 노예 역할 배정 | ON | 의식에서 노예 역할 배정 허용 |
| Stage 5 작업 해제 | ON | Stage 5 도달 시 모든 작업 해금 |
| 동화 | ON | Stage 5 노예가 플레이어 팩션 합류 |
| 확고한 충성심 무시 | OFF | 확고한 충성심 노예도 Stage 5 도달 허용 |
| 콘솔 전용 원격 | ON | 목걸이 제어를 콘솔로 제한 |
| 목걸이 충전 | ON | 배터리/충전 시스템 활성화 |
| 목걸이 교란 | ON | EMP/태양 흑점 취약성 활성화 |
| 소모 배율 | 1.0x | 글로벌 충전 소모 속도 |
| Stage 1-4 기간 | 각 15일 | 단계별 소요 일수 |

### 설치

- [GitHub Releases](https://github.com/) 또는 [Steam Workshop](https://steamcommunity.com/)에서 다운로드 후 `Mods` 폴더에 넣으세요.
- RimWorld 1.6 이상 필요.
- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) 필수.
- 추가 모드 의존성 없음.

### 크레딧

- **원작자**: Thirite -- [포럼 링크](https://ludeon.com/forums/index.php?topic=33631.0)
- **업데이트**:
  - Ziehn -- [스팀 링크](https://steamcommunity.com/sharedfiles/filedetails/?id=1635565299)
  - MarkSill (1.1-1.2) -- [스팀 링크 (보존)](https://steamcommunity.com/sharedfiles/filedetails/?id=2144935009)
- **Collars 버전 및 1.3-1.6 포팅**: TRIBeagle
- **번역**:
  - Proxyer (일본어) -- [번역 링크](https://steamcommunity.com/sharedfiles/filedetails/?id=1636672484)
  - Aramati (포르투갈어-BR)
  - xRg (러시아어)
  - lzw-723 (중국어 간체)

### 라이선스

이 모드는 CC-BY-NC-SA 4.0 라이선스를 따릅니다.
자세한 내용은 [LICENSE](./LICENSE) 파일 참고.
