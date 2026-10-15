using TMPro;
using UnityEngine;

/// <summary>
/// 日数・登録者数・所持金を画面に表示するUIクラス。
/// CanvasなどのUIオブジェクトにアタッチする。
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("表示するテキスト")]

    [Tooltip("日数を表示するTextMeshPro")]
    public TMP_Text dayText;

    [Tooltip("登録者数を表示するTextMeshPro")]
    public TMP_Text subscriberText;

    [Tooltip("所持金を表示するTextMeshPro")]
    public TMP_Text moneyText;

    /// <summary>
    /// ゲーム開始時に表示内容を更新する。
    /// </summary>
    private void Start()
    {
        RefreshUI();
    }

    /// <summary>
    /// GameManagerのプレイヤーデータを画面に反映する。
    /// </summary>
    public void RefreshUI()
    {
        // GameManagerがまだ存在しない場合は何もしない
        if (GameManager.Instance == null)
            return;

        // 現在のプレイヤーデータを取得する
        PlayerRuntimeData data = GameManager.Instance.playerData;

        // 日数を表示する
        if (dayText != null)
            dayText.text = $"{data.day}日目";

        // 登録者数を表示する
        if (subscriberText != null)
            subscriberText.text =
                $"登録者数　{data.subscribers:N0}人";

        // 所持金を表示する
        if (moneyText != null)
            moneyText.text = $"所持金　    {data.money:N0}円";
    }
}
