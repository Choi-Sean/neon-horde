# NEON HORDE — 캐릭터 & 몬스터 AI 이미지 프롬프트

탑다운 서바이버. 캐릭터/몬스터 모두 **위에서 살짝 내려다본 3/4 뷰, 화면(플레이어) 쪽을
바라봄, 전신**. 전부 같은 스타일로 뽑아야 한 게임처럼 보임. 색은 각자 고유색 유지
(게임이 원본 색 그대로 씀 — 실제 아트면 틴트 안 함).

---

## 0. 공통 규칙

1. **배경 투명 PNG**, 정사각 1024×1024 (또는 512)
2. **피사체 1개**, 중앙, 프레임의 ~80% 채우기
3. **탑다운 3/4 뷰**, 아래/카메라 쪽을 향함. 측면 뷰·정면 초상화 ❌
4. **바닥·그림자·배경·텍스트 없음**
5. **캐릭터 4 + 몬스터 10 전부 같은 스타일·같은 조명** — 첫 결과 1장을 레퍼런스로
   잡고 이후 전부 같은 `--sref` / seed·모델
6. 상대 크기: 플레이어 = 기준. Swarmer·Splitterling 작게, Tank·FrostTank 크게,
   Boss 훨씬 크게 (캔버스는 같아도 프레임 꽉 채우면 됨)
7. 애니: 우선 **히어로 포즈 1장**씩. 나중에 걷기 4~6프레임 (캐릭터는 그대로, 팔다리만)

## 공통 스타일 프리픽스 (매 프롬프트 앞에)

```
dark sci-fi neon, top-down 3/4 view, facing the camera / downward, full body,
bold readable silhouette, strong rim light, glowing emissive accents,
isolated on transparent background, centered, no ground, no shadow, no text --ar 1:1
```

---

## 1. 캐릭터 4종

| 파일명 | 프롬프트 (프리픽스 뒤) |
|---|---|
| `char_pulse.png` | agile scout runner, lightweight bodysuit with thin armor plates, sleek visor helmet, **cyan** energy lines, athletic lean build, confident ready stance, faint shockwave ring at the feet |
| `char_volt.png` | glass-cannon striker, very lean and tall, exposed **electric-blue** power conduits over a thin frame, sparks arcing off the shoulders, crackling gauntlets, aggressive forward stance |
| `char_aegis.png` | heavy guardian, bulky layered plate armor, broad shoulders, a large hexagonal energy **deep-blue** shield on one forearm, planted immovable stance, slow and solid |
| `char_halo.png` | support caster, sleek hooded bodysuit, an ethereal **magenta** ring / halo floating behind the head, two small orbs orbiting the hands, weightless graceful pose, soft glow |

게임 내 시작 무기: Pulse=블라스터, Volt=번개봉, Aegis=오라 방출기, Halo=포크 궤도구
(무기는 별 프롬프트 `WEAPON_ART_PROMPTS.md` 참고 — 캐릭터 이미지엔 무기 없어도 됨).

---

## 2. 몬스터 10종

| 파일명 | 프롬프트 |
|---|---|
| `enemy_walker.png` | basic grunt monster, hunched bipedal shambler, cracked chitin skin, two dim **magenta** eyes, slow lumbering pose, arms hanging |
| `enemy_swarmer.png` | small fast swarm creature, low four-legged body close to the ground, snapping mandibles at the front, glowing **yellow** underbelly, mid-scuttle pose |
| `enemy_tank.png` | large heavy bruiser, thick armored quadruped, overlapping **blue** carapace plates, two blunt tusks, ridged back, heavy planted stance |
| `enemy_spitter.png` | ranged acid creature, hunched body with a wide dripping maw pointed forward, three curved spines on the back, glowing **green** throat sac, tense coiled pose |
| `enemy_exploder.png` | bloated kamikaze creature, near-spherical swollen body, glowing **orange-red** cracks leaking light, tiny stubby legs, unstable wobble, faint smoke |
| `enemy_splitter.png` | crystalline slime cluster, semi-transparent **purple** gel body with embedded shards, splitting seams across the surface, three small eyes clustered on top |
| `enemy_splitterling.png` | tiny fragment creature, small **light-purple** gel blob with one eye and four thin legs, quick nervous pose (miniature of enemy_splitter) |
| `enemy_phantom.png` | ghostly wraith, no legs, upper body solid fading to a tapered wispy **cyan** tail, two narrow glowing eyes, drifting forward, translucent trailing smoke |
| `enemy_frosttank.png` | icy bruiser, thick quadruped like the tank but encased in frost, jagged **ice-blue** icicles jutting from the back and shoulders, frozen breath, heavy stance |
| `boss.png` | towering horde overlord, massive multi-layered armored torso, several heavy limbs and two back tentacles, a bright pulsing **magenta-white** core in the chest, menacing looming pose, radiating aura |

**엘리트**는 별도 이미지 불필요 — 일반 몬스터 스프라이트 + 엔진이 발광 테두리/왕관/크기
증가로 처리.

---

## 3. 전달

- `D:\Project\game\Assets\_Project\Art\characters\` 와 `...\Art\monsters\` 에
  파일명 그대로 저장 (폴더 없으면 만들어).
- 같이: 모델 이름 + 스타일 레퍼런스 1장.
- 캐릭터 4 + 몬스터 기본 5종(walker/swarmer/tank/spitter/exploder) 먼저 줘도 붙여봄.

## 4. 받은 뒤 내가 하는 것

- `NeonShape`(졸라맨) → 캐릭터 스프라이트 기반으로 교체 (없으면 졸라맨 폴백).
  이동 방향 좌우 플립 + 절차적 상하 까딱, 나중에 프레임 애니.
- `CreatureArt`(절차적 크리처) → 몬스터 스프라이트로 교체 (없는 종은 절차적 폴백).
  이동 시 상하 바운스 + 방향 플립 + 피격 플래시.
- 실제 아트는 원본 색 유지 (곱셈 틴트 제거), 뒤에 옅은 글로우만.
- 상대 크기 자동 스케일 (EnemyCatalog radius 기준), pivot 재설정.
- 보스는 전용 큰 스케일 + 등장 연출.
