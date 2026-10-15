using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 自転車の配置、購入、台数、購入価格を管理するクラス。
/// 自転車Prefabを生成し、横一列に並べる。
/// </summary>
public class BicycleManager : MonoBehaviour
{
    [Header("固定設定")]

    [Tooltip("自転車の配置・価格設定を管理するScriptableObject")]
    public BicycleConfig config;

    [Header("Prefab設定")]

    [Tooltip("生成する自転車のPrefab")]
    public GameObject bicyclePrefab;

    [Tooltip("生成した自転車をまとめる親Transform")]
    public Transform bicycleParent;

    // 生成済みの自転車を管理するリスト
    private readonly List<GameObject> bicycles =
        new List<GameObject>();

    // 購入した自転車の台数（初期配置分は含めない）
    private int purchasedBicycles = 0;

    /// <summary>
    /// 現在配置されている自転車の台数を返す。
    /// </summary>
    public int BicycleCount => bicycles.Count;

    /// <summary>
    /// 最後に配置されている自転車のX座標を返す。
    /// 自転車が存在しない場合は0を返す。
    /// </summary>
    public float LastBicycleX
    {
        get
        {
            if (bicycles.Count == 0)
                return 0f;

            return bicycles[bicycles.Count - 1]
                .transform.position.x;
        }
    }

    /// <summary>
    /// 次に購入する自転車の価格を返す。
    /// 購入済み台数が増えるほど価格が高くなる。
    /// </summary>
    public long NextBicyclePrice
    {
        get
        {
            // 設定が割り当てられていない場合は購入不可とする
            if (config == null)
                return 0L;

            // 現在の台数から価格を計算する
            double price =
            config.baseBicyclePrice +
            (double)config.priceIncrease * purchasedBicycles;

            if (price >= long.MaxValue)
                return long.MaxValue;

            return System.Math.Max(1L, (long)price);
        }
    }

    /// <summary>
    /// ゲーム開始時に初期台数分の自転車を配置する。
    /// </summary>
    private void Start()
    {
        // 必要な設定がない場合は処理を中止する
        if (config == null)
        {
            Debug.LogError(
                "BicycleConfigが設定されていません。",
                this
            );
            return;
        }

        if (bicyclePrefab == null)
        {
            Debug.LogError(
                "Bicycle Prefabが設定されていません。",
                this
            );
            return;
        }

        // 設定された初期台数分の自転車を生成する
        for (int i = 0; i < config.initialBicycles; i++)
        {
            AddBicycle();
        }

        // 初期配置した自転車の台数をプレイヤーデータに反映する
        GameManager.Instance.playerData.bicycleCount = BicycleCount;

        // 初期配置後に必要な更新処理を実行する
        UpdateBicycles();
    }

    /// <summary>
    /// お金を消費して自転車を1台購入する。
    /// 購入に成功した場合のみ自転車を生成する。
    /// </summary>
    public void BuyBicycle()
    {
        // ゲーム管理クラスが存在しない場合は購入しない
        if (GameManager.Instance == null)
            return;

        // ゲーム終了後は購入できない
        if (GameManager.Instance.GameEnded)
            return;

        // 現在の購入価格を取得する
        long price = NextBicyclePrice;

        // ScriptableObjectの設定が不足している場合は購入しない
        if (config == null || bicyclePrefab == null)
        {
            Debug.LogError(
                "自転車の設定またはPrefabがありません。",
                this
            );
            return;
        }

        // 所持金から購入価格を支払う
        if (!GameManager.Instance.SpendMoney(price))
        {
            // お金が足りない場合はメッセージを表示する
            Debug.Log("お金が足りません！");
            return;
        }

        // 購入成功後に自転車を1台追加する
        AddBicycle();

        // 購入回数を増やす
        purchasedBicycles++;

        // 自転車の配置とUIを更新する
        UpdateBicycles();
        GameManager.Instance.RefreshUI();

        GameManager.Instance.playerData.bicycleCount = BicycleCount;
    }

    /// <summary>
    /// 自転車Prefabを1台生成し、横一列に配置する。
    /// </summary>
    private void AddBicycle()
    {
        // 設定がなければ生成しない
        if (config == null || bicyclePrefab == null)
            return;

        // 新しい自転車の並び順を取得する
        int index = bicycles.Count;

        // 台数と間隔から配置位置を計算する
        Vector3 position = new Vector3(
            config.startX + index * config.bicycleSpacing,
            config.bicycleY,
            0f
        );

        // 自転車Prefabを生成する
        GameObject bicycle = Instantiate(
            bicyclePrefab,
            position,
            Quaternion.identity,
            bicycleParent
        );

        // 生成した自転車をリストに登録する
        bicycles.Add(bicycle);
    }

    /// <summary>
    /// 自転車の追加・削除後に必要な更新処理を行う。
    /// 現時点では配置済みの自転車を移動させない。
    /// カメラ追従やステージ拡張を追加するときに利用する。
    /// </summary>
    public void UpdateBicycles()
    {
        // 現在は追加の更新処理なし
    }
}