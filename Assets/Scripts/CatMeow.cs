using UnityEngine;
using UnityEngine.InputSystem;

// E 키로 울기: 들리는 거리 안의 주인이 고양이 자리로 오거나, 걷는 중이면 멈춘다
public class CatMeow : MonoBehaviour
{
    [Header("주인 부르기")]
    [SerializeField] private OwnerController owner;
    [SerializeField] private float hearRadius = 10f;   // 주인이 울음소리를 들을 수 있는 거리
    [SerializeField] private float cooldown = 0.5f;

    private CatAnimator anim;
    private float lastMeowTime = -999f;

    void Awake()
    {
        anim = GetComponent<CatAnimator>();
        if (owner == null) owner = FindAnyObjectByType<OwnerController>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (NoteUI.IsReading || HideController.IsHidden) return; // 쪽지 읽는 중·숨은 중에는 울기 안 됨

        if (kb.eKey.wasPressedThisFrame && Time.time - lastMeowTime >= cooldown)
            Meow();
    }

    private void Meow()
    {
        lastMeowTime = Time.time;
        if (anim != null) anim.PlayMeow();
        if (owner == null) return;

        float dist = Vector2.Distance(transform.position, owner.transform.position);
        if (dist <= hearRadius)
        {
            Debug.Log("야옹! 주인이 들었다");
            owner.OnMeow(transform.position);
        }
        else
        {
            Debug.Log($"야옹... 주인이 너무 멀다 (거리 {dist:F1})");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, hearRadius);
    }
}
