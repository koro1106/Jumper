using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 成功演出用(芋が踊るバージョン)
/// </summary>
public class SuccessEffectUI : MonoBehaviour
{
    [Header("成功テキスト")]
    [SerializeField] private TMP_Text successText;

    [Header("報酬テキスト")]
    [SerializeField] private TMP_Text moneyRewardText;
    [SerializeField] private TMP_Text subscriberRewardText;

    [Header("追加分報酬テキスト")]
    [SerializeField] private TMP_Text addMoneyRewardText;
    [SerializeField] private TMP_Text addSubscriberRewardText;

    [Header("踊る芋")]
    [SerializeField] private PotatoDanceEffect potatoDance;

    [Header("演出設定")]
    [SerializeField] private float effectDuration = 2.0f;
    [SerializeField] private float popScale = 1.2f;

    private Coroutine effectCoroutine;
    private Vector3 successTextOriginalScale;
    private Vector3 moneyTextOriginalScale;
    private Vector3 subscriberTextOriginalScale;

    private void Awake()
    {
        if (successText != null)
            successTextOriginalScale = successText.rectTransform.localScale;
        if (moneyRewardText != null)
            moneyTextOriginalScale = moneyRewardText.rectTransform.localScale;
        if (subscriberRewardText != null)
            subscriberTextOriginalScale = subscriberRewardText.rectTransform.localScale;

        HideEffects();
    }

    public void Play(long money, long subscribers)
    {
        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        Show(successText, "ジャンプ成功!!!!");
        Show(moneyRewardText, $"+{money:N0}円 GET!!");
        Show(subscriberRewardText, $"登録者 +{subscribers:N0}人!!");
        Show(addMoneyRewardText, $"+{money:N0}円");
        Show(addSubscriberRewardText, $"+{subscribers:N0}人");

        if (potatoDance != null)
            potatoDance.Show(effectDuration);

        effectCoroutine = StartCoroutine(PlayEffect());
    }

    private void Show(TMP_Text text, string value)
    {
        if (text == null) return;
        text.gameObject.SetActive(true);
        text.text = value;
    }

    private IEnumerator PlayEffect()
    {
        float elapsed = 0f;

        while (elapsed < effectDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / effectDuration);

            // テキストは最初にポンッと出て、落ち着く(色は変えない)
            float pop = 1f + (popScale - 1f) * Mathf.Sin(Mathf.Clamp01(t * 4f) * Mathf.PI);

            if (successText != null)
                successText.rectTransform.localScale = successTextOriginalScale * pop;
            if (moneyRewardText != null)
                moneyRewardText.rectTransform.localScale = moneyTextOriginalScale * pop;
            if (subscriberRewardText != null)
                subscriberRewardText.rectTransform.localScale = subscriberTextOriginalScale * pop;

            yield return null;
        }

        HideEffects();
        effectCoroutine = null;
    }

    private void HideEffects()
    {
        Hide(successText, successTextOriginalScale);
        Hide(moneyRewardText, moneyTextOriginalScale);
        Hide(subscriberRewardText, subscriberTextOriginalScale);
        Hide(addMoneyRewardText, moneyTextOriginalScale);
        Hide(addSubscriberRewardText, subscriberTextOriginalScale);

        if (potatoDance != null)
            potatoDance.Clear();
    }

    private void Hide(TMP_Text text, Vector3 originalScale)
    {
        if (text == null) return;
        text.gameObject.SetActive(false);
        text.rectTransform.localScale = originalScale;
    }
}