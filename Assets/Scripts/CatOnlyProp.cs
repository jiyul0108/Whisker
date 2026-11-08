using UnityEngine;

// 고양이만 부딪히는 물건 (2번 방의 의자)
// 고양이는 밀거나 올라설 수 있고, 주인과 존재는 그냥 지나간다
// 주인은 Ground 레이어를 벽으로 보고 멈추는데, 이 물건은 벽으로 보지 않는다 (OwnerController가 확인)
public class CatOnlyProp : MonoBehaviour
{
    void Start()
    {
        var mine = GetComponentsInChildren<Collider2D>();

        var owner = FindAnyObjectByType<OwnerController>();
        if (owner != null) Ignore(mine, owner.GetComponentsInChildren<Collider2D>());

        var presence = FindAnyObjectByType<PresenceAI>();
        if (presence != null) Ignore(mine, presence.GetComponentsInChildren<Collider2D>());
    }

    private static void Ignore(Collider2D[] a, Collider2D[] b)
    {
        foreach (var x in a)
            foreach (var y in b)
                if (x != null && y != null) Physics2D.IgnoreCollision(x, y, true);
    }

    // 이 콜라이더가 고양이 전용 물건인가
    public static bool Is(Collider2D c)
    {
        return c != null && c.GetComponentInParent<CatOnlyProp>() != null;
    }
}
