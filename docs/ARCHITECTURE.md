# NEON HORDE — 아키텍처 설계

> Flutter + Flame · Survivor-like · 수백~수천 엔티티 동시 처리가 핵심 제약

---

## 1. 설계 원칙

1. **데이터 지향 (data-oriented)** — 적/투사체/젬은 Flame `Component`가 아니라 순수 데이터 구조체 배열. 시스템이 배열을 일괄 갱신, 렌더러 1개가 일괄 드로우. Component 오버헤드/GC 회피.
2. **오브젝트 풀링** — 적·투사체·젬·데미지숫자·파티클 전부 풀에서 재사용. 런 중 할당 0에 수렴.
3. **고정 타임스텝 시뮬레이션** — 60Hz 고정 틱 + 렌더 보간. 결정론 확보(일일 시드, 추후 리플레이/랭킹 검증).
4. **시드 RNG 단일 소스** — 런당 하나의 PRNG. 모든 랜덤은 여기서만.
5. **시뮬 ↔ UI 분리** — 매 프레임 Riverpod 리빌드 금지. UI엔 throttle된 뷰모델(~10Hz)만 전달.
6. **튜닝 = 데이터** — 밸런스 상수/레벨 테이블은 코드 카탈로그(or JSON). 로직과 분리.
7. **서비스는 인터페이스** — 광고/IAP/애널리틱스/저장은 추상화. 스텁으로 개발, 나중에 실물 주입.

---

## 2. 레이어

```
┌─────────────────────────────────────────────────────────────┐
│ Presentation (Flutter widgets)                              │
│  메뉴 · 상점 · 스테이지선택 · 설정 · 결과 · 인게임 오버레이     │
│  (HUD, 레벨업 3택, 일시정지, 부활)                            │
└───────────────▲─────────────────────────────────────────────┘
                │ Riverpod providers (메타/네비게이션/반응형 UI)
┌───────────────┴─────────────────────────────────────────────┐
│ Application / State                                         │
│  MetaController(영구), RunController(런 수명주기 + 레벨업 흐름) │
│  RunHudVM (ValueNotifier, throttled)                        │
└───────────────▲─────────────────────────────────────────────┘
                │
┌───────────────┴─────────────────────────────────────────────┐
│ Game Simulation (Flame)                                     │
│  SurvivorsGame(FlameGame) → GameWorld                       │
│  systems: input, movement, director, enemyAI, weapon,       │
│           projectile, damage, pickup, playerCollision, fx   │
│  entity pools + single-draw renderers                       │
│  core: SpatialHash, SeededRng, FixedLoop, Pool             │
└───────────────▲─────────────────────────────────────────────┘
                │
┌───────────────┴─────────────────────────────────────────────┐
│ Domain                                                      │
│  catalog: weapons / passives / enemies / characters / stages│
│  balance: balance_config, xp_curve, scaling                 │
│  models: meta_state, run_state, player_stats, ...           │
└───────────────▲─────────────────────────────────────────────┘
                │
┌───────────────┴─────────────────────────────────────────────┐
│ Services (인터페이스 + 구현)                                  │
│  SaveService(Hive) · AudioService · AdsService              │
│  AnalyticsService · IapService(선택) · ConsentService       │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. 상태 관리

### MetaState (영구, 저장됨 — JsonUtility 직렬화 가능한 plain class)
```
version: int                              // 마이그레이션용

// --- 재화 3종 ---
gold: long                                // 인게임 파밍 (영구 업그레이드용)
cores: int                                // 캐릭터 해금용 하드 재화 (퀘스트/IAP)

// --- 언락 ---
powerLevels: Map<PowerId,int>             // 영구 업그레이드
unlockedCharacters: Set<CharId>           // 항상 PULSE 포함
selectedCharacter: CharId
unlockedWeapons: Set<WeaponId>

// --- 퀘스트 ---
dailyQuests: { dateIso, list[ {questId, progress, claimed} ] }
milestoneQuests: Map<QuestId, {progress, claimed}>

// --- IAP ---
ownedProducts: Set<ProductId>             // char.volt / remove_ads / starter_pack ...
adsRemoved: bool

// --- 기타 ---
stats: {totalKills, bestTimeSec, totalRuns, bossKills, ...}
settings: {bgm, sfx, haptics, damageNumbers, joystickSide, quality}
daily: {lastDateIso, lastSeed, bestScore}
```
- `MetaController` (plain C#, `ServiceLocator`에 등록)가 소유. UI는 이벤트/폴링으로 읽음.
- 저장 트리거: 런 종료 / 구매 / 퀘스트 수령 / 설정 변경 / `OnApplicationPause`.
- Set/Map은 JsonUtility 한계상 List + 직렬화 래퍼로 저장.

### RunState (런 중 임시, 저장 안 함 — MVP)
```
timeSec, seed, rngState
characterId                              // 선택 캐릭터 (statMods·ability·시작무기 적용)
mapPlan: {
  themeId, modifierIds[],
  arenaSize, borderRect,
  enemyRoster: EnemyDefSO[] (+themeWeights),
  terrain: TerrainPiece[] (blocker/crate/slow/hazard/impassable, pos, size, hp?),
  navGrid: NavGrid { w, h, cellSize, byte[] flags },
  hazardZones: [...]
}
flowField: { int[] dir  (셀별 이동방향 인덱스), lastRebuildTick, targetCell }
player: {pos, vel, hp, maxHp, iFrameTimer, abilityCooldowns}
stats(파생): {moveSpeed, might, area, cooldownMult, projAdd, duration,
             pickupRadius, xpMult, armor, luck, revivesLeft, regen, crit}
weapons: List<WeaponInstance{ defId, level, cooldownTimer, subTimers }>
passives: Map<PassiveId, int>
level, xp, xpToNext
goldCollected, kills
pools: enemies[], projectiles[], gems[], pickups[], damageTexts[], particles[]
director: { spawnAccumulator, nextBossIndex, waveCursor, eliteTimer }
pendingEvents: Queue<GameEvent>          // LevelUp, RunEnded, BossSpawn ...
```
- **시뮬(RunManager)이 소유**. 메타 상태와 분리.
- UI 필요 값만 `RunHudModel`(plain struct)로 ~10Hz push: hp/maxHp, level, xp%, timeSec, gold, kills, bossHp?.

### 레벨업 흐름
```
PickupSystem이 xp 임계 도달 → pendingEvents에 LevelUpEvent
틱 경계에서 감지 → RunManager.Paused = true (시뮬 루프 정지, Time.timeScale 은 건드리지 않음)
RunController가 3택 옵션 생성:
  - 보유 무기 레벨업 / 슬롯 여유 시 신규 무기 / 특성
  - luck 가중, 중복 제거, (해금 시) reroll·banish 반영
→ 'LevelUp' uGUI 오버레이 표시 (옵션 리스트 바인딩)
→ 카드 선택 콜백 → 시뮬에 적용 → RunManager.Paused = false 재개
```

---

## 4. 시뮬레이션 — 고정 틱 시스템 순서

`RunManager`가 `Update()`에서 누산기로 60Hz 고정 스텝 실행 (M0 구현 완료). 렌더는 최신 상태를 그대로 사용 (필요 시 alpha 보간).

| # | 시스템 | 역할 |
|---|---|---|
| 1 | PlayerInputReader | 조이스틱/드래그/WASD → 목표 속도 (Update에서 수집) |
| 2 | PlayerMovementSystem | 적분 + **아레나 경계 클램프** + 장애물 collide-and-slide + 감속지대 배율 |
| 3 | DirectorSystem | 시간기반 스폰 예산, `MapPlan.enemyRoster` 가중치+RNG로 타입 선택, 카메라 밖(아레나 내) 링에 스폰, 멀어진 적 재활용, 엘리트/보스 트리거 |
| 4 | FlowFieldSystem | N틱(~0.25s)마다 플레이어 향 플로우 필드 재계산 (BFS/Dijkstra, nav 격자, blocked 셀 회피) |
| 5 | EnemyAiSystem | 플로우 필드 샘플 + 로컬 분리(스패셜 해시 이웃) + 특수행동(돌진/원거리/자폭/분열). 장애물 없는 구역은 직접 추적 폴백 |
| 6 | WeaponSystem | 무기별 쿨다운 → 발사, 타겟 질의(최근접/범위, 스패셜 해시), 투사체 생성 or 오라 데미지 |
| 7 | ProjectileSystem | 이동, 수명, 관통, 적 충돌(스패셜 해시), blocked 셀 충돌(무기별 통과 여부) → 데미지 이벤트 |
| 8 | DamageSystem | 데미지/크리 적용, 사망 시 젬(+골드/상자) 스폰, 파괴가능 엄폐물 파괴 시 nav 격자 갱신, 킬 카운트, 히트 FX |
| 9 | PickupSystem | 자석: pickupRadius 내 젬 가속 → 접촉 시 xp/골드, 레벨업 체크 |
| 10 | PlayerCollisionSystem | 적 접촉 → 무적프레임 고려 데미지, 해저드 지대 지속 데미지, 픽업 접촉 |
| 11 | FxSystem | 파티클/데미지숫자/화면흔들림/히트플래시 (전부 풀) |
| 12 | CameraSystem | 플레이어 추적(LateUpdate), 아레나 경계에서 뷰 클램프 |
| 13 | HudSyncSystem | throttle된 값만 `RunHudModel`에 push (~10Hz) |
| 14 | GameOverCheck | hp<=0 → 부활 프롬프트 or RunEnded |

---

## 5. 충돌 / 공간 분할 / 길찾기 (성능 핵심)

### 스패셜 해시 (동적 엔티티)
- **균일 격자 스패셜 해시** (`SpatialHash`, M0 구현 완료). 셀 크기 ≈ 최대 적 지름.
- 적은 매 틱 이동 → 매 틱 `Clear()` + 재삽입 O(n). 질의: 원/AABB 이웃.
- 용도: 투사체↔적, 무기 타겟팅, 플레이어↔적, 적↔적 분리(같은+인접 셀, 상위 k개만).
- 내로우페이즈는 거리 제곱 비교(sqrt 회피).

### Nav 격자 (정적 지형)
- `MapGenerator`가 아레나를 셀(≈1 유닛)로 래스터화 → `NavGrid { byte[] flags }`: `Blocked` / `Slow` / `Hazard` / `Open`.
- 파괴가능 엄폐물 파괴 시 해당 셀 `Blocked→Open`, `FlowFieldSystem` 다음 재계산에 반영.

### 플로우 필드 (호드 길찾기)
- 목표 = 플레이어 셀. `Blocked` 제외하고 BFS(균일 비용) 또는 Dijkstra(Slow 가중) → 각 셀에 "다음 이동 방향" 저장.
- 재계산 주기 ~0.25s (플레이어가 셀 경계 넘을 때 즉시 1회). 아레나 100×100 = 1만 셀, BFS 1회 < 0.3ms.
- `EnemyAiSystem`: 적 위치 셀의 플로우 벡터 + 분리 스티어링 합성. 지형이 성긴 GRID 테마는 필드 스킵하고 직접 추적.
- M2에서 필드 계산을 Burst Job으로 옮길 수 있음 (프로파일 후).

---

## 6. 렌더링 전략

| 대상 | 방식 |
|---|---|
| 적 | `EnemyRenderer` 1개가 적 배열 순회, `Graphics.RenderMeshInstanced`로 타입별 배치 드로우 (쿼드 메시 + 인스턴스드 언릿 머티리얼, HDR 색 → Bloom). 스프라이트/아틀라스 없음 |
| 투사체 | `ProjectileRenderer` 인스턴스드 (쿼드/라인 + 트레일) |
| 젬/픽업 | `GemRenderer` 인스턴스드 (작은 마름모 + 글로우) |
| FX/데미지숫자 | 풀링된 `ParticleSystem` + 풀링 TMP, 카운트 상한, 저사양 시 감소 |
| 플레이어/보스 | 개별 `SpriteRenderer` (소수) — M0는 `NeonShape` 런타임 생성 스프라이트 |
| 지형 | 정적 배치 `MeshRenderer`/`SpriteRenderer` (런당 1회 생성), 경계벽 라인 |
| 배경 | `NeonGridBackground` — 카메라 추적 쿼드 + repeat 텍스처 (M0 구현 완료) |

- 목표: 중급 안드로이드에서 화면 500+ 엔티티 60fps (M2 성능 패스에서 Burst/instancing로 수천).
- 품질 토글: 파티클 밀도 / 데미지숫자 / 화면흔들림 / 최대 동시 적 수 / Bloom 해상도.
- M0 현재: 표준 Universal Renderer + Bloom. **2D Renderer + Light2D 교체는 M3** (네온 발광 강화).

---

## 7. 결정론 & RNG

- `SeededRng` (xoshiro128**, M0 구현 완료 + 테스트). 런당 1개.
- 시드: 일반 런 = `DateTime.UtcNow.Ticks`, 일일 챌린지 = 날짜 해시(고정), 맵 지형 = 런 시드에서 파생.
- 고정 타임스텝. 모든 게임플레이 랜덤은 `SeededRng` 경유 (`UnityEngine.Random` 직접 사용 금지).
- Burst 병렬 잡은 float 누적 순서가 기기별로 미세하게 달라질 수 있음 → 스폰/드랍/카드/지형 등 "공정성"에 중요한 것만 메인스레드 `SeededRng`로 결정, 적 위치 미세 차이는 허용.

---

## 8. 도메인 데이터 모델

### 카탈로그 (ScriptableObject — `Assets/_Project/Data/`, `GameDatabaseSO`가 집계)

```
WeaponDefSO {
  id, name, icon, tags[], behavior,           // linear|seek|orbit|aura|arc|chain|lob|boomerang
  levels: [ { damage, cooldown, count, area, speed, pierce, duration } x8 ],
  evolutionReq: { passiveId, intoWeaponId }?,  // M4
  sfx, vfx
}
PassiveDefSO { id, name, icon, stat, perLevel, maxLevel }

EnemyDefSO {
  id, shape, color, size, behavior,            // seek|charge|ranged|exploder|splitter|phantom|frosttank
  hpBase, hpScaleCurve(t), speedBase, speedCurve(t), contactDamage, xpValue, goldChance,
  themeWeights: Map<MapThemeId, float>         // 어느 맵에서 얼마나 자주 나오는지
}

CharacterDefSO {
  id,                                          // PULSE | VOLT | AEGIS | HALO
  displayName, icon,
  startWeaponId, startPassiveId?,
  statMods: { moveSpeedMul, maxHpMul, mightMul, xpMul, ... },
  ability: { type, params },                   // shockwave | killChainChance | lowHpInvuln | levelUpBlast ...
  unlock: {
    ownedByDefault: bool,                      // PULSE = true
    coreCost: int,                             // VOLT 60 / AEGIS 90 / HALO 120
    iapProductId: string                       // "char.volt" 등 — 직접 구매 경로
  }
}

MapThemeDef SO {
  id,                                          // GRID | VOID | FURNACE | CRYO
  palette, bgParams, music,
  enemySet: WeaponDef… no → EnemyDefSO[] (+themeWeights 사용),
  hazards: [ { type, params } ],               // fireColumn | visionShrink | slipperyMove | none
  bossSkin,
  difficultyTier: int                          // 생존 시간에 따른 가중 선택용
}

MapModifierDef SO {
  id, description,
  effects: { enemySpeedMul?, enemyHpMul?, goldMul?, eliteRateMul?, cooldownMul?, visionMul? }
}

QuestDefSO {
  id, description, scope,                       // daily | milestone
  goal: { type, target },                       // playCount | kills | surviveSec | reachLevel | bossKill | themeBossKill
  reward: { cores, gold }
}

IapProductDef SO {
  productId, kind,                              // character | coreBundle | removeAds | starterPack
  grants: { characterId?, cores?, gold?, removeAds? }
}
```

### 맵 생성 (런 시작 시, 시드 기반)
```
seed → SeededRng
theme   = weightedPick(MapThemeDef, byDifficultyTier, biasedBy runHistory/survivalStats)
mods    = pickDistinct(MapModifierDef, count = rng.range(1,2))
enemyRoster = EnemyDefSO where themeWeights[theme] > 0   (가중치 = 스폰 확률)
bossSkin = theme.bossSkin
→ RunContext.MapPlan { themeId, modifierIds[], enemyRoster, hazards }
DirectorSystem은 MapPlan.enemyRoster + themeWeights로만 스폰
```

### BalanceConfig (단일 파일, 모든 튜닝 상수)
```
xpToNext(level), enemyHpScale(t), enemySpeedScale(t), spawnRate(t),
eliteIntervalSec, bossTimesSec=[300,600,900,1200],
upgradeChoiceCount=3, rerollCostGold, banishCount,
reviveInvulnSec, magnetBaseRadius, pickupAccel,
critBaseChance, critMult, offscreenRecycleDist, maxEnemies(quality)
```

### PlayerStats 파생 계산
```
moveSpeed  = base * character.mod * (1 + passive.moveSpeed*k) * meta.power
might(dmg%) = (1 + passive.might*k) * meta.power
cooldownMult = clamp(1 - passive.atkSpeed*k, floor)
area, duration, projAdd, pickupRadius, xpMult, armor, luck ... 동일 패턴
```

---

## 9. 저장 시스템 (M0 구현: `JsonSaveService`)

- **JSON 파일** `Application.persistentDataPath/save.json` (`JsonUtility`). Set/Map은 List + 래퍼로 직렬화.
- 자동 저장 트리거: 런 종료 / 구매 / 퀘스트 수령 / 설정 변경 / `OnApplicationPause(true)`.
- MVP는 **런 중간 저장 없음** (런이 짧음). 추후 RunState 직렬화로 "이어하기" 가능.
- 로드 실패(손상/없음) → 기본 프로필 폴백 (M0 구현됨).
- 경량 무결성: 저장 blob 체크섬 (단순 편집 억제용, 진짜 안티치트 아님) — M5.
- 마이그레이션: `version` 필드 + 단계별 마이그레이터.

---

## 10. 서비스 (인터페이스 + 구현, `ServiceLocator` 등록 — M0 골격)

### ISaveService → `JsonSaveService` ✅ (M0 구현)
`MetaState Load()` / `void Save(MetaState)` / `void Delete()`.

### IAdsService → `LevelPlayAdsService` (M5) — 라이트, 리워드 전용
```
LoadRewarded()
ShowRewarded(placement) -> Task<bool>     // 시청 완료 시 true
// placement: revive | double_gold | reroll | chest_x2
// show 후 다음 광고 프리로드. no-fill 시 false → 버튼 비활성/대체 보상
```
- Unity LevelPlay(ironSource) 미디에이션. iOS ATT (`ATTrackingStatusBinding`) + UMP 동의 (`IConsentService`).

### IIapService → `UnityIapService` (M5) — `com.unity.purchasing`
```
Purchase(productId) -> Task<PurchaseResult>
Restore()
// productId: char.volt | char.aegis | char.halo | cores.small|medium|large | remove_ads | starter_pack
// 성공 → MetaController.GrantProduct(IapProductDef)  (캐릭터 해금 / 코어 지급 / adsRemoved)
```

### IAnalyticsService → `UgsAnalyticsService` (M5)
`run_start`, `run_end{timeSec,kills,cause,level,themeId}`, `level_up{choiceId}`,
`weapon_evolved{id}`, `meta_upgrade_bought{id,level}`, `quest_claimed{id,cores}`,
`character_unlocked{id,method}`, `ad_impression{placement,result}`. PII 최소.

### IAudioService → `AudioService` (M3)
테마/메뉴별 BGM 루프, SFX 풀(히트/레벨업/픽업/보스/사망). 프리로드. 보스 시 BGM 덕킹. 설정·무음스위치 존중.

### MetaController (plain C#, M3)
재화(gold/cores) 가감, 캐릭터/무기 언락, 퀘스트 진행·수령, 영구 업그레이드 구매. 변경 시 `ISaveService.Save`.

### QuestService (M3)
일일 퀘스트 롤(날짜 시드), 런 종료 이벤트로 진행 갱신(플레이수/처치/생존/레벨/보스), 마일스톤 추적, 수령 시 코어 지급(→ MetaController).

### MapGenerator / FlowFieldSystem (M2)
`MapGenerator.Generate(seed, themeDef, modifiers) -> MapPlan` (아레나·지형·navGrid·hazardZones).
`FlowFieldSystem` — navGrid + 플레이어 셀 → BFS 플로우 필드, ~0.25s 주기 재계산.

---

## 11. Unity 패키지 / 모듈

```
현재(M0):
  com.unity.render-pipelines.universal  17.3.0
  com.unity.inputsystem                 1.19.0
  com.unity.ugui                        2.0.0
  com.unity.2d.sprite / 2d.tilemap
  com.unity.test-framework              1.6.0
  com.unity.ide.rider / ide.visualstudio

M2 성능 패스:
  com.unity.burst, com.unity.collections, com.unity.mathematics   # Jobs 하이브리드
  (com.unity.cinemachine  — 선택, 카메라)

M3:
  URP 2D Renderer + com.unity.2d.* (Light2D)

M5:
  com.unity.services.levelplay (또는 Unity Ads Mediation)
  com.unity.purchasing         5.4.2
  com.unity.services.analytics
  com.unity.services.core
```

---

## 12. 프로젝트 구조 (Unity `Assets/_Project/`)

```
Assets/_Project/
  NeonHorde.asmdef                       # 런타임 (M1에서 Core/Domain/Game 분리 예정)
  Scenes/            Boot.unity(M1)  MainMenu.unity(M3)  Game.unity ✅
  Settings/          NeonHorde_URP.asset ✅  NeonHorde_Renderer.asset ✅  NeonHorde_PostFX.asset ✅
  Scripts/
    Core/            SeededRng ✅  ObjectPool ✅  SpatialHash ✅  ServiceLocator ✅
                     RuntimeShapes ✅  Palette ✅  FixedStepClock(M1)  NavGrid(M2)
    Domain/
      Balance ✅  PlayerStats ✅  MetaState ✅
      Catalog/       WeaponDefSO  PassiveDefSO  EnemyDefSO  CharacterDefSO
                     MapThemeDef  MapModifierDef  QuestDefSO  IapProductDef  GameDatabaseSO   (M2~M3)
      Models/        RunState  WeaponInstance  UpgradeOption  MapPlan  QuestProgress          (M1~M3)
    Game/
      GameBootstrap ✅  RunManager ✅  PlayerController ✅  PlayerInputReader ✅
      CameraFollow ✅  NeonShape ✅  NeonGridBackground ✅
      Enemies/       EnemyData  EnemyManager  EnemyAiSystem  EnemyRenderer                    (M1)
      Projectiles/   ProjectileData  ProjectileManager  ProjectileRenderer                   (M1)
      Pickups/       GemData  PickupManager  PickupRenderer                                   (M1)
      Weapons/       WeaponSystem  Behaviors/{Linear,Seek,Orbit,Aura,Arc,Chain,Lob,Boomerang}(M1~M2)
      Director/      DirectorSystem  WaveTables                                               (M1)
      Map/           MapGenerator  FlowFieldSystem  TerrainRenderer  HazardZone               (M2)
      Combat/        DamageResolver  DamageCommand                                            (M1)
      Fx/            FxSystem  DamageNumberPool  ScreenShake  HitFlash                        (M3)
      Events.cs      LevelUpEvent  RunEndedEvent  BossSpawnEvent ...
    State/
      MetaController  RunController  RunHudModel  QuestService                                (M1~M3)
    Services/
      ISaveService ✅  JsonSaveService ✅
      IAudioService/AudioService(M3)  IAdsService/LevelPlayAdsService(M5)
      IIapService/UnityIapService(M5)  IAnalyticsService/UgsAnalyticsService(M5)  IConsentService(M5)
    Debug/           DebugOverlay  Cheats                                                     (M1)
  Editor/
    NeonHorde.Editor.asmdef ✅  ProjectBootstrapper ✅  (+ SO 생성 툴 M2)
  Tests/EditMode/
    NeonHorde.Tests.EditMode.asmdef ✅  SeededRngTests ✅  SpatialHashTests ✅
    (+ XpCurveTests, PlayerStatsTests, UpgradeGeneratorTests, MapGeneratorTests, FlowFieldTests)
  Data/              Weapons/ Passives/ Enemies/ Characters/ MapThemes/ Modifiers/ Quests/ Iap/
                     BalanceConfig.asset  GameDatabase.asset
  Art/               Materials/(instanced neon)  Meshes/(quad)  VFX/
  Audio/             BGM/  SFX/
```
✅ = M0 완료.

---

## 13. 데이터 흐름 예시 — "볼트가 적을 처치"

```
WeaponSystem: bolt.cooldownTimer<=0
  → SpatialHash.Query(player.pos, range) → 최근접 target
  → projectilePool.Get() 초기화(pos, dir=to(target), dmg=level.damage*might, pierce)
  → bolt.cooldownTimer = level.cooldown * stats.cooldownMult
ProjectileSystem: p.pos += p.dir*p.speed*dt
  → navGrid.flags[cell]==Blocked && !p.piercesTerrain → release
  → SpatialHash.Query(p.pos, p.radius) → hitEnemy
  → DamageCommand(hitEnemy, p.dmg, crit?) ; p.pierce-- ; pierce<0 → release
DamageResolver: enemy.hp -= dmg ; FxSystem.SpawnDamageText(...)
  → enemy.hp<=0: gemPool.Get(enemy.pos, xpValue)
                 rng.Chance(goldChance) → pickupPool.Get(gold)
                 rng.Chance(eliteChestChance) → chest
                 enemyPool.Release(enemy) ; run.kills++
PickupSystem: gem within pickupRadius → 가속 → 접촉 → run.xp += xpValue*xpMult
  → run.xp >= run.xpToNext → pendingEvents.Enqueue(LevelUpEvent)
RunManager: LevelUpEvent → Paused=true → RunController 3택 생성 → LevelUp 오버레이
```

---

## 14. 테스트 / 개발 지원

- **인앱 디버그 오버레이** (`DEVELOPMENT_BUILD` / 에디터): FPS/엔티티 수, 스폰 강제, 경험치 지급, 무적, 시간 스크럽, 무기 최대레벨, 시드 고정, 맵 테마 강제.
- EditMode 단위테스트: `SeededRng` 재현성 ✅, `SpatialHash` ✅, (예정) XP 커브 / `PlayerStats` 파생 / 레벨업 옵션 생성기 / `MapGenerator` 연결성·시드 재현 / 플로우 필드 도달성.
- 성능 벤치 씬: N마리 스폰 후 `ProfilerRecorder`로 프레임타임/GC 로깅.

---

## 15. 다음 액션 — M1 (코어 전투)

1. `EnemyData` 풀 + `EnemyManager` (per-enemy Update 없이 매니저 단일 루프), 인스턴스드 `EnemyRenderer`
2. `SpatialHash` 연동 추적 AI + 분리 스티어링 (플로우 필드는 M2, 우선 직접 추적)
3. `DirectorSystem` 시간기반 스폰 (아레나 없이 무한 링 스폰으로 시작)
4. 무기 1종(Bolt) → `ProjectileManager` → 충돌 → `DamageResolver` → 적 사망
5. XP 젬 풀 + `PickupManager` 자석 → 경험치바 → `LevelUpEvent` → 3택 uGUI 오버레이
6. 플레이어 HP + 접촉 데미지 + 무적 프레임 + 게임오버 → 결과 화면 스텁
7. 플로팅 조이스틱 위젯 (uGUI), 성능 벤치(적 300 @60fps 확인)
8. `RunController`(런 시작/종료 배선), `RunHudModel` throttle 동기화

