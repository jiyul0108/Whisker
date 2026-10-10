# Whisker 고양이 스프라이트 (후처리 완료본, HD)

Gemini 원본 → 초록 배경 제거 → 프레임 분리 → 크기 통일 → 축소 → 발 기준(하단 중앙) 정렬

## 폴더 구성

| 폴더 | 파일 | 셀 크기 (px) | 비고 |
| --- | --- | --- | --- |
| reference | cat_ref_stand / sit / walk | 125 x 98 | 기준 캐릭터 (앉기 자세는 필요 시 대기로 활용 가능) |
| idle | cat_idle_calm, cat_idle_alert | 119 x 98 | 평온 / 경계 |
| walk | cat_walk_01~04 | 124 x 91 | 03번은 가까운 다리·먼 다리 명암을 바꿔 01번과 구분 |
| scaredwalk | cat_scaredwalk_01~04 | 142 x 60 | |
| jump | cat_jump_01~04 | 130 x 142 | 01 준비 / 02 상승 / 03 낙하 / 04 착지 |
| crawl | cat_crawl_01~02 | 140 x 77 | 원본 1·3번 프레임 |
| wallclimb | cat_wallclimb_01~04 | 61 x 137 | 03번은 01번 좌우 반전 |
| hide | cat_hide_01~02 | 105 x 80 | 01 눈 뜸 / 02 깜빡임 |
| scared | cat_scared_01~03 | 125 x 103 | 떨림 반복 |
| meow | cat_meow_01~02 | 106 x 98 | 작게 울기 |
| _preview | all_frames.png, 모션별 .gif | | 확인용 (Unity에 넣지 않아도 됨) |

각 모션 폴더에는 개별 프레임 PNG와 가로로 이어 붙인 `<모션>_sheet.png`가 함께 있어요. 둘 중 편한 쪽을 쓰면 돼요.

## Unity 임포트 설정

PNG를 `Assets/Art/Characters/Cat/Sprites/` 아래에 폴더째 넣고, 전부 선택한 뒤 Inspector에서:

| 항목 | 값 |
| --- | --- |
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single (개별 PNG) / Multiple (sheet 사용 시) |
| Pixels Per Unit | 128 |
| Pivot | Bottom Center (sheet는 Sprite Editor에서 지정) |
| Filter Mode | Point (no filter) |
| Compression | None |

Pixels Per Unit 128이면 걷는 고양이가 약 1 x 0.7 유닛이라, 지금 프로토타입의 네모 고양이(1 x 0.6)와 거의 같은 크기예요.

sheet를 쓸 때는 Sprite Editor → Slice → **Grid By Cell Size**에 위 표의 셀 크기를 넣으면 프레임별로 잘려요.

## 알아둘 점

- 모션마다 Gemini가 그린 고양이 크기와 픽셀 밀도가 달라서, 기준 캐릭터 크기에 맞춰 눈대중으로 크기를 통일했어요. Unity에서 나란히 놓고 어긋나 보이는 모션이 있으면 해당 모션의 Scale만 조정하면 돼요.
- 원본 해상도가 낮았던 걷기·겁먹은 채 걷기는 다른 모션보다 픽셀이 약간 굵어요.
- 서 있는 고양이 기준 약 96px 높이로, 원본의 외곽선과 무늬가 유지되는 해상도예요.
