# BOMB STACK 보드 UI 이미지 v1

승인한 UI 컨셉을 바탕으로 만든 보드용 이미지 2종과 수동 배치 안내입니다. 원본은 그대로 복사했으며 픽셀 편집·리사이즈를 하지 않았습니다.

## 전달 파일

| 파일 | 실제 크기 | 용도 |
|---|---|---|
| `Assets/Main/05_Assets/UI/Board/BoardFrame_9Slice_v1.png` | 1254×1254, RGBA | 중앙이 투명한 도트 금속 프레임. 9-slice로 세로 보드에 적용 |
| `Assets/Main/05_Assets/UI/Board/BoardEmptyCell_v1.png` | 1254×1254, RGB | 어두운 빈 셀 배경. 10×18칸에 반복 |

프레임이 정사각형인 이유는 모서리를 유지한 채 가로·세로 변만 늘리는 9-slice 원본이기 때문입니다. 정사각형 이미지를 Simple 방식으로 세로로 늘리지 않습니다. 빈 셀은 일반 블록 이미지가 아니라 고정 격자 배경입니다.

## 현재 프로젝트와의 관계

- `BoardDefinition`은 가로 10칸, 세로 18칸입니다. 위험 표시 시작 행은 아래에서 센 y=14입니다.
- 현재 `BombStackPrototype.GUI.cs`는 IMGUI로 보드와 셀을 그립니다. 이 이미지들을 넣어도 기존 프로토타입 화면이 자동으로 바뀌지는 않습니다.
- 이번 작업에서는 PNG와 안내만 추가했습니다. C#·Scene·Prefab·패키지·Inspector·프로젝트 설정·.meta 파일은 직접 변경하지 않았습니다.
- Unity 실행·컴파일·빌드 검증은 수행하지 않았습니다.

## 수동 임포트 권장값

아래는 사용자가 Unity에서 직접 적용할 시작값입니다. 실제 Inspector에 적용하거나 화면 결과를 검증한 값은 아닙니다.

공통:
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Pixels Per Unit: 100
- Mesh Type: Full Rect
- Wrap Mode: Clamp
- Filter Mode: Point (no filter)
- Generate Mip Maps: 끄기
- Compression: None
- Max Size: 2048 이상 — 실제 원본은 1254px이므로 1024를 선택하면 축소됩니다.
- UI Image Color: 흰색. 장식용 Image의 Raycast Target은 끕니다.

프레임:
- Alpha Is Transparency: 켜기
- Sprite Editor의 Border: Left / Bottom / Right / Top 모두 192px
- UI Image Type: Sliced
- UI Image Pixels Per Unit Multiplier: 6
- Fill Center: 끄기
- Sprite Editor에서 Apply

빈 셀:
- UI Image Type: Simple
- UI Image Pixels Per Unit Multiplier: 1
- 셀 RectTransform은 정사각형
- RGB 원본으로, 투명 배경이 필요한 이미지가 아닙니다.

프레임의 192px 경계는 네 모서리 장식을 보존하기 위한 초기 권장값입니다. Canvas의 Reference Pixels Per Unit이 100이고 Sprite PPU가 100일 때, Image의 배율 6을 적용하면 각 9-slice 경계는 32 UI 단위가 됩니다. 실제 장식의 안쪽 가장자리와 slice 경계는 다르므로 아래 Backdrop으로 빈틈을 덮습니다. 다른 PPU 값을 사용하면 여백도 다시 맞춥니다.

## 권장 계층과 치수

Canvas는 Screen Space - Overlay, Canvas Scaler는 Scale With Screen Size / Reference Resolution 1920×1080 / Match 0.5를 초기안으로 권장합니다.

```text
BoardRoot                       544 × 928, 중앙 기준
├─ Backdrop                     496 × 880, 중앙 / 기본 Image #0A1024
├─ BoardArea                    480 × 864, 중앙 — 실제 10×18 플레이 영역
│  ├─ EmptyGrid                 빈 셀 배경 180개
│  ├─ GhostLayer                착지 고스트
│  ├─ FixedCellLayer            고정된 N / B / T
│  ├─ ActivePieceLayer          현재 조작 중인 네 셀
│  ├─ PreviewLayer              파괴 테두리 / 점화 / 중계 점선
│  └─ EffectLayer               폭발 / 낙하 표시
├─ Frame                        544 × 928, Sliced
└─ WarningLayer                 상단 4행 위험 표시, 필요할 때 표시
```

이름과 계층은 배치 제안이며 새 스크립트나 Prefab을 만든 것이 아닙니다. Backdrop은 별도 그림 없이 Unity 기본 Image 색상으로 만듭니다. 프레임의 투명한 안쪽 여유까지 덮도록 BoardArea보다 16단위 크게 잡았습니다.

EmptyGrid에만 GridLayoutGroup을 사용합니다:
- Cell Size: 48×48
- Spacing: 0×0
- Padding: 모두 0
- Constraint: Fixed Column Count / 10
- Start Corner: Upper Left
- Start Axis: Horizontal
- Child Alignment: Upper Left
- 빈 셀 Image 180개를 배경으로 배치하고 모두 같은 스프라이트 사용

배경 타일 자체에 가는 테두리가 있으므로 별도 Spacing을 더하지 않습니다. 실제 셀·고스트·폭발은 EmptyGrid 자식으로 넣지 않습니다. GridLayoutGroup이 위치를 강제하지 않도록 형제 레이어에 둡니다. 셀 배경은 파괴하거나 아래로 움직이지 않습니다.

480×864가 정확한 플레이 영역이며 프레임 여백은 판정 좌표에 포함하지 않습니다. UI 좌표 48은 게임 규칙을 바꾸는 값이 아닙니다. 1280×720에서는 Canvas 축소에 따라 셀 32px, 보드 320×576px 수준으로 표시되는 구상입니다. 실제 기기에서의 가독성은 아직 확인하지 않았습니다.

## 데이터 좌표와 셀 위치

게임 좌표는 왼쪽 아래가 (0,0)입니다. BoardArea 좌표를 아래쪽 기준으로 배치한다면 셀 중심은:
- x = (열 + 0.5) × 48
- y = (행 + 0.5) × 48

EmptyGrid는 위쪽 행부터 표시되므로, 같은 좌표의 배경 자식 인덱스는 `(17 - y) × 10 + x`입니다. 위쪽 기준 배치와 아래쪽 기준 배치를 섞지 않습니다. 나중에 표시 코드를 연결할 때 이 변환을 한 곳으로 모으면 됩니다.

위험 표시는 BoardArea 상단 192단위(4×48)에 해당합니다. 이미지에 빨간 경고를 고정으로 그리지 않았습니다. y=14~17에 고정 셀이 있는지를 기준으로 별도 표시하며, 그 선 자체가 패배 조건인 것처럼 표현하지 않습니다.

## 확인한 사항과 남은 확인

- 실제 PNG 크기와 색상 모드를 파일에서 확인했습니다.
- 프레임은 알파 채널이 있으며 중앙 픽셀은 alpha=0입니다. 중앙 60% 영역 알파 범위는 0~1/255로 사실상 투명하지만 모든 픽셀이 정확히 0인 마스크는 아닙니다. Sliced의 Fill Center를 끄는 구성을 권장합니다.
- 빈 셀은 RGB 불투명 이미지입니다.
- 이미지를 육안으로 확인했습니다. 프레임에 격자·문자·폭탄이 포함되지 않았고, 빈 셀에는 심볼이 없습니다.
- 배치 치수는 10×18과 셀 48 기준으로 계산한 제안입니다.
- Unity 임포트·9-slice 모서리·축소 시 경계선·Canvas의 실제 화면·IMGUI 대체 연결은 검증하지 않았습니다.
- Frame을 실제로 배치한 다음 BoardArea의 10×18 격자와 맞는지 확인해야 합니다. 원본을 단순히 세로로 늘린 이미지는 최종 보드로 사용하지 않습니다.

생성 방식은 내장 ImageGen이며 CLI/API fallback은 사용하지 않았습니다. 실제 입력 프롬프트는 같은 폴더의 `BOMB_STACK_보드_UI_생성_프롬프트_v1.md`에 기록했습니다.

