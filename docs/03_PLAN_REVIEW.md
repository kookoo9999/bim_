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

---

## 6. 2차 — 답변 반영 결정사항

| # | 답변 | 결정 / 설계 영향 |
|---|---|---|
| 1 | `AABB`, `BBAA` 등 A/B 임의 조합 | **"1열 = A" 가정 전면 제거.** 레이아웃은 4자리 A/B 문자열(검증: 길이=`StripIter.Count`, 문자 ∈ {A,B}). `refCol(v)` = 해당 Variant 최초 등장 열 (BBAA면 B=0, A=2). A 정의 = **기존 모델이 티칭된 방향(FOV 우상단=얼라인2)** → 기존 데이터 전부 `A`로 하위호환 |
| 2 | FOV에는 한 열만 보임 → A/B 각각 티칭 | §2 자동생성안 **철회**, 계획서의 별도 티칭 방식 채택. 대신 §1.4 결합 문제를 아래 6.1로 해소 |
| 3 | 순수 180° 회전이나 자동생성은 무리 | 별도 티칭 확정. (선택) "A→B 초기값 생성" 버튼 정도만 편의기능으로 고려, 필수 아님 |
| 4 | 첫 사이클 폐기, 사이클 내 ID 동일·끝번호만 변경 | 6.2 참조 — 현행 문자열 규칙 그대로, **열별 좌/우 스왑**만 추가 |
| 5 | 시프트 검사 미사용 | 범위 제외 유지. 단 레이아웃에 B 포함 + `MakringShiftCheck=true`면 **시작 차단** 가드 1줄 |
| 6 | 양면, 하부도 AABB | 상·하부 모두 Variant별 레시피/블록/검사 ROI. `BotMarkY`도 Variant별 |
| 7 | 거버 정상 추출 | `StripIter` 변경 없음 |

### 6.1 Variant별 모션 기준점 (권장)

BBAA처럼 1열이 B일 수 있으므로 "A 저장 시에만 `TopAlignY` 갱신"은 성립하지 않음. 각 Variant가 **자기 모션 기준점을 따로** 갖게 하면 A/B 결합이 사라짐.

```
저장 시:   BaseTopY[v] = 현재 Y − StripIter[refCol(v)]     (BaseBotY[v] 동일)
런타임:    열 i 이동 = BaseTopY[variant(i)] + StripIter[i] (+CamToY)
티칭 가드: |현재 Y − (기존 기준 + StripIter[refCol(v)])| 확인 → 기존 `posY > 90` 대체
```

- A 재티칭이 B에 영향 없음(각 ROI 세트와 그 촬상 위치가 한 쌍으로 저장됨).
- 저장 위치: `marking_info`에 `TopAlignY_B`, `TopMarkY_B`, `BotMarkY_B` 추가(또는 variant 행 분리). 기존 컬럼 = A.
- 티칭창에 "선택 Variant 기준열로 이동" 버튼(`StripIter[refCol(v)]` 반영) 추가.
- 얼라인 기울기 계산은 실제 위치 사용: `ΔY = (BaseTopY[v_last] + StripIter[last] + roiCY_last·res) − (BaseTopY[v_first] + roiCY_first·res)` 기반 (§3.1과 통합).

### 6.2 strip ID 문자열 조합 위치 (현행)

사이클 내 기준 `stripID`(=S)는 4열 공통, 마크 위치가 **우(=이전 스트립)** 이면 끝번호만 −1:

| 함수 | 위치 | 규칙 |
|---|---|---|
| `GetInternal1IDString` | `WorkManagement.cs:2921` | `"사내1 우"` 포함 → S−1 |
| `GetCustomIDString` | `:2986-2989` | `"고객1 우"`/`"고객2 우"` 포함 → S−1 (이후 포맷 조합) |
| `ConvertInternal1ID` (ITS 기록용) | `:2051` | `"사내1 좌, 고객1 우"` → −1, `"사내1 우, 고객1 좌"` → +1 (사내ID를 고객ID와 같은 물리 스트립으로 맞춤) |
| `SaveWorkHistory(string,string)` | `:93` | 위와 동일 규칙으로 DB 이력 stripID 산출 (`Int1_id[0]` 사용 → **0열 Variant 문자열**로 호출해야 함) |
| 미리보기 `GetMarkData` | `MainWindow.xaml.cs:638` | 위 두 함수 호출 |

→ `GetRowMarkingLocation(i)`(B열이면 좌↔우 스왑) 하나로 위 호출부 모두 해결. 사이클 내 S 동일 유지라는 기존 원칙과 일치.

**ITS 결과 (S=10 사이클 예, `"사내1 좌, 고객1 우"` 고객):**

| 열 | 인쇄: 좌(S) / 우(S−1) | ITS 레코드 (lot 파일은 열별 `lots[i]`) |
|---|---|---|
| A | 사내1=0010 / 고객1=0009 | `0009, 0009` (완성된 스트립) |
| B | 고객1=0010 / 사내1=0009 | `0010, 0010` (사내1은 다음 사이클에 인쇄) |

- 첫 사이클: A열 lot 파일엔 미완성 strip 1 레코드가 남고, B열 lot 파일엔 strip 1 레코드 자체가 없음(첫 레코드=strip 2).
- 마지막: A열은 마지막 strip이 미기록, B열은 마지막 strip이 **사내1 미인쇄 상태로 기록**됨.
- 이는 기존 `"사내1 우, 고객1 좌"` 고객 모델에서 이미 발생하는 동작과 동일 → ITS 쪽 수용 여부만 확인 필요.

### 6.3 1열이 B일 때 추가 점검 지점

| 지점 | 근거 | 조치 |
|---|---|---|
| 첫 사이클 RightOnly 얼라인 | `TeachingManagement.cs:290` (`number==2` 고정) | 0열/말열 Variant의 **화면 우측** 얼라인 번호·템플릿 선택 (A: 2번, B: 1번) |
| 피딩 보정 | `WorkManagement.cs:1456` `alignOffset[0].X` | 변경 불필요(변위값) |
| 이력 저장 | `SaveWorkHistory(Int1_id[0], …)` | 0열 Variant 문자열 사용 |
| AOI ID 리더 | `WorkManagement.cs:1173` `IDReaders[0].ReadID()` | **질문 A** |
| 레이저 프로젝트 로드 | `LoadProjectTop/Bot` | A/B 블록군 모두 등록(1열 Variant와 무관) |

### 6.4 추가 질문

- **A. AOI ID 리더**(`UseAOIMark`): 리더가 읽는 AOI 마크가 특정 열/위치 고정인가? 1열이 B가 되면 마크가 180° 반대편으로 가서 리더 시야를 벗어나는지?
- **B. ITS**: 6.2 표의 B열 동작(레코드 strip 번호가 A열보다 1 큼, 마지막 strip 미완성 기록)이 ITS/MES 측에서 문제 없는지? 기존 `"사내1 우, 고객1 좌"` 고객 모델 운영 실적이 있으면 그걸로 갈음 가능.
- **C. 티칭 정지 위치**: A/B 티칭 시 스테이지 Y가 열 피치만큼만 다르고 동일한 상대 위치인지(6.1 Variant별 기준점으로 가도 될지, 아니면 공통 기준점 + B 위치 잠금을 선호하는지)?
- **D. 레이저 블록 상한**: Keyence 모델의 프로그램당 블록 수 상한 (A/B 블록군 오프셋 20 사용 가능 여부) — 현재 `index`는 얼라인 ROI까지 포함해 매겨짐.

---

## 7. 3차 — 답변 반영

| # | 답변 | 결정 |
|---|---|---|
| A | AOI는 이번 범위에서 제외 (180° 회전 시 사이클 간 피딩 간격이 제품 길이와 달라짐) | 레이아웃에 B가 있고 `UseAOIMark=true`이면 **작업 시작 차단** (시프트 검사 가드와 같은 위치) |
| B | 화면 좌/우와 스트립 끝단의 관계는 엔지니어 확인 필요 | 확인용 그림: [`images/ab_stripid_its.png`](images/ab_stripid_its.png) |
| C | 마킹 품질이 기존과 같아야 함 | 7.1 참조 → **공통 기준점 + B 티칭 위치 잠금 + 기준점 변경 감지** |
| D | 블록 오프셋 20이면 충분 | 계획서대로 A=`index`, B=`20+index` |

### 7.1 기준점 방식: 품질 관점

- 레이저 위치 = ROI 중심 오프셋 + `CamToX/Y` + `alignOffset[i]` + `motionOffset[i]`. 두 방식 모두 이 계산은 같다. 차이는 ROI를 찍을 때의 스테이지 위치와 런타임 위치를 어떻게 일치시키느냐뿐이다.
- **공통 기준점 + 위치 잠금**: B를 `기준점 + StripIter[refCol]`에서 티칭하고, 런타임에는 `기준점 + StripIter[i]`로 이동한다. 이는 **현재 2~4열이 마킹되는 방식(1열 기준 + StripIter)과 똑같다.** 따라서 품질이 기존과 같다는 것이 구조적으로 보장된다.
- Variant별 기준점: 품질은 같지만 이동 경로가 새로 생긴다. 검증 부담이 늘어나는 데 비해 얻는 것이 적다.
- **채택안**
  1. 기준점(`TopAlignY`/`TopMarkY`/`BotMarkY`)은 **1열 디자인을 티칭할 때만** 갱신한다. BBAA라면 B가 기준점을 소유한다.
  2. 다른 디자인을 티칭할 때는 스테이지를 `기준점 + StripIter[refCol]`로 자동 이동하고, Y 조작과 모션 좌표 저장을 막는다. 하부도 같다.
  3. 다른 디자인을 저장할 때 그 시점의 기준점 값을 함께 기록한다. 이후 1열 디자인을 다시 티칭해서 기준점이 바뀌면 시작을 차단하고 "B(또는 A) 재티칭 필요"를 알린다.
  4. 티칭 순서 제약: 1열 디자인을 먼저 티칭해야 한다.

---

## 8. 4차 — 답변 반영

### 8.1 결정사항

| # | 답변 | 결정 |
|---|---|---|
| 1 | 정방향/역방향 | UI 명칭은 **정방향 / 역방향**. 기존 모델 데이터 = 정방향. (이하 A=정, B=역) |
| 2 | lot마다 변경 가능해야 함 | 배열은 모델이 아니라 **lot 단위**로 정함 → `LotWindow`에서 선택(8.2) |
| 3 | 각도 = ROI 옵션의 각도(`cbDegree`) | 역방향 티칭 화면에 **자동 보정 버튼** 배치(8.3) |
| 4 | Keyence 180° 지원 여부 미확인 | 질문 재정리(8.4) |

### 8.2 "모델관리에 이미 있지 않나?" → 없음

모델관리의 **"Strip 열 배열"(`rbStripUP`/`rbStripDown` → `MarkingDirection`)은 다른 기능**이다. 물리 열과 lot 입력칸(A~D)·ID `{ROW}` 문자의 순서(A→D / D→A)를 정한다(`LotWindow.xaml.cs:127-140`, `WorkManagement.GetStringRow`). 정/역(180°) 배치를 표현하는 설정은 현재 없다.

lot 단위 선택 설계:
- `LotWindow`의 `tbLotA~D` 옆에 열별 **정/역 토글**을 둔다. 내부 저장은 lot 입력값과 같은 `MarkingDirection` 매핑을 거쳐 **물리 열 인덱스 기준** 배열 `orient[0..3]`로 한다. 그대로 저장하면 D→A 모델에서 정/역이 뒤집힌다.
- 역방향 레시피가 티칭되지 않은 모델이면 역 토글을 비활성화한다.
- 같은 lot을 이어서 작업할 때 배열이 바뀌지 않도록 `work_history`(lot, model)에 배열을 저장하고, 재시작 시 복원·비교한다.

### 8.3 기준점 규칙 수정 (lot마다 배열이 바뀌는 경우)

ROI는 FOV 좌표이고 런타임은 `기준점 + StripIter[i]`로 이동하므로, 한 번 티칭한 레시피는 **어느 열에서든** 그대로 쓸 수 있다(현재 2~4열과 같은 원리). 따라서 기준점은 "1열 디자인"이 아니라 **정방향 소유**로 고정한다.

- **정방향 저장**: 현재 티칭 중인 열을 k라 할 때 `기준점 = 현재Y − StripIter[k]`. 정방향이 1열이 아니어도 된다. 기존 `posY > 90` 가드는 `현재Y − StripIter[k]` 기준으로 바꾼다.
- **역방향 저장**: Y를 `기준점 + StripIter[k]`로 잠그고 모션 좌표는 저장하지 않는다. 저장 시점의 기준점을 함께 기록하고, 이후 정방향 재티칭으로 기준점이 바뀌면 "역방향 재티칭 필요"로 표시하고 작업을 차단한다.
- **역방향 자동 보정 버튼**: 같은 `reference`(고객1/사내1…)의 정방향 마크에서 크기·포맷·파워·스피드·flip을 복사한다. 각도는 +180°(범위 정규화), 좌/우(`markPos`)는 반전한다. **작업자는 ROI 위치만 잡으면 된다.**

### 8.4 Keyence 질문 정리

180° 회전 자체는 지원된다. 현재 코드가 이미 `BlockLayout`에 각도를 보내고 있다(`KeyenceTCP.cs` `TEXT_BLK`/`BARCODE_BLK`).

확인할 것은 **회전 기준점**이다. 위치는 ROI 중심으로 보내는데(`GetCenterX/Y`는 각도를 무시함), Keyence가 블록을 **중심 기준**으로 돌리면 문제가 없다. **모서리 기준**으로 돌리면 역방향 마크가 마크 크기만큼 밀린다.
- 기존 모델 중 각도 ≠ 0(예: 90°)으로 운영 중인 것이 있으면 이미 검증된 것으로 본다.
- 없으면 시편 1회로 확인한다: 같은 좌표에 0°/180° 바코드를 각각 마킹하고 위치를 비교한다.

### 8.5 기존 모델 재사용 vs 별도 모델 → **기존 모델에 역방향 추가 권장**

| | 기존 모델에 역방향 추가 | 정/역 배열마다 별도 모델 |
|---|---|---|
| 기존 레시피 | **그대로 사용**(정방향 = 기존 데이터, 재티칭 불필요) | 정방향을 다시 티칭하거나 복사 |
| lot별 배열 변경 | 자연스럽게 가능 | 배열마다 모델을 바꿔야 함 → 2번 답과 충돌 |
| 유지보수 | 레시피 1벌 | 같은 제품 레시피가 2벌로 갈라짐 |
| 위험 | 역방향 티칭이 정방향 데이터를 건드릴 가능성 | 낮음 |

- 추천 이유: 모든 제품이 대칭 배열로 바뀌어도 **기존 레시피는 정방향으로 남고 역방향만 추가**하면 된다.
- 위험 대응
  - 모든 `para_*` 쿼리에 `variant_key`를 넣어 격리한다(계획서 §2.2).
  - 역방향 저장 시 정방향 모션 좌표, `Align.bmp`, `Align2.bmp`는 쓰기 금지한다.
  - 역방향을 처음 저장하기 전에 모델(DB 행 + 모델 폴더)을 자동 백업한다.
  - 정방향만 있는 모델은 정/정/정/정 lot에서 기존과 똑같이 동작한다(역 블록은 레이저에 등록만 되고 Disable).

### 8.6 참고 — 기존 버그 (이번 범위 밖)

`ModelManager.RestoreSelectedModel`(`ModelManager.xaml.cs:114-115`)이 `MarkingDirection`에 `MarkingLeft` 값을 덮어쓰고 `MarkingLeft`는 읽지 않는다(`LoadModel` `:188-189`는 정상). 모델관리에서 수정을 **취소**한 뒤 프로그램을 재시작하지 않고 작업하면, lot 입력칸과 열 매핑(A→D/D→A)이 뒤집힐 수 있다.

---

## 9. 5차 — 답변 반영

| # | 답변 | 결정 |
|---|---|---|
| 1 | lot 입력창에 정/역 토글 | `LotWindow`에 열별 정/역 토글 추가 (8.2 매핑 규칙 적용) |
| 2 | ITS/작업이력에는 남기지 않음 | lot별 배열은 **로그 파일에만** 기록 (작업 시작 로그 `LogMarkData` 위치). 따라서 8.2의 "work_history 저장/복원"은 철회. 같은 lot 재시작 시에는 작업자가 다시 선택 |
| 3 | 그림 v1의 S / S−1 이해 어려움 (시작 2 → 좌 1, 우 2로 알고 있음) | 현재 코드 규칙은 **화면 '우' 마크 = 입력 stripID − 1**(좌 2, 우 1). ~~확인용 그림 v2~~ (§10에서 방향 정정, 삭제). 화면이 실물 대비 좌우 반전인지(가), 실물에서도 우측이 1번인지(나)만 확인하면 됨. **어느 쪽이든 역방향 설계(B열 = 위치 문자열 좌/우 스왑)는 변하지 않음** |

---

## 10. 6차 — 피딩 방향 확정 및 정정

**확정 사항**: 화면에서 피딩은 **우→좌**로 진행하고, 우측으로 갈수록 번호가 크다.
- 화면 좌측 = 이전 스트립(S−1), 화면 우측 = 신규 스트립(S).
- 시작 2를 입력하면 첫 사이클에 좌측의 1번째 스트립이 −1로 1을 받는다. 한쪽 끝만 마킹되므로 폐기한다.
- 2번째 스트립은 사이클 1에 우측에서 2를 받고, 사이클 2에 좌측에서 3−1=2를 받는다. 양 끝이 같은 번호이므로 양품이다.

**정정 1**: §1.1 표와 그림 v1의 "FOV 좌 = stripID, 우 = stripID−1"은 반대였다. 코드는 화면 위치를 보지 않는다. 고객사 위치 문자열에서 **'우'로 적힌 마크에 −1**을 할 뿐이며, 이 '우'가 실제로는 **화면 좌측(이전 스트립)에 있는 마크**에 해당한다. 문자열의 좌/우는 화면 기준이 아니라 제품(스트립 끝단) 기준이다. 그림은 v3로 교체했다: [`images/ab_stripid_its.png`](images/ab_stripid_its.png).

**설계 결론은 변하지 않는다**
- 화면의 같은 쪽은 4개 열 모두 같은 물리 스트립이다.
- 역방향 열에서는 그 자리에 오는 마크의 종류만 바뀐다(정: 사내1 ↔ 역: 고객1).
- 따라서 **역방향 열은 위치 문자열의 좌/우를 스왑**하는 방식(`GetRowMarkingLocation`)이 그대로 맞다.

**정정 2**: ITS 비대칭은 열 간에 방향만 바뀐다. 사진2 배치와 정방향 설정 "사내1 우, 고객1 좌"를 가정하면 다음과 같다.
- 정방향 열: 고객1만 인쇄된 시점에 strip S로 기록한다.
- 역방향 열: 완성 시점에 strip S−1로 기록한다.

**정정 3**: 티칭 다이얼로그의 좌/우 라디오(`markPos`)는 문자열에서 자동으로 채워지므로 화면 위치와 반대로 표시될 수 있다. MarkingPC에서는 `markPos`를 사용하지 않으므로(`BothMarking` 기본값 2) 동작에는 영향이 없다. 다만 역방향 티칭 시 혼동을 막기 위해 라벨을 "−1 대상(이전 스트립)"처럼 의미 기준으로 바꾸는 것을 검토한다.

---

## 11. 7차 — 고객사 위치 문자열 확정 및 계획서 v2 반영

- 고객사 정보창(Ctrl+I → 비밀번호 → `CustomInfoWindow`)의 "2D 위치" 콤보박스(사내1/고객1/고객2 좌·우 8개 조합)로 고객사별 사양을 정한다. **역방향 열도 이 값을 기준으로 좌/우를 모두 맞바꿔 계산**하도록 확정했다.
- 지금까지의 결정사항을 `02_REFACTOR_PLAN.md` **v2**로 다시 썼다.
- 미결 항목은 ITS 수용 여부, Keyence 180° 회전 기준점 두 가지다.
