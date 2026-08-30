# NEON HORDE — 개발 로그

## M0 — 프로젝트 셋업 ✅ (2026-08-30)

- 엔진: Unity **6000.3.20f1**, URP 17.3.0 / Input System 1.19.0 / uGUI 2.0.0
- 프로젝트 루트: `D:\Project\game` (Assets/, Packages/, ProjectSettings/ + docs/)
- URP 파이프라인 자동 구성: `Assets/_Project/Settings/NeonHorde_URP.asset` (+ Renderer, PostFX 프로필)
  - GraphicsSettings + 전 퀄리티 레벨에 할당, Color Space = **Linear**
  - PostFX: Bloom(threshold 0.9 / intensity 1.1 / scatter 0.7) + Tonemapping ACES, Global Volume
- 씬: `Assets/_Project/Scenes/Game.unity` (빌드 세팅 등록)
  - Main Camera: 오소 size 6, post-processing on, `CameraFollow`
  - Systems: `RunManager` — 60Hz 고정 타임스텝 누산기 루프
  - Player: 네온 원(HDR), `PlayerInputReader`(드래그 조이스틱 + WASD), `PlayerController`(고정틱 이동)
  - Neon Grid: 카메라 추적 무한 그리드 배경(단일 드로우)
- 코어 유틸: `SeededRng`(xoshiro128\*\*), `ObjectPool<T>`, `SpatialHash`, `ServiceLocator`, `RuntimeShapes`, `Palette`
- 도메인: `Balance`, `PlayerStats`, `MetaState`
- 서비스: `ISaveService` + `JsonSaveService`(persistentDataPath/save.json), `GameBootstrap` 부팅 배선
- 테스트: EditMode 8개 통과 (`SeededRngTests`, `SpatialHashTests`)
- 어셈블리: `NeonHorde` / `NeonHorde.Editor` / `NeonHorde.Tests.EditMode` (M1에서 Core/Domain/Game 분리 예정)
- 에디터 툴: `NeonHorde/Setup Project` 메뉴 = 위 구성 재생성

### 검증 방법
Unity Hub에서 `D:\Project\game` 열기 → `Assets/_Project/Scenes/Game.unity` → Play.
플레이어 원을 드래그(또는 WASD)로 이동, 카메라가 부드럽게 추적, 네온 그리드가 무한히 스크롤되면 정상.
Console에 `[NeonHorde] boot — gold=0 runs=0 save=...` 로그 확인.

### 알려진 후속 작업
- 2D Renderer(Light2D) 교체는 M3 주스 단계에서 (현재는 표준 Universal Renderer + Bloom)
- Burst/Collections/Jobs 하이브리드 적 시스템은 M2 성능 패스에서 도입 (M1은 풀링된 MonoBehaviour)
- Boot 씬 분리, 메인메뉴는 M1/M3

---

## 설계 변경 로그

- **2026-08-30**: 캐릭터 4종(PULSE 기본 + VOLT/AEGIS/HALO), **코어(Core)** 재화(퀘스트 + IAP),
  캐릭터 개별 IAP 직접 구매, 퀘스트 시스템(일일/마일스톤), 맵 = **유한 아레나 절차적 지형/장애물 생성**
  + 플로우 필드 길찾기 + 테마 4종(GRID/VOID/FURNACE/CRYO). → GAME_DESIGN §3·§7·§8, ARCHITECTURE §3·§5·§8 반영.
  마일스톤: 맵/지형 → M2(2주), 캐릭터·코어·퀘스트 → M3, 캐릭터·코어 IAP → M5. 총 9~10주.

---

## M1 — 코어 전투 루프 ✅ (2026-08-30)

컴파일 클린 · EditMode 테스트 8/8 통과 · `NeonHorde/Setup M1 Scene` 메뉴로 씬 배선.

- **RunManager**: 60Hz 고정 틱, `RunState` 소유, `Paused`(레벨업/게임오버), `OnLevelUp`/`OnGameOver` 이벤트
- **EnemyManager**: struct 배열 풀(Capacity 6000, swap-remove), 매니저 단일 루프(적별 Update 없음),
  스패셜 해시 추적 + 분리 스티어링, GPU 인스턴싱 드로우(`Graphics.RenderMeshInstanced`, 종류별 1배치)
- **DirectorSystem**: 시간 비례 스폰율(2→60/s), 플레이어 주변 링 스폰, 시드 RNG, 60초 후 탱크 등장
- **WeaponSystem**: 볼트 1종 — 최근접 적 조준 자동 발사
- **ProjectileManager** / **PickupManager**: 동일 데이터지향 패턴, 인스턴싱 드로우. 젬 자석 → `RunState.AddXp`
- **PlayerController**: HP·무적프레임(0.6s)·접촉 데미지·사망
- **UI(레거시 uGUI)**: XP바/HP바/타이머·레벨·킬 HUD, 레벨업 3택 오버레이(생성형 스탯 카드 5종),
  게임오버 오버레이(생존/레벨/킬 + 다시하기), 플로팅 가상 조이스틱
- **DebugSpawner**: P=300마리, O=1000마리 스폰(성능 벤치)
- 적 3종 하드코딩(`EnemyTypes`): Walker/Swarmer/Tank

### 검증
`Game.unity` Play → 조이스틱/WASD 이동, 볼트 자동 발사로 적 처치, 젬 흡수 → 레벨업 카드 3택 →
스탯 상승, 피격 시 HP 감소 → 0이면 게임오버. P키로 대량 스폰해 프레임 확인.

### M1에서 미룬 것
- 플로우 필드 길찾기 → M2 (현재 직접 추적)
- 데이터지향 렌더를 Burst/Jobs로 → M2 성능 패스
- WeaponDefSO/PassiveDefSO 카탈로그, 무기 8종·특성 12종 → M2
- 유한 아레나·지형 → M2
- TMP 폰트, 2D Light → M3 (현재 레거시 Text + Bloom)

---

## M2 — 콘텐츠 + 절차 아레나 ✅ (2026-08-30)

- 코드 카탈로그(SO 대신, 밸런스 iteration 집중): `WeaponCatalog`(8종+8진화, 8레벨 커브),
  `PassiveCatalog`(15종), `EnemyCatalog`(9종 + 행동 7종), `MapThemeCatalog`(테마 4 + 모디파이어 5)
- `DerivedStats` — 패시브×캐릭터×메타 배수 합성. `WeaponInstance`/`RunState` 재작성
- `WeaponSystem` — 8행동(Linear/Aura/Orbit/Arc/Homing/Lob/Chain/Boomerang), 크리, 투사체 다발
- `ProjectileManager` — 5 kind(Straight/Homing/Boomerang/Lob/Orbit) + `HostileProjectileManager`(적 탄)
- `EnemyManager` 재작성 — 행동별(추적/돌진/원거리/자폭/분열/팬텀/프로스트), 엘리트/보스, 시간 HP 스케일
- **맵**: `NavGrid` + `FlowField`(BFS) + `MapGenerator`(유한 아레나 80~110, 블로커·크레이트·감속·해저드
  스캐터 + 연결성 pruning + 테마 몬스터 roster) + `Arena`(런타임, 인스턴싱 렌더, 크레이트 파괴) + `FlowFieldSystem`
- `PlayerController` — collide-and-slide, 감속/해저드 지대, 재생, 캐릭터 능력 훅
- `DirectorSystem` — 맵 roster 스폰, 엘리트 인터벌, 보스 5/10/15/20분
- `PickupManager` — 젬/골드/상자, `UpgradeGenerator`(카탈로그 기반 3택), 20분 승리 + 골드 정산
- 테스트 18/18. **미룸**: Burst/Jobs 성능 패스(관리형으로 수백~1천 처리, 문서에 계획)

## M3 — 메타 + 주스 ✅ (2026-08-30)

- `MetaController`(골드/코어, 캐릭터 해금·선택, 영구 업그레이드, 런 배수), `QuestService`(일일 3개 날짜시드 롤 + 마일스톤, 진행/수령)
- `PowerCatalog`(영구 강화 8종), `CharacterCatalog`(4캐릭터 + 능력: 충격파/연쇄/저HP무적/레벨업폭발)
- 씬 분리: **Boot → MainMenu → Game** + 빌드 세팅 정렬. `MainMenuController`(코드 빌드 메뉴 + 캐릭터/상점/퀘스트 패널)
- 주스: `ScreenShake`(CameraFollow 통합), `DamageNumberSystem`(풀 TextMesh, 프레임 캡), `BossBanner`
- `IAudioService` 스텁, `GameConfig` 품질 토글(적 캡/데미지숫자), 무료 부활(PowerId.Revive)
- 테스트 18/18. **미룸**: 2D Renderer+Light2D(현재 Bloom), TMP(레거시 Text), 실제 오디오 클립

## M4 — 로그라이트 심화 ✅ (2026-08-30)

- 무기 진화(만렙 + 짝 특성 Lv3 → 상위 무기), 리롤(런당 1 + 광고), 삭제/banish(런당 1, key 기반 풀 제외)
- 일일 도전 버튼(날짜 시드 고정), 생존시간 기반 테마 난이도 가중(`MapGenerator.RollTheme` × bestTime)
- 보스 10/15/20 + 20분 승리는 M2에서 이미. 테스트 21/21

## M5 — 수익화 + 계정 ✅ (2026-08-30)

- 서비스 인터페이스 + 스텁: `IAdsService`(리워드: 부활/골드2배/리롤), `IIapService`(+`IapCatalog` 8상품 +`IapFulfillment`),
  `IConsentService`(ATT/UMP), `IAnalyticsService`(이벤트 지점 코드에 삽입)
- **계정/가입 유도**: `IAccountService` + `GuestAccountService`(로컬). 게스트 배너("삭제 시 진행상황 소실") +
  3판 후 1회 넛지 모달 + 설정 내 연결. Google/Apple/이메일 링크 스텁
- 게임오버 = 사망 프롬프트(광고 부활/결과 보기) → 결과(골드 2배 광고/재시작/메뉴). 이중 정산 방지
- 스토어 패널(코어 번들·광고제거·스타터팩), 설정 화면(볼륨·품질·언어·계정·세이브 삭제)
- `ConfigureBuild` 에디터: Android IL2CPP/ARM64/minSDK24, iOS IL2CPP/13.0, Linear. 테스트 21/21

## M6 — 폴리시 + 출시 문서 ✅ (2026-08-30)

- `FirstRunHint`(첫 런 조작 힌트 페이드), `Loc`(KO/EN 스트링 테이블 + 언어 토글, 메뉴/힌트 적용 — 나머지 UI는 점진 확장)
- 문서: [EXTERNAL_SETUP.md](EXTERNAL_SETUP.md)(iOS/광고/IAP/백엔드·DB/애널리틱스 — 네가 줄 것 정리),
  [STORE_LISTING.md](STORE_LISTING.md)(한/영 스토어 카피 + 스샷 리스트), [DIFFERENTIATION.md](DIFFERENTIATION.md)(오마주 차별점 + 추가 아이디어)

---

## 현재 상태 요약

- **컴파일 클린, EditMode 21/21 통과.** `NeonHorde/Setup Everything` 메뉴로 전체 재생성.
- Unity에서 `Boot.unity` Play → 메뉴 → 플레이까지 엔드투엔드 동작(에디터 기준; 실기기 미검증).
- 외부 SDK(광고/IAP/UGS/analytics)는 스텁 → 계정·키 받으면 실물 교체(각 1파일).
- 성숙도: 시스템/통합/플로우 = 구현 완료. 밸런스 수치·아트·오디오·실물 결제/백엔드 = 미완(의도).
