# 05_CODE_REVIEW.md — 혼합배열(정/역, AABB) 구현 코드 리뷰

> **대상 브랜치**: `혼합배열기능개발` (uncommitted working tree, 22개 파일, +1,607 / -298)  
> **검토 일자**: 2026-11-20  
> **빌드 검증**: `MSBuild` Debug 빌드 통과 (컴파일 오류 0건, 기존 코드 경고만 존재)

---

## 1. 총평

기존 AAAA(전 열 정방향) 하위 호환을 유지하면서 정/역 혼합 배열(AABB, BBAA 등)을 지원하는 핵심 아키텍처가 일관되게 구현되어 있습니다.

- **잘된 점**
  - DB 스키마 변경 없이 `orientation` 컬럼 값(`top`/`bot` vs `top_r`/`bot_r`)과 `Reverse.ini`로 정/역 레시피 및 기준점 스냅샷을 안전하게 분리
  - 단일 Keyence 프로그램 내 정방향(`index`) + 역방향(`20 + index`) 블록 이중 등록 및 열별 `DISABLE` 토글로 레이저 재로드 없이 택트 유지
  - `MarkSet` 기반 런타임 접근(`GetTopSet`/`GetBotSet`)으로 티칭 화면 상태와 런타임 실행 데이터 격리
  - 얼라인 뱅크(`0`/`1`) 선택, 화면 같은 쪽 얼라인 매칭(`GetSameSideAlign`), 정/역 혼합 시 베이스라인 보정(`GetRoiBaselineOffset`) 적용
  - 완성 strip 기준 ITS/작업이력 통합 규칙 및 `m_printedCust1` 캐싱
  - 기존 코드 결함 동시 수정: `MainHRControl.Save()` INSERT 콤마 누락, `DecodeMarkJob`의 열 인덱스 `0` 고정, 하부 디코딩의 상부 포맷 참조, `DataMatrix.SaveRecogImg`의 열별 방향 반영

다만 실제 설비 운용 및 엣지 케이스에서 오동작·오인쇄로 이어질 수 있는 **HIGH 4건, MEDIUM 6건, LOW 3건**의 보완 사항이 확인되었습니다.

---

## 2. 요약표

| 심각도 | 번호 | 대상 파일 | 요약 |
|---|---|---|---|
| **HIGH** | H-1 | `SubWindow/WorkSettingWindow.xaml.cs` | 역방향 초기화(`btnDelete_Click`) 후 레이저 프로그램 미갱신 → AAAA 작업 시 잔존 역방향 블록 이중 마킹 위험 |
| **HIGH** | H-2 | `MainHRControl.xaml.cs`, `MarkParaSettingWindow.xaml.cs`, `OCRParaSettingWindow.xaml.cs` | 재사용 설정창이 정/역 전환 후 좌/우(`m_markPos`)를 갱신하지 않음 (`Window.Loaded` 1회 실행 한계) |
| **HIGH** | H-3 | `MainWindow.xaml`, `Properties/AssemblyInfo.cs` | 운영 UI에 `btntestShift`(TEST 버튼) 노출 및 앱 버전 다운그레이드 (`2.0.7` → `2.0.6`) |
| **HIGH** | H-4 | `ManagementClass/WorkManagement.cs` | ITS↔DB 불일치 수동 수정 시 완성번호가 아닌 기준번호(`newStripID`)가 DB에 기록됨 |
| **MEDIUM** | M-1 | `ManagementClass/TeachingManagement.cs` | `CheckAlign11/12/21/22`의 직전 값(`bfY*`, `isFirst*`)이 lot/방향 변경 시 초기화되지 않음 |
| **MEDIUM** | M-2 | `SubWindow/WorkSettingWindow.xaml.cs` | 정방향 하부(`bot`) 저장 시 상부 축(Axis 1) 검사, 블록 번호 상한(`max index`) 및 템플릿 저장 실패 시 후처리 |
| **MEDIUM** | M-3 | `ManagementClass/WorkManagement.cs` | `CheckMarks2`/`CheckMarksParallel`에서 `topSet.customMarks.Count == 0`일 때 예외 및 `checkID` 변수 공유 |
| **MEDIUM** | M-4 | `ManagementClass/WorkManagement.cs` | 단일 측 마크만 티칭된 모델에서 정/역 열 간 ITS 완성 strip 번호 불일치 및 EOL 삭제 미동작 |
| **MEDIUM** | M-5 | `MainHRControl.xaml.cs` | `CopyMarksFromForward`의 `1 - src.markPos`, 미저장 드래그 위치 복귀, 하부 미리보기 각도(`-90°`) 불일치 |
| **MEDIUM** | M-6 | `ModelManager.xaml.cs` | 모델 전환 시 얼라인 뱅크 0(정방향) 미초기화로 이전 모델 템플릿 잔존 가능 |
| **LOW** | L-1 | `SubWindow/WorkSettingWindow.xaml.cs` | `btnMove1Row~4Row`와 `cbTeachRow` 미연동 (계획서 §3.1과 차이) |
| **LOW** | L-2 | `ManagementClass/TeachingManagement.cs` | 정/역 혼합 얼라인 시 `workInfoViewer.imgAlign` 표시 이미지 불일치 |
| **LOW** | L-3 | `ManagementClass/WorkManagement.cs` | `WriteITSInfo` EOL 재시도 시 `StreamWriter` append 모드로 인한 temp 파일 중복 가능성 |

---

## 3. 상세 검토 내용

### HIGH

#### [H-1] 역방향 티칭 "초기화" 후 레이저 프로그램 미갱신 → AAAA 작업 시 잔존 역방향 블록 오인쇄 위험
- **위치**
  - `HDS_ID_Marker/HDS_ID_Marker/SubWindow/WorkSettingWindow.xaml.cs` L1580–1679 (`DeleteAllClear`), L1956–1969 (`btnDelete_Click`)
  - `HDS_ID_Marker/HDS_ID_Marker/ManagementClass/LaserMarkingManagement.cs` L138–145 (`BuildDisableBlocks`)
- **재현 시나리오**
  1. AABB lot 실행 → 마지막 역방향 열 마킹으로 Keyence 레이저 메모리에 역방향 블록(`20 + index`)이 `MarkingEnable=2,1` 상태로 남음.
  2. 티칭창 진입 → 역방향 선택 → **초기화(`btnDelete_Click`)** 실행 → 별도 저장 없이 닫기(`btnCancel_Click`).
  3. 메모리와 DB의 `m_revSet`은 비어 있게 되지만 **`LoadProjectTop` / `LoadProjectBot`이 호출되지 않음.**
  4. 이후 AAAA lot을 시작하면 `otherSet`(`m_revSet`)이 비어 있어 `BuildDisableBlocks(otherSet, 20)`가 빈 문자열을 반환 → Keyence에 남아 있던 `20 + index` 블록이 비활성화되지 않은 채 정방향 블록과 함께 이중 마킹됨.
- **수정 제안**
  - `DeleteAllClear()` 완료 직후(또는 `btnDelete_Click` 내) 레이저 사용 옵션이 켜져 있으면 `LoadProjectTop` / `LoadProjectBot`을 호출해 레이저 프로그램을 정방향 블록만으로 재등록.

---

#### [H-2] 재사용 마크 설정창이 정/역 전환 후 좌/우(`m_markPos`)를 갱신하지 않음
- **위치**
  - `HDS_ID_Marker/HDS_ID_Marker/MainHRControl.xaml.cs` L35–37, L58–62 (`BindActiveSet`)
  - `HDS_ID_Marker/HDS_ID_Marker/SubWindow/MarkParaSettingWindow.xaml.cs` L118–149 (`Window_Loaded`)
  - `HDS_ID_Marker/HDS_ID_Marker/SubWindow/OCRParaSettingWindow.xaml.cs` L36–65 (`Window_Loaded`)
- **원인**
  - `customMarkSettingWindow`, `interMarkSettingWindow`, `ocrParaSettingWindow`는 생성 후 `Show()`/`Hide()`로 재사용되는 단일 인스턴스입니다.
  - WPF의 `Window.Loaded` 이벤트는 창이 **최초 1회 열릴 때만 실행**됩니다.
  - 정방향에서 설정창을 한 번 연 뒤 역방향으로 전환하면 `IsReverse`는 `true`로 바뀌지만, 창 내부의 `m_markPos`와 라디오 버튼(`rbLeft`/`rbRight`)은 정방향 값으로 유지됩니다.
  - 이 상태에서 수동으로 ROI를 추가(`AddObject`)하면 역방향 ROI가 정방향 기준 `markPos`로 생성됩니다.
- **수정 제안**
  - `MarkParaSettingWindow`, `OCRParaSettingWindow`에 `SyncMarkPosFromOrientation()`(또는 `IsVisibleChanged`에서 `IsVisible == true`일 때 갱신) 메서드를 추가하고, `BindActiveSet()` 및 창 표시 시 호출.

---

#### [H-3] 운영 UI에 `TEST` 버튼 노출 및 버전 다운그레이드
- **위치**
  - `HDS_ID_Marker/HDS_ID_Marker/MainWindow.xaml` L11: `version 2.0.7 [2026.10.12]` → `version 2.0.6 [2026.04.07]`
  - `HDS_ID_Marker/HDS_ID_Marker/MainWindow.xaml` L64: `btntestShift` `Visibility="Hidden"` → `Visibility="Visible"`
  - `HDS_ID_Marker/HDS_ID_Marker/Properties/AssemblyInfo.cs` L52–53: `2.0.7.0` → `2.0.6.0`
- **원인**
  - 이전 버전 파일이 섞여 반영된 것으로 보입니다.
- **수정 제안**
  - `btntestShift`를 `Visibility="Hidden"`으로 원복하고, `Title`과 `AssemblyVersion`/`AssemblyFileVersion`을 `2.0.7` 이상(예: `2.0.8.0`)으로 갱신.

---

#### [H-4] ITS↔DB 불일치 시 수동 입력한 기준번호가 DB `StripID`에 그대로 기록됨
- **위치**
  - `HDS_ID_Marker/HDS_ID_Marker/ManagementClass/WorkManagement.cs` L1137–1152 (`MarkingPC`)
- **원인**
  - 정상 마킹 루프에서는 `SaveCompletedStrip(GetCompletedStrip(0, ptrMain.model.stripID))`를 호출해 **완성 번호(`S - 1`)** 를 `work_history.StripID`에 기록하여 ITS 마지막 번호와 일치시킵니다.
  - 반면 L1149에서는 사용자가 수동 입력한 **다음 시작 기준번호(`newStripID`)** 를 마킹 전에 `SaveWorkHistory(ptrMain.model.stripID, newStripID)`로 DB에 바로 기록합니다.
  - 수동 입력 후 첫 사이클의 얼라인 실패·길이 불일치 등으로 마킹 전에 중단되면, ITS는 이전 번호 그대로인데 DB `StripID`만 `newStripID`(실제 완성보다 +2 앞선 값)로 저장되어 다음 재시작 시 또다시 ITS↔DB 불일치가 발생합니다.
- **수정 제안**
  - 마킹 전에는 `ptrMain.model.stripID = newStripID;`만 반영하고(마킹 완료 시 `SaveCompletedStrip`이 DB 갱신), 즉시 DB 동기화가 필요하면 `GetCompletedStrip(0, newStripID) - 1` 등 완성번호 규칙과 맞출 것.

---

### MEDIUM

#### [M-1] `TeachingManagement.CheckAlign11/12/21/22`의 직전 값(`bfY*`, `isFirst*`) 미초기화
- **위치**: `HDS_ID_Marker/HDS_ID_Marker/ManagementClass/TeachingManagement.cs` L442–487
- **원인**: `isFirst11..22`가 한 번 `false`가 된 뒤 lot 변경 시 초기화되지 않습니다. AAAA lot 직후 AABB/BBAA lot을 시작할 때 첫/마지막 열의 얼라인 편차 기준이 달라져 1.5mm 이상 차이가 나면 첫 사이클에서 `-4`(`ALIGN_FIRST_SECOND_MISMATCH`) 오알람이 발생할 수 있습니다.
- **수정 제안**: `ResetAlignHistory()` 메서드를 추가해 `MarkingPC()` 시작 시 `isFirst11 = isFirst21 = isFirst12 = isFirst22 = true;`로 초기화.

---

#### [M-2] `WorkSettingWindow.btnSave_Click`의 정방향 하부(`bot`) 축 검사 및 예외 흐름
- **위치**: `HDS_ID_Marker/HDS_ID_Marker/SubWindow/WorkSettingWindow.xaml.cs` L1051–1064, L1089–1097, L1119–1169
- **세부 내용**
  1. **축 불일치 (L1054)**: 정방향 저장 시 `m_mode == "bot"`인데도 상부 축 `GetActPos(1) - TeachRowOffset()`을 검사합니다(`SaveBotMotionPos`는 축 2를 저장). 역방향(L1041)과 동일하게 `m_mode`에 맞는 축을 검사하는 것이 일관적입니다.
  2. **블록 번호 상한 (L1091–1092)**: `ObjectCount > 20`만 검사하지만 실제 Keyence 블록 등록은 `blockBase + block.index`를 사용합니다. 비연속 인덱스로 `max index >= 20`인 데이터가 존재할 가능성까지 차단하려면 최대 `index >= ReverseLayout.RevBlockBase` 검사도 함께 두는 것이 안전합니다.
  3. **템플릿 저장 실패 시 후처리 (L1119–1169)**: `SaveReverseAlignTemplate()`이 `false`를 반환해도(`isSaved = false`) 아래 `LoadProjectTop/Bot`과 `CaptureTeachingScreen()`은 그대로 실행됩니다. `if (!isSaved) return;` 조기 반환이 깔끔합니다.

---

#### [M-3] `CheckMarks2` / `CheckMarksParallel`에서 `topSet.customMarks.Count == 0`일 때 예외 및 `checkID` 공유
- **위치**: `HDS_ID_Marker/HDS_ID_Marker/ManagementClass/WorkManagement.cs` L2691–2713, L2995–3028, L3032–3062
- **세부 내용**
  1. `topSet.customMarks.Count == 0` 분기에서 `roi = topSet.interMarks[0].roi;`로 처리하지만, 바로 아래 L2713 / L3027에서 `topSet.customMarks[0].format`을 그대로 참조해 `ArgumentOutOfRangeException`이 발생합니다.
  2. `CheckMarksParallel` L3032–3047에서 `checkID` 변수가 상부/하부 블록 간 공유되어, 하부에 `customMarks`가 비어 있을 때 상부 `checkID`가 하부 job에 그대로 전달됩니다.
- **수정 제안**: `customMarks.Count > 0 ? customMarks[0].format : interMarks[0].format`으로 폴백하고, 상부/하부 `checkID`를 각각 지역변수로 분리.

---

#### [M-4] 단일 측 마크만 있는 모델에서 정/역 열의 ITS 완성 strip 번호 불일치
- **위치**: `HDS_ID_Marker/HDS_ID_Marker/ManagementClass/WorkManagement.cs` L2188–2260 (`GetRowMarkSides`, `GetCompletedStrip`, `WriteITSEnd`)
- **원인**
  - 사내1 + 고객1이 경계 좌/우 양쪽에 모두 있는 제품에서는 정/역 열 모두 `hasPrev == true, hasCur == true`로 동작하여 문제가 없습니다.
  - 그러나 만약 `고객1`만 티칭된 모델에 혼합배열을 적용하면 정방향 열은 `hasPrev = false` (`S` 기록), 역방향 열은 `hasPrev = true` (`S - 1` 기록)로 열별 ITS 기록 번호가 1씩 어긋나며, `WriteITSEnd()`의 `hasPrev && hasCur`도 `false`가 되어 폐기 strip 삭제가 동작하지 않습니다.
- **수정 제안**: 양측 마크 모델만 혼합배열을 허용한다면 `GetReverseTeachingIssue()`에 가드를 추가하거나, 단일 측 모델의 ITS 정책을 명시적으로 통일.

---

#### [M-5] `CopyMarksFromForward`의 세부 동작 이슈
- **위치**: `HDS_ID_Marker/HDS_ID_Marker/MainHRControl.xaml.cs` L2259–2372, L813/829/851
- **세부 내용**
  1. `m.markPos = 1 - src.markPos;` → 기존 DB에 `0`/`1` 외의 값이 있으면 음수 등이 될 수 있으므로 `(src.markPos == 1) ? 0 : 1`이 안전합니다.
  2. 기존 역방향 ROI를 드래그해 이동한 뒤 저장 전에 "정방향 파라미터 복사"를 누르면 캔버스 현재 좌표가 아닌 DB 로드 시점의 `m.roi` 기준으로 `ResizeRoiKeepCenter`를 계산해 ROI가 이전 위치로 되돌아갑니다.
  3. 새로 추가된 하부(`bot`) 마크는 `refresh()`를 호출하는데, `refresh()`는 `PreviewDegree`(하부 `-90°`)를 적용하지 않아 저장 후 재로딩 전까지 미리보기 각도가 90° 어긋납니다.

---

#### [M-6] 모델 전환 시 얼라인 뱅크 0(정방향) 미초기화
- **위치**: `HDS_ID_Marker/HDS_ID_Marker/ModelManager.xaml.cs` L722–735 (`SetMarkData`)
- **원인**: 뱅크 1(역방향)은 `LoadReverseAlignData`에서 `ClearAlignBank(1)`을 호출하지만, 뱅크 0은 `ClearAlignBank(0)` 없이 `File.Exists(alignPath)`일 때만 로드합니다. 기존 모델에서 얼라인 이미지가 없는 신규 모델로 전환하면 이전 모델의 정방향 템플릿이 메모리에 남을 수 있습니다.
- **수정 제안**: `SelectAlignBank(0)` 직전에 `ClearAlignBank(0)`을 호출하고, 파일이 없을 때 `imgtemplate.Source = null; imgAlign.Source = null;` 처리.

---

### LOW

- **[L-1] 티칭 열 이동 버튼 연동**: `WorkSettingWindow.xaml.cs` L1881–1928의 `btnMove1Row~4Row`는 레이저 위치 이동(`+ CamToY`)으로 기존 그대로 유지되고 `cbTeachRow`와 연동되지 않습니다(`02_REFACTOR_PLAN.md` §3.1과 차이). 의도된 분리라면 문서 현행화로 충분합니다.
- **[L-2] 얼라인 뷰어 이미지 표시**: `TeachingManagement.cs` L269–285에서 정/역 혼합 시 마지막 열 `alignLast`가 2번이면 `SearchPointAlign2ToROI`가 `cmpAlignBmp2`를 갱신하므로 UI의 `imgAlign`(`cmpAlignBmp`)에는 반대쪽 얼라인 이미지가 표시됩니다(연산에는 영향 없음).
- **[L-3] `WriteITSInfo` EOL 재시도 시 temp 파일 누적**: `WorkManagement.cs` L2066, L2085의 `StreamWriter(itsTempPathTemp, true, ...)`가 append 모드라 네트워크 오류로 재시도 시 기존 temp 파일 뒤에 중복 기록될 수 있습니다(`false` 덮어쓰기 권장, 기존 코드 동일).

---

## 4. 권장 조치 순서

1. **즉시 수정 (배포 차단 수준)**: H-1(역방향 초기화 후 레이저 재로드), H-2(설정창 `m_markPos` 동기화), H-3(`TEST` 버튼 숨김 및 버전 복구), H-4(ITS↔DB 수동 수정 시 DB 기록 정합성)
2. **현장 테스트 전 보완**: M-1(lot 시작 시 얼라인 이력 리셋), M-2(`bot` 축 검사 및 저장 실패 시 조기 반환), M-3(`CheckMarks` `customMarks` 빈 리스트 방어), M-6(뱅크 0 클리어)
3. **실기 검증**: `04_TEST_CHECKLIST.md` 항목에 H-1(AABB → 역방향 초기화 → AAAA) 및 H-2(정방향 설정창 열기 → 역방향 전환 후 ROI 추가) 시나리오 추가 권장
