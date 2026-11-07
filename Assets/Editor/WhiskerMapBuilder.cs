using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 에디터 메뉴: Whisker > 추가한 방 배치하기
// 3~10번 방(1층·지하·2층)의 범위, 배경, 출입구 기둥, 바닥·벽, 계단, 열쇠, 숨을 곳, 탈출문을 한 번에 배치한다
// 1·2번 방은 씬에 있는 것을 그대로 쓴다
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
        public RoomDef(string name, float left, float floor, float width, string bg)
        { this.name = name; this.left = left; this.floor = floor; this.width = width; this.bg = bg; }
        public float Right => left + width;
    }

    // 방 이름, 왼쪽 끝 X, 층 바닥 높이, 가로 길이, 배경 파일
    // 계단이 위아래로 겹치도록 지하·2층 방의 X를 맞춤
    private static readonly RoomDef[] Rooms =
    {
        new RoomDef("3번 방",  26f,     F1, 25f,    "room03_bg"),  // 복도 (열쇠 A)
        new RoomDef("4번 방",  51f,     F1, 17.88f, "room04_bg"),  // 계단실
        new RoomDef("5번 방",  68.88f,  F1, 17.88f, "room05_bg"),  // 로비 (탈출문)
        new RoomDef("6번 방",  45.355f, B1, 17.88f, "room06_bg"),  // 지하 복도 (올라가는 계단)
        new RoomDef("7번 방",  63.235f, B1, 17.88f, "room07_bg"),  // 창고 (열쇠 B, 상자 더미)
        new RoomDef("8번 방",  49.71f,  F2, 17.88f, "room08_bg"),  // 진료실 (내려가는 계단, 책상)
        new RoomDef("9번 방",  31.83f,  F2, 17.88f, "room09_bg"),  // 병실
        new RoomDef("10번 방", 13.95f,  F2, 17.88f, "room10_bg"),  // 수술실 (열쇠 C, 수술대)
    };

    [MenuItem("Whisker/추가한 방 배치하기")]
    private static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Whisker", "Play를 끈 상태에서 실행해 주세요.", "확인");
            return;
        }

        Sprite pillar = LoadSprite("Assets/Art/Props/exit_pillar.png");
        Sprite key = LoadSprite("Assets/Art/Props/key.png");
        Sprite stairsDoor = LoadSprite("Assets/Art/Props/stairs_door.png");
        Sprite stairsLock = LoadSprite("Assets/Art/Props/stairs_door_lock.png");
        if (pillar == null || key == null || stairsDoor == null || stairsLock == null) return;

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
            SetString(rc, "roomName", r.name);

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

        // 3) 바닥·벽: 1층은 씬의 Floor·WallRight를 늘리고 옮김, 지하·2층은 새로 만듦
        float f1Right = 68.88f + 17.88f;
        GameObject floor = GameObject.Find("Floor");
        if (floor != null)
        {
            Undo.RecordObject(floor.transform, "바닥 늘리기");
            float l = -10.5f, rr = f1Right + 1.5f;
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
        Stairs downB1 = MakeStairs(root, "Stairs_1F_toB1", 55.0f, F1, "지하로 내려가기", "A", "녹슨 열쇠");
        Stairs upFromB1 = MakeStairs(root, "Stairs_B1_to1F", 55.0f, B1, "1층으로 올라가기", "", "");
        Stairs up2F = MakeStairs(root, "Stairs_1F_to2F", 64.955f, F1, "2층으로 올라가기", "B", "작은 열쇠");
        Stairs downFrom2F = MakeStairs(root, "Stairs_2F_to1F", 64.955f, F2, "1층으로 내려가기", "", "");
        // 잠긴 계단 입구를 막는 문 (4번 계단실 그림의 출입구 안쪽에 맞춤)
        StairsDoor(downB1, stairsDoor, stairsLock, 0f, 0.14f, 1f);
        StairsDoor(up2F, stairsDoor, stairsLock, -0.035f, 0.14f, 0.8f);   // 오른쪽은 어두운 벽이라 조금 어둡게
        Link(downB1, upFromB1);
        Link(upFromB1, downB1);
        Link(up2F, downFrom2F);
        Link(downFrom2F, up2F);

        // 5) 열쇠 (높은 곳은 점프해서 줍기)
        Key(root, "Key_A", new Vector3(46f, F1 + 1.64f, 0f), "A", "녹슨 열쇠", key);       // 3번 복도 손잡이 위
        Key(root, "Key_B", new Vector3(66.235f, B1 + 1.17f, 0f), "B", "작은 열쇠", key);   // 7번 창고 선반
        Key(root, "Key_C", new Vector3(17.0f, F2 + 1.48f, 0f), "C", "출구 열쇠", key);     // 10번 수술실 기구 카트 위

        // 6) 숨을 곳 (배경에 그려진 물건 위치)
        Hide(root, "Hide_Boxes_07", new Vector3(72.285f, B1, 0f), new Vector2(2.4f, 1.2f), "상자 더미");
        Hide(root, "Hide_Desk_08", new Vector3(58.885f, F2, 0f), new Vector2(3.0f, 1.2f), "책상");
        Hide(root, "Hide_Table_10", new Vector3(23.12f, F2, 0f), new Vector2(3.0f, 1.2f), "수술대");

        // 7) 탈출문: 5번 로비의 쇠사슬 정문 (그림은 배경에 있으므로 판정만)
        ExitDoor(root, "ExitDoor_Lobby", 84.06f, F1, 2.8f, "C", "출구 열쇠");

        // 8) 예전 시험용 잠긴 문은 이제 계단 잠금으로 대신함
        GameObject oldDoor = GameObject.Find("LockedDoor_01");
        if (oldDoor != null) Undo.DestroyObjectImmediate(oldDoor);

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log($"방 {Rooms.Length}개 + 계단 4개 배치 완료. Ctrl+S로 씬을 저장하세요.");
    }

    // ---------- 만들기 도우미 ----------

    private static Sprite LoadSprite(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogError("그림을 찾지 못했어요: " + path);
        return s;
    }

    private static GameObject Child(GameObject root, string name, Vector3 pos)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "방 배치");
        go.transform.SetParent(root.transform, false);
        go.transform.position = pos;
        return go;
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

    private static void SetGround(GameObject go)
    {
        int ground = LayerMask.NameToLayer("Ground");
        if (ground >= 0) go.layer = ground;
        else Debug.LogWarning("Ground 레이어가 없어요");
    }

    private static void Pillar(GameObject root, string name, float x, float y, bool flip, Sprite sprite)
    {
        GameObject go = Child(root, name, new Vector3(x, y, 0f));
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.flipX = flip;
        sr.sortingOrder = 3;
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

    private static Stairs MakeStairs(GameObject root, string name, float x, float floorY, string label, string keyId, string keyName)
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

        // 쇠사슬·자물쇠 (문의 자식, 열 때 떨어짐)
        var lk = new GameObject("Lock");
        Undo.RegisterCreatedObjectUndo(lk, "방 배치");
        lk.transform.SetParent(go.transform, false);
        var lsr = lk.AddComponent<SpriteRenderer>();
        lsr.sprite = lockSprite;
        lsr.sortingOrder = 3;
        lsr.color = new Color(shade, shade, shade, 1f);

        var so = new SerializedObject(st);
        so.FindProperty("lockedDoorVisual").objectReferenceValue = go;
        so.FindProperty("lockVisual").objectReferenceValue = lk.transform;
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

    private static void Hide(GameObject root, string name, Vector3 pos, Vector2 size, string spotName)
    {
        GameObject go = Child(root, name, pos);
        var spot = go.AddComponent<HideSpot>();
        var box = go.GetComponent<BoxCollider2D>();
        box.size = size;
        box.offset = new Vector2(0f, size.y * 0.5f);
        box.isTrigger = true;
        SetString(spot, "spotName", spotName);
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
        so.FindProperty("lockedMessage").stringValue = "쇠사슬로 잠겨 있다";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(Object target, string field, string value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning(target.name + ": " + field + " 칸을 찾지 못했어요"); return; }
        p.stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
