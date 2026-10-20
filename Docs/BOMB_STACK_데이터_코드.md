# BOMB STACK 데이터 코드

기준: `BOMB_STACK_B2EC84_B8ED8D84%9C_v0.2.md` 본문 v1.2.
위치: `Assets/Main/02_Scripts/Data`, 네임스페이스: `BombStack.Data`.

후속 프로토타입에서 `Core`와 `Prototype` 계층을 추가했습니다. 아래 내용은 데이터 계층의 책임을 설명하며, 현재 게임 진행과 실행 방법은 [프로토타입 문서](BOMB_STACK_프로토타입.md)를 참고합니다.

## 구성

| 파일 | 역할 |
|---|---|
| GameDefinition | 기본 데이터 생성, 폭탄/조각/스테이지 조회, 실제 콘텐츠 표 |
| RuleDefinitions | 보드, 공급, 조작 시간, 점수, 성장, 연출 기준값 |
| BombDefinition | 종류별 레벨, 중심 기준 파괴 좌표, 별도 중계 거리 |
| PieceDefinition | 7종 초기 셀 좌표, 두 배 정수로 표현한 회전 중심 |
| ProgressionDefinitions | 스테이지 목표/조각 제한/완료 XP, 공통 강화, 유물 효과 |
| GameEnums / GridPosition | 종류 식별자와 Unity에 의존하지 않는 정수 좌표 |
| BoardData / PieceData | 고정 셀 저장, 조각 생성 정보, 활성 조각 위치/방향/타이머 |
| SupplyData | Bag/소비 위치, 미리 보기, 첫 등장 FIFO/예약 조각 ID, 독립 난수 상태 |
| RunBuildData / RunData | 판내 빌드, 선택 후보, 점수/XP/진행 상태/기록 |
| PlacementResultData | 묶음별 폭탄/중계/제거/이동 기록, 점수 내역과 정산 여부 |
| PersistentData | 설정/안내 확인/최고 기록/누적 횟수의 저장 DTO |

정의 데이터는 읽기 전용이며 새 판마다 복사하거나 수정하지 않습니다. 판내 상태는 별도 객체로 만듭니다. 범용 프레임워크, ScriptableObject, Scene/Prefab 참조는 추가하지 않았습니다.

## 사용 시작점

```csharp
using BombStack.Data;

GameDefinition definition = GameDefinition.CreateDefault();
RunData run = new RunData(definition, seed: 12345);
BombLevelDefinition basicLevel2 = definition.GetBomb(BOMB_TYPE.B01_BASIC).GetLevel(2);
StageDefinition firstStage = definition.GetStage(1);
```

이 시점의 상태는 `PREPARING`입니다. 빈 10×18 보드, 기본 폭탄 Lv.1, 플레이어 Lv.1, 0 XP, 남은 조각 20개만 준비합니다. 조각 추첨이나 자동 낙하는 시작하지 않습니다.

## 데이터 사용 규칙

- `CellData`와 `PieceData`에는 폭탄 **종류만** 보관합니다. 현재 레벨은 `run.Build.Bombs`에서 조회합니다. 기존 보드/미리 보기에도 같은 레벨을 적용할 수 있습니다.
- `DestructionOffsets`는 중심을 포함한 상대 좌표입니다. 보드 밖 절삭은 판정 코드에서 처리합니다. 중계는 `RelayDistance` 이내 같은 행/열의 다른 B만 대상으로 하며 파괴 좌표에 섞지 않습니다.
- `PieceDefinition.Cells`의 순서는 위 행부터 왼쪽→오른쪽입니다. 특수 셀 인덱스를 유지하면서 각 셀 좌표를 회전해야 O의 B/T 위치도 회전합니다.
- 배율은 두 배 정수입니다. 3은 1.5배입니다. R02의 `Value=1`은 +0.5배이며 기본 배율 상한 적용 후 더합니다. 파괴 점수 전체에서 한 번만 나눠 버림해야 합니다.
- 유물의 `DisplayName`은 개발용 한국어 이름입니다. 실제 한국어/영어 UI 문구 및 설명 테이블은 아직 추가하지 않았습니다.
- Bag 목록은 섞인 전체 목록, 인덱스는 다음 소비 위치입니다. `Preview`와 `FirstAppearances`는 앞에서부터 처리합니다. 미배정 예약의 `QueuedPieceId`는 null이며, 실제 생성 시 예약을 제거합니다.
- 종류 Bag은 보유 종류로 구성합니다. 신규 획득 시 Bag 재구성/미리 보기 예약은 `Core/SupplyRules`에서 처리합니다.
- 난수 스트림은 형태/속성/특수 위치/폭탄 종류/성장 후보/유물 후보의 6개입니다. 데이터는 `IsInitialized=false`로 시작하며, `Core/SupplyRules`가 시드로 초기화하고 SplitMix64로 전진시킵니다.
- 판내 상태의 쓰기 가능한 속성과 목록은 규칙 코드가 갱신할 공간입니다. 보유 상한, 후보 자격, 레벨업/유물 적용, 상태 전이와 중복 정산은 `Core/GameSession`을 통해 처리합니다. 데이터 객체를 직접 수정하면 이 절차를 우회합니다.
- `StageCompletionPending`, `StageCompletionExperienceGranted`, `IsSettled`는 중복 지급을 막을 처리에서 사용할 상태값입니다. 데이터 선언만으로 중복 지급이 방지되지는 않습니다.
- 스테이지 전환 시 `RunData`를 새로 만들면 안 됩니다. 보드/공급/빌드를 유지하고 스테이지 점수, 남은 조각, 완료 처리 상태를 갱신해야 합니다. 새 판에서만 새 `RunData`를 만듭니다.
- `RunRecordData.FromRun`은 결과 상태를 독립 복사합니다. 이후 판내 폭탄 레벨/유물을 바꾸어도 기록이 바뀌지 않습니다. 최고 기록 비교와 실제 파일 저장은 별도 작업입니다.
- 저장 DTO는 `[Serializable]`과 public 필드를 사용합니다. 진행 중 보드/시드/XP/Bag은 포함하지 않습니다. 판내 데이터는 저장 DTO가 아닙니다. 로드 후 버전/누락값/범위 검사와 실패 복구는 저장 처리에서 구현해야 합니다.

## 기획서에 없는 기본값

설정 초기값은 한국어, 1280×720 창 모드, 음량 각각 100으로 정했습니다. 화면 흔들림 켜짐, 번쩍임 약하게, 기본 연출은 기획서를 따릅니다. 설정을 실제 장치에 적용하는 코드는 없습니다.

## 이번 범위

이 폴더는 데이터 정의/기본값/상태 컨테이너를 담당합니다. 후속 프로토타입에서 폭발·회전·중력·점수/XP·공급·후보·승패·저장·임시 UI·입력·연출을 다른 계층에 연결했습니다. 수치가 기획서와 일치하는 것과 실제 밸런스가 적절한 것은 별개입니다.

## 최초 데이터 작업의 검증 기록

- 전체 데이터 코드의 C# 9 / .NET Standard 2.1 독립 컴파일 통과. 경고를 오류로 처리했습니다.
- 별도 .NET 실행기로 649개 데이터 검사 통과: 폭탄 범위/중계 분리, 조각 7종 좌표, 회전 중심으로 4회 회전 시 정수 좌표/생성 경계/셀 인덱스 복원, O 특수 셀 위치 변화, 스테이지/성장 기본값, 잘못된 셀/인덱스 거부, 새 판 상태 분리, 결과 기록 복사 등을 확인했습니다.
- 저장 DTO의 필드 왕복은 .NET의 System.Text.Json으로만 확인했습니다. 실제 Unity 직렬화기와 파일 저장/복구는 검증하지 않았습니다.
- Unity 에디터 컴파일/Scene 연결/Play Mode/배포 빌드는 실행하지 않았습니다. 회전 검사도 데이터의 기하 검증이며 실제 회전 조작 구현의 테스트가 아닙니다.
