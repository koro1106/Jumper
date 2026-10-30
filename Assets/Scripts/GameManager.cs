using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("固定設定")]
    public GameConfig config;

    [Header("スキル")]
    public SkillManager skillManager;

    [Header("自転車管理")]
    public BicycleManager bicycleManager;

    [Header("実行時データ")]
    public PlayerRuntimeData playerData = new PlayerRuntimeData();

    [Header("UI")]
    public GameUI gameUI;

    [Header("演出")]
    public SuccessEffectUI successEffectUI;

    [Header("失敗演出")]
    public FailureEffectUI failureEffectUI;

    // 失敗演出中はジャンプ操作を禁止する
    public bool ResultEffectPlaying =>
        failureEffectUI != null && failureEffectUI.IsPlaying;
    public bool GameEnded { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        playerData.Initialize(config);
        skillManager.SetPlayerData(playerData);
    }

    /// <summary>
    /// ジャンプの結果に応じて報酬・登録者数・日数を更新する。
    /// 成功時のみ日数を1日進める。
    /// </summary>
    public void OnJumpResult(bool success)
    {
        // ゲーム終了後は結果を処理しない
        if (GameEnded)
            return;

        // 設定データがない場合は処理を中止する
        if (config == null)
        {
            Debug.LogError("GameConfigが設定されていません。");
            return;
        }

        if (success)
        {
            // スキルによる報酬倍率を取得
            float moneyMultiplier = skillManager != null
                ? skillManager.GetMultiplier(SkillType.MoneyReward)
                : 1f;

            float subscriberMultiplier = skillManager != null
                ? skillManager.GetMultiplier(SkillType.SubscriberReward)
                : 1f;

            // 自転車の台数に応じて固定額を加算
            long moneyBase =
                config.baseMoneyReward +
                (long)playerData.bicycleCount * config.moneyBonusPerBicycle;

            long subscriberBase =
                config.baseSubscriberReward +
                (long)playerData.bicycleCount * config.subscriberBonusPerBicycle;

            // スキル倍率を適用
            long moneyReward = Math.Max(
                1L,
                (long)(moneyBase * moneyMultiplier)
            );

            long subscriberReward = Math.Max(
                1L,
                (long)(subscriberBase * subscriberMultiplier)
            );

            // お金と登録者を加算
            playerData.money += moneyReward;
            playerData.subscribers += subscriberReward;

            // 成功演出を再生
            if (successEffectUI != null)
            {
                successEffectUI.Play(
                    moneyReward,
                    subscriberReward
                );
            }

            // 成功したので翌日へ進む
            playerData.day++;

            Debug.Log(
                $"ジャンプ成功！ {moneyReward}円獲得、" +
                $"登録者 +{subscriberReward}人。{playerData.day}日目！"
            );

            // 登録者100万人を達成したらゲームクリア
            if (playerData.subscribers >= config.targetSubscribers)
            {
                playerData.subscribers = config.targetSubscribers;
                GameEnded = true;

                Debug.Log(
                    $"ゲームクリア！ {playerData.day}日で登録者100万人達成！"
                );
            }
        }
        else
        {
            // 減少する登録者数を記録
            long subscriberLoss = Math.Min(
                playerData.subscribers,
                config.baseSubscriberLoss
            );

            // 登録者を減らす
            playerData.subscribers = Math.Max(
                0L,
                playerData.subscribers - config.baseSubscriberLoss
            );

            Debug.Log(
                $"ジャンプ失敗！ 登録者数：{playerData.subscribers}人"
            );

            // 登録者が0人になったらゲームオーバー
            if (playerData.subscribers <= 0)
            {
                GameEnded = true;
                Debug.Log($"ゲームオーバー：{playerData.day}日目");
            }

            // 失敗演出を再生
            if (failureEffectUI != null)
            {
                failureEffectUI.Play(
                    subscriberLoss,
                    OnFailureEffectFinished
                );
            }
            else if (!GameEnded)
            {
                // 演出が未設定の場合はすぐ翌日へ
                playerData.day++;
            }
        }

        // 登録者数に応じてアンチ数を更新する
        playerData.antiCount =
            playerData.subscribers /
            Math.Max(1L, config.subscribersPerAnti);

        // お金・登録者数・日数の表示を更新する
        RefreshUI();
    }

    /// <summary>
    /// 指定された金額を所持金から支払う。
    /// 支払いに成功した場合はtrueを返す。
    /// </summary>
    public bool SpendMoney(long amount)
    {
        // ゲーム終了後、負の金額、所持金不足の場合は支払わない
        if (GameEnded || amount < 0 || playerData.money < amount)
            return false;

        // 所持金から購入金額を差し引く
        playerData.money -= amount;

        return true;
    }

    /// <summary>
    /// プレイヤーデータを画面のUIに反映する。
    /// </summary>
    public void RefreshUI()
    {
        if (gameUI != null)
        {
            gameUI.RefreshUI();
        }
    }

    private void OnFailureEffectFinished()
    {
        // ゲームオーバーなら翌日に進まない
        if (!GameEnded)
        {
            playerData.day++;

            Debug.Log($"{playerData.day}日目になりました！");
        }

        RefreshUI();
    }
}
