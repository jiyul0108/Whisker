using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 에디터 메뉴: Whisker > 추가한 방 배치하기
// 3~10번 방(1층·지하·2층)의 범위, 배경, 출입구 기둥, 바닥·벽, 계단, 열쇠, 숨을 곳, 탈출문과
// 퍼즐(조사할 물건·의자·벽 타기), 이벤트(전원·추격), 화면(인벤토리·팝업)을 한 번에 배치한다
// 1·2번 방은 씬에 있는 것을 그대로 쓴다 (2번 방의 Desk_01만 옮김)
// 다시 실행하면 전에 만든 Rooms_Added를 지우고 새로 만든다 (Ctrl+Z로 되돌리기 가능)
public static class WhiskerMapBuilder
{
    private const string RootName = "Rooms_Added";

    // 층별 바닥 높이 (바닥판 윗면)
    private const float F1 = -2.5f;    // 1층
    private const float B1 = -22.5f;   // 지하
    private const float F2 = 17.5f;    // 2층

    private struct RoomDef
    {
        public string name; public float left; public float floor; public float width; public string bg;
        public bool dark; public bool litByPower;
        public RoomDef(string name, float left, float floor, float width, string bg, bool dark = false, bool litByPower = false)
        { this.name = name; this.left = left; this.floor = floor; this.width = width; this.bg = bg; this.dark = dark; this.litByPower = litByPower; }
    }

    // 방 이름, 왼쪽 끝 X, 층 바닥 높이, 가로 길이, 배경 파일, 어두운 방, 전기가 들어오면 밝아짐
    private static readonly RoomDef[] Rooms =
    {
        new RoomDef("3번 방",  26f,     F1, 25f,    "room03_bg"),               // 복도 (녹슨 열쇠)
        new RoomDef("4번 방",  51f,     F1, 17.88f, "room04_bg"),               // 계단실 (소화기함 = 땜납)
        new RoomDef("5번 방",  68.88f,  F1, 17.88f, "room05_bg"),               // 로비 (화분·안내데스크·탈출문)
        new RoomDef("6번 방",  45.355f, B1, 17.88f, "room06_bg", true, true),   // 지하 복도
        new RoomDef("7번 방",  63.235f, B1, 17.88f, "room07_bg", true, true),   // 창고 (두꺼비집, 상자 더미)
        new RoomDef("8번 방",  49.71f,  F2, 17.88f, "room08_bg"),               // 진료실 (금고, 책상)
        new RoomDef("9번 방",  31.83f,  F2, 17.88f, "room09_bg", true, false),  // 병실 (침대 4개 단서)
        new RoomDef("10번 방", 13.95f,  F2, 17.88f, "room10_bg"),               // 수술실 (수술대 = 조각 붙이기)
    };

    // 1번 방: 조작 안내 그림을 넣으려고 왼쪽으로 16칸 늘림 (-8 ~ 9 → -24 ~ 9, 배경도 3300px로 늘림)
    private const float Room1Left = -24f;
    private const float Room1Right = 9f;

    [MenuItem("Whisker/추가한 방 배치하기")]
    private static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Whisker", "Play를 끈 상태에서 실행해 주세요.", "확인");
            return;
        }

        // 반드시 Main 씬에 배치 (다른 씬이 열려 있으면 Main을 열고 진행)
        const string MainPath = "Assets/Scenes/Prototype/Main.unity";
        var active = EditorSceneManager.GetActiveScene();
        if (active.path != MainPath)
        {
            if (!EditorUtility.DisplayDialog("Whisker", $"지금 열린 씬은 '{active.name}'이에요.\n배치는 Main 씬에만 할 수 있어요. Main을 열고 배치할까요?",
                                             "Main 열고 배치", "취소")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(MainPath);
        }

        Sprite pillar = LoadSprite("Assets/Art/Props/exit_pillar.png");
        Sprite key = LoadSprite("Assets/Art/Props/key.png");
        Sprite stairsDoor = LoadSprite("Assets/Art/Props/stairs_door.png");
        Sprite stairsLock = LoadSprite("Assets/Art/Props/stairs_door_lock.png");
        Sprite fusebox = LoadSprite("Assets/Art/Props/fusebox.png");
        Sprite safe = LoadSprite("Assets/Art/Props/safe.png");
        Sprite chairSprite = LoadSprite("Assets/Art/Props/chair.png");
        Sprite bedCloseup = LoadSprite("Assets/Art/UI/bed_closeup.png");
        if (pillar == null || key == null || stairsDoor == null || stairsLock == null
            || fusebox == null || safe == null || chairSprite == null) return;

        // 전에 만든 것은 지우고 새로
        GameObject old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "방 배치");

        // 1) 방 범위 + 배경
        foreach (RoomDef r in Rooms)
        {
            string id = r.bg.Replace("_bg", "");   // room03

            GameObject room = Child(root, id, new Vector3(r.left + r.width * 0.5f, r.floor + 2f, 0f));
            var rc = room.AddComponent<Room>();
            var box = room.GetComponent<BoxCollider2D>();
            box.size = new Vector2(r.width, 7f);
            box.isTrigger = true;
            var so = new SerializedObject(rc);
            so.FindProperty("roomName").stringValue = r.name;
            so.FindProperty("isDark").boolValue = r.dark;
            so.FindProperty("litByPower").boolValue = r.litByPower;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject bg = Child(root, id + "_BG", new Vector3(r.left, r.floor, 0f));
            var sr = bg.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Assets/Art/Backgrounds/" + r.bg + ".png");
            sr.sortingOrder = -10;
        }

        // 2) 같은 층 방 사이 출입구 기둥 (출구 기둥 X = 경계 - 0.28, 입구 기둥 X = 경계 + 0.28)
        float[,] borders = { { 26f, F1 }, { 51f, F1 }, { 68.88f, F1 }, { 63.235f, B1 }, { 31.83f, F2 }, { 49.71f, F2 } };
        for (int i = 0; i < borders.GetLength(0); i++)
        {
            float x = borders[i, 0], y = borders[i, 1];
            Pillar(root, $"Pillar_{x}_L", x - 0.28f, y, false, pillar);
            Pillar(root, $"Pillar_{x}_R", x + 0.28f, y, true, pillar);
        }

        // 2-1) 1번 방 늘리기 (방 범위·배경·왼쪽 벽·고양이와 주인 시작 위치)
        SetupRoom1();

        // 2-2) 1번 → 2번 방 출입구: 둘 다 넘어가면 문이 닫힘 (1번 방 쪽 출입구 기둥 위치 8.72에 맞춤)
        GameObject door1 = Child(root, "Door_Room1", new Vector3(9f, F1, 0f));   // 출구 기둥(8.72)과 입구 기둥(9.28) 사이
        var d1col = door1.AddComponent<BoxCollider2D>();
        d1col.size = new Vector2(0.5f, 6f);
        d1col.offset = new Vector2(0f, 3f);
        var d1 = door1.AddComponent<AutoCloseDoor>();
        SetProps(d1, so =>
        {
            so.FindProperty("closeClip").objectReferenceValue = LoadClip("sfx_door_close");
            so.FindProperty("swingTime").floatValue = 1.65f;   // 끼익 소리 길이에 맞춰 천천히 닫히고, 끝에 쾅
        });
        var d1v = new GameObject("Visual");
        Undo.RegisterCreatedObjectUndo(d1v, "방 배치");
        d1v.transform.SetParent(door1.transform, false);
        var d1sr = d1v.AddComponent<SpriteRenderer>();
        d1sr.sprite = LoadSprite("Assets/Art/Props/room_door_closed.png");
        d1sr.sortingOrder = 4;   // 기둥(3)의 어두운 출입구를 덮음

        // 3) 바닥·벽: 1층은 씬의 Floor·WallRight를 늘리고 옮김, 지하·2층은 새로 만듦
        float f1Right = 68.88f + 17.88f;
        GameObject floor = GameObject.Find("Floor");
        if (floor != null)
        {
            Undo.RecordObject(floor.transform, "바닥 늘리기");
            float l = Room1Left - 2.5f, rr = f1Right + 1.5f;
            Vector3 p = floor.transform.position;
            floor.transform.position = new Vector3((l + rr) * 0.5f, p.y, p.z);
            Vector3 s = floor.transform.localScale;
            floor.transform.localScale = new Vector3(rr - l, s.y, s.z);
        }
        else Debug.LogWarning("Floor 오브젝트를 찾지 못했어요. 1층 바닥을 직접 늘려 주세요.");

        GameObject wall = GameObject.Find("WallRight");
        if (wall != null)
        {
            Undo.RecordObject(wall.transform, "오른쪽 벽 이동");
            Vector3 p = wall.transform.position;
            wall.transform.position = new Vector3(f1Right + 0.5f, p.y, p.z);
        }
        else Debug.LogWarning("WallRight 오브젝트를 찾지 못했어요.");

        FloorAndWalls(root, "B1", 45.355f, 63.235f + 17.88f, B1);
        FloorAndWalls(root, "F2", 13.95f, 49.71f + 17.88f, F2);

        // 4) 계단 (4번 계단실 ↔ 지하 6번, 4번 계단실 ↔ 2층 8번). 1층 쪽만 잠김
        //    지하: 녹슨 열쇠(자물쇠), 2층: 전자 잠금(전기가 들어오면 열림)
        Stairs downB1 = MakeStairs(root, "Stairs_1F_toB1", 55.0f, F1, "지하로 내려가기", "A", "녹슨 열쇠", "");
        Stairs upFromB1 = MakeStairs(root, "Stairs_B1_to1F", 55.0f, B1, "1층으로 올라가기", "", "", "");
        Stairs up2F = MakeStairs(root, "Stairs_1F_to2F", 64.955f, F1, "2층으로 올라가기", "", "", "POWER");
        Stairs downFrom2F = MakeStairs(root, "Stairs_2F_to1F", 64.955f, F2, "1층으로 내려가기", "", "", "");
        StairsDoor(downB1, stairsDoor, stairsLock, 0f, 0.14f, 1f);
        StairsDoor(up2F, stairsDoor, null, -0.035f, 0.14f, 0.8f);   // 전자 잠금: 자물쇠 없이 문만
        Link(downB1, upFromB1);
        Link(upFromB1, downB1);
        Link(up2F, downFrom2F);
        Link(downFrom2F, up2F);

        // 5) 열쇠: 녹슨 열쇠만 (작은 열쇠·출구 열쇠는 퍼즐로 바뀜)
        Key(root, "Key_A", new Vector3(46f, F1 + 0.1f, 0f), "A", "녹슨 열쇠", key);        // 3번 복도 바닥에 떨어져 있음
        var keyA = GameObject.Find("Key_A");
        if (keyA != null) SetProps(keyA.GetComponent<KeyItem>(), so => so.FindProperty("bobHeight").floatValue = 0f);   // 바닥이라 둥실거리지 않음
        DestroyIfFound("Key_B");
        DestroyIfFound("Key_C");

        // 6) 숨을 곳 (배경에 그려진 물건 위치). 숨을 때 소리: 상자 = sfx_hide_box, 책상·수술대 = sfx_hide_desk
        AudioClip boxClip = LoadClip("sfx_hide_box");
        AudioClip deskClip = LoadClip("sfx_hide_desk");
        // 3번 복도 오른쪽: 2번 방 배경의 책상과 같은 그림 (desk.png) + 숨을 곳 (들킬 때 이 그림이 들썩임)
        var desk03 = Prop(root, "Desk_03", LoadSprite("Assets/Art/Props/desk.png"), new Vector3(44.6f, F1 + 0.47f, 0f));   // 다리가 캐릭터 발 높이에, 녹슨 열쇠(46) 왼쪽
        Hide(root, "Hide_Desk_03", new Vector3(44.6f, F1, 0f), new Vector2(2.0f, 1.2f), "책상", deskClip, desk03);
        Hide(root, "Hide_Boxes_07", new Vector3(72.285f, B1, 0f), new Vector2(2.4f, 1.2f), "상자 더미", boxClip);
        Hide(root, "Hide_Desk_08", new Vector3(58.885f, F2, 0f), new Vector2(3.0f, 1.2f), "책상", deskClip);
        Hide(root, "Hide_Table_10", new Vector3(23.12f, F2, 0f), new Vector2(3.0f, 1.2f), "수술대", deskClip);
        // 5번 로비: 배경의 안내데스크 뒤 (탈출문 쪽에서 쫓길 때 숨을 곳)
        Hide(root, "Hide_Desk_05", new Vector3(78.68f, F1, 0f), new Vector2(2.6f, 1.2f), "안내데스크", deskClip);
        // 1번 방: 숨기 안내 그림(x -0.8) 바로 밑에 연습용 상자
        var tutBox = Prop(root, "TutorialBox_01", LoadSprite("Assets/Art/Props/box.png"), new Vector3(-0.8f, F1, 0f));
        if (tutBox != null) tutBox.color = new Color(0.55f, 0.5f, 0.45f);   // 어두운 병원 분위기에 맞게 어둡게
        Hide(root, "Hide_TutorialBox_01", new Vector3(-0.8f, F1, 0f), new Vector2(1.8f, 1.2f), "상자", boxClip, tutBox);
        MoveDesk01(23.26f, deskClip);   // 2번 방 배경의 책상 위치에 맞춰 숨을 곳도 옮김 (문과 너무 가까워 128px 왼쪽으로)

        // 7) 탈출문: 5번 로비의 쇠사슬 정문 (그림은 배경에 있으므로 판정만, 지나갈 수 있음)
        ExitDoor(root, "ExitDoor_Lobby", 84.06f, F1, 2.8f, "C", "출구 열쇠");

        // 7-1) 지하: 오른쪽 안쪽(7번 창고)의 고장 난 두꺼비집이 지지직거림 → 소리로 오른쪽에 무언가 있다는 걸 알려줌
        //      (같은 층에서만, 가까울수록 크게, 오른쪽에서 들림. 퓨즈를 끼워 전기가 들어오면 멈춤)
        var buzz = Child(root, "Buzz_Fusebox_07", new Vector3(77.2f, B1 + 1.5f, 0f)).AddComponent<SoundEmitter>();
        SetProps(buzz, so =>
        {
            so.FindProperty("clip").objectReferenceValue = LoadClip("sfx_electric_buzz");
            so.FindProperty("volume").floatValue = 0.7f;
            so.FindProperty("hearDistance").floatValue = 28f;   // 계단(55)에서도 희미하게 들림
            so.FindProperty("stopFlag").stringValue = "POWER";
        });

        // 8) 예전 시험용 잠긴 문은 이제 계단 잠금으로 대신함
        DestroyIfFound("LockedDoor_01");

        // 9) 깜짝 등장 연출 (고양이가 처음 지나갈 때 한 번, 고양이가 바라보는 앞쪽에 잠깐 보였다 사라짐)
        Glimpse(root, "Glimpse_Corridor_03", 28.5f, F1, "", "", 34f);   // 3번 복도에 처음 들어서면 → 쪽지(x 34) 바로 위에 서 있음
        Note(root, "FuseNote_03", 34f, F1);                                // 퓨즈 위치를 암시하는 쪽지
        Glimpse(root, "Glimpse_Basement_06", 52.5f, B1, "", "");       // 지하에 내려와 왼쪽(지하 복도)으로 가면
        Glimpse(root, "Glimpse_2F_08", 61f, F2, "", "");               // 2층에 처음 올라와 진료실로 가면
        Glimpse(root, "Glimpse_Ward_09", 48.5f, F2, "", "");           // 병실에 처음 들어서면

        // 9) 퍼즐
        BuildPuzzles(root, fusebox, safe, chairSprite, key);

        // 10) 이벤트 (전원·추격)
        var power = Child(root, "PowerEvent", Vector3.zero).AddComponent<PowerEvent>();
        var pso = new SerializedObject(power);
        pso.FindProperty("doorStairs").objectReferenceValue = up2F;
        pso.FindProperty("arrivalStairs").objectReferenceValue = upFromB1;
        pso.FindProperty("hideTime").floatValue = 6f;      // 문이 열리고 존재가 내려오기까지 (퓨즈를 끼우는 동안에는 존재가 오지 않음)
        pso.FindProperty("sweepSpeed").floatValue = 2f;    // 지하를 훑는 속도 조금 느리게
        // 훑는 순서: 계단(55)을 내려오면 바로 오른쪽 창고 끝(80.2) → 왼쪽 지하 복도(46.2) → 계단(55)
        var sweep = pso.FindProperty("sweepXs");
        float[] sweepOrder = { 80.2f, 46.2f, 55f };
        sweep.arraySize = sweepOrder.Length;
        for (int i = 0; i < sweepOrder.Length; i++) sweep.GetArrayElementAtIndex(i).floatValue = sweepOrder[i];
        pso.FindProperty("thudClip").objectReferenceValue = LoadClip("sfx_power_thud");
        pso.FindProperty("doorClip").objectReferenceValue = LoadClip("sfx_door_open");
        pso.ApplyModifiedPropertiesWithoutUndo();

        var chase = Child(root, "ChaseEvent", Vector3.zero).AddComponent<ChaseEvent>();
        var cso = new SerializedObject(chase);
        cso.FindProperty("tableTop").vector2Value = new Vector2(23.12f, F2 + 1.55f);
        cso.FindProperty("floorY").floatValue = F2;
        cso.FindProperty("triggerItem").stringValue = "C";
        cso.FindProperty("triggerX").floatValue = 29f;
        cso.FindProperty("dropClip").objectReferenceValue = LoadClip("sfx_drop_impact");
        cso.ApplyModifiedPropertiesWithoutUndo();

        // 11) 화면 (오른쪽 위 아이템 + Tab 인벤토리, 가운데 팝업)
        var ui = Child(root, "UI_Managers", Vector3.zero);
        var inv = ui.AddComponent<InventoryUI>();
        SetProps(inv, so => so.FindProperty("pickupClip").objectReferenceValue = LoadClip("sfx_item_pickup"));
        var amb = ui.AddComponent<Ambience>();   // 배경 소리
        // 1번 방 벽의 그림 안내 (이동·점프·울기·숨기·조사·가방). 그림은 Resources/Tutorial에서 스스로 불러옴
        Child(root, "TutorialHints", Vector3.zero).AddComponent<TutorialHints>();
        SetProps(amb, so =>
        {
            so.FindProperty("loopClip").objectReferenceValue = LoadClip("amb_hospital_loop");
            so.FindProperty("chaseClip").objectReferenceValue = LoadClip("bgm_presence_loop");   // 존재가 나왔을 때 음악 (없으면 바뀌지 않음)
        });
        // Esc 일시정지·설정 메뉴 (소리 크기를 바꿀 때 아이템 줍는 소리로 미리 들려줌)
        var pause = ui.AddComponent<PauseMenu>();
        SetProps(pause, so =>
        {
            so.FindProperty("previewClip").objectReferenceValue = LoadClip("sfx_item_pickup");
            so.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Galmuri11.ttf");
        });
        ui.AddComponent<OwnerStatusUI>();   // 가운데 위: 주인이 따라오는 중인지 표시
        var craft = ui.AddComponent<CraftUI>();
        SetProps(craft, so => so.FindProperty("loopClip").objectReferenceValue = LoadClip("sfx_craft_loop"));
        var popup = ui.AddComponent<PopupUI>();
        SetProps(popup, so =>
        {
            so.FindProperty("safeOpenClip").objectReferenceValue = LoadClip("sfx_safe_open");
            so.FindProperty("safeFailClip").objectReferenceValue = LoadClip("sfx_safe_fail");
            so.FindProperty("paperClip").objectReferenceValue = LoadClip("sfx_paper");
        });
        if (bedCloseup != null)
        {
            var uso = new SerializedObject(popup);
            uso.FindProperty("bedCloseup").objectReferenceValue = bedCloseup;
            uso.ApplyModifiedPropertiesWithoutUndo();
        }

        // 12) 고양이·주인에 새 기능 붙이기 (이미 있으면 그대로)
        SetupCatAndOwner();

        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);   // 저장까지 한 번에 (Ctrl+S 안 해도 됨)
        Selection.activeGameObject = root;
        Debug.Log($"[{root.scene.name}] 방 {Rooms.Length}개 + 계단 4개 + 퍼즐·이벤트 배치 완료, 씬 저장함");
    }

    // ---------- 퍼즐 ----------
    private static void BuildPuzzles(GameObject root, Sprite fusebox, Sprite safe, Sprite chairSprite, Sprite key)
    {
        // [2번 방] 의자(밀 수 있음) + 시계 밑 벽 타기 + 시계 = 퓨즈 (두꺼비집을 먼저 조사해야 나옴)
        var chair = Child(root, "Chair_02", new Vector3(19.5f, F1, 0f));
        SetGround(chair);
        var csr = chair.AddComponent<SpriteRenderer>();
        csr.sprite = chairSprite;
        csr.sortingOrder = 2;
        var crb = chair.AddComponent<Rigidbody2D>();
        crb.mass = 3f;
        crb.linearDamping = 2f;
        crb.freezeRotation = true;
        crb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        crb.interpolation = RigidbodyInterpolation2D.Interpolate;   // 높은 프레임에서도 부드럽게
        var ccol = chair.AddComponent<BoxCollider2D>();
        ccol.size = new Vector2(0.48f, 0.42f);     // 좌석 높이까지만 (올라설 수 있게)
        ccol.offset = new Vector2(0f, 0.21f);
        chair.AddComponent<CatOnlyProp>();         // 주인·존재는 부딪히지 않고 지나감
        // 2번 방 안에서만 밀림 (다른 방으로 못 옮기고, 벽에 딱 붙지 않아 다시 꺼낼 수 있음)
        var limit = chair.AddComponent<PushLimit>();
        SetProps(limit, so =>
        {
            so.FindProperty("minX").floatValue = 10.8f;    // 1번 방 문(9)에서 고양이 한 마리 들어갈 틈
            so.FindProperty("maxX").floatValue = 24.4f;    // 3번 방 출입구(26) 앞
        });
        var chairSound = chair.AddComponent<ChairSound>();   // 밀 때 끌리는 소리
        SetProps(chairSound, so => so.FindProperty("dragClip").objectReferenceValue = LoadClip("sfx_chair_drag"));

        Vector3 clock = new Vector3(21.8f, F1 + 2.22f, 0f);
        var climb = Child(root, "ClimbSpot_Clock", clock).AddComponent<ClimbSpot>();
        var clso = new SerializedObject(climb);
        clso.FindProperty("chair").objectReferenceValue = ccol;
        clso.ApplyModifiedPropertiesWithoutUndo();

        var clockI = Inter(root, "Clock_02", clock, new Vector2(1.0f, 1.0f), 0f, "시계 조사하기");
        SetProps(clockI, so =>
        {
            so.FindProperty("requireClimbing").boolValue = true;
            // 두꺼비집을 먼저 조사해야 퓨즈가 나옴 (그 전에는 노란 메시지로 안내, 조사한 뒤부터 시계가 반짝임)
            so.FindProperty("requiredFlag").stringValue = "FUSEBOX_CHECKED";
            so.FindProperty("flagMissingMessage").stringValue = "시계 뒤에 무언가 붙어 있다. 먼저 지하 두꺼비집을 확인해 보자";
            so.FindProperty("alwaysShowMessages").boolValue = true;
            so.FindProperty("rewardItem").stringValue = "FUSE";
            so.FindProperty("successMessage").stringValue = "시계 뒤에 무언가 붙어 있다… 퓨즈를 얻었다";
            so.FindProperty("doneMessage").stringValue = "멈춘 시계다";
        });

        // [7번 방] 두꺼비집 (퓨즈 없으면 조사만 → FUSEBOX_CHECKED, 퓨즈 있으면 전원 이벤트)
        Prop(root, "Fusebox_07", fusebox, new Vector3(77.2f, B1 + 1.5f, 0f));
        var fuseI = Inter(root, "FuseboxCheck_07", new Vector3(77.2f, B1, 0f), new Vector2(1.4f, 2.5f), 1.25f, "두꺼비집 작동하기");
        SetProps(fuseI, so =>
        {
            SetArray(so, "requiredItems", "FUSE");
            so.FindProperty("missingMessage").stringValue = "작동하지 않는다. 퓨즈가 하나 빠져 있다… 예비 퓨즈를 찾아야 한다";
            so.FindProperty("alwaysShowMessages").boolValue = true;   // 두꺼비집 문구는 노란 메시지로
            so.FindProperty("listMissing").boolValue = false;
            so.FindProperty("missingSetsFlag").stringValue = "FUSEBOX_CHECKED";
            so.FindProperty("readyLabel").stringValue = "퓨즈 끼우기";
            so.FindProperty("successMessage").stringValue = "퓨즈를 끼웠다";
            so.FindProperty("useGauge").boolValue = true;
            so.FindProperty("craftTime").floatValue = 6f;
            so.FindProperty("craftTitle").stringValue = "녹슨 두꺼비집에 퓨즈를 끼우는 중…";
            so.FindProperty("doneMessage").stringValue = "전기가 들어와 있다";
            so.FindProperty("action").enumValueIndex = (int)Interactable.Action.PowerOn;
        });

        // [4번 방] 계단 사이 벽의 소화기함 = 땜납 (그림은 배경에 있음, 바닥부터 소화기함 높이까지 조사 범위)
        var extI = Inter(root, "Extinguisher_04", new Vector3(61.46f, F1, 0f), new Vector2(1.2f, 2.4f), 1.2f, "소화기함 살펴보기");
        SetProps(extI, so =>
        {
            so.FindProperty("rewardItem").stringValue = "SOLDER";
            so.FindProperty("successMessage").stringValue = "소화기 뒤에 무언가 끼워져 있다… 땜납을 얻었다";
            so.FindProperty("doneMessage").stringValue = "녹슨 소화기다";
        });

        // [5번 방] 화분 = 열쇠 조각 (끝), 안내데스크 = 간호 일지 (색 순서)
        var potI = Inter(root, "Flowerpot_05", new Vector3(72.58f, F1, 0f), new Vector2(1.0f, 1.4f), 0.7f, "화분 살펴보기");
        SetProps(potI, so =>
        {
            so.FindProperty("rewardItem").stringValue = "PIECE2";
            so.FindProperty("successMessage").stringValue = "화분 흙 속에서 열쇠 조각을 찾았다";
            so.FindProperty("doneMessage").stringValue = "깨진 화분이다";
        });

        var docI = Inter(root, "NurseLog_05", new Vector3(78.68f, F1, 0f), new Vector2(2.2f, 1.4f), 0.7f, "문서 읽기");
        SetProps(docI, so =>
        {
            so.FindProperty("action").enumValueIndex = (int)Interactable.Action.ShowText;
            so.FindProperty("popupTitle").stringValue = "간호 일지";
            so.FindProperty("popupBody").stringValue =
                "…오늘부터 새벽 회진 순서를 바꾼다.\n\n" +
                "해가 뜨는 색의 침대에서 시작해,\n" +
                "숲을 지나,\n" +
                "피를 본 뒤,\n" +
                "바다에서 끝낸다.\n\n" +
                "순서를 잊지 말 것. 진료실 금고 번호도 같은 순서다.";
        });

        // [9번 방] 침대 4개 (왼쪽부터 빨강 7, 파랑 2, 노랑 9, 초록 4) → 정답 9472
        //        단서는 그림(bed_closeup_1~4)의 이불 끝 꼬리표에 그려져 있음
        Bed(root, "Bed1_09", 38.23f, new Color(0.75f, 0.15f, 0.12f), "7", 1);
        Bed(root, "Bed2_09", 41.43f, new Color(0.20f, 0.35f, 0.75f), "2", 2);
        Bed(root, "Bed3_09", 44.53f, new Color(0.85f, 0.75f, 0.20f), "9", 3);
        Bed(root, "Bed4_09", 47.63f, new Color(0.20f, 0.60f, 0.30f), "4", 4);

        // [8번 방] 금고 = 열쇠 조각 (손잡이)
        Prop(root, "Safe_08", safe, new Vector3(52.71f, F2, 0f));
        var safeI = Inter(root, "SafeLock_08", new Vector3(52.71f, F2, 0f), new Vector2(1.2f, 1.2f), 0.6f, "금고 조사하기");
        SetProps(safeI, so =>
        {
            so.FindProperty("action").enumValueIndex = (int)Interactable.Action.Keypad;
            so.FindProperty("keypadCode").stringValue = "9472";
            so.FindProperty("rewardItem").stringValue = "PIECE1";
            so.FindProperty("successMessage").stringValue = "철컥— 금고가 열렸다. 열쇠 조각을 얻었다";
            so.FindProperty("doneMessage").stringValue = "빈 금고다";
        });

        // [10번 방] 수술대: 조각 2개 + 땜납 → 10초 동안 만들기(게이지) → 출구 열쇠가 바닥에 떨어짐
        //            열쇠를 줍고 문 쪽(오른쪽)으로 가면 ChaseEvent가 추격 시작
        GameObject exitKey = Child(root, "ExitKey_10", new Vector3(24.4f, F2 + 0.2f, 0f));
        var ekBox = exitKey.AddComponent<BoxCollider2D>();
        ekBox.size = new Vector2(0.5f, 0.4f);
        ekBox.isTrigger = true;
        var ekItem = exitKey.AddComponent<KeyItem>();
        SetProps(ekItem, so =>
        {
            so.FindProperty("keyId").stringValue = "C";
            so.FindProperty("keyName").stringValue = "출구 열쇠";
        });
        var ekVisual = new GameObject("Visual");
        Undo.RegisterCreatedObjectUndo(ekVisual, "방 배치");
        ekVisual.transform.SetParent(exitKey.transform, false);
        var ekSr = ekVisual.AddComponent<SpriteRenderer>();
        ekSr.sprite = key;
        ekSr.color = new Color(1f, 0.85f, 0.55f);   // 새로 붙인 납 빛깔 (녹슨 열쇠와 구분)
        ekSr.sortingOrder = 6;
        exitKey.SetActive(false);                   // 다 만들면 나타남

        var tableI = Inter(root, "OperatingTable_10", new Vector3(23.12f, F2, 0f), new Vector2(3.0f, 1.6f), 0.8f, "열쇠 조각 붙이기");
        SetProps(tableI, so =>
        {
            SetArray(so, "requiredItems", "PIECE1", "PIECE2", "SOLDER");
            // 처음부터 "열쇠 조각 붙이기"로 보이고, 재료가 모자라면 노란 메시지로 모자란 것을 알려줌
            so.FindProperty("hideUntilReady").boolValue = false;
            so.FindProperty("alwaysShowMessages").boolValue = true;
            so.FindProperty("listMissing").boolValue = true;
            so.FindProperty("missingMessage").stringValue = "재료가 부족하다";
            so.FindProperty("rewardItem").stringValue = "";
            so.FindProperty("successMessage").stringValue = "출구 열쇠가 완성됐다! 톡— 바닥에 떨어졌다";
            so.FindProperty("doneMessage").stringValue = "피 묻은 수술대다";
            so.FindProperty("action").enumValueIndex = (int)Interactable.Action.Craft;
            so.FindProperty("craftTime").floatValue = 10f;
            so.FindProperty("craftTitle").stringValue = "전기 소작기로 열쇠 조각을 붙이는 중…";
            so.FindProperty("readyLabel").stringValue = "열쇠 조각 붙이기";
            so.FindProperty("revealObject").objectReferenceValue = exitKey;
            so.FindProperty("revealDropHeight").floatValue = 1.4f;
        });
    }

    private static void Bed(GameObject root, string name, float x, Color color, string number, int imageIndex)
    {
        Sprite img = LoadSprite($"Assets/Art/UI/bed_closeup_{imageIndex}.png");
        var bed = Inter(root, name, new Vector3(x, F2, 0f), new Vector2(2.4f, 1.4f), 0.7f, "침대 살펴보기");
        SetProps(bed, so =>
        {
            so.FindProperty("action").enumValueIndex = (int)Interactable.Action.ShowBed;
            so.FindProperty("bedColor").colorValue = color;
            so.FindProperty("bedNumber").stringValue = number;
            so.FindProperty("bedImage").objectReferenceValue = img;
        });
    }

    // ---------- 고양이·주인 ----------
    private static void SetupCatAndOwner()
    {
        // 물리는 1초에 50번 움직이므로, 그 사이 프레임을 채워 그리게 함 (60·120·144 프레임에서 끊김 없이)
        foreach (var rb in new Rigidbody2D[] {
                     Object.FindAnyObjectByType<CatController>()?.GetComponent<Rigidbody2D>(),
                     Object.FindAnyObjectByType<OwnerController>()?.GetComponent<Rigidbody2D>(),
                     Object.FindAnyObjectByType<PresenceAI>(FindObjectsInactive.Include)?.GetComponent<Rigidbody2D>() })
        {
            if (rb == null) continue;
            Undo.RecordObject(rb, "보간 켜기");
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        var cat = Object.FindAnyObjectByType<CatController>();
        if (cat != null)
        {
            // 벽 타기 그림
            var anim = cat.GetComponent<CatAnimator>();
            if (anim != null)
            {
                var so = new SerializedObject(anim);
                var arr = so.FindProperty("climbFrames");
                arr.arraySize = 4;
                for (int i = 0; i < 4; i++)
                    arr.GetArrayElementAtIndex(i).objectReferenceValue =
                        LoadSprite($"Assets/Art/Characters/Cat/Sprites/wallclimb/cat_wallclimb_0{i + 1}.png");
                so.ApplyModifiedProperties();
            }
            // 심장 소리
            var hb = cat.GetComponent<Heartbeat>();
            if (hb == null) hb = Undo.AddComponent<Heartbeat>(cat.gameObject);
            var hso = new SerializedObject(hb);
            hso.FindProperty("beatClip").objectReferenceValue = LoadClip("sfx_heartbeat");
            hso.ApplyModifiedProperties();
            // 숨기·나오기 소리
            var hide = cat.GetComponent<HideController>();
            if (hide != null)
            {
                var hdso = new SerializedObject(hide);
                hdso.FindProperty("hideClip").objectReferenceValue = LoadClip("sfx_hide");
                hdso.FindProperty("boxClip").objectReferenceValue = LoadClip("sfx_hide_box");     // 상자에 숨을 때
                hdso.FindProperty("deskClip").objectReferenceValue = LoadClip("sfx_hide_desk");   // 책상(2번 방 포함)·수술대에 숨을 때
                hdso.ApplyModifiedProperties();
            }
            // 고양이 걸음 소리는 쓰지 않음 (예전에 붙인 것이 있으면 제거)
            var catStep = cat.GetComponent<FootstepLoop>();
            if (catStep != null) Undo.DestroyObjectImmediate(catStep);
            // 울음소리
            var meow = cat.GetComponent<CatMeow>();
            if (meow != null)
            {
                var mso = new SerializedObject(meow);
                var arr = mso.FindProperty("meowClips");
                arr.arraySize = 3;   // 울 때마다 3개 중 하나
                for (int i = 0; i < 3; i++)
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = LoadClip($"sfx_meow_{i + 1}");
                mso.ApplyModifiedProperties();
            }
            // 시야 (주변은 어둡게, 눈에서 원뿔 모양 빛)
            if (cat.GetComponent<CatVision>() == null) Undo.AddComponent<CatVision>(cat.gameObject);
        }
        else Debug.LogWarning("고양이(CatController)를 찾지 못했어요");

        // 존재 발견 소리, 잡혔을 때 소리
        var presence = Object.FindAnyObjectByType<PresenceAI>(FindObjectsInactive.Include);
        if (presence != null)
        {
            var pso = new SerializedObject(presence);
            pso.FindProperty("noticeClip").objectReferenceValue = LoadClip("sfx_presence_notice");
            pso.FindProperty("crawlClip").objectReferenceValue = LoadClip("sfx_presence_crawl");   // 기어다니는 소리
            pso.FindProperty("stairsClip").objectReferenceValue = LoadClip("sfx_stairs");          // 계단 (고양이와 같은 소리)
            // 나타나기: 조금 더 멀리서, 천천히 드러나게 (갑자기 튀어나오는 느낌 줄이기)
            pso.FindProperty("appearDistance").vector2Value = new Vector2(8f, 11f);
            pso.FindProperty("emergeTime").floatValue = 2.5f;
            // 고양이가 원뿔 빛으로 보는 거리(7)에서 존재도 고양이를 발견
            pso.FindProperty("noticeRange").floatValue = 7f;
            pso.FindProperty("sightRadius").floatValue = 7f;
            // 1번 방에는 절대 들어가지 않음. 2번 방에는 갑자기 나타나지 않지만 쫓아서 들어올 수는 있음
            SetArray(pso, "safeRoomNames", "1번 방");
            SetArray(pso, "noAppearRoomNames", "2번 방");
            // 쫓을 때 쉽게 포기하지 않음 (멀어지면 빨라지고, 더 멀리·더 오래 떨어져야 포기)
            pso.FindProperty("loseRange").floatValue = 16f;
            pso.FindProperty("loseTime").floatValue = 6f;
            // 깜짝 등장: 다가가면 사라지며 들리는 웃음소리 (파일이 없으면 소리 없음)
            pso.FindProperty("glimpseClip").objectReferenceValue = LoadClip("sfx_presence_laugh");
            pso.FindProperty("glimpseVolume").floatValue = 0.8f;
            pso.FindProperty("glimpseChance").floatValue = 0f;   // 무작위 깜짝 등장 끄기 (정해진 자리에서만)
            // 숨은 곳 들추는 그림 (Presence/Sprites/lift/presence_lift_01~ 이 있으면 씀, 없으면 기울이는 동작으로 대신)
            var lift = pso.FindProperty("liftFrames");
            var liftSprites = new System.Collections.Generic.List<Sprite>();
            for (int i = 1; i <= 6; i++)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Characters/Presence/Sprites/lift/presence_lift_0{i}.png");
                if (sp != null) liftSprites.Add(sp);
            }
            lift.arraySize = liftSprites.Count;
            for (int i = 0; i < liftSprites.Count; i++) lift.GetArrayElementAtIndex(i).objectReferenceValue = liftSprites[i];
            pso.ApplyModifiedProperties();
        }
        else Debug.LogWarning("존재(PresenceAI)를 찾지 못했어요");
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            var gso = new SerializedObject(gm);
            gso.FindProperty("caughtClip").objectReferenceValue = LoadClip("sfx_caught");
            gso.FindProperty("clearClip").objectReferenceValue = LoadClip("sfx_escape");
            gso.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Galmuri11.ttf");
            gso.ApplyModifiedProperties();
        }

        var owner = Object.FindAnyObjectByType<OwnerController>();
        if (owner != null)
        {
            var fear = owner.GetComponent<OwnerFear>();
            if (fear == null) fear = Undo.AddComponent<OwnerFear>(owner.gameObject);
            var fso = new SerializedObject(fear);
            fso.FindProperty("screamClip").objectReferenceValue = LoadClip("sfx_owner_scream");   // 공포도가 가득 차면 비명
            fso.ApplyModifiedProperties();
            // 걸음 소리 (작게)
            var ownerStep = owner.GetComponent<FootstepLoop>();
            if (ownerStep == null) ownerStep = Undo.AddComponent<FootstepLoop>(owner.gameObject);
            var oso = new SerializedObject(ownerStep);
            oso.FindProperty("stepClip").objectReferenceValue = LoadClip("sfx_owner_step");
            oso.FindProperty("volume").floatValue = 0.35f;
            oso.ApplyModifiedProperties();
        }
        else Debug.LogWarning("주인(OwnerController)을 찾지 못했어요");
    }

    // ---------- 만들기 도우미 ----------

    private static Sprite LoadSprite(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogError("그림을 찾지 못했어요: " + path);
        return s;
    }

    // Assets/Audio/의 소리 파일 (없으면 빈칸 = 소리 없음)
    private static AudioClip LoadClip(string name)
    {
        var c = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + name + ".wav");
        if (c == null) Debug.LogWarning("소리를 찾지 못했어요: Assets/Audio/" + name + ".wav");
        return c;
    }

    private static GameObject Child(GameObject root, string name, Vector3 pos)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "방 배치");
        go.transform.SetParent(root.transform, false);
        go.transform.position = pos;
        return go;
    }

    private static void DestroyIfFound(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) Undo.DestroyObjectImmediate(go);
    }

    // 씬에 같은 이름이 이미 있으면(예: Key_A) 옮기고, 없으면 새로 만든다
    private static GameObject FindOrCreate(GameObject root, string name, Vector3 pos, out bool created)
    {
        GameObject go = GameObject.Find(name);
        if (go != null)
        {
            Undo.RecordObject(go.transform, "옮기기");
            go.transform.position = pos;
            created = false;
            return go;
        }
        created = true;
        return Child(root, name, pos);
    }

    private static void SetupRoom1()
    {
        GameObject room = GameObject.Find("Room01");
        if (room != null)
        {
            Undo.RecordObject(room.transform, "1번 방 늘리기");
            Vector3 p = room.transform.position;
            room.transform.position = new Vector3((Room1Left + Room1Right) * 0.5f, p.y, p.z);
            var box = room.GetComponent<BoxCollider2D>();
            Undo.RecordObject(box, "1번 방 늘리기");
            box.size = new Vector2(Room1Right - Room1Left, box.size.y);
            box.offset = Vector2.zero;
        }
        else Debug.LogWarning("Room01을 찾지 못했어요");

        MoveX("Room01_BG", Room1Left);          // 배경 기준점이 왼쪽 끝
        MoveX("WallLeft", Room1Left - 0.5f);
        MoveX("Cat", Room1Left + 2f);           // 안내 그림을 차례로 지나가며 오른쪽으로
        MoveX("Owner", -7.2f);                 // 안내 그림 'E 울기'(TutorialHints 3번째, x -7.2) 아래
    }

    private static void MoveX(string name, float x)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) { Debug.LogWarning(name + "을(를) 찾지 못했어요"); return; }
        Undo.RecordObject(go.transform, "옮기기");
        Vector3 p = go.transform.position;
        go.transform.position = new Vector3(x, p.y, p.z);
    }

    private static void MoveDesk01(float x, AudioClip hideClip)
    {
        GameObject desk = GameObject.Find("Desk_01");
        if (desk == null) { Debug.LogWarning($"Desk_01을 찾지 못했어요. 2번 방 책상 숨을 곳을 X {x}으로 옮겨 주세요."); return; }
        Undo.RecordObject(desk.transform, "책상 숨을 곳 옮기기");
        Vector3 p = desk.transform.position;
        desk.transform.position = new Vector3(x, p.y, p.z);

        // 책상은 배경에 그려져 있으므로 임시 박스 그림은 끄고, 안내 문구도 "책상"으로
        var sr = desk.GetComponent<SpriteRenderer>();
        if (sr != null) { Undo.RecordObject(sr, "박스 그림 끄기"); sr.enabled = false; }
        var spot = desk.GetComponent<HideSpot>();
        if (spot != null)
        {
            var so = new SerializedObject(spot);
            so.FindProperty("spotName").stringValue = "책상";
            so.FindProperty("hideClip").objectReferenceValue = hideClip;
            so.ApplyModifiedProperties();
        }
    }

    private static void SetGround(GameObject go)
    {
        int ground = LayerMask.NameToLayer("Ground");
        if (ground >= 0) go.layer = ground;
        else Debug.LogWarning("Ground 레이어가 없어요");
    }

    // 바닥에 떨어진 쪽지: 예비 퓨즈 위치 (1층 대기실 = 2번 방 벽시계 뒤). 1차 테스트에서 유추하기 어려워서 한 곳만 분명히 적음
    private static void Note(GameObject root, string name, float x, float floorY)
    {
        Prop(root, name + "_Visual", LoadSprite("Assets/Art/Props/note.png"), new Vector3(x, floorY + 0.05f, 0f));
        var note = Inter(root, name, new Vector3(x, floorY, 0f), new Vector2(1.0f, 1.0f), 0.5f, "쪽지 읽기");
        SetProps(note, so =>
        {
            so.FindProperty("action").enumValueIndex = (int)Interactable.Action.ShowText;
            so.FindProperty("popupTitle").stringValue = "시설과 메모";
            so.FindProperty("popupBody").stringValue =
                "예비 퓨즈 보관 위치\n\n" +
                "지하 창고 두꺼비집의 퓨즈가 또 나갔다.\n" +
                "예비 퓨즈는 1층 대기실 벽시계 뒤에 붙여 두었음.\n" +
                "(의자와 책상이 있는 방)\n\n" +
                "시계가 높으니 의자를 밀어서 밟고 올라갈 것.";
        });
    }

    private static void Glimpse(GameObject root, string name, float x, float floorY, string flag, string item, float fixedX = float.NaN)
    {
        GameObject go = Child(root, name, new Vector3(x, floorY, 0f));
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(3f, 3f);    // 넓게 (존재가 바빠서 못 나오면 범위 안에 있는 동안 다시 시도)
        box.offset = new Vector2(0f, 1.5f);
        box.isTrigger = true;
        var g = go.AddComponent<GlimpseTrigger>();
        SetProps(g, so =>
        {
            so.FindProperty("requiredFlag").stringValue = flag;
            so.FindProperty("requiredItem").stringValue = item;
            so.FindProperty("useFixedX").boolValue = !float.IsNaN(fixedX);
            so.FindProperty("fixedX").floatValue = float.IsNaN(fixedX) ? 0f : fixedX;
        });
    }

    private static void Pillar(GameObject root, string name, float x, float y, bool flip, Sprite sprite)
    {
        GameObject go = Child(root, name, new Vector3(x, y, 0f));
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.flipX = flip;
        sr.sortingOrder = 3;
    }

    private static SpriteRenderer Prop(GameObject root, string name, Sprite sprite, Vector3 pos)
    {
        GameObject go = Child(root, name, pos);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 2;   // 배경 위, 캐릭터 아래
        return sr;
    }

    // 조사 범위 (발밑 기준: offsetY만큼 위로 올린 상자)
    private static Interactable Inter(GameObject root, string name, Vector3 pos, Vector2 size, float offsetY, string label)
    {
        GameObject go = Child(root, name, pos);
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        box.offset = new Vector2(0f, offsetY);
        box.isTrigger = true;
        var it = go.AddComponent<Interactable>();
        SetProps(it, so => so.FindProperty("label").stringValue = label);
        return it;
    }

    private static void SetProps(Object target, System.Action<SerializedObject> edit)
    {
        var so = new SerializedObject(target);
        edit(so);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(SerializedObject so, string field, params string[] values)
    {
        var p = so.FindProperty(field);
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    // 층 바닥(윗면 = floorY)과 양쪽 끝 벽
    private static void FloorAndWalls(GameObject root, string tag, float left, float right, float floorY)
    {
        GameObject f = Child(root, "Floor_" + tag, new Vector3((left + right) * 0.5f, floorY - 0.5f, 0f));
        SetGround(f);
        f.AddComponent<BoxCollider2D>().size = new Vector2(right - left + 2f, 1f);

        GameObject wl = Child(root, "WallLeft_" + tag, new Vector3(left - 0.5f, floorY + 2.5f, 0f));
        SetGround(wl);
        wl.AddComponent<BoxCollider2D>().size = new Vector2(1f, 10f);

        GameObject wr = Child(root, "WallRight_" + tag, new Vector3(right + 0.5f, floorY + 2.5f, 0f));
        SetGround(wr);
        wr.AddComponent<BoxCollider2D>().size = new Vector2(1f, 10f);
    }

    private static Stairs MakeStairs(GameObject root, string name, float x, float floorY, string label,
                                     string keyId, string keyName, string requiredFlag)
    {
        GameObject go = Child(root, name, new Vector3(x, floorY, 0f));
        var st = go.AddComponent<Stairs>();
        var box = go.GetComponent<BoxCollider2D>();
        box.size = new Vector2(2.0f, 2.5f);
        box.offset = new Vector2(0f, 1.25f);
        box.isTrigger = true;

        var so = new SerializedObject(st);
        so.FindProperty("label").stringValue = label;
        so.FindProperty("keyId").stringValue = keyId;
        so.FindProperty("keyName").stringValue = keyName;
        so.FindProperty("requiredFlag").stringValue = requiredFlag;
        so.FindProperty("unlockClip").objectReferenceValue = LoadClip("sfx_key_unlock");
        so.FindProperty("travelClip").objectReferenceValue = LoadClip("sfx_stairs");
        so.ApplyModifiedPropertiesWithoutUndo();
        return st;
    }

    private static void StairsDoor(Stairs st, Sprite sprite, Sprite lockSprite, float dx, float dy, float shade)
    {
        var go = new GameObject("LockedDoorVisual");
        Undo.RegisterCreatedObjectUndo(go, "방 배치");
        go.transform.SetParent(st.transform, false);
        go.transform.localPosition = new Vector3(dx, dy, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 2;       // 배경 위, 기둥·캐릭터 아래
        sr.color = new Color(shade, shade, shade, 1f);

        Transform lockT = null;
        if (lockSprite != null)
        {
            // 쇠사슬·자물쇠 (문의 자식, 열 때 떨어짐)
            var lk = new GameObject("Lock");
            Undo.RegisterCreatedObjectUndo(lk, "방 배치");
            lk.transform.SetParent(go.transform, false);
            var lsr = lk.AddComponent<SpriteRenderer>();
            lsr.sprite = lockSprite;
            lsr.sortingOrder = 3;
            lsr.color = new Color(shade, shade, shade, 1f);
            lockT = lk.transform;
        }

        var so = new SerializedObject(st);
        so.FindProperty("lockedDoorVisual").objectReferenceValue = go;
        so.FindProperty("lockVisual").objectReferenceValue = lockT;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Link(Stairs from, Stairs to)
    {
        var so = new SerializedObject(from);
        so.FindProperty("linked").objectReferenceValue = to;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Key(GameObject root, string name, Vector3 pos, string keyId, string keyName, Sprite sprite)
    {
        GameObject go = FindOrCreate(root, name, pos, out bool created);
        if (created)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.5f, 0.4f);
            box.isTrigger = true;
            go.AddComponent<KeyItem>();

            var visual = new GameObject("Visual");
            Undo.RegisterCreatedObjectUndo(visual, "방 배치");
            visual.transform.SetParent(go.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 6;
        }
        var item = go.GetComponent<KeyItem>();
        if (item == null) { Debug.LogWarning(name + "에 Key Item 컴포넌트가 없어요"); return; }
        var so = new SerializedObject(item);
        so.FindProperty("keyId").stringValue = keyId;
        so.FindProperty("keyName").stringValue = keyName;
        so.ApplyModifiedProperties();
    }

    // clip: 숨을 때 소리 (비우면 기본), prop: 들킬 때 들썩일 그림 (배경에 그려진 물건이면 비움)
    private static void Hide(GameObject root, string name, Vector3 pos, Vector2 size, string spotName,
                             AudioClip clip = null, SpriteRenderer prop = null)
    {
        GameObject go = Child(root, name, pos);
        var spot = go.AddComponent<HideSpot>();
        var box = go.GetComponent<BoxCollider2D>();
        box.size = size;
        box.offset = new Vector2(0f, size.y * 0.5f);
        box.isTrigger = true;
        SetProps(spot, so =>
        {
            so.FindProperty("spotName").stringValue = spotName;
            so.FindProperty("hideClip").objectReferenceValue = clip;
            so.FindProperty("prop").objectReferenceValue = prop;
        });
    }

    private static void ExitDoor(GameObject root, string name, float x, float floorY, float width, string keyId, string keyName)
    {
        GameObject go = Child(root, name, new Vector3(x, floorY, 0f));
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width, 6f);
        box.offset = new Vector2(0f, 3f);
        var door = go.AddComponent<LockedDoor>();
        SetGround(go);

        var so = new SerializedObject(door);
        so.FindProperty("keyId").stringValue = keyId;
        so.FindProperty("keyName").stringValue = keyName;
        so.FindProperty("isExit").boolValue = true;
        so.FindProperty("solid").boolValue = false;   // 막지 않음 (쫓길 때 오른쪽 끝에 갇히지 않게)
        so.FindProperty("lockedMessage").stringValue = "쇠사슬로 잠겨 있다";
        so.FindProperty("unlockClip").objectReferenceValue = LoadClip("sfx_key_unlock");
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
