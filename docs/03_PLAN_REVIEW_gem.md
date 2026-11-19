# 03_PLAN_REVIEW_gem.md — `02_REFACTOR_PLAN.md` 및 구현 코드 아키텍처 검토 보고서

> **검토 기준**: `architect` 지침(모듈성·경계 검증·일관성·트레이드오프·안티패턴 점검) + `docs/01-inventory.md`, `docs/02_REFACTOR_PLAN.md`(v2), `docs/03_PLAN_REVIEW.md`
> **대상 경로**: `HDS_ID_Marker/HDS_ID_Marker/` 기준 상대 경로 (`MarkingPC` 경로, Vision=`MILCL`, Laser=`Keyence`, 상·하부 양면 기준)
> **빌드 확인**: `HDS_ID_Marker.sln` (`Debug|x64`) MSBuild 빌드 성공 — **오류 0건, 신규 경고 0건**

---

## 0. 총평

| 구분 | 내용 |
|---|---|
| 빌드 상태 | `HDS_ID_Marker.sln` (`Debug|x64`) 컴파일 통과 (`bin\HDS_ID_Marker.exe` 생성 확인, 신규 경고 없음) |
| 설계 강점 | DB 스키마 무변경(`top_r`/`bot_r` + `Reverse.ini`), `MarkSet`/`ReverseLayout` 규칙 응집, Keyence `BuildProjectCommand` 상·하부 중복 제거, 기존 SQL/리소스/디코딩 결함 동시 수정, 얼라인 `GetSameSideAlign` 보완 |
| 수정 필요 결함 | **치명 2건**(얼라인 베이스라인 보정 부호 반전, 이어하기 후 EOL 시 폐기 스트립 `0001` 미삭제), **높음 2건**(더블클릭 편집 시 `IsReverse` 누락, 버전 `2.0.6` 및 `btntestShift` 노출 회귀), **중간 3건**(이미지 파일명 strip 번호, DB 저장 확인 순서, 단발 마킹 Disable 및 블록 상한 가드) |
| 구조 리스크 | Stateful 비전 뱅크 스위칭(`SelectAlignBank`)과 `HasAlignBank` 기본값, UI-런타임 가변 리스트 직접 공유(`Sort` in-place), 자정 경계 `DateTime.Now` 덮어쓰기 |

### 잘 설계된 부분
- **하위 호환 및 롤백 안전성**: DB 테이블 스키마 변경 없이 기존 `orientation` 컬럼(`top_r`/`bot_r`)과 모델 폴더 `Reverse.ini`(`MarkingInformation.cs:31-87`)로 역방향 상태를 격리해, 구버전으로 롤백하더라도 기존 정방향(`top`/`bot`) 동작이 유지된다.
- **공통 규칙 응집**: `Data/MarkInformation.cs:74-140`의 `MarkSet`·`ReverseLayout`과 `ManagementClass/LaserMarkingManagement.cs:572-638`의 `BuildProjectCommand`/`BuildBlocks`/`BuildDisableBlocks`로 상·하부 중복 코드가 단일 경로로 정리되었다.
- **기존 잠재 버그 동시 해소**:
  - `MainHRControl.Save`의 SQL 콤마 누락(`MainHRControl.xaml.cs:1983, 1989`)
  - `MainHRControl.isExist`의 `dataReader` 미반납(`MainHRControl.xaml.cs:2126-2135`)
  - `WorkManagement.CheckMarks2`/`CheckMarksParallel`의 하부 디코딩 시 상부 `format` 오참조 및 `Decode(..., 0, ...)` 열 인덱스 고정(`ManagementClass/WorkManagement.cs:2759, 3000, 3043`)
- **얼라인 짝 매칭 개선**: `ManagementClass/TeachingManagement.cs:199-200`의 `GetSameSideAlign`은 180° 회전 시 같은 번호가 반대쪽 스트립을 가리키는 문제를 피하고 화면상 같은 X쪽 마크와 짝을 맞추므로, `02_REFACTOR_PLAN.md` §4("같은 번호끼리")보다 물리적으로 정확하다.

---

## 1. 실기 투입 전 수정 필요 결함 (우선순위 순)

### 1.1 [치명] 얼라인 베이스라인 보정 부호 반전 — `ManagementClass/TeachingManagement.cs:429-438`

- **현상**:
  ```csharp
  // ManagementClass/TeachingManagement.cs:435
  double offset = -(RoiCenterY(roiLast) - RoiCenterY(roiFirst)) * canvasToCam * MainWindow.ri.TopCamResolution;
  ```
- **원인**: 주석에서는 기존 식(`alignDistanceY -= y12`, `TeachingManagement.cs:255`)과 부호를 맞춘다고 `-`를 붙였으나, 코드의 기하와 계획서·검토서(`docs/02_REFACTOR_PLAN.md:113`, `docs/03_PLAN_REVIEW.md:117`) 모두 **`+`**가 맞다.
  - `TeachingManagement.cs:262`에서 `alignOffset[i].Y = y11 + (StripIter[i] * Math.Cos(radian) - StripIter[i])`이며, `Cos(radian) - 1 <= 0`이므로 기울기로 인해 1~4열 간 거리가 짧아지면 4열 마크는 `y` 감소(화면 위쪽)로 관측된다. 즉 **스테이지 이동 `+Y`(1→4열)와 이미지/캔버스 `+Y`(위→아래)는 같은 방향**이다(`PCS/Gerber/GerberLoader.cs:113` 피치 추출 및 `alignDistanceY -= y12`와 일관됨).
  - 예: 1열(정방향) 마크가 열 상단(`roiCY_0` 작음), 4열(역방향) 마크가 열 하단(`roiCY_last` 큼)에 있으면 실제 마크 간 베이스라인은 `StripIter[last] + Δ`로 **더 길어져야** 한다(예: `200 + 50 = 250 mm`). 현재 `-` 부호로는 `200 - 50 = 150 mm`로 계산되어, 기울기 각도와 중간 열 보간 오차가 반대 방향으로 약 2배 이상 왜곡된다.
- **조치**: `GetRoiBaselineOffset`의 `-`를 `+`로 수정한다. (단, 실기 카메라 영상 방향이 스테이지 `Y`와 반전되어 있지 않은지 1열→2열 이동 시 영상 이동 방향으로 1회 교차 확인 권장)

### 1.2 [치명] 이어하기 후 Lot 종료 시 폐기 스트립(`0001`) 미삭제 및 `FirstID` 오염 — `ManagementClass/WorkManagement.cs:267-281, 2213`

- **재현 경로**:
  1. 신규 Lot 시작 시 `stripID = 2`, `fisrtID = 2`로 초기화된다(`WorkManagement.cs:72-73`).
  2. 1사이클(`S = 2`) 완료 후 `SaveCompletedStrip(GetCompletedStrip(0, 2))` → `SaveCompletedStrip(1)` → `CreateWorkHistory(lot, name, 1, historyID)`를 호출한다(`WorkManagement.cs:1344, 1411, 1474`).
  3. `CreateWorkHistory`(`WorkManagement.cs:278-280`)가 **`FirstID`와 `StripID` 컬럼 모두에 인자 `stripID`(= `1`)를 INSERT**한다.
  4. 중간 정지 후 **이어하기** 시 `CheckWorkHistory`(`WorkManagement.cs:65`)가 DB에서 `fisrtID = GetFirstID(lot) = 1`을 복원한다.
  5. Lot 종료 시 `WriteITSEnd()`(`WorkManagement.cs:2213`)가 `removeStrip = ptrMain.model.fisrtID - 1 = 0`을 계산한다.
     → **첫 사이클에 임시 기록해 둔 폐기 스트립 `0001`이 ITS/ITS(temp) 파일에서 삭제되지 않고 그대로 남는다.**
  6. 추가로 `UseAOIMark = true`인 기존 정방향(`AAAA`) 모델에서는 이어하기 시 `WorkManagement.cs:1269`의 `if (ptrMain.model.fisrtID == 1)` 조건이 오작동해 `FirstID`를 현재 `stripID`로 덮어쓰고, EOL 시 정상 양품 스트립 1장을 삭제할 위험이 있다.
- **조치**: `CreateWorkHistory`에서 `FirstID` 컬럼에는 `stripID`(완료 번호)가 아닌 `ptrMain.model.fisrtID`(작업 시작 기준 번호 `2`)를 저장하도록 분리한다.

### 1.3 [높음] 역방향 캔버스 ROI 더블클릭 편집 시 `IsReverse` 미전달 — `MainHRControl.xaml.cs:916, 969, 1022`

- **현상**: `BindActiveSet()`(`MainHRControl.xaml.cs:80-82`)은 멤버 변수 `customMarkSettingWindow`, `interMarkSettingWindow`, `ocrParaSettingWindow`에만 `IsReverse = m_reverse`를 설정한다.
- **원인**: 캔버스에 이미 그려진 ROI를 **더블클릭해 파라미터를 수정**할 때는 매번 새 인스턴스를 생성한다(`916`행 `custom`, `969`행 `inter`, `1022`행 `ocr`). 이때 `IsReverse`를 전달하지 않아 기본값 `false`로 열린다.
- **영향**: 역방향 티칭 화면에서 기존 마크 ROI를 더블클릭해 크기/파워/각도만 확인·수정해도 `MarkParaSettingWindow.Window_Loaded`/`OCRParaSettingWindow.Window_Loaded`에서 `markPos`가 정방향 기준으로 강제 덮어쓰기된다.
- **조치**: `916`, `969`, `1022`행 모두 창 생성 직후 `.IsReverse = m_reverse;`를 설정한다.

### 1.4 [높음] 의도치 않은 버전 다운그레이드 및 디버그 버튼 노출 — `MainWindow.xaml:11, 64`, `Properties/AssemblyInfo.cs:52-53`

- **현상**: 직전 커밋(`5b01d8d`, "tact 체크용 로그추가")과 비교하면 아래 항목이 구버전(`2.0.6`) 상태로 되돌아가 있다.
  - `MainWindow.xaml:11`: `version 2.0.7 [2026.10.12]` → `version 2.0.6 [2026.04.07]`
  - `MainWindow.xaml:64`: `btntestShift` `Visibility="Hidden"` → `Visibility="Visible"`
  - `Properties/AssemblyInfo.cs:52-53`: `2.0.7.0` → `2.0.6.0`
- **조치**: 이전 파일 덮어쓰기 과정에서 혼입된 것으로 보이므로 `2.0.7`(또는 신규 버전) 및 `btntestShift` `Visibility="Hidden"`으로 원복한다.

### 1.5 [중간] 인식 이미지 저장 파일명 strip 번호 계산에 열별 위치 미적용 — `Recognition/DataMatrix.cs:283-287`

- **현상**: `SaveRecogImg`가 `ptrMain.model.markingInformation.customerInformation.MarkingLocation.Contains("고객1 우")`만 검사한다(`DataMatrix.cs:285`).
- **영향**: 예로 `"사내1 우, 고객1 좌"` 고객사의 역방향 열에서는 실제로 `stripID - 1`을 마킹·디코딩하는데, 저장되는 `.jpg` 파일명에는 `stripID`가 붙어 이미지 내용과 파일명의 번호가 1씩 어긋난다.
- **조치**: `ptrMain.GetRowMarkingLocation(colStrip).Contains("고객1 우")`로 변경한다.

### 1.6 [중간] 역방향 DB 저장 실패 시에도 `Reverse.ini` 선기록 및 레이저 로드 진행 — `SubWindow/WorkSettingWindow.xaml.cs:1106-1168`

- **현상**: `btnSave_Click`에서 `RevBaseTopY/BotY`를 `Reverse.ini`에 저장(`1108-1109`, `1122-1123`)한 뒤, 뒤쪽 `1133`행에서 `ctrl.GetMarkSet(true).ObjectCount != ctrl.CanvasObjectCount()` DB 저장 건수 불일치를 검사한다.
- **영향**: DB `orientation` 컬럼 길이 부족 등으로 역방향 저장이 실패해 `isSaved = false`가 되더라도, 이미 `Reverse.ini`는 최신 기준점으로 갱신되고 `LoadProjectTop/Bot`(`1157, 1159`)도 그대로 호출된다.
- **조치**: DB 건수 불일치 검증을 `SaveReverseInfo` 및 `LoadProjectTop/Bot` 이전으로 이동하고, 불일치 시 `isSaved = false`와 함께 즉시 `return`하도록 정리한다.

### 1.7 [중간] 단발 마킹 테스트 시 반대 방향 블록 미비활성화 & 블록 상한(`< 20`) 가드 부재 — `ManagementClass/LaserMarkingManagement.cs:1079-1216, 1357-1488`

- **현상**:
  1. 티칭창 단발 마킹(`TopCustomMark`, `TopInterMark`, `TopOCRMark`, `BotCustomMark`, `BotInterMark`, `BotOCRMark`)에서 `ActiveBlockBase + block.index`를 사용하도록 수정했으나, `BuildDisableBlocks`를 보내지 않는다. 이전 마킹에서 Enable된 반대 방향 블록이 레이저 메모리에 남아 함께 발사될 위험이 있다. (범위 제외라면 역방향 모드에서 단발 마킹 버튼을 비활성화하거나, 허용할 경우 `BuildDisableBlocks`를 함께 전송해야 한다.)
  2. `ReverseLayout.RevBlockBase = 20` 구조상 정방향 `AllObjectCount() >= 20`이 되면 정방향 블록과 역방향 `20 + index` 블록 번호가 충돌한다. 티칭 추가/저장 또는 레이저 프로젝트 로드 시 블록 상한 가드가 없다.
- **조치**: 단발 마킹 버튼의 역방향 비활성화 또는 `BuildDisableBlocks` 추가 중 정책을 확정하고, `index >= ReverseLayout.RevBlockBase` 시 경고·차단 가드를 둔다.

---

## 2. 문서(`02_REFACTOR_PLAN.md`, `03_PLAN_REVIEW.md`) ↔ 코드 불일치

| 항목 | 문서 기술 | 실제 코드 | 권장 정리 |
|---|---|---|---|
| **첫 strip 처리** | `02_REFACTOR_PLAN.md:164-167`은 "기록 후 EOL 시 삭제", 같은 문서 `185`행과 `03_PLAN_REVIEW.md:355`는 "첫 strip은 스킵" (문서 내부 모순) | 기록 후 EOL 시 삭제 (`WriteITSCompleted` + `WriteITSEnd`, `WorkManagement.cs:2189-2228`) | 문서(`02_REFACTOR_PLAN.md:185`, `03_PLAN_REVIEW.md` §13)를 "첫 strip 기록 후 EOL 시 삭제"로 통일 |
| **얼라인 짝 매칭** | `02_REFACTOR_PLAN.md:112` "같은 번호끼리 짝을 짓는다" | `GetSameSideAlign` (화면 X가 가까운 쪽 짝, `TeachingManagement.cs:199, 388-399`) | **코드가 물리적으로 맞음** → 문서를 코드 기준으로 수정 |
| **`LotWindow` 역 토글 비활성화** | `02_REFACTOR_PLAN.md:212` "미티칭 또는 **재티칭 필요** 상태이면 역 토글 비활성화 + 사유 표시" | `LotWindow.InitOrientToggles`(`LotWindow.xaml.cs:276-288`)는 `BlockCount > 0`만 검사 (`IsRev*Stale`, 템플릿 누락 미검사 → `StartMarking` 시 뒤늦게 차단) | `WorkManagement.GetReverseLayoutBlockReason()` 기반 검증(또는 공용 정적 헬퍼)을 `LotWindow`에서 재사용해 사유를 바로 표시 |
| **사이클별 Laser 로그** | `02_REFACTOR_PLAN.md:229` "사이클마다 열별 정/역, 사용한 블록군, 위치 문자열을 Laser 로그에 남긴다" | `TopMark`/`BotMark`(`LaserMarkingManagement.cs:937-1067, 1218-1346`)에 방향·블록군·위치 문자열 로그 없음 | `TopMark`/`BotMark` 진입 시 열별 방향·`blockBase`·`GetRowMarkingLocation` 요약 로그 1줄 추가 |
| **`CopyMarksFromForward` 재실행** | `02_REFACTOR_PLAN.md:82-86` 파라미터 복사 + 각도 보정 | `MainHRControl.CopyMarksFromForward`(`2216, 2236, 2256, 2273`)가 기존 마크 갱신 시 `MarkSet` 메모리만 바꾸고 캔버스 `TextRectangle` 크기/회전은 갱신 안 함 (저장 시 캔버스 크기 기준으로 `roi` 덮어씀) | 기존 항목 갱신 시 캔버스 `TextRectangle`의 크기/회전 표시 동기화 또는 `DrawObjects()` 재호출 |
| **`MarkingTEST` 범위** | `02_REFACTOR_PLAN.md:190` "`MarkingTEST` 같은 호출 구조 → 같은 규칙 적용", `240` Phase 4에서 ITS 검증 언급 | `MarkingTEST`(`WorkManagement.cs:1653-1940`)는 원래 ITS 호출이 없고 `SaveWorkHistory()`(`1835, 1881, 1928`)도 구버전 그대로 | `MarkingTEST`의 `SaveWorkHistory()`를 `SaveCompletedStrip`으로 맞출지, ITS 검증 대상을 문서에서 분리할지 결정 |
| **템플릿 미리보기 좌/우** | - | `ApplyOrientation`(`WorkSettingWindow.xaml.cs:176-177`)과 `ModelManager.SetMarkData`(`ModelManager.xaml.cs:725, 729`)는 `imgtemplate=Align1, imgAlign=Align2`, 반면 `SaveReverseAlignTemplate`(`WorkSettingWindow.xaml.cs:259-260`)과 기존 `SaveAlignTemplateAndTopMotionPos`(`864-865`)는 `imgAlign=Align1, imgtemplate=Align2`로 반대로 대입 | 모델 로드 직후와 저장 직후의 썸네일 위치가 뒤바뀌므로 `imgtemplate=Align1, imgAlign=Align2`로 통일 권장 |

---

## 3. 아키텍처 트레이드오프 및 구조 리스크 (`architect` 관점)

### 3.1 비전 API: Stateless 오버로드(계획) vs Stateful 뱅크 스위칭(구현)
- **현황**: `02_REFACTOR_PLAN.md` §4는 기존 `align`/`align2` 필드를 교체하지 않고 `Mat` 인자를 받는 오버로드를 추가하도록 설계했으나, 구현에서는 `Vision.cs:68-73`과 `MILCL.cs:47-51, 492-520`에 `SelectAlignBank(0/1)` 뱅크 스위칭을 도입했다.
- **Trade-off**:
  - *장점*: 기존 `SearchPointAlignToROI` / `SearchPointAlign2ToROI` / `BitmapHelper` 시그니처를 변경하지 않아 수정 범위가 작다.
  - *리스크*:
    1. 호출 순서에 의존하는 암묵적 상태(`activeAlignBank`)가 생긴다. 예를 들어 티칭창에서 역방향을 보다 저장 없이 닫고 작업을 시작하면 `bank=1` 상태로 남을 수 있다(`TeachingManagement`/`UpdateTopMarkInfo`가 호출 전 `SelectAlignBank`를 다시 부르긴 하나 누락 시 오탐).
    2. `Vision.HasAlignBank(int bank)`의 기본 구현이 `return true;`(`Vision.cs:70`)여서, `MILCL` 외 드라이버나 미구현 경로에서 템플릿 누락 검사가 항상 통과한다.
    3. `MILCL.ClearAlignBank`/`ImageToAlign` 재로드 시 기존 `OpenCvSharp.Mat`을 `Dispose()`하지 않아 반복 모델 전환 시 네이티브 메모리 누수가 쌓인다.

### 3.2 UI-런타임 결합 잔존 (`MarkSet` 가변 리스트 직접 공유)
- **현황**: `MainWindow.GetTopSet`/`GetBotSet`(`MainWindow.xaml.cs:675-684`)이 `MarkRecipe` 불변 스냅샷 대신 `MainHRControl` 내부의 가변 리스트(`m_fwdSet`/`m_revSet`)를 그대로 반환한다.
- **리스크**:
  1. `WorkManagement.CheckMarks2`(`2629, 2740`)와 `CheckMarksParallel`(`2933, 2974`)가 `topSet.customMarks.Sort(new RefComparer())`로 UI 리스트를 **in-place 정렬**한다. 만약 티칭 저장이나 블록 인덱스 순서와 얽히면 순서가 변형된다.
  2. 티칭창(`WorkSettingWindow`)에서 수정 후 저장하지 않고 취소(`Hide()`)한 경우, 메모리 리스트와 DB 상태가 어긋난 상태로 런타임에 노출될 수 있다(`SetReverse`는 DB 재로드를 수행하지만 일반 `btnCancel_Click`은 미복원).
- **권장**: 당장 `MarkRecipe` 스냅샷 분리가 어렵다면, 최소한 `CheckMarks*` 정렬은 리스트 복사본(`OrderBy`)을 사용하도록 바꾸고 티칭 취소 시 DB 재로드를 보장하는 편이 안전하다.

### 3.3 자정 경계 엣지 케이스 (참고)
- `SEC` 등 일부 고객 분기의 `GetCustomIDString`(`WorkManagement.cs:3259`)이 인자로 받은 `time` 대신 내부에서 `time = DateTime.Now`로 덮어쓴다.
- 마킹 시점(`23:59:59`)과 다음 사이클 `WriteITSCompleted` 호출 시점(`00:00:01`) 사이에 자정이 걸치면, 실제 스트립에 각인된 날짜와 ITS 기록 날짜가 1일 달라질 수 있다.

---

## 4. 권장 후속 조치 순서

1. **[필수/치명]** `TeachingManagement.cs:435` `GetRoiBaselineOffset` 부호(`-` → `+`) 수정 및 실기 카메라 축 방향 교차 검증
2. **[필수/치명]** `WorkManagement.cs:267-281` `CreateWorkHistory`의 `FirstID` 컬럼 저장값 수정 (`stripID` → `ptrMain.model.fisrtID`)
3. **[필수/높음]** `MainHRControl.xaml.cs:916, 969, 1022` 더블클릭 편집 창에 `IsReverse = m_reverse` 전달
4. **[필수/높음]** `MainWindow.xaml:11, 64` 및 `Properties/AssemblyInfo.cs:52-53` 버전(`2.0.7`)·`btntestShift` 숨김 원복
5. **[권장/중간]** `DataMatrix.cs:285` `GetRowMarkingLocation` 적용, `WorkSettingWindow.xaml.cs` 저장 순서 보정, `LaserMarkingManagement.cs` 단발 마킹 Disable/상한 가드 및 Laser 요약 로그 추가
6. **[문서/정리]** `02_REFACTOR_PLAN.md`·`03_PLAN_REVIEW.md` 내 첫 strip 처리 모순, 얼라인 매칭 설명, `MarkingTEST` 범위 일치화
