using UnityEngine;

/// <summary>
/// 自転車の配置や購入価格に関する固定設定を管理するデータ。
/// ScriptableObjectとして作成し、BicycleManagerに設定する。
/// </summary>
[CreateAssetMenu(
    fileName = "BicycleConfig",
    menuName = "BikeJump/Bicycle Config"
)]
public class BicycleConfig : ScriptableObject
{
    [Header("初期配置設定")]

    [Tooltip("ゲーム開始時に配置する自転車の台数")]
    public int initialBicycles = 3;

    [Tooltip("自転車同士の横方向の間隔")]
    public float bicycleSpacing = 2f;

    [Tooltip("最初の自転車を配置するX座標")]
    public float startX = 4f;

    [Tooltip("自転車を配置するY座標")]
    public float bicycleY = 0.5f;

    [Header("購入価格設定")]

    [Tooltip("自転車の初期購入価格")]
    public long baseBicyclePrice = 100;

    [Tooltip("購入するたびに価格に加算する金額")]
    public long priceIncrease = 500;
}
