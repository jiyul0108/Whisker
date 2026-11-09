using UnityEngine;

// 정해진 자리의 깜짝 등장 연출 (존재가 고양이 앞에 잠깐 보였다가 사라짐, 쫓아오지 않음)
// 고양이가 이 범위(Box Collider 2D, Is Trigger)에 들어오면 한 번만 나온다
// 존재가 이미 나와 있거나 이벤트 중이면 기다렸다가, 고양이가 범위 안에 있는 동안 다시 시도
[RequireComponent(typeof(BoxCollider2D))]
public class GlimpseTrigger : MonoBehaviour
{
    [SerializeField] private string requiredFlag = "";   // 이 진행이 있어야 나옴 (예: POWER). 비우면 항상
    [SerializeField] private string requiredItem = "";   // 이 아이템을 가졌을 때만 (예: A). 비우면 항상
    [SerializeField] private bool useFixedX = false;      // 켜면 항상 Fixed X 자리에 나타남 (끄면 고양이 앞쪽 알맞은 곳)
    [SerializeField] private float fixedX = 0f;

    private bool done;
    private PresenceAI presence;

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    void Start()
    {
        presence = FindAnyObjectByType<PresenceAI>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (done || presence == null) return;
        if (other.GetComponentInParent<CatController>() == null) return;
        if (!string.IsNullOrEmpty(requiredFlag) && !GameFlags.Has(requiredFlag)) return;
        if (!string.IsNullOrEmpty(requiredItem) && !KeyInventory.Has(requiredItem)) return;
        if (useFixedX ? presence.PlayGlimpse(fixedX) : presence.PlayGlimpse())
        {
            done = true;
            GameLog.Info($"깜짝 등장 연출: {name}");
        }
    }
}
