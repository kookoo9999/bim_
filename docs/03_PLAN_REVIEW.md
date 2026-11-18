# 03_PLAN_REVIEW.md — `02_REFACTOR_PLAN.md` 검토 의견

> 범위: `MarkingPC` 경로(실기 `Setting(origin).ini`: `MachineType=PC`, Vision=`MILCL`, Laser=`Keyence`) 기준.
> 기준 자료: 스트립 도면(사진1), 패널 4열 배치 + 마킹 영역(사진2, 초록 박스), 현행 소스.
> 경로는 `HDS_ID_Marker/HDS_ID_Marker/` 기준 상대 경로.

---

## 0. 요약

| 구분 | 내용 |
|---|---|
| 방향성 | 열별 Variant(A/B) 매핑, Keyence 블록 이중 등록(Enable 토글), 열별 비전 ROI 같은 큰 방향은 맞음 |
| 치명 | ① FOV 좌/우 배치가 반대로 기술됨 → ID(stripID / stripID-1) 오배정 위험 ② "시프트 검사 제외"인데 B열에서 오알람으로 설비 정지 ③ B 티칭 좌표가 A의 `TopAlignY`에 종속돼 있는데 이를 관리하지 않음 |
| 구조 제안 | B는 A를 180° 회전한 것뿐이므로 **B를 별도로 티칭하지 말고 A에서 자동 생성**하는 쪽을 권장함. DB `variant_key` 격리, `MainHRControl` 이중 리스트, B 템플릿 파일이 모두 필요 없어져 변경 범위와 회귀 위험이 크게 줄어듦 |

---

## 1. 치명 (구현 전 반드시 수정)

### 1.1 FOV 기준 좌/우 배치가 계획서와 반대

계획서 §1.1 다이어그램과 §2.6은 **스트립 1장 기준(사진1)** 배치를 그대로 FOV에 옮겼다. 하지만 카메라와 레이저가 보는 영역(사진2 초록 박스)은 **스트립 경계**에 걸쳐 있다. 그래서 왼쪽에는 앞 스트립의 **오른쪽 끝**이, 오른쪽에는 다음 스트립의 **왼쪽 끝**이 들어온다.

| | FOV 좌측 (`markPos=1`, stripID) | FOV 우측 (`markPos=0`, stripID-1) |
|---|---|---|
| **A (정방향)** | 얼라인1(상) + 사내1 | 얼라인2(상) + 고객1(하) |
| **B (180°)** | 고객1(상) + 얼라인2(하) | 사내1 + 얼라인1(하) |
| 계획서 기술 (A) | 얼라인2 + 고객 ❌ | 얼라인1 + 사내 ❌ |

코드 근거: `TeachingManagement.MarkingAlignToROI_RightOnly`(`ManagementClass/TeachingManagement.cs:277-351`)가 `number == 2`를 "우상단"으로 사용한다. 즉 현 설비의 카메라 좌표계에서도 얼라인2가 우측이고, 사진2와 일치한다.

**결과**: A 열은 `"사내1 좌, 고객1 우"`, B 열은 `"사내1 우, 고객1 좌"`가 된다. 계획서대로 구현하면 A/B 모두 ID가 1씩 어긋난다. 다이어그램과 §2.6 문구를 고쳐야 한다.

### 1.2 ID 계산은 `markPos`가 아니라 `MarkingLocation` 문자열을 쓴다

- 계획서 §2.6은 "각 마크의 `markPos`를 참조"하라고 하지만, 현행 ID 계산은 모두 고객사 공통 문자열 `customerInformation.MarkingLocation`의 `Contains("사내1 우")` 같은 검사로 동작한다(`GetInternal1IDString` `WorkManagement.cs:2921`, `GetCustomIDString` `:2986`, `ConvertInternal1ID` `:2051`, `SaveWorkHistory(string,string)` `:93`).
- 게다가 `MarkParaSettingWindow.Window_Loaded`(`SubWindow/MarkParaSettingWindow.xaml.cs:121-136`)와 `OCRParaSettingWindow`(`:56-61`)는 창이 열릴 때마다 `markPos`를 `MarkingLocation`으로 **강제로 덮어쓴다**. 따라서 B 티칭 시 `markPos`는 A 값으로 들어간다.

**권장 (최소 변경)**: 열 단위로 쓸 `MarkingLocation`을 만드는 헬퍼 하나를 추가한다.

```csharp
// B 열이면 좌/우를 맞바꾼 문자열 반환
string GetRowMarkingLocation(int row) =>
    IsRotated(row) ? SwapLR(customerInformation.MarkingLocation)   // "좌"↔"우"
                   : customerInformation.MarkingLocation;
```

이 헬퍼를 아래 호출부 **전부**에 적용한다. 계획서에서 빠진 곳이 있다.
- `WorkManagement.MarkingPC` ID 생성 `:1229-1231`, 열별 `WriteITSOneShot(ConvertInternal1ID(...))` `:1264`
- 종료 시 `WriteITS(ConvertInternal1ID(...))` `:1553` ← 배열 전체를 한 문자열로 변환하므로 열별 처리가 필요함
- **작업 시작 전 미리보기** `MainWindow.GetMarkData` `MainWindow.xaml.cs:638-645` ← 계획서에 없음. 작업자가 보는 ID가 실제와 달라짐
- `SaveWorkHistory(Int1_id[0], ...)`는 0열 기준이므로 "1열은 항상 A"가 보장되면 그대로 둬도 됨

### 1.3 마킹 시프트 검사를 범위에서 빼면 B열에서 설비가 멈춤

- 계획서는 `CheckMarkShift`를 범위 밖으로 뒀다. 그런데 `MakringShiftCheck`가 켜진 모델에서는 `DecodeMarkJob`(`WorkManagement.cs:2856`)이 모든 열에 대해 A 기준 `lstMarkInitPos[..][0]`로 판정한다. B열은 마크 위치가 완전히 달라 항상 `markShift[col]=true`가 되고, 사이클 끝(`:1506-1527`)에서 `MARKING_FAIL`로 정지한다.
- **최소 대응 (범위에 포함 필요)**: B열은 시프트 검사를 건너뛰는 가드를 넣거나, B가 포함된 레이아웃에서는 `MakringShiftCheck` 사용을 막는다.

### 1.4 B 티칭 좌표가 A의 모션 기준점에 종속됨

- ROI는 캔버스(FOV) 좌표다. 런타임에는 `TopMarkY + StripIter[i]`로 이동한 뒤 같은 ROI를 쓴다. 따라서 B ROI는 **정확히 `TopAlignY + StripIter[refCol]` 위치에서 티칭했을 때만** 유효하다.
- 계획서는 "B 저장 시 모션 좌표를 덮어쓰지 않음"까지만 다룬다. 그런데 **이후 A를 다시 티칭하면 `TopAlignY`가 바뀌어(`SaveAlignTemplateAndTopMotionPos` `WorkSettingWindow.xaml.cs:703-706`) B가 아무 경고 없이 어긋난다.**
- 하부도 마찬가지로 B 저장 시 `SaveBotMotionPos`(`BotMarkY`)를 건너뛰어야 하는데, 계획서에는 상부만 언급돼 있다.
- 대응: (a) B 티칭 모드에서는 `TopAlignY + StripIter[refCol]`로 자동 이동하고 Y 수동 조작을 잠근다. (b) A의 모션 좌표가 바뀌면 B를 "재검증 필요"로 표시한다. **또는 §2의 자동 생성 방식을 쓰면 이 종속 문제가 없어진다.**

---

## 2. 구조 제안: B는 별도로 티칭하지 말고 A에서 자동 생성

사진2를 보면 B열 FOV 내용은 A열 FOV 내용을 **(스트립 간 갭 중심 X, 스트립 중심선 Y) = C** 기준으로 점대칭(180° 회전)한 것과 같다. 각 스트립을 자기 중심 기준으로 180° 돌리면, 경계 FOV 안에서는 갭 중심 기준 점대칭이 된다. 피치가 일정하다면 수식으로 성립한다.

### 2.1 변환 규칙

| 항목 | A → B 변환 |
|---|---|
| ROI `(top,left,w,h)` | `left' = 2Cx − (left+w)`, `top' = 2Cy − (top+h)`, w/h 동일 |
| 레이저 `degree` | `degree + 180` (Keyence 허용 범위로 정규화) |
| `markPos` | `1 − markPos` |
| 얼라인 `number` | 번호는 그대로 두고 위치만 변환 (B: 얼라인1=우하, 얼라인2=좌하 → 사진2와 일치) |
| 얼라인 템플릿 | `Cv2.Rotate(align, Rotate180)`로 메모리에서 생성 |
| size/format/power/speed/flip | A와 동일 |

### 2.2 회전 중심 C 캘리브레이션 (1회)

B 첫 열로 이동해 촬상한 뒤, 회전시킨 템플릿으로 넓은 ROI에서 얼라인1/2를 찾는다. 그리고 `C = (P_A + P_B) / 2`로 계산한다. 얼라인1 쌍과 얼라인2 쌍으로 각각 구한 C가 일치하는지 교차 검증하고, 오버레이로 작업자가 확인한다.
`model_info`에는 `column_layout`과 `rot_center_x/y`(필요하면 B 미세 오프셋 dx/dy)만 저장한다.

### 2.3 효과

- 계획서 §2.2 대부분이 필요 없어진다: `para_*` 4개 테이블의 `variant_key`, 9곳 이상의 쿼리 격리, `MainHRControl` A/B 이중 리스트, `DeleteAllClear` 격리, B 템플릿 파일.
  → 계획서가 직접 "심각한 버그"라고 짚은 DB 교차 삭제 위험 자체가 없어진다.
- A를 수정하면 B도 자동으로 따라간다. 이중 티칭에 따른 파라미터 불일치도 생기지 않는다.
- §1.4의 좌표 종속 문제가 해소된다(C는 FOV 상대 좌표).

### 2.4 전제 (확인 필요, §5 질문 3)

- 얼라인과 마크를 포함해 B가 A의 순수 180° 회전일 것
- 열 내 스트립 피치와 갭이 일정할 것
- 조명이 비대칭이면 회전한 템플릿의 매칭률이 떨어질 수 있음. 매칭률이 기준 미달이면 B 템플릿만 별도로 티칭하는 폴백을 둔다.

### 2.5 런타임 구조

현재 런타임(레이저, 얼라인, 비전 검사)은 UI 컨트롤 리스트 `workSettingWindow.topHRControl.customMarks`를 직접 참조한다. A/B 분기를 여기저기 `if`로 넣지 말고, 레시피 스냅샷을 도입하는 편이 낫다.

```csharp
class MarkRecipe { List<CustomMark> Custom; List<InterMark> Inter; List<OCRMark> Ocr; List<AlignROI> Align; Mat Tpl1, Tpl2; int BlockBase; }
MarkRecipe GetRecipe(int row, Surface s)   // A: 원본, B: Rotate180(A, C), BlockBase=20
```

모델 로드나 티칭 저장 시점에 만든다. `TopMark`/`BotMark`, `MarkingAlignToROI*`, `CheckMarks*`는 `GetRecipe(i)`만 사용한다.

---

## 3. 얼라인 (`MarkingAlignToROI`) 보완

1. **베이스라인 길이**: `alignDistanceY = StripIter[last] − y12`(`TeachingManagement.cs:242`)는 두 마크가 열 안에서 같은 높이에 있다고 가정한다. A 마크는 열 상단, B 마크는 열 하단에 있으므로 실제 거리는 `StripIter[last] + (ROIcenterY_B − ROIcenterY_A)·res`이다.
   - 4열 보정값 자체는 x12로 맞춰진다. 다만 각도가 과대 추정돼 1~3열 보간 오차가 생긴다. 예: 0.2mm/200mm 기울기, Δ=50mm이면 약 0.05mm. `AlignAlarmThreshold` 0.2와 비교하면 무시하기 어려운 수준이다.
   - 실제 ROI 중심 Y를 반영하도록 수정하는 것을 권장한다.
2. **좌우 짝 맞춤**: 계획서의 X좌표 기준 매칭에 동의한다. 다만 위 2.1 규칙대로 하면 "같은 번호 = 같은 물리 마크"가 유지된다. 따라서 `MarkingAlignToROI`는 번호 그대로(1열 얼라인1 ↔ 4열 얼라인1) 계산해도 물리적으로 맞는 쌍이다.
   - `RightOnly`만 "화면 우측"이 필요하다(A: 2번, B: 1번).
   - 또 `RightOnly`는 `SearchPointAlign2ToROI`(템플릿2)로 고정돼 있으므로, 우측 마크의 번호에 맞는 템플릿을 고르도록 바꿔야 한다.
3. **기존 결함 (참고)**: `lastFoundAlignPos`를 구하는 `SearchPointAlignToROI_GetCenter`는 **템플릿1(`align`)로 얼라인2 ROI를 검색**한다(`TeachingManagement.cs:198`, `MILCL.cs` `GetCenter`는 `align` 사용). 이번에 일반화하면서 의도대로 맞추는 것이 좋다(시프트 검사 기준점).

---

## 4. 기타 (중/소)

| # | 항목 | 의견 |
|---|---|---|
| 4.1 | 범위 | 계획서 매핑표에 `MarkingPLC`/`MarkingTEST`가 들어 있다. MarkingPC만 대상이므로 PLC(`MarkingPLC`, `WorkSettingControl`)는 제외하고, `MachineType != PC` + B 레이아웃이면 시작을 차단하는 가드만 둔다. `MarkingTEST`(TestMode=1)는 하드웨어 없이 검증할 때 유용하므로 ID/ROI 계산만이라도 같이 바꾸는 것을 권장한다. |
| 4.2 | Keyence 180° 블록 | `BlockLayout`의 회전 기준점이 블록 중심인지 시편으로 확인해야 한다(현 코드 `GetCenterX`는 degree를 무시함). degree 허용 범위(0~360 / −180~180)로 정규화하고, 블록 번호 상한도 확인한다. A/B 그룹 Enable 토글 방식 자체는 동의한다. **매 열 MARK 명령에서 모든 블록의 Enable을 명시적으로 보내야 한다**(이전 상태가 남음). 참고로 `TopMark`의 `markPos<2` 경로는 `continue`만 하고 Disable을 보내지 않는 기존 잠재 결함이 있다(MarkingPC에서는 미사용). |
| 4.3 | 비전 디코딩 | ZXing은 `AutoRotate=true`라 180°를 디코딩한다. `Get_Algo_Conv_Square_DataMatrixHJ` 폴백은 B 시료로 따로 확인해야 한다. `DecodeMarkJob`이 `Decode(job.Bmp, side, 0, ...)`로 열 인덱스를 0으로 고정해, 저장 이미지 파일명이 모든 열에서 1열 lot으로 찍힌다(`WorkManagement.cs:2829`). 이번에 같이 수정하는 것을 권장한다. |
| 4.4 | 시작 전 검증 | B 티칭 누락 검사에 더해 다음을 검증한다: 레이아웃 문자열 형식, 1열 = A(AOI 리더와 `SaveWorkHistory`가 0열을 기준으로 함), C 캘리브레이션 완료 여부, `MakringShiftCheck`와 B 동시 사용 금지(1.3). |
| 4.5 | ITS 의미 변화 | 한 사이클 안에서 A열 레코드는 strip S−1, B열 레코드는 strip S를 가리키게 된다(1.1 결과). 첫 스트립(`stripID==2`, RightOnly)과 종료(`isEnd` → `WriteITS`) 처리 시점도 A열/B열이 반 사이클씩 다르다. 하위 시스템(ITS/MES)에서 문제가 없는지 확인해야 한다. |
| 4.6 | UI 표시 | `WorkMarkInfoWindow`와 작업 화면에 열별 A/B 표시를 추가해 작업자가 확인할 수 있게 한다. |

---

## 5. 질문

1. **레이아웃 범위**: 1열은 항상 A인가? `B,A,A,A`나 `A,B,A,B` 같은 배열 가능성도 있는가? (얼라인 기준 열, AOI, `SaveWorkHistory` 0열 가정과 관련)
2. **FOV / 레이저 필드**: 한 열 정지 위치에서 A 마크(열 상단)와 B 마크(열 하단)가 카메라 FOV(4080×3000 px × TopCamResolution)와 레이저 마킹 필드 안에 모두 들어오는가? 계획서의 "추가 Y 이동 없음" 전제가 여기에 달려 있다.
3. **자동 생성 전제**: B가 얼라인 마크까지 포함해 A의 순수 180° 회전이고, 열 안 스트립 피치와 갭이 일정한가? 조명은 동축인가(회전 템플릿의 매칭률)?
4. **ITS/MES**: 한 사이클의 4개 열 레코드가 서로 다른 strip 번호(S−1 / S)를 가져도 되는가? 첫/마지막 스트립 처리 규칙은?
5. **시프트 검사**: 대상 제품에서 `MakringShiftCheck`를 실제로 사용하는가? (사용한다면 1.3이 1차 범위에 들어가야 함)
6. **하부 마킹**: 이 제품은 `MarkingMethod=3`(양면)인가? 하부도 B 회전이 적용되는가?
7. **거버**: 혼합 배열 제품의 거버에서도 `GetStripIteration`이 4열 피치를 정상으로 뽑는가?
