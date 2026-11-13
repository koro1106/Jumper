using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// ジャンプまわりの演出（紙吹雪・ホコリ・画面揺れ・効果音・PERFECT文字・節目バナー）をまとめて管理する。
/// 未設定の項目はスキップされるので、作れたものから順に割り当ててOK。
/// </summary>
public class JumpEffects : MonoBehaviour
{
    [Header("パーティクル")]
    [SerializeField] private ParticleSystem confetti;     // 紙吹雪（Simulation Space = World）
    [SerializeField] private ParticleSystem dust;         // 着地のホコリ（Simulation Space = World）
    [SerializeField] private int confettiCount = 40;
    [SerializeField] private int perfectConfettiCount = 90;
    [SerializeField] private int dustCount = 14;

    [Header("画面揺れ（失敗時）")]
    [SerializeField] private float failShakeDuration = 0.35f;
    [SerializeField] private float failShakeAmount = 0.25f;

    [Header("PERFECT文字")]
    [SerializeField] private TMP_Text perfectText;
    [SerializeField] private float perfectTextDuration = 1.6f;
    [SerializeField] private float colorInterval = 0.1f;
    [SerializeField]
    private Color[] flashColors =
        { Color.yellow, Color.magenta, Color.white, Color.cyan };

    [Header("登録者の節目バナー")]
    [SerializeField] private CanvasGroup bannerGroup;
    [SerializeField] private TMP_Text bannerText;
    [Tooltip("小さい順に並べる")]
    [SerializeField]
    private long[] milestones =
        { 500, 1000, 3000, 5000, 10000, 30000, 50000, 100000, 500000, 1000000 };
    [SerializeField] private float bannerShowTime = 2.5f;
    [SerializeField] private float bannerFadeTime = 0.3f;

    [Header("効果音")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioClip chargeSe;
    [SerializeField] private AudioClip launchSe;
    [SerializeField] private AudioClip landSe;
    [SerializeField] private AudioClip successSe;
    [SerializeField] private AudioClip perfectSe;
    [SerializeField] private AudioClip failSe;
    [SerializeField] private AudioClip milestoneSe;

    private int nextMilestone = 0;
    private readonly Queue<long> bannerQueue = new Queue<long>();
    private Coroutine bannerRoutine;
    private Coroutine shakeRoutine;
    private Coroutine perfectRoutine;
    private Vector3 shakeOrigin;

    private void Start()
    {
        // すでに超えている節目は演出しない
        if (GameManager.Instance != null)
        {
            long s = GameManager.Instance.playerData.subscribers;
            while (nextMilestone < milestones.Length && s >= milestones[nextMilestone])
                nextMilestone++;
        }

        if (perfectText != null) perfectText.gameObject.SetActive(false);
        if (bannerGroup != null) bannerGroup.alpha = 0f;
    }

    // ========================================
    // JumpController から呼ぶ
    // ========================================

    public void OnChargeStart()
    {
        PlaySe(chargeSe);
    }

    public void OnLaunch(Vector3 position)
    {
        PlaySe(launchSe);
        Burst(dust, position, dustCount / 2);
    }

    /// <summary>着地（成功・失敗どちらも）</summary>
    public void OnLand(Vector3 contactPoint, bool success, bool perfect)
    {
        Burst(dust, contactPoint, dustCount);
        PlaySe(landSe);

        if (success)
        {
            Burst(confetti, contactPoint + Vector3.up, perfect ? perfectConfettiCount : confettiCount);
            PlaySe(perfect ? perfectSe : successSe);
            if (perfect) ShowPerfectText();
        }
        else
        {
            PlaySe(failSe);
            Shake(failShakeDuration, failShakeAmount);
        }
    }

    // ========================================
    // GameManager から呼ぶ
    // ========================================

    /// <summary>登録者数が変わったあとに呼ぶ。節目を超えていたらバナーを出す。</summary>
    public void CheckMilestone(long subscribers)
    {
        while (nextMilestone < milestones.Length && subscribers >= milestones[nextMilestone])
        {
            bannerQueue.Enqueue(milestones[nextMilestone]);
            nextMilestone++;
        }

        if (bannerQueue.Count > 0 && bannerRoutine == null)
            bannerRoutine = StartCoroutine(BannerRoutine());
    }

    // ========================================
    // 各演出
    // ========================================

    private void Burst(ParticleSystem ps, Vector3 position, int count)
    {
        if (ps == null) return;
        ps.transform.position = position;
        ps.Emit(count);
    }

    private void PlaySe(AudioClip clip)
    {
        if (seSource != null && clip != null) seSource.PlayOneShot(clip);
    }

    public void Shake(float duration, float amount)
    {
        if (Camera.main == null) return;

        Transform cam = Camera.main.transform;
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            cam.localPosition = shakeOrigin;   // 揺れの途中なら元の位置へ戻してからやり直す
        }
        shakeRoutine = StartCoroutine(ShakeRoutine(cam, duration, amount));
    }

    private IEnumerator ShakeRoutine(Transform cam, float duration, float amount)
    {
        shakeOrigin = cam.localPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float falloff = 1f - t / duration;
            cam.localPosition = shakeOrigin + (Vector3)(Random.insideUnitCircle * amount * falloff);
            yield return null;
        }
        cam.localPosition = shakeOrigin;
        shakeRoutine = null;
    }

    private void ShowPerfectText()
    {
        if (perfectText == null) return;
        if (perfectRoutine != null) StopCoroutine(perfectRoutine);
        perfectRoutine = StartCoroutine(PerfectRoutine());
    }

    private IEnumerator PerfectRoutine()
    {
        perfectText.gameObject.SetActive(true);

        float t = 0f;
        int i = 0;
        while (t < perfectTextDuration)
        {
            if (flashColors != null && flashColors.Length > 0)
                perfectText.color = flashColors[i % flashColors.Length];
            i++;
            yield return new WaitForSecondsRealtime(colorInterval);
            t += colorInterval;
        }

        perfectText.gameObject.SetActive(false);
        perfectRoutine = null;
    }

    private IEnumerator BannerRoutine()
    {
        while (bannerQueue.Count > 0)
        {
            long n = bannerQueue.Dequeue();
            if (bannerText != null) bannerText.text = $"登録者 {n:N0}人突破!";
            PlaySe(milestoneSe);

            yield return Fade(0f, 1f);
            yield return new WaitForSecondsRealtime(bannerShowTime);
            yield return Fade(1f, 0f);
        }
        bannerRoutine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (bannerGroup == null) yield break;

        float t = 0f;
        while (t < bannerFadeTime)
        {
            t += Time.unscaledDeltaTime;
            bannerGroup.alpha = Mathf.Lerp(from, to, t / bannerFadeTime);
            yield return null;
        }
        bannerGroup.alpha = to;
    }
}