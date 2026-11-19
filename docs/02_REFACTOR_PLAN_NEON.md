# 02_REFACTOR_PLAN_NEON.md — 정/역 혼합 배열 구현 검토 결과 및 보완 수정 계획

> **검토 대상 커밋**: `6d67b77` (`정/역(180°) 혼합 배열 마킹 지원 (MarkingPC)`)
> **기준 문서**: `docs/02_REFACTOR_PLAN.md`, `docs/03_PLAN_REVIEW.md`, `docs/images/ab_stripid_its.png`
> **목적**: `6d67b77` 구현 결과를 설계 문서 및 실기 운용 조건과 대조 검토하여 발견된 결함·미반영 사항·현장 확인 결과를 정리하고, 실기 투입 전 후속 수정 범위를 확정한다.
> 파일 경로는 `HDS_ID_Marker/HDS_ID_Marker/` 기준이다.

---

## 1. 현장 확인 완료 사항 (확정 조건)

| 항목 | 확인 내용 및 결정 |
|---|---|
| 실기 카메라 Y축 ↔ 스테이지 Y축 | 화면 기준 **1열 = 위쪽, 4열 = 아래쪽**. 스테이지를 4열(`+Y`) 방향으로 이동시키면 화면상 피사체는 **위쪽(`-Y`)** 으로 이동한다. 즉, 스테이지 `+Y`와 카메라 픽셀 `+Y`는 **같은 물리 방향(1열→4열)** 이다. |
| `AABB` 정/역 얼라인 매칭 | `B`는 `A`를 180° 회전한 디자인(`docs/images/ab_stripid_its.png`). 역방향 열은 FOV 좌측 = Align2, FOV 우측 = Align1이 되므로, 1열과 4열은 **FOV 내 같은 좌/우 쪽 마크(`GetSameSideAlign`)** 로 짝짓는 현재 코드 방향이 타당하다. |
| DB 스키마 / ITS·MES 규칙 | DB `para_*` 테이블의 `orientation` 컬럼 길이(5자 이상: `top_r`/`bot_r`) 및 완성 strip 기준 ITS 기록 규칙 확인 완료. |
| 역방향 티칭 Y축 조그 잠금 | 당분간 현행 유지(Y 조그 허용 + 저장 시 `±0.05mm` 이탈 차단). |
| 티칭 창 단발 테스트 마킹 함수 | 현장 미사용 (`Top/Bot` `Custom/Inter/OCRMark`). 필수 수정 범위에서 제외. |
| 첫/마지막 미완성 strip 처리 | 첫/마지막 스트립은 티칭에 따라 양쪽 마킹이 모두 들어가지 않아 폐기 가능하며, 첫 스트립은 공정상 절단해 별도 사용. ITS에 남아 있더라도 후공정에서 미존재/폐기 strip은 정리한다. |
| 이어하기 번호 체계 | 완성 strip 기준 기록 의도. 작업자 교육과 병행해 시스템 중복 차단 및 안내 문구 보강(방안 A)을 적용한다. |

---

## 2. 필수 수정 항목 (실기 투입 전 반드시 반영)

### 2.1 얼라인 베이스라인 보정 부호 오류 수정
- **위치**: `ManagementClass/TeachingManagement.cs` — `GetRoiBaselineOffset` (L429~438)
- **원인**:
  - 스테이지 `+Y`와 카메라 픽셀 `+Y`가 같은 물리 방향(1열→4열)이므로, 1열(첫 열) 얼라인과 마지막 열 얼라인 사이의 실제 물리 Y 거리는 다음과 같다.
    $$\Delta Y_{\text{phys}} = \text{StripIter}[\text{last}] + (\text{roiCY}_{\text{last}} - \text{roiCY}_{0}) \cdot \text{canvasToCam} \cdot \text{TopCamResolution}$$
  - `AABB` 배열 기준 1열(정) 얼라인은 열 상단(`roiCY` 작음), 4열(역) 얼라인은 열 하단(`roiCY` 큼)에 위치해 실제 거리는 `StripIter[3]`보다 **더 길어야(`+`)** 한다.
  - 현재 코드는 기존 `alignDistanceY -= y12` 규칙을 맞추려다 `-(RoiCenterY(roiLast) - RoiCenterY(roiFirst))`로 **부호가 반대로 구현**되어 있다. (`AABB`에서 거리를 짧게, `BBAA`에서 길게 계산해 기울기 `radian` 오차 유발)
- **수정 내용**:
  - `GetRoiBaselineOffset`의 부호를 `-` → `+`로 변경하고 주석도 실기 좌표계 기준으로 수정한다. (`docs/02_REFACTOR_PLAN.md` §4 수식과 일치)

### 2.2 역방향 티칭 화면에서 기존 ROI 더블클릭 수정 시 `IsReverse` 누락
- **위치**: `MainHRControl.xaml.cs` — `BindActiveSet` (L71~83) 및 더블클릭 수정 분기 (L916, L969, L1022)
- **원인**:
  - `BindActiveSet`은 신규 추가용 창 인스턴스(`customMarkSettingWindow`, `interMarkSettingWindow`, `ocrParaSettingWindow`)에만 `IsReverse = m_reverse`를 전달한다.
  - 기존 ROI 더블클릭 시에는 `new MarkParaSettingWindow(...)` / `new OCRParaSettingWindow(...)`를 새로 생성하면서 `IsReverse = m_reverse`를 지정하지 않아, 창이 열리는 순간(`Window_Loaded`) 정방향 `MarkingLocation` 기준으로 `markPos`가 덮어씌워진다.
- **수정 내용**:
  - 더블클릭 수정 분기 3곳(`custom`, `inter`, `ocr`)에서 새 창 생성 직후 `IsReverse = m_reverse`를 설정한다.

### 2.3 티칭 취소(`Cancel`) 시 미저장 `MarkSet` 메모리 잔존 및 Align Bank 미복원
- **위치**: `SubWindow/WorkSettingWindow.xaml.cs` — `btnCancel_Click` (L1178~1188)
- **원인**:
  - 런타임(`GetTopSet`/`GetBotSet`)이 `MainHRControl`의 `m_fwdSet`/`m_revSet`를 직접 참조한다.
  - 역방향에서 `정방향 복사`·ROI 추가/수정/삭제 후 저장하지 않고 취소를 눌러도 DB 재로드를 하지 않아 미저장 데이터가 그대로 메모리에 남아 이후 마킹 및 가드 판단에 사용된다.
  - 역방향 탭(Align Bank 1)에서 취소를 눌러 창을 닫으면 카메라 Align Bank가 0으로 복원되지 않는다.
- **수정 내용**:
  - `btnCancel_Click` 시 `topHRControl`/`botHRControl`의 정·역 `MarkSet`을 DB 기준으로 다시 로드하고(`SetReverse(false)` 포함), `Visions[0].SelectAlignBank(0)`으로 복원한다.

### 2.4 역방향 DB 저장 검증 전 `Reverse.ini` 선갱신 순서 수정
- **위치**: `SubWindow/WorkSettingWindow.xaml.cs` — `btnSave_Click` (L1106~1140)
- **원인**:
  - `markingInfo.RevBaseTopY` / `RevBaseBotY` 갱신 및 `SaveReverseInfo(...)`가 DB 저장 건수 검증(`GetMarkSet(true).ObjectCount != CanvasObjectCount()`)보다 먼저 실행된다.
  - DB 저장 실패 시에도 `Reverse.ini`가 최신 기준 Y로 덮어써져 Stale 판정이 무력화될 수 있다.
- **수정 내용**:
  - DB 저장 및 건수 일치 검증이 모두 성공한 뒤에만 `RevBaseTopY`/`RevBaseBotY`를 갱신하고 `SaveReverseInfo`를 호출하도록 순서를 조정한다.

### 2.5 이어하기 ITS 중복 체크 기준 일치 및 작업자 안내 보강 (방안 A)
- **위치**: `ManagementClass/WorkManagement.cs` (L1086), `SubWindow/WorkHistoryWindow.xaml.cs` (L44~53)
- **원인**:
  - ITS에는 완성 번호(`GetCompletedStrip(0, stripID)` = `stripID - 1`)가 기록되지만, `MarkingPC`의 `ITSHistoryWindow` 중복 체크는 기준 번호(`ptrMain.model.stripID`)를 넘기고 있다.
  - 예를 들어 ITS 마지막 줄이 `0010`일 때 작업자가 실수로 `11`로 수동 수정해 재개하면 `ITSHistoryWindow`는 `0011`만 검색해 통과시키고, 실제로는 이미 존재하는 `0010`이 ITS에 중복 기록된다.
- **수정 내용**:
  1. `WorkManagement.cs` L1086에서 `ITSHistoryWindow`에 넘기는 번호를 `GetCompletedStrip(0, ptrMain.model.stripID)`로 변경하여 중복 기록을 코드 레벨에서 차단한다.
  2. `WorkHistoryWindow` UI 안내에 **"마지막 완성(ITS) 번호 / 다음 기준 번호 (이번 사이클 완성 번호)"** 관계를 명확히 표기해 작업자 오입력을 예방한다.

---

## 3. 권장 수정 항목 (문서 사양 정합성 및 운영 안정성)

### 3.1 디코드 이미지 저장 시 `MarkingLocation` 직접 참조 교체
- **위치**: `Recognition/DataMatrix.cs` — `SaveRecogImg` (L285)
- **내용**: 정방향 `customerInformation.MarkingLocation.Contains("고객1 우")` 직접 참조를 `ptrMain.GetRowMarkingLocation(colStrip).Contains("고객1 우")`로 교체한다. (역방향 열 저장 이미지 파일명의 strip 번호 1 어긋남 해소)

### 3.2 `CreateWorkHistory`의 `FirstID` 저장 일관성 수정
- **위치**: `ManagementClass/WorkManagement.cs` — `CreateWorkHistory` (L267~280)
- **내용**: 신규 Lot 첫 사이클 후 `SaveCompletedStrip(1)` 호출 시 `FirstID` 컬럼에 완성 번호(`1`) 대신 시작 기준 번호(`ptrMain.model.fisrtID`, 기본 `2`)가 저장되도록 수정한다.
- **효과**: 이어하기 후에도 `ptrMain.model.fisrtID`가 `2`로 유지되어 EOL 시 `removeStrip = fisrtID - 1 = 1` 삭제 규칙, 텐션 계산(`Math.Abs(stripID - fisrtID)`), AOI 미초기화 기준값(`fisrtID == 1`)이 신규/이어하기 간 일관되게 동작한다.

### 3.3 `LotWindow.InitOrientToggles` 진입 가드 보강
- **위치**: `SubWindow/LotWindow.xaml.cs` — `InitOrientToggles` (L276~288)
- **내용**: 현재는 `topRev.BlockCount > 0 || botRev.BlockCount > 0`만 확인하고 있다. `docs/02_REFACTOR_PLAN.md` §8.1 기준에 맞춰 아래 조건까지 반영해 진입 단계에서 역방향 토글을 비활성화하고 사유를 표시한다.
  - `MarkingMethod`별 필수 면(상부/하부/양면) 역방향 블록 존재 여부
  - `IsRevTopStale()` / `IsRevBotStale()` (기준점 변경으로 재티칭 필요 상태)
  - 역방향 얼라인 ROI 및 템플릿(`HasAlignBank(1)`) 존재 여부 (1열/마지막 열 역방향 허용 조건)

### 3.4 Laser 사이클별 방향/블록군/위치 로그 추가
- **위치**: `ManagementClass/LaserMarkingManagement.cs` — `TopMark` (L937~1065), `BotMark` (L1229~1340)
- **내용**: `docs/02_REFACTOR_PLAN.md` §8.3에 따라 사이클·열별 Laser 로그에 방향(`정`/`역`), 블록 기준 오프셋(`0`/`20`), 적용된 `GetRowMarkingLocation(colStrip)` 정보를 기록한다.

### 3.5 `MarkingTEST` 경로에 완성 strip ITS/작업이력 규칙 반영
- **위치**: `ManagementClass/WorkManagement.cs` — `MarkingTEST` (L1781~1931)
- **내용**: `docs/02_REFACTOR_PLAN.md` §6.1·Phase 4 기준에 맞춰 `MarkingTEST`도 구형 `SaveWorkHistory()` 대신 `SaveCompletedStrip` / `WriteITSCompleted` / `WriteITSEnd` 경로를 사용하도록 맞춰 하드웨어 없는 사전 검증이 가능하게 한다.

### 3.6 티칭·모델 관리 세부 정리
- `MainHRControl.GetRemainROICount()` (L1470): `para_*` 4개 테이블 카운트 쿼리에 `model` 및 `orientation = DbOrientation` 조건 추가 (`docs/02_REFACTOR_PLAN.md` §3.2).
- `MainHRControl.CopyMarksFromForward()` (L2216~2273) & `Modify()` (L2063~2065): 기존 역방향 항목(`i >= 0`) 갱신 시 캔버스 `TextRectangle` 표시(회전각 등)도 갱신하고, `Modify`의 `align` 분기가 `roi` 외 `size`/`match`/`type`도 함께 UPDATE하도록 보완.
- `SubWindow/WorkSettingWindow.xaml.cs` (L167~168 vs L256~257, L864~865): `imgAlign`과 `imgtemplate`에 바인딩되는 템플릿 1/2 순서를 일관되게 통일.
- `ManagementClass/LaserMarkingManagement.cs` — `UpdateBotMarkInfo()` (L863~910): `UpdateTopMarkInfo()`와 동일하게 `botHRControl.GetMarkSet(false)`를 명시 참조하도록 통일.
- `ModelManager.xaml.cs` — `btnStart_Click()` (L588~603): `MainWindow.orients` 초기화를 `if (Setting.Device.MotionUse)` 바깥으로 이동하여 `MotionUse=0` 테스트 모드에서도 모델 전환 시 이전 배열이 남지 않도록 수정.

### 3.7 설계 문서 현행화
- `docs/02_REFACTOR_PLAN.md` §4의 *"같은 번호끼리 짝을 짓는다"* 문구를 실제 구현 및 `ab_stripid_its.png` 기준인 **"FOV 내 같은 좌/우 쪽 마크끼리 짝을 짓는다(`GetSameSideAlign`)"** 로 현행화한다.

---

## 4. 현장 테스트 시 확인 및 유념 사항

1. **첫 사이클(`stripID == 2`) 역방향 열 마킹 검사(`RunMarkCheck`)**
   - `MarkingPC`에서 `cntCycle = 0`으로 시작하므로 첫 사이클(`stripID == 2`)에 마킹 검사가 즉시 실행된다.
   - 역방향 열은 `고객1`이 FOV 좌측(strip 1)에 위치한다. 현장 셋업 시 첫 사이클 FOV 좌측에 기판이 없어 인식 불량 알람(`TOP/BOTTOM_MARK_COGNITION_FAIL`)이 발생한다면, `stripID == 2`에서 `"고객1 우"`(FOV 좌측 고객1) 열만 검사 스킵하거나 `cntCycle` 초기값을 `1`로 두는 보완을 적용한다.
2. **정/역 얼라인 티칭 기준**
   - `SearchAlign`은 각 템플릿 캡처 위치 대비 오차를 반환하므로, 정방향(예: 1열)과 역방향(예: 3~4열) 얼라인 템플릿은 **틀어짐 없는 동일 기준 패널에서 스테이지 Y축만 열 간격 이동해 캡처**해야 `x12 - x11`의 X 기준 편향이 0으로 맞는다.
3. **티칭 창 단발 테스트 마킹 함수(미사용) 참고**
   - `Top/Bot` `Custom/Inter/OCRMark` 6개 함수는 현재 미사용이라 수정 제외했으나, 향후 재사용할 경우 `BuildDisableBlocks`로 반대 방향 블록군을 비활성화하고 `customMarks[0].flip` 대신 `GetHeadFlip`을 사용하도록 보완해야 한다.

---

## 5. 정상 반영 확인 완료 영역 (참고)

- **데이터 모델 및 DB 분리**: `MarkSet`, `ReverseLayout`, `Reverse.ini` (`RevBaseTopY`/`RevBaseBotY`), `para_*` `top`/`bot`/`top_r`/`bot_r` 격리, 기존 INSERT 문법 오류 및 `isExist` DataReader 누수 수정 완료.
- **레이저 블록 2세트 운영**: `KeyenceProtocol.DISABLE`, `LoadProjectTop`/`LoadProjectBot`의 `0..19` + `20..39` 등록, `TopMark`/`BotMark`에서 매 열 반대 방향 블록 명시 비활성화 정상.
- **열별 방향·위치 매핑**: `LotWindow.GetPhysicalRow`, `WorkMarkInfoWindow`, `MainWindow.GetRowMarkingLocation`이 `MarkingDirection` 정/역 방향 모두 일관 동작.
- **시작 차단 가드**: `CheckReverseLayoutReady`에서 PLC 모드, `UseAOIMark`, `MakringShiftCheck`, `MarkingMethod`별 역방향 블록 유무, 1열/마지막 열 역방향 시 얼라인 ROI·템플릿 유무, `RevBaseTopY`/`RevBaseBotY` Stale 차단 정상.
- **이어하기 번호 수식**: `GetCompletedStrip` 및 `WorkHistoryWindow`의 `GetResumeOffset(0)` 보정 수식이 정/역/혼합 및 단일측 마크 조합 모두 정확히 일치.

---

## 6. 후속 작업 순서 및 검증 체크리스트

1. **Step 1 (필수 결함 수정)**: §2.1 ~ §2.5 반영
2. **Step 2 (권장 사양 보강)**: §3.1 ~ §3.7 반영
3. **Step 3 (검증)**
   - [ ] `AAAA` 기존 모델 회귀: 얼라인(`GetRoiBaselineOffset == 0`), 마킹, ITS, 이어하기 기존과 100% 동일 동작
   - [ ] `AABB` / `BBAA` 혼합 모델 얼라인 로그 확인: `AABB`에서 베이스라인 보정값이 **양수(`+`)**, `BBAA`에서 **음수(`-`)** 로 출력되고 실기 기울기 보정 방향 일치
   - [ ] 역방향 티칭: ROI 더블클릭 수정 후 `markPos` 유지 확인, `Cancel` 후 메모리 원복 확인, DB 저장 실패 시 `Reverse.ini` 미갱신 확인
   - [ ] 이어하기 시나리오: 중단 → 재개 시 `ITSHistoryWindow` 중복 차단(수동 1단계 앞 번호 입력 차단) 및 `FirstID` 유지·EOL 정상 처리 확인
   - [ ] 역방향 열 `SaveRecogImg` 파일명 strip 번호 일치 및 `LotWindow` Stale 토글 비활성화 확인
