using UnityEngine;
/// <summary>
/// ゲーム全体の設定をアセットとして管理
/// </summary>
[CreateAssetMenu(
    fileName = "GameConfig",
    menuName = "BikeJump/Game Config"
)]
public class GameConfig : ScriptableObject
{
    [Header("初期データ")]
    public long initialSubscribers = 100;
    public long initialMoney = 100;
    public int initialBicycles = 3;

    [Header("ゲーム目標")]
    public long targetSubscribers = 1_000_000;

    [Header("クリア報酬")]
    public long baseMoneyReward = 50;
    public long baseSubscriberReward = 100;

    [Header("自転車1台ごとに追加される報酬")]
    public long moneyBonusPerBicycle = 50;
    public long subscriberBonusPerBicycle = 100;

    [Header("失敗ペナルティ")]
    public long baseSubscriberLoss = 50;

    [Header("アンチ設定")]
    public long subscribersPerAnti = 1000;
}