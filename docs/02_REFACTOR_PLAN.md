# 02_REFACTOR_PLAN.md — 4열 이종 제품 배치(`A,A,A,B | A,A,B,B | A,B,B,B`) 대응 리팩토링 아키텍처 설계서

> **설계 목적**: 기존 4열(1~4열) 마킹 공정은 모든 열이 동일한 정방향 제품 디자인(`A,A,A,A`)임을 전제하고 설계되어 있다. 향후 4열 내 제품 디자인(배치)이 `A,A,A,B`, `A,A,B,B`, `A,B,B,B` 등 **정방향(`A`)과 180° 회전 대칭(`B`)의 열별 이종 조합**으로 구성되더라도, 기존과 동일한 정밀도·Tact Time·안정성으로 얼라인, 레이저 마킹, 비전 검증, 이력 관리가 동작하도록 하는 아키텍처 리팩토링 설계를 정의한다.
> **기준 문서**: [`docs/01-inventory.md`](file:///d:/BGA_ID_MARKING/docs/01-inventory.md) 및 `D:\BGA_ID_MARKING\HDS_ID_Marker` 코드베이스 (읽기 전용 분석 기반).
> **범위 제외(Out-of-Scope) 참고**:
> 1. 마킹 시프트 검사(`CheckMarkShift` / `UpdateTopMarkInfo`)의 A·B 개별 대응은 현 단계에서 제외하며, 추후 요청 시 대응한다.
> 2. 티칭창 내 미사용 단발 마킹/리딩 테스트 기능(`BtnCustomMarking_Click`, `BtnInterMarking_Click`, `BtnOCRMarking_Click`, `BtnCustomMarkRead_Click`, `BtnInterMarkRead_Click`)은 현 단계에서 제외한다.

---

## 1. 제품 기하 구조 및 아키텍처 핵심 개념 (Architectural Vision)

### 1.1 `A`(정방향) vs `B`(180° 회전 대칭) 기하학적 배치 정의
현장 작업 기준 `A`와 `B`는 동일한 제품의 **정방향(`0°`)** 과 **180° 회전(`180°`)** 관계이며, 시작 위치는 기존과 동일하게 제품 경계(Boundary)에서 시작한다. 한 카메라 FOV(티칭 캔버스) 내에서의 기하학적 배치는 다음과 같다:

```
+-----------------------------------------------------------------------+
|                       카메라 FOV (티칭 캔버스)                        |
|                                                                       |
|  [좌상단 — A 구역 (좌)]                 [우상단 — A 구역 (우)]        |
|   ┌──────────────┐                       ┌──────────────┐             |
|   │ Align2_A     │                       │ Align1_A     │             |
|   └──────────────┘                       └──────────────┘             |
|   ┌──────────────┐                       ┌──────────────┐             |
|   │ 고객사 마킹A │                       │ 사내 마킹A   │             |
|   └──────────────┘                       └──────────────┘             |
|                                                                       |
| - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - |
|                                                                       |
|   ┌──────────────┐                       ┌──────────────┐             |
|   │ 사내 마킹B   │                       │ 고객사 마킹B │             |
|   │ (180° 회전)  │                       │ (180° 회전)  │             |
|   └──────────────┘                       └──────────────┘             |
|   ┌──────────────┐                       ┌──────────────┐             |
|   │ Align1_B     │                       │ Align2_B     │             |
|   └──────────────┘                       └──────────────┘             |
|  [좌하단 — B 구역 (좌)]                 [우하단 — B 구역 (우)]        |
+-----------------------------------------------------------------------+
```

* **`A` 디자인 (정방향, 예: `A,A,B,B`의 1~2열)**:
  * **좌상단**: `Align2_A` (얼라인마크2) + 그 **아래**에 `고객사 마킹(고객1)` (`좌`, `markPos = 1`)
  * **우상단**: `Align1_A` (얼라인마크1) + 그 **아래**에 `사내 마킹(사내1)` (`우`, `markPos = 0`)
* **`B` 디자인 (180° 회전, 예: `A,A,B,B`의 3~4열)**:
  * **좌하단**: `Align1_B` (얼라인마크1) + 그 **위**에 `사내 마킹(사내1)` (`좌`, `markPos = 1`, 각도 180° 회전)
  * **우하단**: `Align2_B` (얼라인마크2) + 그 **위**에 `고객사 마킹(고객1)` (`우`, `markPos = 0`, 각도 180° 회전)
* **핵심 이점 (중간 피딩 불필요 — 1-Pass 사이클 유지)**:
  * `A`(상단)와 `B`(하단) 모두 동일 제품 경계선의 카메라 FOV 및 레이저 마킹 필드 안에 들어오므로, **1~2열 마킹 후 별도의 중간 피딩(X축 이동) 없이** 기존과 동일하게 Y축 스테이지(`TopMarkY + StripIter[0..3]`)만 이동하여 1~4열을 한 번에 마킹·검사할 수 있다.

---

### 1.2 핵심 도메인 분리: `PanelModel` vs `DesignVariant(A/B)` vs `ColumnLayout(1~4열)`
열(Column Index: 0~3)마다 매번 4번씩 중복 티칭하는 구조를 피하고, **디자인 유형(`VariantKey`: `'A'`, `'B'`) 단위로 티칭 레시피를 정의한 뒤, 4열 슬롯(`ColumnLayout`: `"A,A,A,B" | "A,A,B,B" | "A,B,B,B"`)에 매핑하는 2계층 아키텍처**로 분리한다.

```
[Model (패널 작업 단위)]
 ├── Panel 공통 설정: Name, PanelLength, GerberPath, StripIter[0..3], TopAlignX/Y, TopMarkY, BotMarkY
 ├── ColumnLayout: string ("A,A,A,A" | "A,A,A,B" | "A,A,B,B" | "A,B,B,B")
 └── DesignVariants: Dictionary<string, DesignVariantRecipe> ("A", "B")
      ├── VariantKey ("A" | "B")
      ├── ReferenceColumnIndex ("A" -> 0(1열), "B" -> ColumnLayout에서 'B'가 최초 등장하는 열 인덱스)
      ├── TopTeachingRecipe (customMarks, interMarks, ocrMarks, alignROIs, AlignTemplate1/2)
      ├── BotTeachingRecipe (customMarks, interMarks, ocrMarks)
      └── LaserBlockIndexOffset ("A": 0..19, "B": 20..39)
```

---

## 2. 핵심 리팩토링 포인트 및 코드 근거 (`파일:라인`)

작업자가 `ModelManager`에서 배치 조합을 선택하고, `WorkSettingWindow`에서 `A`(상단 배치, 0°)와 `B`(하단 배치, 180°)를 각각 티칭한 뒤 자동 운전을 돌릴 때, 수정되어야 할 핵심 지점과 설계 원리는 다음과 같다.

### 2.1 모델 관리창(`ModelManager`)의 열 배치 조합(`ColumnLayout`) 선택 및 모델 복사/이름변경/삭제 연동
* **현황 및 근거**:
  * `ModelManager.xaml` (`HDS_ID_Marker\ModelManager.xaml:160-330`)의 `pnlModelSpec` 그리드에는 열별 디자인 조합을 선택하는 UI가 없다.
  * `ModelManager.xaml.cs`의 `LoadModel` (`150-200`), `CreateModelInfo` (`1171-1223`), `ModifyModelInfo` (`1124-1170`)는 `idmarker.model_info` 테이블에서 기본 모델 정보만 읽고 쓴다.
  * 또한 `DeleteModel` (`894-945`)과 `RenameModel` (`982-1017`)은 `model_info`와 `marking_info`만 처리하며, `SetMarkData` (`709-731`)는 `Align.bmp`, `Align2.bmp` 1세트만 비전 메모리에 로드한다.
* **해결 설계**:
  1. `ModelManager.xaml`의 `pnlModelSpec` (`Grid.Row="8"` 또는 `Grid.Row="12"`)에 **`열 디자인 배치` ComboBox (`cbColumnLayout`)** 를 추가한다 (항목: `"A,A,A,A"`, `"A,A,A,B"`, `"A,A,B,B"`, `"A,B,B,B"`).
  2. `LoadModel`, `RestoreSelectedModel`, `CreateModelInfo`, `ModifyModelInfo` 쿼리에 `column_layout` 컬럼을 추가한다.
  3. `SetMarkData` (`709-731`)에서 `Align.bmp`/`Align2.bmp`와 함께 `Align_B.bmp`/`Align2_B.bmp`가 존재할 경우 비전(`Visions[0]`)의 `B` 템플릿 슬롯에 함께 로드한다.

### 2.2 티칭창(`WorkSettingWindow`, `MainHRControl`)의 `A / B` 전환, 인메모리 이중 유지 및 DB 격리
* **현황 및 근거**:
  * **인메모리 덮어쓰기 위험**: 현재 `LaserMarkingManagement` (`558, 985, 1259`), `WorkManagement` (`2416, 2527, 2716`), `TeachingManagement` (`188-189`)는 모두 `ptrMain.workSettingWindow.topHRControl.customMarks` 리스트를 직접 참조한다. 만약 `MainHRControl`이 현재 화면에 선택된 라디오버튼(`A` 또는 `B`)의 리스트 1개만 메모리에 들고 있으면, 작업자가 티칭창에서 `B`를 눌러놓은 채 창을 닫고 자동 운전을 시작했을 때 `A` 데이터가 메모리에 없는 문제가 발생한다.
  * **DB 삭제/인덱스 갱신 충돌 위험**: `MainHRControl.DeleteByIndex` (`MainHRControl.xaml.cs:1487-1526`) 및 `UpdateIndex` (`1527-1566`), `Delete` (`1439-1486`)는 `WHERE orientation = '{0}' AND model = '{1}' AND index = '{2}'` 조건만 사용한다. 만약 여기에 `AND variant_key = '{currentVariant}'` 조건이 빠지면, **`B` 티칭 화면에서 ROI를 삭제·수정할 때 `A` 티칭의 동일 `index` DB 레코드까지 함께 삭제·변조되는 심각한 버그**가 발생한다.
  * **전체 초기화(`DeleteAllClear`, `WorkSettingWindow.xaml.cs:1303-1372`)**: 현재 모델의 모든 ROI를 일괄 삭제하므로, 현재 선택된 Variant(`A` 또는 `B`)만 초기화하도록 격리가 필요하다.
* **해결 설계**:
  1. **인메모리 `A` / `B` 레시피 상시 보유**:
     * `MainHRControl` (또는 `Model.Variants`) 내부에 `A`용 리스트(`customMarks_A`, `interMarks_A`, `ocrMarks_A`, `alignROIs_A`)와 `B`용 리스트(`customMarks_B`, `interMarks_B`, `ocrMarks_B`, `alignROIs_B`)를 **항상 동시에 메모리에 로드·유지**하고, `GetCustomMarks(string variantKey)` 등의 접근자를 제공한다.
     * 티칭 캔버스(`drawingCanvas`)는 현재 라디오버튼(`rbVariantA` / `rbVariantB`)으로 선택된 Variant의 리스트만 화면에 렌더링한다. 이로써 티칭창을 어떤 상태에서 닫든 런타임에는 `A`와 `B` 데이터가 모두 메모리에 보장된다.
  2. **`MainHRControl`의 모든 DB 쿼리에 `variant_key` 조건 강제**:
     * `CreateObjects` (`1780`), `Save` (`1898`), `Modify` (`1957`), `isExist` (`2022`), `Delete` (`1439`), `DeleteByIndex` (`1487`), `UpdateIndex` (`1527`), `GetRemainROICount` (`1398`) 및 `WorkSettingWindow.DeleteAllClear` (`1303`)의 모든 SQL `WHERE` 절과 `INSERT` 절에 `variant_key = '{m_variantKey}'`를 포함하여 `A`와 `B` 데이터를 완전히 격리한다.
  3. **티칭 저장(`posY > 90` 가드) 및 독립 위치 이동 메서드 분리**:
     * **`A` 티칭 저장 시 (`refCol = 0`, 1열)**: 기존과 동일하게 `posY <= 90`에서 1열 모션 기준 좌표(`TopAlignX`, `TopAlignY`, `TopMarkY`, `BotMarkY`)와 `A`의 ROI 및 `Align.bmp`, `Align2.bmp`를 저장한다.
     * **`B` 티칭 저장 시 (`refCol = B 최초 등장 열`)**: 1열 모션 기준 좌표(`TopAlignY`, `TopMarkY`, `BotMarkY`)는 **덮어쓰지 않고 유지**하며, `B`의 ROI와 `Align_B.bmp`, `Align2_B.bmp`만 저장한다.
     * **위치 이동 메서드 분리**: `MoveToAlignPos(string variantKey)`, `MoveToCamPos(string mode, string variantKey)`, `MoveToLaserMarkPos(string mode, string variantKey)`를 UI 선택 이벤트와 분리하여 구현한다.

### 2.3 1열(`A`, 상단)과 4열(`B`, 하단) 얼라인 시 좌/우 짝 맞춤 및 기울기(`radian`) 산출
* **현황 및 근거**:
  * `TeachingManagement.MarkingAlignToROI` (`HDS_ID_Marker\ManagementClass\TeachingManagement.cs:178-275`)는 1열(`TopAlignY`)에서 `align`(`number == 1`)과 `align2`(`number == 2`)를 찾고, 4열(`TopAlignY + StripIter[3]`)로 이동하여 동일한 ROI로 재탐색한 뒤 `alignDistanceX = x12 - x11`, `alignDistanceY = StripIter[3] - y12`로 `radian`을 계산한다(`242-249`).
  * `MarkingAlignToROI_RightOnly` (`TeachingManagement.cs:277-351`, 첫 스트립 `stripID == 2`일 때 호출)는 화면 우측 마크 1개만 사용하여 1열과 4열의 편차를 비교한다(`290`).
* **해결 설계**:
  1. `BitmapHelper.SearchAlign` (`Common\Algo.cs:94-118`)은 각 `roi` 박스 중심을 `(0, 0)` 원점으로 하여 현재 피듀셜의 밀림량 `(x, y)` (단위: mm)을 반환한다.
  2. 따라서 1열(`col = 0`)에서는 1열 Variant(`GetVariantKey(0)`)의 `alignROIs` 및 템플릿으로 `(x11, y11)`을 측정하고, 4열(`col = 3`)에서는 4열 Variant(`GetVariantKey(3)`)의 `alignROIs` 및 템플릿(`Align_B.bmp`, `Align2_B.bmp`)으로 `(x12, y12)`를 측정한다.
  3. **좌/우 피듀셜 짝 맞춤**:
     * `A`에서는 좌상단이 `얼라인2`, 우상단이 `얼라인1`이고, 180° 회전된 `B`에서는 좌하단이 `얼라인1`, 우하단이 `얼라인2`이다.
     * 따라서 각 Variant의 `alignROIs`에서 **캔버스 X좌표(`roi`의 `Left`)가 작은 쪽을 `LeftAlign`, 큰 쪽을 `RightAlign`으로 매칭**(또는 번호 매핑)하여:
       * `MarkingAlignToROI`는 1열의 `RightAlign`(또는 `LeftAlign`)과 4열의 `RightAlign`(또는 `LeftAlign`)끼리 `(x12 - x11)`을 계산하고,
       * `MarkingAlignToROI_RightOnly`는 1열과 4열 모두 항상 화면 우측(`RightAlign`) 피듀셜과 그에 대응하는 템플릿을 탐색하도록 보장한다.

### 2.4 180° 회전에 따른 레이저 블록 각도(`degree`) 차이 및 무손실 스위칭
* **현황 및 근거**:
  * `A`와 `B`는 마크 위치(`roi`)뿐만 아니라 **180° 회전에 의해 인쇄 각도(`degree`, 예: `A`가 `0°`이면 `B`는 `180°`, `A`가 `-90°`이면 `B`는 `90°`)** 가 다르다.
  * Keyence 레이저 프로토콜(`DCS\Laser\KeyenceTCP.cs:70, 93`)에서 블록 각도(`BlockLayout`)는 프로그램 초기 로드 시점(`LoadProjectTop`/`LoadProjectBot` in `LaserMarkingManagement.cs:552-669`)에만 설정되며, 런타임 마킹 명령 `KeyenceProtocol.MARK` (`KeyenceTCP.cs:110-121`)로는 문자열과 `(X, Y, Z)` 위치 및 `MarkingEnable`만 제어할 수 있다.
* **해결 설계**:
  1. `LoadProjectTop` / `LoadProjectBot` (`LaserMarkingManagement.cs:552-669`)에서 단일 Keyence 프로그램(`ProgramNo=0`) 내에:
     * **Variant `A` 블록군**: 블록 번호 `0 ~ 19` (`hwBlockIndex = block.index`, 정방향 `degree`)
     * **Variant `B` 블록군**: 블록 번호 `20 ~ 39` (`hwBlockIndex = 20 + block.index`, 180° 회전 `degree`)
     를 한꺼번에 등록한다.
  2. 런타임 `TopMark` (`LaserMarkingManagement.cs:939-1072`) 및 `BotMark` (`1212-1342`)에서 열 `colStrip` 마킹 시:
     * `colStrip`이 `A` 열이면 `A` 블록군(`0..19`)에 **`A`의 `roi` 좌표**와 `MarkingEnable=2,1`(인쇄)을 전송하고, `B` 블록군(`20..39`)은 `MarkingEnable=2,0`(미인쇄)으로 비활성화한다.
     * `colStrip`이 `B` 열이면 `B` 블록군(`20..39`)에 **`B`의 `roi` 좌표**와 `MarkingEnable=2,1`(인쇄)을 전송하고, `A` 블록군(`0..19`)은 `MarkingEnable=2,0`(미인쇄)으로 비활성화한다.
  3. **효과**: 마킹 루프 도중 레이저 프로그램을 재로드할 필요가 전혀 없어 **Tact Time 지연이 0ms**이며, 180° 회전된 각도와 위치로 정확히 마킹된다.

### 2.5 마킹 후 비전 바코드 인식(`CheckMarks2`, `CheckMarksParallel`)
* **현황 및 근거**:
  * `CheckMarks2` (`WorkManagement.cs:2378-2625`) 및 `CheckMarksParallel` (`2658-2813`)은 모든 열 `i = 3..0`에서 무조건 단일 `topHRControl.customMarks[0].roi` (즉 `A`의 좌상단 ROI)만 잘라내어(`GetTopMarkBmpToROI(roi)`, `2437, 2738`) DataMatrix 디코딩을 수행한다.
  * 또한 하부(BOT) 디코딩 시에도 `topHRControl.customMarks[0].format`을 참조하는 하드코딩이 존재한다(`2545, 2783`).
* **해결 설계**:
  * `CheckMarks`, `CheckMarks2`, `CheckMarksParallel`에서 열 `i`의 Variant(`var vKey = modelInformation.GetVariantKey(i)`)를 조회하여, `A` 열은 `A`의 `customMarks[0].roi`(좌상단), `B` 열은 `B`의 `customMarks[0].roi`(우하단) 영역을 크롭하여 디코딩한다.
  * 하부(BOT) 디코딩 시에도 해당 Variant의 하부 `format`을 참조하도록 수정한다.

### 2.6 `A`(고객=좌, 사내=우)와 `B`(사내=좌, 고객=우)의 좌/우 반전에 따른 `stripID` 계산 및 작업 시작 전 검증
* **현황 및 근거**:
  * 카메라는 제품 경계부를 보며 화면 좌측(`좌`, `markPos = 1`)은 `stripID`번 제품, 화면 우측(`우`, `markPos = 0`)은 `stripID - 1`번 제품이다.
  * 현행 `WorkManagement.cs:1227-1232`는 단일 `customerInformation.MarkingLocation` 문자열을 1~4열 모두에 똑같이 전달하여 `GetInternal1IDString` (`2924`), `GetCustomIDString` (`2988`), `ConvertInternal1ID` (`2060-2061`)에서 `우` 위치 마크에 `stripID - 1`을 적용한다.
  * 그러나 **`A`(1~2열)는 `고객1=좌(좌상단), 사내1=우(우상단)`** 인 반면, **180° 회전된 `B`(3~4열)는 `사내1=좌(좌하단), 고객1=우(우하단)`** 로 화면 기준 좌/우가 반전된다.
* **해결 설계**:
  1. 열 `i`의 ID 문자열(`Cust1_id[i]`, `Cust2_id[i]`, `Int1_id[i]`) 생성 및 `ConvertInternal1ID` 수행 시, 모델 공통 문자열 대신 **해당 열(`i`)의 Variant(`A` 또는 `B`)에 티칭된 각 마크(`고객1`, `고객2`, `사내1`)의 좌/우 설정값(`markPos`: `1=좌`, `0=우`)** 을 참조하여 화면 우측(`markPos == 0`)에 위치한 마크에 `stripID - 1`을 적용한다.
  2. **작업 시작 전 `B` 티칭 누락 검증 (Safety Guard)**:
     * `ColumnLayout`에 `"B"`가 포함된 모델(`A,A,A,B`, `A,A,B,B`, `A,B,B,B`)로 자동 마킹(`RUNMARKING`)을 시작할 때, 만약 `B`의 얼라인 ROI(`Align1_B`, `Align2_B`), 템플릿 파일(`Align_B.bmp`, `Align2_B.bmp`), 또는 마킹 블록이 티칭되어 있지 않다면 즉시 `"Design B 티칭 데이터가 누락되었습니다. 티칭을 확인하세요."` 경고를 띄우고 시작을 차단하여 미마킹 사고를 예방한다.

---

## 3. 데이터베이스 및 파일별 상세 변경 명세

### 3.1 DB 스키마 확장 (`idmarker` 스키마 — 하위 호환 100% 유지)
1. **`idmarker.model_info`**:
   * `column_layout VARCHAR(32) NOT NULL DEFAULT 'A,A,A,A'` 컬럼 추가 (`"A,A,A,A"`, `"A,A,A,B"`, `"A,A,B,B"`, `"A,B,B,B"`).
2. **`idmarker.para_custommark`, `para_intermark`, `para_ocrmark`, `para_alignroi`**:
   * 각 테이블에 `variant_key VARCHAR(8) NOT NULL DEFAULT 'A'` 컬럼 추가.
   * 기존 모든 모델 데이터는 자동으로 `variant_key = 'A'`, `column_layout = 'A,A,A,A'`로 동작하여 기존 모델 재티칭 불필요.
3. **얼라인 템플릿 파일 (`ModelPath` 폴더)**:
   * Variant `A`: 기존 `Align.bmp` (Align1), `Align2.bmp` (Align2)
   * Variant `B`: `Align_B.bmp` (Align1_B), `Align2_B.bmp` (Align2_B)

### 3.2 대상 파일 및 메서드별 리팩토링 매핑표

| 파일 경로 | 대상 클래스 / 메서드 (`라인`) | 리팩토링 상세 내용 |
|---|---|---|
| [`Data\ModelInformation.cs`](file:///d:/BGA_ID_MARKING/HDS_ID_Marker/HDS_ID_Marker/Data/ModelInformation.cs#L8-L115) | `ModelInformation` (`8-115`) | `ColumnLayout` (`"A,A,A,A"`, `"A,A,B,B"` 등) 프로퍼티, 열별 Variant 파서 `GetVariantKey(int col)` 및 `GetReferenceCol(string variantKey)` 추가 |
| [`Data\MarkInformation.cs`](file:///d:/BGA_ID_MARKING/HDS_ID_Marker/HDS_ID_Marker/Data/MarkInformation.cs#L5-L68) | `CustomMark`, `InterMark`, `OCRMark`, `AlignROI` (`5-68`) | `public string variantKey;` (`"A"`/`"B"`), `public int hwBlockIndex;` 필드 추가 |
| [`ModelManager.xaml`](file:///d:/BGA_ID_MARKING/HDS_ID_Marker/HDS_ID_Marker/ModelManager.xaml#L160-L330)<br>[`ModelManager.xaml.cs`](file:///d:/BGA_ID_MARKING/HDS_ID_Marker/HDS_ID_Marker/ModelManager.xaml.cs#L80-L200) | `pnlModelSpec` (`160-330`), `LoadModel`, `CreateModelInfo`, `ModifyModelInfo`, `SetMarkData` (`80-200, 709-731, 1124-1223`) | `cbColumnLayout` UI 추가, `model_info.column_layout` 로드/저장 연동, `SetMarkData`에서 `Align_B.bmp`, `Align2_B.bmp` 추가 로드 |
| [`MainHRControl.xaml.cs`](file:///d:/BGA_ID_MARKING/HDS_ID_Marker/HDS_ID_Marker/MainHRControl.xaml.cs#L303-L2078) | `InitializeObjects` (`303-313`), `CreateObjects`, `Save`, `Modify`, `isExist`, `DeleteByIndex`, `UpdateIndex` (`1382-2078`) | - 인메모리에 `A`와 `B` 마크/얼라인 리스트를 모두 상시 유지(`GetCustomMarks(variantKey)` 등)하고 캔버스에는 선택된 Variant만 표시<br>- 모든 DB 조회·저장·삭제·인덱스 갱신 쿼리에 `AND variant_key = '{m_variantKey}'` 조건 적용하여 A/B 데이터 침범 방지 |
| [`SubWindow\WorkSettingWindow.xaml`](file:///d:/BGA_ID_MARKING/HDS_ID_Marker/HDS_ID_Marker/SubWindow/WorkSettingWindow.xaml#L66-L77)<br>[`SubWindow\WorkSettingWindow.xaml.cs`](file:///d:/BGA_ID_MARKING/HDS_ID_Marker/HDS_ID_Marker/SubWindow/WorkSettingWindow.xaml.cs#L428-L923) | `rbVariantA/B` (`66-77`), `btnSave_Click` (`859-922`), `SaveAlignTemplateAndTopMotionPos` (`672-719`), 위치 이동 핸들러 (`428-452, 535-559, 596-599, 1574-1621`) | - 티칭 대상 디자인(`A` / `B`) 선택 라디오버튼 추가 (`A,A,A,A` 모델은 `B` 비활성화)<br>- `A` 저장 시에만 1열 모션 좌표(`TopAlignY`, `TopMarkY`, `BotMarkY`) 갱신 (`posY <= 90` 유지)<br>- `B` 저장 시에는 모션 좌표 덮어쓰기 없이 `B`의 ROI와 `Align_B.bmp`, `Align2_B.bmp`만 저장<br>- `MoveToAlignPos(variantKey)`, `MoveToCamPos(mode, variantKey)`, `MoveToLaserMarkPos(mode, variantKey)` 독립 메서드 분리 |
| [`DCS\Camera\Vision.cs`](file:///d:/BGA_ID_MARKING/HDS_ID_Marker/DCS/Camera/Vision.cs#L26-L60)<br>[`DCS\Camera\MILCL.cs`](file:///d:/BGA_ID_MARKING/HDS_ID_Marker/DCS/Camera/MILCL.cs#L313-L475) | `resultToAlignToROI`, `SearchPointAlignToROI`, `ImageToAlign` (`313-475`) | Variant `B`용 템플릿 버퍼(`align_B`, `align2_B`) 추가 및 템플릿 선택 파라미터 오버로드 추가 |
| [`ManagementClass\TeachingManagement.cs`](file:///d:/BGA_ID_MARKING/HDS_ID_Marker/HDS_ID_Marker/ManagementClass/TeachingManagement.cs#L178-L351) | `MarkingAlignToROI` (`178-275`), `MarkingAlignToROI_RightOnly` (`277-351`) | - 1열(`col=0`)은 1열 Variant의 상단 ROI/템플릿으로, 4열(`col=3`)은 4열 Variant의 하단 ROI/템플릿으로 검출<br>- 화면 좌측끼리(`LeftAlign`), 화면 우측끼리(`RightAlign`) 얼라인 편차 매칭하여 `radian` 및 `RightOnly` 산출 |
| [`ManagementClass\LaserMarkingManagement.cs`](file:///d:/BGA_ID_MARKING/HDS_ID_Marker/HDS_ID_Marker/ManagementClass/LaserMarkingManagement.cs#L552-L1342) | `LoadProjectTop`/`Bot` (`552-669`), `TopMark`/`BotMark` (`939-1342`) | - `LoadProjectTop`/`Bot`에서 `A` 블록(`0..19`, 0°)과 `B` 블록(`20..39`, 180°) 통합 등록<br>- `TopMark`/`BotMark`에서 `colStrip`의 Variant 블록만 `MarkingEnable=2,1`, 타 Variant 블록은 `2,0`으로 전송 |
| [`ManagementClass\WorkManagement.cs`](file:///d:/BGA_ID_MARKING/HDS_ID_Marker/HDS_ID_Marker/ManagementClass/WorkManagement.cs#L1227-L2995) | `MarkingPC`/`PLC`/`TEST` (`1227-1400`), `CheckMarks2` (`2378-2625`), `CheckMarksParallel` (`2658-2813`), `GetCustomIDString` (`2986-3116`), `ConvertInternal1ID` (`2051-2069`) | - 자동 운전 시작 시 `B` 포함 모델의 `B` 티칭 완료 여부 사전 검증<br>- 열 `i`별 Variant의 `markPos`(좌/우)에 맞춰 ID 문자열(`stripID` vs `stripID - 1`) 생성 및 ITS 기록<br>- 비전 검사 시 열 `i`의 Variant별 `roi`(A: 좌상단, B: 우하단) 및 `format`(하부 포맷 참조 버그 수정 포함) 적용 |

---

## 4. 단계별 구현 로드맵 및 보류 항목

### 4.1 단계별 구현 계획
1. **Phase 1 — 데이터 모델 및 DB 스키마 확장**:
   * `ModelInformation.ColumnLayout`, `MarkInformation.variantKey`/`hwBlockIndex` 추가 및 DB 테이블 `column_layout`, `variant_key` 컬럼(기본값 `'A'`) 연동.
2. **Phase 2 — 티칭창(`WorkSettingWindow`, `MainHRControl`) A/B 디자인 전환, 인메모리 이중 유지 및 분리 저장**:
   * 티칭창 내 `Design A` / `Design B` 전환 토글 구현, `MainHRControl` 내 A/B 리스트 상시 보유 및 DB 삭제/수정 쿼리 `variant_key` 격리, `MoveToAlignPos/CamPos/LaserMarkPos(variantKey)` 독립 메서드 분리, `B` 저장 시 모션 기준점 덮어쓰기 방지.
3. **Phase 3 — Keyence 멀티 블록(0° / 180°) 로드 및 1·4열 이종 얼라인 연동**:
   * `LoadProjectTop`/`Bot`에 A/B 블록 동시 등록, `TopMark`/`BotMark`에서 열별 Variant 블록 Enable/Disable 토글, `MarkingAlignToROI`에서 1열(A 상단 ROI) 및 4열(B 하단 ROI) 좌/우 매칭 검출 연동.
4. **Phase 4 — 비전 바코드 디코딩, 좌/우 ID 인덱싱 및 시작 전 안전 검증 연동**:
   * `CheckMarks2`/`CheckMarksParallel` 열별 Variant ROI 크롭, `GetCustomIDString`/`GetInternal1IDString`/`ConvertInternal1ID`의 Variant별 좌/우(`markPos`) 반영, `B` 미티칭 상태 마킹 시작 차단 가드 적용.

### 4.2 향후 확장/보류 항목 (Out-of-Scope for Initial Release)
1. **마킹 시프트 검사(`CheckMarkShift` / `UpdateTopMarkInfo`)의 A·B 개별 대응**:
   * 초기 리팩토링 범위에서는 제외하며, 추후 현업 요청 발생 시 Variant별 초기 마크 거리(`lstMarkInitPos`) 및 4열 얼라인 중심점 분리 비교 로직을 적용한다.
2. **티칭창 내 단발 수동 마킹/리딩 테스트 버튼(`BtnCustomMarking_Click` 등)**:
   * 현장에서 미사용 중인 기능으로 초기 범위에서 제외한다.
