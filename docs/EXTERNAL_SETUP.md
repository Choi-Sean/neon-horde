# NEON HORDE — 외부 연동에 필요한 것 (네가 준비해야 하는 것)

코드는 **인터페이스 + 스텁**으로 다 만들어져 있어. 아래 계정/키/설정만 채우면 실물로 붙는다.
각 항목: **① 네가 할 일 → ② 나한테 줄 것 → ③ 내가 붙일 위치**

---

## 1. Apple / iOS 배포  ⚠️ Mac + Xcode 필수

빌드 자체가 Mac에서만 된다 (Unity가 Xcode 프로젝트를 뽑고 Xcode가 서명·업로드). 이건 내가 대신 못 함.

### ① 네가 할 일
1. **Apple Developer Program 가입** — 연 $99 (developer.apple.com). 개인/법인 선택.
2. **App Store Connect**에서 앱 레코드 생성:
   - Bundle ID: `com.bitbyte.neonhorde` (원하면 변경 — 정하면 알려줘, `ProjectSettings`에 반영)
   - 앱 이름, SKU, 언어(한국어 기본 + 영어)
3. **인증서/프로비저닝**: Xcode에 Apple ID 로그인 → "Automatically manage signing" 켜면 Xcode가 개발/배포 인증서 자동 생성. (수동으로 하려면 Certificates, Identifiers & Profiles에서 Distribution 인증서 + App Store 프로비저닝 프로파일)
4. **ATT(App Tracking Transparency) 안내 문구** 작성 — 예: "맞춤 광고와 게임 개선을 위해 사용됩니다"
5. Mac에서: `Unity에서 iOS로 Build` → 생성된 `Unity-iPhone.xcodeproj`를 Xcode로 열기 → Team 선택 → Archive → Distribute App → App Store Connect 업로드
6. **TestFlight**에 올려서 내부 테스트 → 심사 제출

### ② 나한테 줄 것
- 확정 Bundle ID
- (선택) 앱 표시 이름 최종본
- ATT 문구 한/영

### ③ 내가 붙일 위치
- `ProjectSettings/ProjectSettings.asset` (Bundle ID, ATT usage string — `NSUserTrackingUsageDescription`)
- `ConfigureBuild()` 에디터 메뉴가 iOS IL2CPP/min 13.0 이미 세팅함

---

## 2. 광고 — Unity LevelPlay (권장) 또는 AdMob

리워드 광고만 쓸 거야 (부활 / 골드2배 / 리롤). 인터스티셜·배너 없음.
현재 `StubAdsService`가 "즉시 보상"으로 동작 중 → 실물 SDK로 교체.

### 옵션 A: Unity LevelPlay (ironSource) — 하이퍼캐주얼 표준, Unity와 통합 쉬움
### ① 네가 할 일
1. **Unity Dashboard**(cloud.unity.com) → 프로젝트에 **LevelPlay/Unity Ads** 활성화
2. LevelPlay 콘솔에서 앱 등록 (iOS/Android 각각) → **App Key** 발급
3. **광고 단위(Ad Unit) 생성**: Rewarded 1개 (또는 배치별로 여러 개)
4. (수익 극대화 하려면) AdMob/Meta/AppLovin 등 **미디에이션 네트워크** 각각 가입 + LevelPlay에 연동 — 나중에 해도 됨

### 옵션 B: Google AdMob 단독
### ① 네가 할 일
1. **AdMob 계정** 가입 (apps.admob.com, Google 계정 필요) + AdSense/결제 정보
2. 앱 등록 (iOS/Android) → **AdMob App ID** (`ca-app-pub-xxxxxxxx~yyyyyyyy`)
3. **Rewarded 광고 단위 ID** 생성 (`ca-app-pub-xxxxxxxx/zzzzzzzz`)

### ② 나한테 줄 것 (택1)
- LevelPlay: iOS App Key, Android App Key, Rewarded Ad Unit ID(들)
- 또는 AdMob: iOS App ID + Rewarded Unit ID, Android App ID + Rewarded Unit ID

### ③ 내가 붙일 위치
- 패키지 추가: `com.unity.services.levelplay` (또는 `com.google.ads.mobile`)
- 새 파일 `LevelPlayAdsService.cs`(또는 `AdMobAdsService.cs`) — `IAdsService` 구현
- `GameBootstrap.Init()` 에서 `new StubAdsService()` → 실물로 교체 (한 줄)
- App ID는 iOS `Info.plist`(`GADApplicationIdentifier`) / Android `AndroidManifest`에 — 에디터 스크립트로 자동 주입

---

## 3. 개인정보 동의 (GDPR/UMP + iOS ATT)

### ① 네가 할 일
- LevelPlay/AdMob 콘솔에서 **UMP(User Messaging Platform) 동의 폼** 생성 (privacy & messaging → GDPR/CCPA 메시지)
- **개인정보처리방침 URL** 필요 (아래 4번 백엔드 없이도 정적 페이지로 만들면 됨 — Notion/GitHub Pages 무료)

### ② 나한테 줄 것
- 개인정보처리방침 URL
- (동의 폼은 콘솔에서 만들면 SDK가 자동으로 불러옴)

### ③ 내가 붙일 위치
- `StubConsentService` → `UmpConsentService.cs` (UMP SDK 호출 + iOS `ATTrackingManager.requestTrackingAuthorization`)
- `ProjectSettings` iOS `NSUserTrackingUsageDescription`

---

## 4. 백엔드 / DB — 계정 + 클라우드 세이브 + (선택)리더보드

**"가입 안 하면 데이터 날아간다"** 유도는 이미 UI에 들어가 있어(게스트 배너 + 3판 후 넛지 모달 + 설정 내 연결).
지금은 `GuestAccountService`가 로컬에만 저장 → 실제 연결하려면 백엔드가 필요.

### 권장: **Unity Gaming Services (서버 코드 0)**
- **Authentication**: 익명 로그인 자동 → 나중에 Google/Apple/이메일로 **링크**
- **Cloud Save**: 세이브 JSON을 그대로 키-값으로 업로드/다운로드 (우리 `MetaState`가 이미 JSON 직렬화됨 → 그대로 올림)
- **Leaderboards**: 일일 챌린지 점수 (선택)
- **Economy / Remote Config**: 밸런스·상품을 서버에서 조정 (선택, 나중)

### ① 네가 할 일
1. cloud.unity.com → 이 프로젝트 링크 (Unity 계정으로 로그인, 이미 org 있음 — `2475666421478`)
2. **Authentication** 활성화 → **Google/Apple/이메일 identity provider** 설정
   - Google: Google Cloud Console에서 OAuth 클라이언트 ID 발급 → UGS에 등록
   - Apple: Apple Developer에서 "Sign in with Apple" 서비스 ID + 키 → UGS에 등록
3. **Cloud Save** 활성화 (설정 거의 없음)
4. (선택) **Leaderboards** 활성화 → 리더보드 ID 생성

### ② 나한테 줄 것
- "UGS 프로젝트 링크 완료" 확인 + **Project ID / Environment 이름**
- Google OAuth 클라이언트 ID (web + iOS + android)
- Apple Sign-in 서비스 ID
- (선택) Leaderboard ID

### ③ 내가 붙일 위치
- 패키지: `com.unity.services.core`, `com.unity.services.authentication`, `com.unity.services.cloudsave`, (선택)`com.unity.services.leaderboards`
- `GuestAccountService` → `UgsAccountService.cs` (`IAccountService` 구현: 익명→링크, 로그인 상태)
- `JsonSaveService` 옆에 `CloudSyncService` — 링크된 계정이면 로드 시 클라우드 우선 + 저장 시 푸시(디바운스)
- 앱 시작 시 익명 로그인 → 게스트도 사실은 서버에 임시 저장됨(기기 교체 시엔 날아감 = 유도 메시지 유효)

### 대안: Firebase (Auth + Firestore/RTDB)
- google-services.json (Android) + GoogleService-Info.plist (iOS) 파일을 주면 됨
- 패키지: Firebase Unity SDK. UGS보다 쿼리·규칙이 유연하지만 규칙 작성 필요.

---

## 5. 인앱 결제 (IAP)

캐릭터 개별 구매(`char.volt/aegis/halo`), 코어 번들(`cores.*`), 광고 제거, 스타터팩. 카탈로그는 `IapCatalog.cs`에 있음.

### ① 네가 할 일
1. **Unity IAP** 패키지 활성화 (Unity Dashboard)
2. **App Store Connect**: In-App Purchase 상품 등록 — Product ID를 코드와 동일하게:
   `char.volt`, `char.aegis`, `char.halo`, `cores.small`, `cores.medium`, `cores.large`, `remove_ads`, `starter_pack`
   - 유형: 캐릭터/광고제거/스타터팩 = **비소모성(Non-Consumable)**, 코어 번들 = **소모성(Consumable)**
   - 가격 티어 설정 (현재 코드의 `priceText`는 표시용 더미 → 실제 가격은 스토어가 소스)
3. **Google Play Console**: 같은 Product ID로 인앱 상품 등록
4. 세금·은행 정보 (양쪽 콘솔)

### ② 나한테 줄 것
- "양쪽 스토어에 8개 상품 등록 완료" + 실제 가격
- (가격을 코드 표시용으로도 쓸 거면 최종 가격표)

### ③ 내가 붙일 위치
- 패키지: `com.unity.purchasing` (이미 네 다른 프로젝트에 5.4.2 캐시됨)
- `StubIapService` → `UnityIapService.cs` (`IIapService` 구현). `IapFulfillment.Grant()`는 그대로 재사용
- Google Play: 서명된 AAB 업로드 후에야 상품 테스트 가능

---

## 6. 애널리틱스

현재 `DebugAnalyticsService`가 콘솔에 이벤트 출력. 이벤트 지점은 이미 코드에 박아둠
(`app_open`, `run_start`, `run_end`, `level_up`, `ad_impression`, `ad_revive`, `iap_purchase`, `account_linked`, `quest_claimed`).

### ① 네가 할 일
- UGS **Analytics** 활성화 (4번에서 프로젝트 링크했으면 토글만)
- 또는 Firebase Analytics (google-services 파일)

### ② 나한테 줄 것
- "UGS Analytics 활성화됨" 한 마디

### ③ 내가 붙일 위치
- 패키지: `com.unity.services.analytics`
- `DebugAnalyticsService` → `UgsAnalyticsService.cs` (동의 상태 연동)

---

## 7. Android 배포 (Mac 불필요, Windows에서 가능)

### ① 네가 할 일
1. **Google Play Console** 등록 — 1회 $25
2. 앱 생성, 패키지명 `com.bitbyte.neonhorde`
3. **업로드 키(keystore)** 생성 — `keytool` 또는 Unity의 Player Settings > Publishing Settings에서 생성.
   ⚠️ 이 .keystore 파일과 비밀번호는 **분실하면 앱 업데이트 불가**. 안전하게 보관.
4. Play App Signing 활성화 (권장)
5. 콘텐츠 등급 설문, 개인정보처리방침 URL, 데이터 보안 양식 작성

### ② 나한테 줄 것
- keystore 파일 경로 + alias + 비밀번호 (또는 네가 직접 Player Settings에 넣어도 됨)
- 최종 패키지명

### ③ 내가 붙일 위치
- `ProjectSettings` Publishing Settings (keystore 경로/비번은 로컬에만, git 커밋 금지)
- `ConfigureBuild()`가 IL2CPP/ARM64/minSDK24 이미 세팅

---

## 8. 아트 에셋 (선택 — 지금은 기하학/네온으로 완결)

현재 아이콘·스플래시가 Unity 기본. 스토어 제출엔 최소한:
- **앱 아이콘** 1024×1024 (iOS) / 512×512 (Play)
- **피처 그래픽** 1024×500 (Play)
- **스크린샷** iPhone 6.7"·6.5"·5.5" 각 3~10장, Android 폰 2~8장

### ② 나한테 줄 것
- 아이콘 PNG (있으면). 없으면 네온 로고를 간단히 만들어 줄 수도 있음(코드 렌더/SVG)

---

## 우선순위 (소프트런치까지 최단 경로)

1. **Android 먼저** (Windows에서 빌드 가능, Mac 불필요) — Play Console $25 + keystore
2. UGS 링크 → Auth + Cloud Save + Analytics (서버 코드 0)
3. LevelPlay 리워드 광고 + UMP 동의
4. Unity IAP + Play 상품 8개
5. Play 내부 테스트 트랙에 올려서 지표 확인
6. 그 다음 iOS (Apple $99 + Mac + Xcode)

각 단계에서 위 "② 나한테 줄 것"만 주면 내가 코드/설정 붙이고 빌드 스크립트까지 만든다.
