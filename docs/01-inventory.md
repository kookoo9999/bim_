# 01-inventory.md — HDS_ID_Marker 코드베이스 분석 인벤토리

> **분석 목적**: 기존 4열(1~4열) 마킹 시 1~4열이 모두 동일한 제품(`A,A,A,A`)임을 전제하고 있으나, 향후 제품 디자인/배치가 `A,A,A,B | A,A,B,B | A,B,B,B` 등 열별 이종 조합으로 변경되더라도 동일하게 마킹이 수행되어야 함. 본 문서는 현행 코드베이스(`D:\BGA_ID_MARKING\HDS_ID_Marker`)를 읽기 전용으로 분석하여 단일 제품·공유 배치 가정 지점과 근거(`파일:라인`)를 정리한 인벤토리이다.

---

## 1. 솔루션 및 아키텍처 개요

- **솔루션**: `D:\BGA_ID_MARKING\HDS_ID_Marker\HDS_ID_Marker.sln` (Visual Studio 16, .NET Framework 4.8, 6개 프로젝트)
  - `HDS_ID_Marker` (`HDS_ID_Marker\HDS_ID_Marker.csproj`): 메인 WPF 실행 프로젝트 (`MainWindow.xaml.cs`, `ModelManager.xaml.cs`, `MainHRControl.xaml.cs`, `SubWindow\WorkSettingWindow.xaml.cs`, `ManagementClass\WorkManagement.cs`, `LaserMarkingManagement.cs`, `TeachingManagement.cs`).
  - `PCS` (`PCS\PCS.csproj`): 장치 통합 컨트롤러(`DeviceController.cs`) 및 거버 파서(`PCS\Gerber\GerberLoader.cs` 등).
  - `DCS` (`DCS\DCS.csproj`): 비전/카메라(`Vision.cs`, `MILCL.cs`, `PylonGigE.cs`), 레이저(`KeyenceTCP.cs`, `EOTCP.cs`, `Laser.cs`), 모션(`Motion.cs`, `Motion2.cs`), 조명, PLC, IDReader.
  - `Common` (`Common\Common.csproj`): XML 설정(`Setting.cs`, `SettingGeneral.cs`, `SettingDevice.cs`), 로거, 이미지 유틸.
  - `Common.Control` (`Common.Control\Common.Control.csproj`): 공용 WPF 컨트롤/다이얼로그.
  - `Common.DataBase` (`Common.DataBase\Common.DataBase.csproj`): MySQL 연동(`ConnectFactory.cs`, `DataBaseConnector.cs`, `QueryHelper.cs`).
- **주요 진입점 및 실행 흐름**:
  - `App.xaml` → `MainWindow` (`HDS_ID_Marker\MainWindow.xaml.cs:77-85`)
  - `Window_ContentRendered` (`HDS_ID_Marker\MainWindow.xaml.cs:262-291`): `ResolutionInformation.LoadData()`, 장치 초기화(`InitializeDevice()`), 매니저 초기화(`InitManagement()`), 모델 선택창(`LoadModelMangement()`).
  - 메뉴 라우팅 (`MainWindow.MenuChangeEvent`, `HDS_ID_Marker\MainWindow.xaml.cs:534-636`):
    - `MODELMANAGER` → `ModelManager` → `SetLaser()` (`681-724`), `MoveModelPos()` (`754-780`)
    - `TEACHING` → `WorkSettingWindow` (PC 모드, `613-619`) 또는 `WorkSettingControl` (PLC 모드, `620-624`)
    - `RUNMARKING` → `LotWindow` → `WorkMarkInfoWindow` → `workManagement.StartMarking()` (`542-589`)

---

## 2. 데이터 모델 및 DB 스키마 (단일 모델/레시피 구조)

### 2.1 인메모리 데이터 모델
- **`Model`** (`HDS_ID_Marker\Data\Model.cs:9-88`)
  - 단일 `ModelInformation m_modelInformation` (`Model.cs:11`)과 단일 `MarkingInformation m_markingInformation` (`Model.cs:12`)만 보유.
  - 작업 상태(`lot`, `stripID = 2`, `fisrtID = 2`, `historyID = 0`)가 모델 단위 단일 스칼라로 관리됨 (`Model.cs:14-17`).
  - 모델/결과 경로도 단일 `modelInformation.Name` 기준 (`Model.cs:55-84`).
- **`ModelInformation`** (`HDS_ID_Marker\Data\ModelInformation.cs:8-115`)
  - 단일 `Name`, `PanelLength`, `GerberPath`, `CustModelName`, `MarkingDirection`, `MarkingCheck`, `MakringShiftCheck`, `UseAOIMark`, `MarkingLeft`, `UncoilerDirection`, `RecoilerDirection` 보유 (`ModelInformation.cs:10-110`).
  - 열 간격 정보는 `List<double> StripIter` (`ModelInformation.cs:70-81, 96`) 하나뿐이며, 각 열의 Y 오프셋만 저장함.
- **`MarkingInformation` / `CustomerInformation`** (`HDS_ID_Marker\Data\MarkingInformation.cs:19-64`)
  - 스테이지 기준 좌표가 단일 세트: `TopAlignX`, `TopAlignY`, `TopAOIX`, `TopAOIY`, `TopMarkY`, `BotMarkY` (`MarkingInformation.cs:22-27`).
  - 고객사 정보도 단일 `CustomerInformation` 객체 (`MarkingInformation.cs:29-41, 44-63`): `MarkingMethod` (1=Top, 2=Bot, 3=Both), `Name`, `Code`, `MarkingLocation`, `MarkingLength`, `MarkingFormat`.
- **마킹/얼라인 구조체** (`HDS_ID_Marker\Data\MarkInformation.cs:5-68`)
  - `CustomMark` (`5-21`), `InterMark` (`23-38`), `OCRMark` (`40-56`), `AlignROI` (`58-67`) 모두 열/레인 구분 필드(`col`, `stripIndex` 등)가 없으며 `orientation`(`"top"`/`"bot"`)만 존재.
- **`ResolutionInformation`** (`HDS_ID_Marker\Data\ResolutionInformation.cs:12-555`)
  - 공통 카메라/레이저 보정값 외에 4열 모션 오프셋이 1~4번 고정 프로퍼티로 하드코딩됨: `TopOffsetX1..4`, `TopOffsetY1..4`, `BotOffsetX1..4`, `BotOffsetY1..4` (`ResolutionInformation.cs:29-44, 230-436`).

### 2.2 데이터베이스 테이블 (`idmarker` 스키마)
- `idmarker.model_info`: `name`당 1행 (`ModelManager.xaml.cs:87-88, 157-158, 1134-1138, 1182-1187`).
- `idmarker.marking_info`: `name`당 1행 (`ModelManager.xaml.cs:88, 1412-1414, 1450-1453`).
- `idmarker.customer_info`: `Name`당 1행 (`CustomInfoWindow.xaml.cs:161-162, 199-201, 279`).
- `idmarker.para_custommark`, `idmarker.para_intermark`, `idmarker.para_ocrmark`, `idmarker.para_alignroi`: `(orientation, model)` 조건으로만 조회 (`MainHRControl.xaml.cs:1793-1803`). 열(column) 구분 컬럼 없음.
- `idmarker.light_info`: `name`당 1행 (`ModelManager.xaml.cs:672`, `WorkSettingWindow.xaml.cs:1237-1238, 1275-1277`).
- `idmarker.resolution_info`: `MotionOffsets`에 16개 값(`TopOffsetX1..4`, `TopOffsetY1..4`, `BotOffsetX1..4`, `BotOffsetY1..4`)을 `;` 구분자로 저장 (`ResolutionInformation.cs:448, 468-487, 521-532`).
- `idmarker.work_history`: `(Lot, Name)` 기준으로 단일 `StripID` 저장 (`WorkManagement.cs:165, 199, 241-242, 278-280`).

---

## 3. 4열(1~4열) 처리 파이프라인별 단일 제품 가정 근거

### 3.1 거버(Gerber) 파싱 및 열 피치 계산
- **`GerberLoader.GetStripIteration(string filename)`** (`PCS\Gerber\GerberLoader.cs:99-134`):
  - 단일 거버 파일을 `GerberLayerType.Unit`으로 검증·로드 (`105-107`).
  - `SectionData[0].SectionTop - SectionData[i + 1].SectionTop`로 **Y축 피치만** 계산하여 `ret`에 저장 (`108-115`).
  - `ret.Count == 8` 또는 `12`일 때 4개로 축소하는 로직이 하드코딩됨 (`117-131`).
  - X 좌표 오프셋, 열별 섹션 크기 차이, 열별 상이한 거버 패턴 정보는 추출/보관하지 않음.

### 3.2 모델 선택 및 4열 배열 초기화
- **`ModelManager.btnStart_Click`** (`HDS_ID_Marker\ModelManager.xaml.cs:585-602`):
  - `gerber.GetStripIteration(ptrMain.model.modelInformation.GerberPath)`로 얻은 `StripIter.Count`(4) 크기로 배열 할당 (`596-599`):
    - `MainWindow.lots = new string[StripIter.Count]` (초기 선언도 `new string[4]` at `MainWindow.xaml.cs:61`)
    - `teachingManagement.alignOffset = new Point[StripIter.Count]`
    - `teachingManagement.motionOffsetBot = new Point[StripIter.Count]`
    - `teachingManagement.motionOffsetTop = new Point[StripIter.Count]`
  - `SetMotionOffset()` (`TeachingManagement.cs:33-55`, PLC 상수 4개) 또는 `SetPCMotionOffset()` (`TeachingManagement.cs:57-79`, `MainWindow.ri.TopOffsetX1..4`, `BotOffsetX1..4`)로 4열 오프셋 초기화.

### 3.3 티칭 UI 및 마크/얼라인 ROI 설정
- **단일 티칭 캔버스**: `WorkSettingWindow`는 `topHRControl`, `botHRControl` 2개만 보유 (`WorkSettingWindow.xaml.cs:124-129, 792-799`). 각 `MainHRControl`은 단일 `customMarks`, `interMarks`, `ocrMarks`, `alignROIs` 리스트만 가짐 (`MainHRControl.xaml.cs:39-42`).
- **1열 기준 티칭 강제**: `WorkSettingWindow.btnSave_Click` (`WorkSettingWindow.xaml.cs:866-875`)에서 `posY > 90`이면 `"1열 스트립으로 위치 보정 바랍니다."` 경고 후 저장을 차단함. 즉 1열에서 티칭한 좌표를 1~4열에 공통 적용함.
- **단일 얼라인 템플릿 이미지**: 모델 폴더 내 단일 `Align.bmp`(Align1)와 `Align2.bmp`(Align2)만 저장/사용 (`WorkSettingWindow.xaml.cs:676-698`, `ModelManager.xaml.cs:711-728`).
- **4열 이동 버튼 하드코딩**: `WorkSettingWindow`의 `btnMove1Row_Click` ~ `btnMove4Row_Click` (`WorkSettingWindow.xaml.cs:1574-1621`)이 `TopMarkY + CamToY + StripIter[0..3]` 및 `BotMarkY + CamToY + StripIter[0..3]` 사용.

### 3.4 런타임 얼라인(Alignment)
- **`TeachingManagement.MarkingAlignToROI`** (`HDS_ID_Marker\ManagementClass\TeachingManagement.cs:178-275`, `stripID != 2`일 때 `WorkManagement.cs:1106`에서 호출):
  - 1열(`TopAlignY`)에서 `alignROIs` (`number == 1`, `2`)를 탐색 (`188-192`)하고, 마지막 열(`StripIter[StripIter.Count - 1] + TopAlignY`)로 이동하여 **동일한** `align`, `align2` ROI/템플릿을 재탐색 (`218-224`).
  - 두 점으로 단일 패널 기울기 `radian = Math.Atan(alignDistanceX / alignDistanceY)`를 구해 `alignOffset[0..3]`을 선형 보간 (`242-249`).
  - `lastFoundAlignPos` (`TeachingManagement.cs:31, 199, 301`)도 첫 번째 얼라인 결과 1개(`Point`)만 저장.
- **`MarkingAlignToROI_RightOnly`** (`TeachingManagement.cs:277-351`, `stripID == 2`일 때 `WorkManagement.cs:1103-1104`) 및 **`MarkingAlign`** (`TeachingManagement.cs:81-146`, PLC 모드) 역시 1열과 마지막 열이 동일한 피듀셜 마크/위치를 갖는다고 가정.

### 3.5 레이저 프로젝트 로드 및 마킹 실행
- **단일 레이저 프로그램 로드**:
  - `MainWindow.SetLaser` (`MainWindow.xaml.cs:681-724`) 및 `WorkSettingWindow.btnSave_Click` (`910-912`)에서 `LoadProjectTop` / `LoadProjectBot` 호출.
  - `LaserMarkingManagement.LoadProjectTop` (`LaserMarkingManagement.cs:552-605`) 및 `LoadProjectBot` (`616-669`)는 `topHRControl` / `botHRControl`의 블록 목록으로 **단일** Keyence 프로그램(`CreateProgramWithAllClear=0`)을 생성·전송함.
  - 따라서 블록 개수, 블록 타입(`BARCODE_BLK`/`TEXT_BLK`), 크기(`sizeX`, `sizeY`), 포맷, 각도(`degree`), 폰트(`font`), 파워/스피드, 반전(`flip`)이 1~4열 모두 동일해야 함.
  - EOTCP 사용 시에도 단일 `{Customer}_{ModelName}.mrk` 파일만 로드 (`MainWindow.xaml.cs:708-720`, `LaserMarkingManagement.cs:194-477`).
- **마킹 루프**:
  - `WorkManagement.MarkingPC` (`WorkManagement.cs:966-1579`, 루프 `1245-1270`), `MarkingPLC` (`567-963`), `MarkingTEST` (`1582-1943`):
  - `for (int i = 0; i < ptrMain.model.modelInformation.StripIter.Count; i++)` 루프에서 스테이지 Y를 `pos1 + StripIter[i]` / `pos2 + StripIter[i]`로 이동 후 (`1249`), `BothMarking` / `TopMarking` / `BotMarking` (`LaserMarkingManagement.cs:1496-1591`) → `TopMark` (`939-1072`) / `BotMark` (`1212-1342`) 호출.
  - `TopMark`/`BotMark`는 공유된 `topHRControl.*Marks` / `botHRControl.*Marks`를 순회하며 `MARK(block.index, content, X, Y, Z)`로 문자열 내용과 위치 오프셋만 갱신하여 마킹함.
  - 즉, 열(`i`)마다 제품 배치(마크 상대좌표, 마크 개수, 크기, 각도, 폰트, 유무 등)가 다르면 현 구조로는 열별 마킹이 불가능함.

### 3.6 마킹 후 비전 검사 및 시프트 체크
- **마킹 시프트 체크**:
  - `LaserMarkingManagement.UpdateTopMarkInfo` (`LaserMarkingManagement.cs:750-808`)는 단일 `topHRControl`의 `alignROIs`(`number != 1`)와 `customMarks`(`reference == "고객1"`) 간 초기 거리 `lstMarkInitPos`를 계산함.
  - `WorkManagement.CheckMarkShift` (`WorkManagement.cs:2881-2909`)는 모든 열 `col`에 대해 `lstMarkInitPos[(int)SURFACE.TOP, (int)TYPE.CUSTOM][0]`(인덱스 `[0]` 단일 기준)과 비교하여 `markShift[col]` (`bool[4]`, `LaserMarkingManagement.cs:43`)을 판정함 (`2899-2903`).
- **마킹 인지 검사 (Barcode Read)**:
  - `CheckMarks` (`WorkManagement.cs:2283-2377`), `CheckMarks2` (`2378-2625`), `CheckMarksParallel` (`2658-2813`) 모두 각 열 `i`로 이동(`TopMarkY + StripIter[i]`, `BotMarkY + StripIter[i]`)한 뒤, **동일한** `topHRControl.customMarks[0].roi` / `botHRControl.customMarks[0].roi` 영역을 잘라 디코딩함 (또한 `CheckMarks2` `2545` 및 `CheckMarksParallel` `2783`은 BOT 디코딩 시에도 `topHRControl.customMarks[0].format`을 참조함).

### 3.7 ID 생성/검증, Lot/ITS 입출력 및 이력/UI
- **ID 생성/검증**:
  - `WorkManagement.MarkingPC` (`WorkManagement.cs:1227-1233`)에서 `i = 0..StripIter.Count-1`에 대해 `GetCustomIDString` (`2986-3116`) 및 `GetInternal1IDString` 호출 시 단일 `customerInformation.Name`, `MarkingLocation`, `MarkingFormat`, `MarkingLength`, `ptrMain.model.stripID`를 공통 사용함 (`CheckCustomID_Length` `2929-2936`도 단일 `MarkingLength` 기준).
- **Lot / ITS / UI**:
  - `LotWindow` (`LotWindow.xaml.cs:127-140`): `tbLotA`~`tbLotD` 4개 입력란이 `MainWindow.lots[0..3]`에 매핑됨.
  - `WorkManagement.ReadITSInfo` (`WorkManagement.cs:3401-3448`): ITS 파일의 `"A"`, `"B"`, `"C"`, `"D"` 행을 `lots[0..3]`에 채우되, `customerInformation.Code`는 단일 필드에 덮어씀 (`3445`).
  - `WorkMarkInfoWindow.SetMarkData` (`WorkMarkInfoWindow.xaml.cs:37-63`): `tbCustInfoA..D`, `tbInterInfoA..D`, `tbRowA..D` 4개 고정 UI.
  - `ITSHistoryWindow` (`ITSHistoryWindow.xaml.cs:45, 88`) 및 `WorkHistoryWindow` (`WorkHistoryWindow.xaml.cs:226`): `MainWindow.lots[0]`만 조회.

---

## 4. 이종 배치(`A,A,A,B | A,A,B,B | A,B,B,B`) 대응을 위한 핵심 제약 요약

| 영역 | 현행 단일 제품(4열 동일) 가정 | 대표 근거 (`파일:라인`) | 이종 배치 시 영향 |
|---|---|---|---|
| **모델/레시피 구조** | 모델당 1개 `ModelInformation`, 1개 `MarkingInformation`, 1개 `CustomerInformation` | `Data\Model.cs:11-12`<br>`Data\ModelInformation.cs:10-110`<br>`Data\MarkingInformation.cs:22-41` | 열별 상이한 제품 디자인/좌표/고객 포맷 표현 불가 |
| **거버(Gerber) 정보** | 단일 거버에서 Y 피치(`StripIter`) 4개만 추출 | `PCS\Gerber\GerberLoader.cs:99-134` | 열별 X 오프셋 또는 열별 상이한 유닛 배치/디자인 반영 불가 |
| **티칭 및 DB 파라미터** | `topHRControl`/`botHRControl` 각 1세트, 1열(`posY<=90`)만 티칭, DB `para_*` 테이블에 열 키 없음 | `WorkSettingWindow.xaml.cs:124-129, 869-874`<br>`MainHRControl.xaml.cs:39-42, 1793-1803` | B 제품 열에 다른 마킹 위치/크기/블록/ROI 지정 불가 |
| **얼라인(Align)** | `Align.bmp`/`Align2.bmp` 1세트, 1열과 4열이 동일 피듀셜이라고 가정 후 선형 보간 | `TeachingManagement.cs:178-275`<br>`WorkSettingWindow.xaml.cs:676-698` | 1열(A)과 4열(B)의 피듀셜 위치·모양이 다르면 얼라인 실패 또는 오보정 |
| **레이저 프로그램** | 모델 로드/저장 시 1개의 Keyence 프로그램(`LoadProjectTop`/`Bot`)만 전송, 마킹 시 `MARK` 내용/좌표만 변경 | `LaserMarkingManagement.cs:552-669, 939-1072`<br>`MainWindow.xaml.cs:691, 700` | 열마다 블록 구성·크기·각도·폰트·파워 등이 다를 때 마킹 불가 |
| **시프트/비전 검사** | `lstMarkInitPos[0]` 단일 기준으로 전 열 시프트 판정, `customMarks[0].roi` 단일 ROI로 전 열 디코딩 | `WorkManagement.cs:2881-2909, 2378-2625`<br>`LaserMarkingManagement.cs:750-808` | B 제품 열의 마킹 위치/ROI가 다르면 시프트 오경보 및 인지 실패 |
| **ID/이력/UI** | `lots[4]`, `tbLotA..D`, `btnMove1..4Row`, `TopOffsetX1..4` 등 4열 고정 및 단일 `stripID`/`MarkingLength` | `LotWindow.xaml.cs:127-140`<br>`ResolutionInformation.cs:29-44`<br>`WorkManagement.cs:1227-1233` | 열별 상이한 포맷/길이/디자인 매핑 확장 시 구조적 제약 |
