# NEON HORDE — AI 아트 에셋 제작 브리프

AI 이미지(미드저니 / Stable Diffusion / DALL·E 등)로 캐릭터·적·배경을 뽑아서
주면 엔진에 연결한다. 이 문서대로만 뽑으면 바로 들어간다.

---

## 0. 절대 규칙 (안 지키면 재작업)

| # | 규칙 | 이유 |
|---|---|---|
| 1 | **배경 투명 PNG** (엔티티 전부) | 게임이 위에서 겹쳐 그림. 배경 있으면 네모로 보임 |
| 2 | **정사각 캔버스**, 피사체 1개, **중앙 정렬**, 여백 ~10% | 스케일·회전 기준이 됨 |
| 3 | **모든 에셋 동일 스타일** — 같은 스타일 문구 + 같은 레퍼런스 이미지(`--sref`) 사용 | 안 맞으면 짜깁기처럼 보임 |
| 4 | **3/4 탑다운 뷰** (카메라가 위에서 30~45° 내려다봄), 피사체는 화면 아래쪽=플레이어 쪽을 향함. **모든 에셋 같은 각도** | 탑다운 게임. 측면 뷰(플랫포머)·정면 초상화 ❌ |
| 5 | 스프라이트에 **그림자 굽지 마라** (엔진이 블롭 그림자 깔음). 바닥면도 그리지 마라 | 겹치면 이상함 |
| 6 | 작게 렌더됨(폰에서 100~200px). **굵은 실루엣 + 강한 림라이트**, 잔디테일 최소 | 가독성 |
| 7 | 텍스트·워터마크·테두리 프레임 ❌ | |

**팔레트:** 어두운 SF, 네온 림라이트(시안/마젠타/옐로). 엔진에 이미 블룸+비네트+색수차가
걸려 있어서 이 톤이면 자동으로 어우러진다.

---

## 1. 캔버스 크기

| 종류 | 크기 | 비고 |
|---|---|---|
| 플레이어 / 적 / 보스 | 512×512 또는 1024×1024 | 투명 PNG |
| 투사체 / 획득물 | 256×256 | 투명 PNG |
| 바닥 타일 | 1024×1024 | **seamless(이음새 없는) 텍스처**, 불투명 |
| 장애물 | 512×512 | 투명 PNG |

import 세팅(pixels-per-unit, 필터, 압축, pivot)은 내가 잡는다. 픽셀만 맞으면 됨.

---

## 2. 파일 목록 & 디자인 브리프

파일명 그대로 저장해서 줘. **★ = 최소 세트**(이것만 먼저 줘도 "게임처럼" 보임).

### 플레이어 (4)
| 파일명 | 컨셉 | 색 |
|---|---|---|
| `player_pulse.png` ★ | 민첩한 정찰병. 가벼운 장갑, 날렵함. 기본 캐릭터 | 시안 |
| `player_volt.png` | 유리대포 딜러. 전기/스파크 모티프, 얇음 | 밝은 청백 |
| `player_aegis.png` | 탱커 가디언. 육중한 장갑 + 팔에 방패/배리어 | 딥블루 |
| `player_halo.png` | 서포터. 매끈함, 뒤에 링/후광 오브 | 마젠타/화이트 |

> 4명 다 **같은 실루엣 규격**(전신, 같은 포즈·각도). 무기는 안 그려도 됨(엔진이 별도).
> 러닝/중립 포즈 1장이면 충분 — 게임이 알아서 흔들고 기울인다.

### 적 (9)
| 파일명 | 컨셉 | 상대 크기 | 색 |
|---|---|---|---|
| `enemy_walker.png` ★ | 기본 근접 잡졸. 느릿한 2족 | 中 | 마젠타 |
| `enemy_swarmer.png` ★ | 작고 빠름, 떼로 몰림. 4족으로 기어옴 | 小 | 옐로 |
| `enemy_tank.png` ★ | 크고 느리고 단단. 묵직한 덩치 | 大 | 블루 |
| `enemy_spitter.png` ★ | 원거리. 웅크린 몸통 + 뱉는 주둥이/가시 | 中 | 그린 |
| `enemy_exploder.png` | 돌진 후 자폭. 부풀어오른 몸통, 균열에서 빛샘 | 中 | 오렌지레드 |
| `enemy_splitter.png` | 죽으면 쪼개짐. 뭉친 슬라임/결정 덩어리 | 中大 | 퍼플 |
| `enemy_splitterling.png` | 위 조각. 미니 버전 | 小 | 라이트퍼플 |
| `enemy_phantom.png` | 빠름, 반투명 유령. 흐릿한 하반신 | 小中 | 시안(반투명) |
| `enemy_frosttank.png` | 얼음 탱커. Tank의 서리 버전, 고드름 | 大 | 아이스블루 |

> 전부 **정면~3/4로 이쪽(플레이어)을 노려보는 위협적 포즈**. 걷기 프레임 안 만들어도 됨.
> 엘리트는 따로 안 만든다 — 일반 적 + 발광 이펙트로 엔진이 처리. 보스만 아래 1장.

### 보스 (1)
| 파일명 | 컨셉 |
|---|---|
| `boss.png` | 위 적들의 우두머리 급. 화면을 압도하는 거대 실루엣, 다층 장갑/촉수/코어. 한 마리 "히어로 샷" |

### 투사체 / 획득물
| 파일명 | 컨셉 |
|---|---|
| `proj_bullet.png` ★ | 플레이어 탄. 가로로 긴 에너지 예광탄/총알. 옐로화이트 |
| `proj_spit.png` | 적 탄. 산성 방울/가시. 그린 or 레드 |
| `pickup_gem.png` ★ | 경험치 결정. 다이아 형태, 그린 발광 |
| `pickup_gold.png` | 골드 코인/조각. 금색 발광 |
| `pickup_chest.png` | 보물상자. 작고 아이코닉, 마젠타 발광 |

### 바닥 타일 (4) — **seamless 필수**
| 파일명 | 컨셉 |
|---|---|
| `ground_grid.png` ★ | SF 격납고 철판 바닥, 패널 이음새 + 미세 발광 라인 |
| `ground_void.png` | 어두운 외계 암석 지대, 균열 사이 보라 발광 |
| `ground_furnace.png` | 갈라진 용암암반 + 달궈진 금속, 균열 주황 발광 |
| `ground_cryo.png` | 얼음/눈밭, 서리 낀 금속 격자 |

> 프롬프트에 `seamless tileable texture, top-down orthographic, flat lighting` 넣기.
> 완벽히 안 물려도 됨 — 내가 엔진에서 미러/블렌드로 이어붙인다.

### 장애물 (4)
| 파일명 | 컨셉 |
|---|---|
| `obstacle_blocker.png` ★ | 못 부수는 벽/바위 덩어리. 탑다운 | 
| `obstacle_crate.png` | 부술 수 있는 상자/배럴 |
| `zone_hazard.png` | 데미지 장판 — 용암 웅덩이 / 증기 분출구 (반투명 가장자리) |
| `zone_slow.png` | 감속 장판 — 타르/진흙/에너지 늪 (반투명 가장자리) |

---

## 3. 프롬프트 템플릿

**공통 스타일 프리픽스(매번 앞에 붙이기):**

```
dark sci-fi neon, top-down 3/4 view, bold readable silhouette, strong rim light,
cyan and magenta accents, isolated on transparent background, centered,
game asset sprite, no shadow, no ground, no text
```

미드저니면 첫 결과 1장을 스타일 기준으로 정하고 이후 전부 `--sref <그 이미지 URL>` 붙여
톤을 고정. SD면 같은 모델·LoRA·seed·샘플러 유지.

**플레이어 예시:**
```
<스타일 프리픽스>, agile scout soldier, light armor, lean athletic build,
neutral running pose, facing camera-down, full body --ar 1:1
```

**적 예시(스포너):**
```
<스타일 프리픽스>, hunched ranged creature, bulbous body, spitting maw with
spines, menacing forward stance, green bioluminescence --ar 1:1
```

**바닥 예시:**
```
seamless tileable texture, top-down orthographic, flat even lighting,
cracked lava rock and scorched metal panels, thin orange glow in the cracks,
no objects, no characters --ar 1:1 --tile
```

---

## 4. 전달 방법

1. PNG를 아래 폴더에 넣어줘 (없으면 만들어):
   `D:\Project\game\Assets\_Project\Art\incoming\`
   또는 zip으로 줘도 됨.
2. 파일명은 위 표 그대로.
3. 같이 알려줄 것: 어떤 모델로 뽑았는지 / 업스케일 했는지 / 스타일 레퍼런스 1장.
4. 최소 세트(★)만 먼저 줘도 바로 붙여서 "이런 느낌" 확인 가능. 나머지는 이어서.

## 5. 내가 하는 것 (에셋 받은 뒤)

- 스프라이트 로더(`SpriteBank`): `Resources/art/<key>` 있으면 실제 그림, 없으면 기존 도형으로 폴백
- 각 매니저의 `NeonArt.Sprite(...)` → `SpriteBank.Get("enemy_walker")` 로 교체
- 엔티티 밑에 블롭 그림자, 이동 방향으로 좌우 플립, 피격 플래시(흰색 틱)
- `NeonGridBackground` 텍스처 → 테마별 `ground_*` 타일로 교체
- import 세팅 일괄(PPU/pivot/압축), 아틀라스 패킹
- HDR 틴트 제거(실제 그림은 색을 곱하면 떡짐), 살짝만 발광 유지
