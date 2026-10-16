using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
/// <summary>
/// 成功演出用
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

    [Header("フラッシュ")]
    [SerializeField] private Image flashImage;

    [Header("演出設定")]
    [SerializeField] private float effectDuration = 1.2f;
    [SerializeField] private float blinkInterval = 0.06f;
    [SerializeField] private float popScale = 1.5f;

    private Coroutine effectCoroutine;
    private Vector3 successTextOriginalScale;
    private Vector3 moneyTextOriginalScale;
    private Vector3 subscriberTextOriginalScale;

    private readonly Color[] flashyColors =
    {
        Color.yellow,
        Color.magenta,
        Color.cyan,
        Color.white
    };

    private void Awake()
    {
        if (successText != null)
            successTextOriginalScale = successText.rectTransform.localScale;

        if (moneyRewardText != null)
            moneyTextOriginalScale = moneyRewardText.rectTransform.localScale;

        if (subscriberRewardText != null)
            subscriberTextOriginalScale =
                subscriberRewardText.rectTransform.localScale;

        HideEffects();
    }

    public void Play(long money, long subscribers)
    {
        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        if (successText != null)
        {
            successText.gameObject.SetActive(true);
            successText.text = "ジャンプ成功!!!!";
        }

        if (moneyRewardText != null)
        {
            moneyRewardText.gameObject.SetActive(true);
            moneyRewardText.text = $"+{money:N0}円 GET!!";
        }

        if (subscriberRewardText != null)
        {
            subscriberRewardText.gameObject.SetActive(true);
            subscriberRewardText.text =
                $"登録者 +{subscribers:N0}人!!";
        }
        if (addMoneyRewardText != null)
        {
            addMoneyRewardText.gameObject.SetActive(true);
            addMoneyRewardText.text = $"+{money:N0}円";
        }

        if (addSubscriberRewardText != null)
        {
            addSubscriberRewardText.gameObject.SetActive(true);
            addSubscriberRewardText.text =
                $"+{subscribers:N0}人";
        }

        effectCoroutine = StartCoroutine(PlayEffect());
    }

    private IEnumerator PlayEffect()
    {
        float elapsed = 0f;
        int colorIndex = 0;

        if (flashImage != null)
        {
            flashImage.gameObject.SetActive(true);
            Color c = Color.white;
            c.a = 0.8f;
            flashImage.color = c;
        }

        while (elapsed < effectDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / effectDuration);

            // 成功テキストを高速点滅
            if (successText != null)
            {
                successText.color =
                    flashyColors[colorIndex % flashyColors.Length];

                colorIndex++;

                float pulse = 1f + (popScale - 1f) *
                    Mathf.Sin(t * Mathf.PI * 10f) *
                    (1f - t * 0.5f);

                successText.rectTransform.localScale =
                    successTextOriginalScale * pulse;
            }

            // 報酬テキストをポンッと拡大
            float pop = 1f + (popScale - 1f) *
                Mathf.Sin(Mathf.Clamp01(t * 4f) * Mathf.PI * 0.5f);

            if (moneyRewardText != null)
            {
                moneyRewardText.rectTransform.localScale =
                    moneyTextOriginalScale * pop;
            }

            if (subscriberRewardText != null)
            {
                subscriberRewardText.rectTransform.localScale =
                    subscriberTextOriginalScale * pop;
            }

            // 白フラッシュを素早く消す
            if (flashImage != null)
            {
                Color c = flashImage.color;
                c.a = Mathf.Lerp(0.8f, 0f, Mathf.Clamp01(elapsed / 0.18f));
                flashImage.color = c;
            }

            yield return new WaitForSecondsRealtime(blinkInterval);
        }

        HideEffects();
        effectCoroutine = null;
    }

    private void HideEffects()
    {
        if (successText != null)
        {
            successText.gameObject.SetActive(false);
            successText.color = Color.white;
            successText.rectTransform.localScale =
                successTextOriginalScale;
        }

        if (addMoneyRewardText != null)
        {
            addMoneyRewardText.gameObject.SetActive(false);
            addMoneyRewardText.rectTransform.localScale =
                moneyTextOriginalScale;
        }

        if (addSubscriberRewardText != null)
        {
            addSubscriberRewardText.gameObject.SetActive(false);
            addSubscriberRewardText.rectTransform.localScale =
                subscriberTextOriginalScale;
        }
        if (moneyRewardText != null)
        {
            moneyRewardText.gameObject.SetActive(false);
            moneyRewardText.rectTransform.localScale =
                moneyTextOriginalScale;
        }

        if (subscriberRewardText != null)
        {
            subscriberRewardText.gameObject.SetActive(false);
            subscriberRewardText.rectTransform.localScale =
                subscriberTextOriginalScale;
        }

        if (flashImage != null)
        {
            Color c = flashImage.color;
            c.a = 0f;
            flashImage.color = c;
            flashImage.gameObject.SetActive(false);
        }
    }
}