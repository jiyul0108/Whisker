using System.Collections.Generic;
using UnityEngine;

// 아이템 이름·설명·그림 (오브젝트에 붙이지 않음)
// 그림은 Assets/Resources/Items/ 폴더의 같은 이름 파일 (예: item_fuse.png)
public static class ItemDB
{
    private struct Info
    {
        public string name, desc, icon;
        public Info(string name, string desc, string icon) { this.name = name; this.desc = desc; this.icon = icon; }
    }

    private static readonly Dictionary<string, Info> items = new Dictionary<string, Info>
    {
        { "A",      new Info("녹슨 열쇠", "어딘가의 문을 열 수 있을 것 같다.", "item_a") },
        { "FUSE",   new Info("퓨즈", "두꺼비집에 끼우면 전기가 들어올 것 같다.", "item_fuse") },
        { "PIECE1", new Info("열쇠 조각 (손잡이)", "부러진 열쇠의 손잡이 쪽이다. 나머지 조각이 어딘가에 있을 것 같다.", "item_piece1") },
        { "PIECE2", new Info("열쇠 조각 (끝)", "부러진 열쇠의 끝부분이다. 손잡이 쪽 조각과 맞을 것 같다.", "item_piece2") },
        { "SOLDER", new Info("땜납", "금속을 붙일 때 쓰는 납 실. 녹일 도구가 있으면 쓸 수 있을 것 같다.", "item_solder") },
        { "C",      new Info("출구 열쇠", "조각을 붙여 만든 열쇠. 로비 정문을 열 수 있을 것 같다.", "item_c") },
    };

    private static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();

    public static string Name(string id) => items.TryGetValue(id, out var i) ? i.name : id;
    public static string Desc(string id) => items.TryGetValue(id, out var i) ? i.desc : "";

    public static Sprite Icon(string id)
    {
        if (icons.TryGetValue(id, out var s)) return s;
        s = items.TryGetValue(id, out var i) ? Resources.Load<Sprite>("Items/" + i.icon) : null;
        icons[id] = s;
        return s;
    }
}
