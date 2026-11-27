using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 自転車の配置、購入、台数、購入価格を管理するクラス。
/// 自転車Prefabを生成し、横一列に並べる。
/// PERFECT着地の位置（最後の自転車の少し先）も管理する。
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

    [Header("PERFECT設定")]

    [Tooltip("最後の自転車の中心から、PERFECTの中心までの距離")]
    public float perfectOffset = 3f;

    [Tooltip("PERFECTの中心から左右に何mまでをPERFECTにするか")]
    public float perfectRadius = 0.8f;

    [Header("PERFECTマーカー（ワールド上のスプライト）")]

    [Tooltip("マーカー全体の親。見た目の中心がPERFECTの位置に合うように動く（任意）")]
    public Transform perfectMarker;

    [Tooltip("判定の幅に合わせて伸縮させる画像（赤丸など）。perfectMarkerの子にする（任意）")]
    public SpriteRenderer markerSprite;

    [Tooltip("マーカーが目標位置へ移動する速さ。0なら瞬間移動")]
    public float markerMoveSpeed = 8f;

    [Header("デバッグ")]

    [Tooltip("判定範囲（黄色い枠）を常にSceneビュー・Gameビューに表示する")]
    public bool showPerfectGizmo = true;

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
    /// PERFECTの中心X座標。自転車が増えると右へ動く。
    /// </summary>
    public float PerfectX => LastBicycleX + perfectOffset;

    /// <summary>
    /// 指定したX座標がPERFECTの範囲内かどうかを返す。
    /// </summary>
    public bool IsPerfect(float x)
    {
        return Mathf.Abs(x - PerfectX) <= perfectRadius;
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

        if (perfectMarker == null)
        {
            Debug.LogWarning(
                "Perfect Marker が未設定です。マーカーは動きません。",
                this
            );
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

        // 最初だけは、滑らかに動かさず正しい位置へ置く
        SnapMarker();
    }

    /// <summary>
    /// マーカーを目標位置へ滑らかに近づける。
    /// </summary>
    private void Update()
    {
        if (perfectMarker == null || markerMoveSpeed <= 0f)
            return;

        Vector3 p = perfectMarker.position;
        float t = 1f - Mathf.Exp(-markerMoveSpeed * Time.deltaTime);
        p.x = Mathf.Lerp(p.x, GetMarkerTargetX(), t);
        perfectMarker.position = p;
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
    /// 自転車の追加後に必要な更新処理を行う。
    /// マーカーの幅を判定幅に合わせ、位置を更新する。
    /// 配置済みの自転車は移動させない。
    /// </summary>
    public void UpdateBicycles()
    {
        // 幅を先に合わせてから、位置を決める（幅で見た目の中心が変わるため）
        FitMarkerWidth();

        // 速度が0なら瞬間移動。それ以外はUpdateで滑らかに動く
        if (markerMoveSpeed <= 0f)
            SnapMarker();
    }

    /// <summary>
    /// マーカーを目標位置へ、すぐに移動させる。
    /// </summary>
    private void SnapMarker()
    {
        if (perfectMarker == null)
            return;

        Vector3 p = perfectMarker.position;
        p.x = GetMarkerTargetX();
        perfectMarker.position = p;
    }

    /// <summary>
    /// マーカー画像の見た目の中心が PerfectX に来るような、
    /// 親（perfectMarker）のX座標を返す。
    /// 子の画像にオフセットがあっても、画像のPivotが中央でなくても、
    /// 見た目の中心がPERFECTの判定の中心と一致する。
    /// </summary>
    private float GetMarkerTargetX()
    {
        if (perfectMarker == null)
            return PerfectX;

        if (markerSprite == null)
            return PerfectX;

        // 親の位置から見た、画像の見た目の中心のずれ
        float offset =
            markerSprite.bounds.center.x - perfectMarker.position.x;

        return PerfectX - offset;
    }

    /// <summary>
    /// マーカー画像の横幅を、PERFECTの判定幅（perfectRadius × 2）に合わせる。
    /// 見た目と判定がずれないようにするための処理。
    /// </summary>
    private void FitMarkerWidth()
    {
        if (markerSprite == null || markerSprite.sprite == null)
            return;

        // 画像そのもののワールド上の幅（スケール1のとき）
        float spriteWidth = markerSprite.sprite.bounds.size.x;
        if (spriteWidth <= 0f)
            return;

        // 親のスケールも考慮して、見た目の幅を判定幅に合わせる
        Transform t = markerSprite.transform;
        float parentScaleX =
            t.parent != null ? t.parent.lossyScale.x : 1f;
        if (Mathf.Approximately(parentScaleX, 0f))
            parentScaleX = 1f;

        Vector3 s = t.localScale;
        s.x = perfectRadius * 2f / spriteWidth / parentScaleX;
        t.localScale = s;
    }

    /// <summary>
    /// PERFECTの判定範囲を、黄色い枠と中心線で常に表示する。
    /// Gameビューでは、右上の「Gizmos」ボタンをオンにすると見える。
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showPerfectGizmo)
            return;

        float x = Application.isPlaying
            ? PerfectX
            : (perfectMarker != null ? perfectMarker.position.x : 0f);
        float y = perfectMarker != null ? perfectMarker.position.y : 0f;

        Gizmos.color = Color.yellow;

        // 判定の中心線
        Gizmos.DrawLine(
            new Vector3(x, y - 1f, 0f),
            new Vector3(x, y + 4f, 0f)
        );

        // 判定範囲（左右perfectRadiusまで）
        Gizmos.DrawWireCube(
            new Vector3(x, y + 1.5f, 0f),
            new Vector3(perfectRadius * 2f, 3f, 0.1f)
        );
    }
}