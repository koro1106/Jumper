using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("ショップ画面")]
    [SerializeField] private GameObject shopPanel;

    [Header("ボタン")]
    [SerializeField] private Button shopOpenButton;
    [SerializeField] private Button buyBicycleButton;
    [SerializeField] private Button closeShopButton;

    [Header("表示テキスト")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text bicyclePriceText;
    [SerializeField] private TMP_Text bicycleCountText;

    [Header("自転車管理")]
    [SerializeField] private BicycleManager bicycleManager;

    private void Start()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (shopOpenButton != null)
            shopOpenButton.onClick.AddListener(OpenShop);

        if (buyBicycleButton != null)
            buyBicycleButton.onClick.AddListener(BuyBicycle);

        if (closeShopButton != null)
            closeShopButton.onClick.AddListener(CloseShop);

        RefreshShopUI();
    }

    public void OpenShop()
    {
        if (shopPanel == null)
            return;

        shopPanel.SetActive(true);
        RefreshShopUI();
    }

    public void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    public void BuyBicycle()
    {
        if (bicycleManager == null ||
            GameManager.Instance == null)
        {
            Debug.LogError("購入に必要な設定がありません。");
            return;
        }

        if (GameManager.Instance.GameEnded)
            return;

        int previousCount = bicycleManager.BicycleCount;

        bicycleManager.BuyBicycle();

        // 台数が増えていれば購入成功
        bool purchased =
            bicycleManager.BicycleCount > previousCount;

        if (purchased)
        {
            Debug.Log("自転車を購入しました！");
        }
        else
        {
            Debug.Log("購入できませんでした。所持金を確認してください。");
        }

        // ショップと通常画面の表示を更新
        RefreshShopUI();
        GameManager.Instance.RefreshUI();
    }

    public void RefreshShopUI()
    {
        if (bicycleManager == null ||
            GameManager.Instance == null)
        {
            return;
        }

        var data = GameManager.Instance.playerData;

        if (moneyText != null)
        {
            moneyText.text =
                $"所持金    {data.money:N0}円";
        }

        if (bicyclePriceText != null)
        {
            bicyclePriceText.text =
                $"自転車    {bicycleManager.NextBicyclePrice:N0}円";
        }

        if (bicycleCountText != null)
        {
            bicycleCountText.text =
                $"{bicycleManager.BicycleCount + 1}台目の自転車";
        }

        if (buyBicycleButton != null)
        {
            buyBicycleButton.interactable =
                !GameManager.Instance.GameEnded &&
                data.money >= bicycleManager.NextBicyclePrice;
        }
    }

    private void OnDestroy()
    {
        if (shopOpenButton != null)
            shopOpenButton.onClick.RemoveListener(OpenShop);

        if (buyBicycleButton != null)
            buyBicycleButton.onClick.RemoveListener(BuyBicycle);

        if (closeShopButton != null)
            closeShopButton.onClick.RemoveListener(CloseShop);
    }
}