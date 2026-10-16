using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ジャンプ失敗時の表示とフェードアウトを管理する。
/// </summary>
public class FailureEffectUI : MonoBehaviour
{
    [Header("失敗テキスト")]
    [SerializeField] private TMP_Text failureText;

    [Header("失敗画像")]
    [SerializeField] private Image failureImage;

    [Header("登録者減少テキスト")]
    [SerializeField] private TMP_Text subscriberLossText;

    [Header("フェード設定")]
    [SerializeField] private CanvasGroup effectCanvasGroup;
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("プレイヤー")]
    [SerializeField] private JumpController jumpController;

    [Header("暗転演出")]
    [SerializeField] private Image blackScreenImage;
    [SerializeField] private float fadeToBlackDuration = 0.5f;
    [SerializeField] private float fadeFromBlackDuration = 0.5f;
    private Coroutine effectCoroutine;

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        if (effectCanvasGroup != null)
            effectCanvasGroup.alpha = 0f;

        HideElements();

        if (blackScreenImage != null)
        {
            Color color = Color.black;
            color.a = 0f;

            blackScreenImage.color = color;
            blackScreenImage.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 失敗演出を再生する。
    /// </summary>
    public void Play(long subscriberLoss, Action onFinished = null)
    {
        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        if (failureText != null)
        {
            failureText.text = "タイヤに足が絡まってしまった。";
            failureText.gameObject.SetActive(true);
        }

        if (failureImage != null)
            failureImage.gameObject.SetActive(true);

        if (subscriberLossText != null)
        {
            subscriberLossText.text =
                $"登録者 -{subscriberLoss:N0}人";

            subscriberLossText.gameObject.SetActive(true);
        }

        if (effectCanvasGroup != null)
            effectCanvasGroup.alpha = 1f;

        IsPlaying = true;
        effectCoroutine = StartCoroutine(
            PlayFadeEffect(onFinished)
        );
    }

    private IEnumerator PlayFadeEffect(Action onFinished)
    {
        // ① 失敗UIを数秒間表示
        yield return new WaitForSecondsRealtime(displayDuration);

        // ② 画面を徐々に暗くする
        yield return StartCoroutine(
            FadeBlack(0f, 1f, fadeToBlackDuration)
        );

        // ③ 真っ暗になってから失敗UIを消す
        if (effectCanvasGroup != null)
            effectCanvasGroup.alpha = 0f;

        HideElements();

        // ④ 画面を徐々に明るくする
        yield return StartCoroutine(
            FadeBlack(1f, 0f, fadeFromBlackDuration)
        );

        // ⑤ 画面が明るくなってからPlayerを初期位置に戻す
        if (jumpController != null)
            jumpController.ResetPlayer();

        // ⑥ 演出終了
        IsPlaying = false;
        effectCoroutine = null;

        // 翌日へ進む
        onFinished?.Invoke();
    }

    private IEnumerator FadeBlack(
    float startAlpha,
    float endAlpha,
    float duration)
    {
        if (blackScreenImage == null)
            yield break;

        blackScreenImage.gameObject.SetActive(true);

        Color color = Color.black;
        color.a = startAlpha;
        blackScreenImage.color = color;

        if (duration <= 0f)
        {
            color.a = endAlpha;
            blackScreenImage.color = color;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            blackScreenImage.color = color;

            yield return null;
        }

        color.a = endAlpha;
        blackScreenImage.color = color;
    }
    private void HideElements()
    {
        if (failureText != null)
            failureText.gameObject.SetActive(false);

        if (failureImage != null)
            failureImage.gameObject.SetActive(false);

        if (subscriberLossText != null)
            subscriberLossText.gameObject.SetActive(false);
    }
}