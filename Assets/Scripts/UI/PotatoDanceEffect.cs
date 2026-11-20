using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 芋を画面に複数置いて、全員で同じ踊りをさせる。
/// </summary>
public class PotatoDanceEffect : MonoBehaviour
{
    [Header("配置先(画面全体のRectTransform)")]
    [SerializeField] private RectTransform container;

    [Header("芋の画像")]
    [SerializeField] private Sprite potatoSprite;
    [SerializeField] private Vector2 potatoSize = new Vector2(160f, 220f);

    [Header("配置")]
    [SerializeField] private int count = 8;
    [SerializeField] private Vector2 margin = new Vector2(120f, 140f);
    [SerializeField] private Vector2 scaleRange = new Vector2(0.7f, 1.1f);
    [SerializeField] private float popTime = 0.25f;

    private readonly List<PotatoDancer> potatoes = new List<PotatoDancer>();
    private readonly List<RectTransform> rects = new List<RectTransform>();
    private readonly List<Vector3> targets = new List<Vector3>();
    private Coroutine routine;

    public void Show(float duration)
    {
        // 前回の演出が残っていたら止めて片付ける
        Clear();
        routine = StartCoroutine(Run(duration));
    }

    /// <summary>
    /// 演出を即座に止めて、芋をすべて消す。
    /// コルーチンも止めるので、破棄済みオブジェクトを触ることはない。
    /// </summary>
    public void Clear()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        foreach (var p in potatoes)
            if (p != null) Destroy(p.gameObject);

        potatoes.Clear();
        rects.Clear();
        targets.Clear();
    }

    private IEnumerator Run(float duration)
    {
        Spawn();

        // 全員同じ開始時刻 = 完全に同じ踊り
        float start = Time.unscaledTime;

        foreach (var p in potatoes)
        {
            var r = (RectTransform)p.transform;
            targets.Add(r.localScale);
            r.localScale = Vector3.zero;
            rects.Add(r);
        }

        // ポンッと登場
        float t = 0f;
        while (t < popTime)
        {
            t += Time.unscaledDeltaTime;
            SetScaleAll(EaseOutBack(Mathf.Clamp01(t / popTime)));
            yield return null;
        }
        SetScaleAll(1f);

        foreach (var p in potatoes)
            if (p != null) p.StartDance(start);

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, duration - popTime * 2f));

        // 縮んで退場
        foreach (var p in potatoes)
            if (p != null) p.StopDance();

        t = 0f;
        while (t < popTime)
        {
            t += Time.unscaledDeltaTime;
            SetScaleAll(1f - Mathf.Clamp01(t / popTime));
            yield return null;
        }

        // 自分自身のコルーチン内で終わるので、routineを先に空にしてからClear
        routine = null;
        Clear();
    }

    // 破棄済みの芋はスキップして、全員のスケールを targets × k にする
    private void SetScaleAll(float k)
    {
        for (int i = 0; i < rects.Count; i++)
        {
            if (rects[i] == null) continue;
            rects[i].localScale = targets[i] * k;
        }
    }

    private void Spawn()
    {
        Vector2 half = container.rect.size * 0.5f - margin;
        var list = new List<(Vector2 pos, float scale)>();

        for (int i = 0; i < count; i++)
        {
            // 重なりすぎないよう、少し離れた位置を探す
            Vector2 pos = Vector2.zero;
            for (int tries = 0; tries < 20; tries++)
            {
                pos = new Vector2(Random.Range(-half.x, half.x),
                                  Random.Range(-half.y, half.y));
                bool ok = true;
                foreach (var o in list)
                    if (Vector2.Distance(o.pos, pos) < potatoSize.x * 0.7f) { ok = false; break; }
                if (ok) break;
            }
            list.Add((pos, Random.Range(scaleRange.x, scaleRange.y)));
        }

        // 下にいる芋ほど手前に表示する
        list.Sort((a, b) => b.pos.y.CompareTo(a.pos.y));

        foreach (var item in list)
        {
            var go = new GameObject("Potato", typeof(RectTransform), typeof(Image), typeof(PotatoDancer));
            go.transform.SetParent(container, false);

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = potatoSize;
            rt.anchoredPosition = item.pos;
            rt.localScale = Vector3.one * item.scale;

            var img = go.GetComponent<Image>();
            img.sprite = potatoSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            potatoes.Add(go.GetComponent<PotatoDancer>());
        }
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}