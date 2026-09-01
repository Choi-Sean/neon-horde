# NEON HORDE — 무기 AI 이미지 프롬프트 (16종)

held-weapon 스프라이트용. 졸라맨이 손에 들고 적을 향해 회전시키므로 **오른쪽(→)을
가리키는 측면 뷰**로 뽑는다. 색은 무기별로 지정 (게임이 원본 색 그대로 씀 — 실제 아트가
있으면 코드에서 틴트 제거).

---

## 0. 공통 규칙 (안 지키면 재작업)

1. **배경 투명 PNG**, 정사각 1024×1024 (또는 512)
2. **무기 1개**, 수평으로 눕혀서 **총구/날 끝이 오른쪽**, 손잡이는 왼쪽, 세로 중앙
3. 프레임 가로의 **~75%** 채우기. 진화형은 더 크고 화려해도 되지만 여백 두고 다 들어오게
4. **손·팔·캐릭터·배경·바닥 그림자 없음**
5. **16개 전부 같은 스타일·같은 조명** — 첫 결과 1장을 스타일 기준으로 잡고 이후 전부
   같은 레퍼런스(`--sref` / 같은 seed·모델) 로 고정. "한 무기고에서 나온 세트"처럼
6. 텍스트·워터마크·테두리 프레임 없음

## 공통 스타일 프리픽스 (매 프롬프트 앞에 붙이기)

```
dark sci-fi neon game weapon, side view, held horizontally pointing RIGHT,
grip end on the left, bold clean silhouette, strong rim light, subtle inner glow,
metallic body with emissive energy accents, isolated on transparent background,
centered, no hand, no arm, no character, no background, no shadow, no text --ar 1:1
```

---

## 1. 기본 8종

| 파일명 | 프롬프트 (프리픽스 뒤에 붙임) |
|---|---|
| `wpn_bolt.png` | compact neon blaster pistol, single short barrel, glowing **amber** energy cell, minimal sleek frame |
| `wpn_aura.png` | short metallic emitter wand, a pulsing **cyan** plasma orb hovering at the tip, faint concentric energy rings around the orb |
| `wpn_orbit.png` | twin-pronged fork device, two chrome prongs, three small **teal** orbs hovering between and around the prongs |
| `wpn_whip.png` | energy whip, ribbed dark metal handle on the left, a long segmented **magenta** plasma lash uncoiling to the right with a soft motion trail |
| `wpn_seeker.png` | compact single-tube missile launcher, one bore, small targeting fin, an **orange** warhead glowing inside the tube |
| `wpn_lob.png` | stubby grenade launcher, fat short barrel angled slightly up, a glowing **red-orange** incendiary shell visible in the breech |
| `wpn_chain.png` | arc-rod wand, two electrode prongs at the tip, crackling **electric-blue** lightning arcing between the prongs |
| `wpn_boomerang.png` | curved throwing blade, single crescent shape, sharpened glowing **green** edge, wrapped grip at the center |

## 2. 진화 8종 (더 크고 정교하게, 같은 색 계열)

| 파일명 | 프롬프트 |
|---|---|
| `wpn_railgun.png` | heavy railgun rifle, long twin parallel rails, three glowing **amber** accelerator coils spaced along the barrel, chunky stock and foregrip |
| `wpn_nova.png` | ornate emitter staff, crowned with a large radiant **white-cyan** energy star with sharp spikes, a soft halo of light around it |
| `wpn_halo.png` | rod topped with a large glowing **teal** ring, a gyroscopic outer band around the ring, small orbs circling it |
| `wpn_reaper.png` | energy scythe, long dark shaft, a huge sweeping **magenta** plasma blade curving to the right, thin wisps of dark smoke off the blade |
| `wpn_swarm.png` | triple-barrel missile pod, three stacked bores, bristling with small **orange** homing missiles, a targeting sensor array on top |
| `wpn_meteor.png` | massive shoulder cannon, wide flared muzzle, a molten glowing core with cracks leaking **lava-orange** light down the body |
| `wpn_tesla.png` | tesla-coil weapon, a stacked copper coil column, a chrome sphere on top, a web of **electric-white** lightning arcs radiating outward |
| `wpn_cyclone.png` | double-bladed glaive, two **green** crescent energy blades on opposite ends of a central wrapped grip, circular spin motion blur |

---

## 3. 전달

- `D:\Project\game\Assets\_Project\Art\weapons\` 에 파일명 그대로 저장 (폴더 없으면 만들어).
- 같이 알려줄 것: 어떤 모델로 뽑았는지 + 스타일 레퍼런스 1장.
- 8개 기본만 먼저 줘도 붙여서 확인 가능. 진화형은 이어서.

## 4. 받은 뒤 내가 하는 것

- `WeaponArt` 절차적 → 실제 스프라이트로 교체 (`SpriteBank`/폴더 로더). 없는 무기는 절차적 폴백.
- 손잡이 위치 기준으로 pivot 재설정 (지금 0.22,0.5 → 실제 아트 grip 지점).
- 실제 아트는 원본 색 유지 (곱셈 틴트 제거), 발광만 약간.
- 같은 이미지를 레벨업 카드 아이콘으로도 축소 사용 (별도 아이콘 세트 불필요; 원하면 액자형 버전 따로).
- 레벨업 시 살짝 커지기 / 진화 시 스프라이트 교체는 그대로 동작.
