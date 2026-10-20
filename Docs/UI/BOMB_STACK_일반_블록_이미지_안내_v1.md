# BOMB STACK 일반 블록 이미지 v1

- 파일: `Assets/Main/04_Resource/UI/Blocks/BlockNormal_v1.png`
- 실제 크기: 1254×1254
- 형식: PNG / RGB / 전체 불투명
- 용도: 일반 셀 N 하나. I/O/T/S/Z/J/L 각각의 조각 이미지가 아니라, 네 셀을 조합해 사용하는 공통 스프라이트.
- 스타일: 승인한 컨셉의 회청색 돌 블록, 밝은 위·왼쪽 테두리와 어두운 아래·오른쪽 테두리. 폭탄·트리거의 심볼과 색상이 먼저 읽히도록 일반 셀은 중립색을 사용.
- 기존 보드 이미지가 현재 `04_Resource/UI/Board`에 있으므로 같은 리소스 영역의 `UI/Blocks`에 추가.
- 원본 픽셀 편집·크롭·리사이즈 없이 복사.

## 수동 사용 권장값

- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Mesh Type: Full Rect
- Pixels Per Unit: 100
- Filter Mode: Point (no filter)
- Wrap Mode: Clamp
- Mip Maps: 끄기
- Compression: None
- Max Size: 2048 이상
- Image Type: Simple. 블록은 정사각형 그대로 사용하며 9-slice를 적용하지 않음.
- Image Color: 흰색. 원본에 색이 포함되어 있으므로 기존 NORMAL_COLOR를 그대로 곱하면 색이 더 어두워질 수 있음.
- Raycast Target: 끄기.
- 48×48 슬롯 안에 46×46으로 중앙 배치하는 초기안. 현재 InGameBoardGraphic.CellRect의 한쪽 1단위 여백과 같은 치수.
- 고정 블록과 활성 조각에 같은 이미지를 사용. 형태별로 다른 색을 임의 배정하지 않음.
- 고스트는 별도 외곽선 표시가 현재 기획과 맞으며, 일반 블록 PNG 하나로 모든 고스트 표시를 대신한 것은 아님.

## 현재 표시 코드와 연결

현재 Canvas UI의 `InGameBoardGraphic`과 `NextPieceGraphic`은 `ACellGraphic`으로 셀을 도형 렌더링합니다. 일반 블록용 Sprite를 지정하는 필드는 현재 없습니다.

따라서 이 PNG를 폴더에 추가하는 것만으로 게임 블록이 자동 교체되지는 않습니다. 나중에 Sprite 표시를 연결할 때 고정 셀·활성 조각·다음 조각을 함께 확인해야 합니다. 이번 요청은 이미지 제작이므로 C#·Scene·Prefab·Inspector·패키지·설정·.meta는 직접 변경하지 않았습니다.

## 확인 범위

파일을 열어 크기·RGB 모드·전체 불투명을 확인했습니다. 중앙 픽셀은 RGBA 환산 (106,121,157,255)입니다. PNG 무결성과 생성 원본과 복사본의 SHA-256 일치도 확인했습니다.

Unity 임포트·Play Mode·46px 축소 가독성·기존 렌더러 연결은 검증하지 않았습니다.

## 생성 방식과 실제 프롬프트

내장 ImageGen을 사용했습니다. 첫 결과는 중앙이 투명해 일반 블록으로 사용할 수 없었으며, 아래 수정 프롬프트로 불투명한 최종본을 만들었습니다. 첫 결과는 프로젝트에 복사하지 않았습니다.

### 최초 생성

참고: 승인한 전체 플레이 화면 컨셉. `transparent_background: true`.

```text
Use case: ui-mockup. Asset type: production Unity game UI sprite, ONE SINGLE normal occupied block tile for Bomb Stack, NOT a mockup, not a sprite sheet. Input reference image is ONLY the approved art style and material reference: match the small muted blue-grey stone blocks on its central game board. Primary request: exactly ONE square neutral normal block, straight-on orthographic 2D face, fills the square canvas edge-to-edge, zero external padding; all four block edges parallel to image edges, perfectly square width equals height. Export with true transparent alpha only in the tiny clipped corner pixels; rest is opaque. No background, no floor, no shadow or glow outside the square. Desired use: displayed around46x46 UI pixels inside a48x48 grid slot, so use broad calm shapes with clearly readable thin edges; do not add tiny details. Style: premium restrained crisp pixel art with a virtual approximately48x48-pixel visual design enlarged cleanly, controlled chunky square pixel steps, 6-8 muted cool slate/lavender/blue-grey shades, brightest face color around hex #8294B3 and base face #697B99, far brighter than near-black board, but substantially less saturated than bombs and orange trigger. Front face large quiet matte blue-grey stone/metal surface. Subtle narrow light cool bevel along TOP and LEFT; dark indigo narrow bevel along BOTTOM and RIGHT, darkest thin navy outer outline, small clipped1-virtual-pixel corners only. The bevel should feel shallow withNO perspective depth, not an isometric cube, no extruded side wall. Just two or three restrained pixel material flecks or very short hairline stone marks away from center; no bold X pattern, no large diagonal slash, no deep crack, no broken corner, no complex decorative frame. Center80percent must stay calm and visually empty, so groups of4 blocks read cleanly as a tetromino. No screws, no gems, no bright specular hotspot, no icon, no symbol, no text, no letters, no number, no bomb, no trigger, no grid, no second tile, no tile array. Keep the complete visible square tile snug against canvas bounds, no large transparent margins. This is an occupied NORMAL block in the puzzle, not an empty background tile. Match approved screenshot's hand-crafted subdued pixel stone block feeling and navy outlines. Return just this one usable isolated front-facing square sprite.
```

### 불투명 수정 — 전달한 최종 결과

편집 대상: 최초 생성 블록. `transparent_background: false`.

```text
Use case: precise-object-edit. Edit target: the supplied single square blue-grey normal block sprite. Fix ONLY the incorrect opacity and central surface: this must be a SOLID BLOCK, not a hollow frame. COMPLETELY FILL the entire central black/transparent hole and the surrounding dark translucent band with OPAQUE matte desaturated slate blue-grey stone, around RGB(110,127,154). The entire output canvas must be100percent opaque RGB, alpha255 everywhere, even the corners. There is NO cutout, NO transparent part, NO black hole, NO window, NO glass. Keep the outer square silhouette snug to canvas edges and retain the light upper/left bevel and dark bottom/right bevel. Use the same subdued pixel art stone block style but make the front face quiet and evenly lit, no strong gradient, a few tiny material flecks only. Broad flat opaque center is visibly blue-grey, not black. The front face should be the same lightness and color family as the existing solid area near upper-left face. Preserve a SINGLE perfectly square orthographic2D tile occupying the entire square canvas; no background beyond it, no shadows beyond it, no text, icon, bomb or extra tiles. This game sprite will cover a dark board cell, so it must be an opaque solid surface everywhere. Return exactly one solid normal block tile.
```

