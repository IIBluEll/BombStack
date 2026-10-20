# BOMB STACK 본 개발 인게임 UI

기획서 v1.2의 인게임 화면을 HM CodeBase의 `AView` / `APresenter`로 구성한 코드입니다. `BombStackPrototype`에 연결했던 임시 uGUI 경로는 제거했습니다. 본 개발은 Scene에 저장한 UI와 `BombStack.Gameplay.InGameController`를 사용합니다.

## 시작하기

1. `Assets/Main/01_Scene/InGame.unity`를 엽니다. `SampleScene`은 기존 IMGUI 프로토타입용으로 유지합니다.
2. Unity 메뉴에서 **Bomb Stack > UI > Create In-game Screen**을 실행합니다.
3. 생성 창에서 한글 Font 에셋과 보드 프레임·빈 셀 Sprite를 선택합니다. 이미 Sprite로 임포트된 두 PNG는 자동으로 찾아 초기값에 넣습니다.
4. **현재 Scene에 인게임 화면 생성**을 누릅니다. Canvas, HUD, 패널, 버튼 참조, 게임 컨트롤러와 필요한 EventSystem이 한 번 생성됩니다.
5. 생성된 `InGame` 루트를 Scene에 저장합니다. Prefab으로 관리하려면 루트를 `Assets/Main/03_Prefabs`로 드래그해 저장합니다. 이미 생성된 화면이 있으면 생성 메뉴는 그것을 선택하며 덮어쓰지 않습니다.
6. 이후에는 Hierarchy/Inspector/Prefab 편집으로 배치·색상·이미지·폰트를 변경합니다. 플레이 중 Factory가 이 계층을 다시 만들지 않습니다.

이 작업에서 위 생성 메뉴는 실행하지 않았습니다. 기존 Scene·Prefab·이미지 임포트·Inspector·패키지·프로젝트 설정 파일도 직접 수정하지 않았습니다. 생성 버튼은 사용자가 편집 상태에서 실행하는 작업이며, Scene 저장과 Prefab 저장은 자동으로 하지 않습니다. 생성 직후 Undo로 취소할 수 있습니다.

폰트를 지정하지 않으면 편집 화면에는 Unity 기본 폰트를 넣고, 플레이에서는 OS 동적 폰트를 한 번 만들어 기본 폰트를 사용하는 Text에 연결합니다. 개별 Text에 지정한 다른 Font는 유지합니다. 배포할 폰트는 프로젝트 에셋으로 명시하는 것을 권장하며, OS 폰트의 설치 여부와 다른 PC에서의 글자 표시는 이번에 확인하지 않았습니다.

## 저장되는 계층

```text
InGame                         InGameController
├─ InGameUI                    Canvas / InGameUI_view
│  ├─ Background
│  └─ Content 1920x1080
│     ├─ 헤더·일시정지 버튼
│     ├─ Stage Score
│     ├─ Run Build
│     ├─ Owned Relics
│     ├─ BoardRoot
│     │  ├─ Backdrop
│     │  ├─ BoardArea
│     │  │  ├─ EmptyGrid         Sprite를 지정한 경우
│     │  │  └─ Cells and Effects
│     │  └─ Frame               Sprite를 지정한 경우
│     ├─ Supply and Relics      다음 조각과 배치 내역 영역
│     ├─ Growth and Relic Choices
│     ├─ Pause Guide Confirmation Result
│     └─ Controls
└─ EventSystem                 Scene에 기존 EventSystem이 없을 때만 생성
```

Scene에 EventSystem이 이미 있으면 재사용하며 해당 설정은 바꾸지 않습니다. 직접 만든 EventSystem을 사용할 때도 활성화된 EventSystem은 하나만 둡니다. 새로 생성되는 `InGamePointerInputModule`은 포인터만 uGUI로 보내고 키보드·패드 메뉴 입력은 `GameInput`이 한 번만 처리합니다. 버튼의 자동 Navigation은 끄고 Presenter의 선택 표시를 사용합니다.

## 연결한 화면과 동작

| 화면 | 내용 |
|---|---|
| 플레이 | 보드·활성 조각·고스트, 첫 묶음 파괴/발동 예고·중계 점선, 다음 조각 3개와 첫 등장 예약 |
| HUD | 스테이지 목표/점수·진행률·남은 조각·총점, 레벨/XP·폭탄 3종·숙련·보유 유물 4개 |
| 연출 | 기존 판정 결과의 폭발 묶음·제거·셀 낙하 재생, 배치 종료 후 점수 구성과 XP |
| 레벨업 | 3개 중 선택, 신규/강화/공통 구분, 현재→다음 범위·레벨, 종류 공급 비중과 첫 등장 예약 |
| 유물 | 3개 후보와 효과 설명, 선택 후 다음 스테이지 |
| 일시정지 | 계속, 연출 속도, 게임 방법, 다시 시작, 판 포기 후 메뉴 |
| 안내 | 최초 시작 전 4장, 일시정지에서 다시 열기 |
| 확인 | 진행 중 재시작·메뉴 복귀 전에 현재 판이 저장되지 않음을 알림 |
| 결과 | 종료 이유·스테이지·총점·배치 수·최대 폭탄/묶음·시간·최종 레벨/빌드, 재시작·메뉴 복귀 |

레벨업·유물 선택은 오른쪽 다음 조각 영역을 선택 카드로 바꾸며 보드·보유 폭탄·유물은 그대로 표시합니다. 일시정지·안내·결과는 공통 모달 영역을 사용합니다. 이전 IMGUI 화면으로 전환하지 않습니다.

방향키/D패드로 선택을 이동하고 Enter/아래쪽 페이스 버튼으로 확인합니다. 마우스로도 선택할 수 있습니다. 화면 전환 후에는 키보드·패드·마우스 클릭을 해제해야 다음 입력을 받도록 했으며 같은 프레임에 요청을 중복 처리하지 않습니다. 일시정지 중에는 GameSession과 연출 재생의 Tick을 호출하지 않습니다. 포커스 상실과 사용 중 패드 연결 해제도 일시정지 요청으로 처리합니다.

## 코드의 책임

| 코드 | 책임 |
|---|---|
| `Gameplay/InGameController` | GameSession 소유, 입력/연출 진행, UI 프레임 전달, Presenter Open/Close/Dispose, 사용자 요청 처리 |
| `Gameplay/GameInput` | 게임·메뉴 입력, Enable/Disable/Dispose, 동시 방향 입력 우선순위 |
| `Gameplay/ResolutionPlayback` | 이미 계산된 배치 결과를 표시 보드에서 재생 |
| `UI/InGameUIModel_model` | 표시 보드·활성/고스트 좌표·연출 단계·패널·입력 잠금 상태 |
| `UI/InGameUI_view : AView` | 직렬화한 Text/Image/Graphic/패널 참조, UI 갱신과 이벤트 전달 |
| `UI/InGameUIPresenter_presenter : APresenter` | HUD 바인딩·선택 초점·메뉴 구성·사용자 요청 전달 |
| `UI/InGameUIPresenter_presenter.Menu` | 성장·유물·일시정지·안내·확인·결과의 표시 데이터 구성 |
| `UI/InGameMenuButton` | 마우스 클릭/진입과 선택 표시 |
| `UI/InGameBoardGraphic`, `NextPieceGraphic`, `ACellGraphic` | 실제 게임 데이터에 맞는 보드·조각 도형 표시 |
| `UI/InGameUIFactory` | `UNITY_EDITOR`에서만 컴파일하는 초기 계층 제작 코드. 런타임 호출 없음 |
| `Editor/InGameUIEditor` | 사용자가 생성 메뉴를 실행할 때 계층 생성·참조 연결·Undo 등록 |

규칙·성장·공급·정산 코드는 기존 Data/Core를 사용하며 이번 작업에서 변경하지 않았습니다. 본 개발 코드에는 `BombStack.Prototype` 참조가 없습니다. 기존 프로토타입은 별도 비교용으로 보존하기 위해 입력·연출의 기존 파일을 이동하거나 변경하지 않고, 필요한 처리만 Gameplay 영역에 분리했습니다.

## 이미지와 치수

- Frame: `Assets/Main/05_Assets/UI/Board/BoardFrame_9Slice_v1.png`
- Empty Cell: `Assets/Main/05_Assets/UI/Board/BoardEmptyCell_v1.png`
- 임포트: Sprite / Single, PPU 100, Full Rect, Point, Clamp, Mip Maps 끄기, Compression None, Max Size 2048 이상을 시작값으로 사용합니다.
- Frame Border 네 방향 192px, Alpha Is Transparency 켜기, Sprite Editor에서 Apply합니다.
- 생성 코드는 Frame을 Sliced / Pixels Per Unit Multiplier 6 / Fill Center 끄기로 연결합니다.
- Sprite가 없는 부분은 기본 도형으로 구성합니다. 생성 후 Sprite를 추가하려면 기존 Frame/EmptyGrid를 편집하거나 해당 계층을 직접 구성합니다. 런타임 Sprite 필드로 UI를 재생성하지 않습니다.

1920×1080 / CanvasScaler Expand 기준입니다. 프레임 544×928, 배경 496×880, 플레이 영역 480×864, 빈 셀 48×48 × 10×18이며 플레이 영역을 게임 좌표와 대응합니다. 셀 배경은 파괴·낙하시키지 않습니다. 고정 셀·고스트·효과는 별도 Graphic에서 그립니다. 자세한 원본 설명은 [보드 이미지 안내](UI/BOMB_STACK_보드_UI_배치_안내_v1.md)를 참고합니다. 그 문서의 IMGUI 설명은 이미지 추가 당시의 프로토타입을 설명한 기록입니다.

## 상위 화면·저장 연결 지점

인게임 컨트롤러가 메인 메뉴나 저장 시스템을 임의로 생성하지 않습니다. 아래 API로 연결합니다.

- `Start On Enable`: Scene 단독 실행 시 새 판을 시작합니다. 상위 메뉴에서 초기화 순서를 제어하려면 끄고 설정/이벤트 연결 후 `StartNewGame()`을 호출합니다.
- `ApplyPreferences(GameSettingsData, bool hasSeenGuide)`: 시작 전 설정·최초 안내 확인 여부를 전달합니다. 음량·해상도 등의 실제 적용 코드는 이 인게임 UI 작업에 포함하지 않았습니다.
- `RunFinished`: 종료된 판의 독립 `RunRecordData`를 한 판당 한 번 전달합니다. 진행 중 재시작/포기는 ABANDONED로 종료한 뒤 전달합니다. 최고 기록 비교와 파일 저장은 상위 저장 처리에 연결합니다.
- `GuideCompleted`: 안내 확인 여부를 저장할 때 사용합니다.
- `SettingsChanged`: 연출 속도 변경을 저장할 때 사용합니다.
- `ReturnToMenuRequested`: 상위 메뉴 전환을 요청합니다. 구독자가 없으면 Inspector의 `Menu Scene Name`을 사용하며, 로드 가능한 Scene일 때만 메뉴 버튼을 활성화합니다. 구독자와 Scene 설정이 모두 없으면 메뉴 버튼은 비활성입니다.

메인 메뉴 자체·전체 설정 화면·기록 저장 연결·다국어·최종 아이콘/사운드/연출 아트는 이 작업의 완료 항목이 아닙니다. 최초 안내의 도식 이미지는 아직 없고 텍스트 안내입니다. 이번에 작성한 것은 해당 기능들과 연결할 수 있는 본 개발용 인게임 UI 및 실행 제어 코드입니다.

## 확인 범위

기획서 v1.2의 화면/선택/일시정지 요구사항, 기존 Core의 상태 전이, 설치된 HM CodeBase와 InputSystem API, 직렬화 참조와 입력/수명 코드를 읽고 대조했습니다.

사용자 요청에 따라 컴파일·빌드·Play Mode·자동 테스트·생성 메뉴 실행은 하지 않았습니다. 따라서 Scene/Prefab의 실제 저장·재로드, Unity 컴파일, 레이아웃·텍스트 잘림·9-slice 정렬, 키보드/패드/마우스의 실제 동작은 아직 검증하지 않았습니다.
