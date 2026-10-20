# BOMB STACK 보드 UI 이미지 생성 프롬프트 v1

방식: 내장 ImageGen. 참고 이미지: 승인한 플레이 화면 컨셉. 아래 텍스트는 실제 도구에 전달한 프롬프트입니다. 프롬프트의 요청 크기 1024와 달리 실제 결과는 두 이미지 모두 1254×1254이며, 원본 크기 그대로 전달했습니다.

## BoardFrame_9Slice_v1.png

transparent_background: true

```text
Use case: ui-mockup. Asset type: production game UI sprite, SINGLE isolated square hollow border frame for Unity uGUI 9-slice, not a mockup screen. Reference image is ONLY approved visual style reference, specifically the steel-violet rectangular border surrounding the center game board. Generate a reusable square source sprite for this border: 1024x1024 square output requested. Shape is a perfectly front-facing flat square ring, axis aligned, straight parallel sides, NO perspective. Outer perimeter reaches the image edges, no generous blank margins. The central 768x768 window from approximately x=128 to896 and y=128 to896 must be COMPLETELY TRANSPARENT alpha zero. Outside the small clipped outer corners must also be transparent, no colored background. Border thickness about128px all around. Large corner bevel plates entirely inside each128x128 corner, a tiny inset bolt on each plate; all FOUR corners same visual weight. The middle lengths of all four edges are very simple straight smooth parallel dark metal bands; no central ornament, no bolts in straight sections, no cracks, no scratches, no texture noise in straight sections, so nine-slice stretching works cleanly. Outer dark navy almost-black outline, indigo blue steel body, subtle purple recessed inner bevel and restrained cool lavender top-left highlights, darker bottom-right shade. Premium restrained crisp pixel-art videogame UI, chunky coherent square pixel steps, no blurry antialias halos, no glow or cast shadow outside frame. All the material detail belongs to the frame ring. Transparent window has NO glass, no fill, NO gradient, NO grid, NO cells. NO red warning stripe, no text, no arrows, no logo, no bombs, no background city, no other sprite or screen. A practical empty metal picture frame style sprite matching the approved Bomb Stack screenshot, deliberately square because it will be nine-sliced into a tall 10x18 game board later. Full true RGBA transparency through the hollow middle and around clipped corners. No checkerboard baked in.
```

## BoardEmptyCell_v1.png

transparent_background: false

```text
Use case: ui-mockup. Asset type: ONE production game UI background tile sprite for a grid-based puzzle, opaque 1024x1024 square PNG, flat front orthographic. Reference image 1 is ONLY an approved style reference. Create exactly one EMPTY BOARD CELL TILE matching the almost-black navy empty squares in the central Bomb Stack game board, NOT an occupied stone block and NOT a frame. Tile fills entire square canvas edge-to-edge, absolutely zero margins, zero perspective, exact square, square unrounded corners. Very dark navy flat interior around hex #0A1024, restrained thin uniform desaturated slate-purple border around hex #242943, thin outermost darker outline. Border about20 source pixels wide on1024 square, evenly balanced and aligned to canvas edges, creating a thin1pixel-style grid when repeated and scaled down to48px each. The interior is completely empty, opaque matte, almost uniform, no design, no scratches, no bevel depth, no protruding corners, no dots, no large highlights, no noise or grain, no gradients, no texture, no background scene, no transparency, no text, no icon, no bomb, no spark, no diagonal lines, no diagonal highlight, no chess/checker pattern, no separate components, no 3D block shading, no ornamental metal frame, no inset floating square. Visual priority very low compared with foreground blocks, underlay only. Sharp clean pixel-art simple UI sprite, symmetrical thin flat border all four edges, square fills all pixels.
```

