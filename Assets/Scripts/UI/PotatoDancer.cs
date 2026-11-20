using UnityEngine;

/// <summary>
/// 芋1匹ぶんの踊り。全員が同じ startTime を使うので完全に同期して踊る。
/// 踊り = ジャンプ + 左右にゆれる + 伸び縮み(1拍ごとに繰り返し)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PotatoDancer : MonoBehaviour
{
    [Header("踊りの設定")]
    [SerializeField] private float bpm = 150f;
    [SerializeField] private float jumpHeight = 40f;   // ジャンプの高さ(px)
    [SerializeField] private float tiltAngle = 14f;    // 左右の傾き(度)
    [SerializeField] private float swayX = 12f;        // 横ゆれ(px)
    [SerializeField] private float squash = 0.15f;     // 伸び縮みの量

    private RectTransform rect;
    private Vector2 basePos;
    private Vector3 baseScale;
    private float startTime;
    private bool dancing;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void StartDance(float sharedStartTime)
    {
        basePos = rect.anchoredPosition;
        baseScale = rect.localScale;
        startTime = sharedStartTime;
        dancing = true;
    }

    public void StopDance()
    {
        dancing = false;
        if (rect == null) return;
        rect.anchoredPosition = basePos;
        rect.localRotation = Quaternion.identity;
        rect.localScale = baseScale;
    }

    private void Update()
    {
        if (!dancing) return;

        float beat = (Time.unscaledTime - startTime) * bpm / 60f;
        float p = beat - Mathf.Floor(beat);            // 1拍の中の進み具合 0~1

        // ジャンプ(1拍ごとに1回)
        float jump = Mathf.Sin(p * Mathf.PI);

        // 左右ゆれ(2拍で1往復)
        float sway = Mathf.Sin(beat * Mathf.PI);

        // 着地でつぶれ、空中で伸びる
        float landSquash = Mathf.Pow(1f - p, 6f) * 0.5f;
        float sy = 1f + squash * (jump - landSquash);
        float sx = 1f - squash * 0.6f * (jump - landSquash);

        rect.anchoredPosition = basePos + new Vector2(sway * swayX, jump * jumpHeight);
        rect.localRotation = Quaternion.Euler(0f, 0f, -sway * tiltAngle);
        rect.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
    }
}