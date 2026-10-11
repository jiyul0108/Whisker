using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CatMeow : MonoBehaviour
{
    [Header("주인 부르기")]
    [SerializeField] private OwnerController owner;
    [SerializeField] private float callRadius = 4f;
    [SerializeField] private float cooldown = 0.5f;

    [Header("울기 그림 (선택)")]
    [SerializeField] private Sprite meowSprite;
    [SerializeField] private float meowSpriteTime = 0.35f;

    private SpriteRenderer sr;
    private Sprite normalSprite;
    private float lastMeowTime = -999f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) normalSprite = sr.sprite;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.eKey.wasPressedThisFrame && Time.time - lastMeowTime >= cooldown)
            Meow();
    }

    private void Meow()
    {
        lastMeowTime = Time.time;

        if (meowSprite != null && sr != null) StartCoroutine(ShowMeowSprite());

        if (owner == null) return;

        float dist = Vector2.Distance(transform.position, owner.transform.position);
        if (dist <= callRadius)
        {
            Debug.Log("야옹! 주인이 들었다");
            owner.OnMeow();
        }
        else
        {
            Debug.Log($"야옹... 주인이 너무 멀다 (거리 {dist:F1})");
        }
    }

    private IEnumerator ShowMeowSprite()
    {
        sr.sprite = meowSprite;
        yield return new WaitForSeconds(meowSpriteTime);
        sr.sprite = normalSprite;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, callRadius);
    }
}