using UnityEngine;
using UnityEngine.EventSystems;
/// <summary>
/// プレイヤーのドラッグ操作、ジャンプ、着地判定を管理する。
/// PlayerにRigidbody2DとLineRendererを付けて使用する。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JumpController : MonoBehaviour
{
    [Header("ジャンプ設定")]

    [Tooltip("引っ張る距離をジャンプ力に変換する倍率")]
    public float powerMultiplier = 5f;

    [Tooltip("引っ張れる最大距離")]
    public float maxPullDistance = 2f;

    [Tooltip("ジャンプ力の最大値")]
    public float maxLaunchPower = 18f;

    [Tooltip("ジャンプ力の最小値")]
    public float minLaunchPower = 2f;

    [Header("リスポーン設定")]

    [Tooltip("リスポーンする位置")]
    public Vector2 respawnPosition = new Vector2(-11f, 0f);

    [Header("地面判定")]

    [Tooltip("地面として扱うレイヤー")]
    public LayerMask groundLayer;

    [Header("ドラッグ線設定")]

    [Tooltip("ドラッグ中に表示する線の太さ")]
    public float dragLineWidth = 0.08f;

    [Tooltip("ドラッグ線の色")]
    public Color dragLineColor = Color.yellow;

    [Header("プレイヤー画像設定")]

    [Tooltip("通常時の画像")]
    public Sprite normalSprite;

    [Tooltip("ジャンプ中の画像")]
    public Sprite jumpSprite;

    // 画像を変更するコンポーネント
    private SpriteRenderer spriteRenderer;

    // プレイヤーの物理挙動を制御する
    private Rigidbody2D rb;

    // ドラッグ開始時の位置
    private Vector2 startPosition;

    // 現在のマウス位置
    private Vector2 dragPosition;

    // ドラッグ中かどうか
    private bool isDragging;

    // 地面に接地しているか
    private bool isGrounded;

    // ジャンプ済みかどうか
    private bool hasLaunched;

    // ドラッグ線を描画する
    private LineRenderer dragLine;

    [Header("UI設定")]
    [SerializeField] private GameObject shopUI;
    /// <summary>
    /// コンポーネントを取得する。
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        dragLine = GetComponent<LineRenderer>();

        // LineRendererがなければ自動で追加する
        if (dragLine == null)
        {
            dragLine = gameObject.AddComponent<LineRenderer>();
        }

        // ワールド座標で線を描画する
        dragLine.useWorldSpace = true;
        dragLine.positionCount = 2;

        // 線の見た目を設定する
        dragLine.startWidth = dragLineWidth;
        dragLine.endWidth = dragLineWidth;
        dragLine.startColor = dragLineColor;
        dragLine.endColor = dragLineColor;

        // マウスのドラッグ中だけ線を表示する
        dragLine.enabled = false;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }
    }

    /// <summary>
    /// 初期状態では接地状態にする。
    /// </summary>
    private void Start()
    {
        isGrounded = true;
    }

    /// <summary>
    /// マウス入力とドラッグ線を毎フレーム更新する。
    /// </summary>
    private void Update()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.GameEnded)
            return;

        // ショップが開いている間は操作禁止
        if (shopUI != null && shopUI.activeInHierarchy)
        {
            if (isDragging)
            {
                isDragging = false;

                if (dragLine != null)
                    dragLine.enabled = false;
            }

            return;
        }

        if (Camera.main == null)
            return;

        // ドラッグ開始
        if (!hasLaunched && !isDragging && isGrounded &&
            Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return;

            startPosition = rb.position;
            dragPosition = GetMouseWorldPosition();

            isDragging = true;
            dragLine.enabled = true;

            UpdateDragLine();
        }

        // ドラッグ中
        if (isDragging && Input.GetMouseButton(0))
        {
            dragPosition = GetMouseWorldPosition();
            UpdateDragLine();
        }

        // マウスを離したらジャンプ
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            Debug.Log("マウスを離した");

            dragPosition = GetMouseWorldPosition();
            UpdateDragLine();

            isDragging = false;
            dragLine.enabled = false;

            Launch();

            Debug.Log("Launch実行");
        }
    }

    /// <summary>
    /// マウスのスクリーン座標をワールド座標に変換する。
    /// </summary>
    private Vector2 GetMouseWorldPosition()
    {
        // プレイヤーと同じZ平面までの距離を指定する
        Vector3 screenPosition = Input.mousePosition;
        screenPosition.z = Mathf.Abs(
            Camera.main.transform.position.z - transform.position.z
        );

        Vector3 worldPosition =
            Camera.main.ScreenToWorldPoint(screenPosition);

        return new Vector2(worldPosition.x, worldPosition.y);
    }

    /// <summary>
    /// 引っ張った方向と逆向きに照準線を表示する。
    /// </summary>
    private void UpdateDragLine()
    {
        // プレイヤーからマウスまでの引っ張り量
        Vector2 pull = startPosition - dragPosition;

        // 引っ張り量が大きすぎる場合は制限する
        pull = Vector2.ClampMagnitude(pull, maxPullDistance);

        // 引っ張った方向と逆向きの方向を求める
        Vector2 aimDirection = pull.normalized;

        // 引っ張りがほぼゼロなら線を短くする
        float pullDistance = pull.magnitude;

        // 引っ張った距離に応じて照準線の長さを変える
        float lineLength = pullDistance;

        // 線の始点はプレイヤーの位置
        Vector3 lineStart = new Vector3(
            startPosition.x,
            startPosition.y,
            transform.position.z
        );

        // 線の終点を引っ張りと逆方向へ伸ばす
        Vector3 lineEnd = lineStart + new Vector3(
            aimDirection.x * lineLength,
            aimDirection.y * lineLength,
            0f
        );

        dragLine.SetPosition(0, lineStart);
        dragLine.SetPosition(1, lineEnd);

        dragLine.startWidth = dragLineWidth;
        dragLine.endWidth = dragLineWidth;
        dragLine.startColor = dragLineColor;
        dragLine.endColor = dragLineColor;
    }

    /// <summary>
    /// 引っ張った距離・方向・スキル倍率を使ってジャンプする。
    /// </summary>
    private void Launch()
    {
        // プレイヤーからマウスを引いた方向を計算する
        Vector2 pull = startPosition - dragPosition;

        // 最大距離を超えないようにする
        pull = Vector2.ClampMagnitude(
            pull,
            maxPullDistance
        );

        // 引っ張った距離に応じてジャンプ力を決める
        float power = Mathf.Clamp(
            pull.magnitude * powerMultiplier,
            minLaunchPower,
            maxLaunchPower
        );

        // スキル倍率の初期値
        float jumpMultiplier = 1f;
        float distanceMultiplier = 1f;

        // スキルの効果を取得する
        if (GameManager.Instance.skillManager != null)
        {
            jumpMultiplier =
                GameManager.Instance.skillManager.GetMultiplier(
                    SkillType.JumpPower
                );

            distanceMultiplier =
                GameManager.Instance.skillManager.GetMultiplier(
                    SkillType.JumpDistance
                );
        }

        // 左方向へのジャンプを防ぐ
        // 左へ引っ張ったときは右方向への力が大きくなる
        float horizontal = Mathf.Max(0.2f, pull.x);

        // 上方向への力を最低限確保する
        float vertical = Mathf.Max(0.2f, pull.y);

        // 引っ張った方向に応じてジャンプ角度を決める
        Vector2 direction = new Vector2(
            horizontal,
            vertical
        ).normalized;

        // ジャンプ前に速度をリセットする
        rb.linearVelocity = Vector2.zero;

        // JumpDistanceは横方向の力に適用する。
        // 正規化し直さないことで飛距離倍率が弱まらないようにする。
        Vector2 launchForce = new Vector2(
            direction.x * distanceMultiplier,
            direction.y
        );

        // ジャンプ力とジャンプスキルを適用する
        rb.AddForce(
            launchForce * power * jumpMultiplier,
            ForceMode2D.Impulse
        );

        // ジャンプ中の状態にする
        hasLaunched = true;
        isGrounded = false;

        // ジャンプ中の画像に変更する
        if (spriteRenderer != null && jumpSprite != null)
        {
            spriteRenderer.sprite = jumpSprite;
        }
    }

    /// <summary>
    /// 地面との衝突時に着地判定を行う。
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ジャンプ中でなければ判定しない
        if (!hasLaunched)
            return;

        // 自転車にぶつかったら失敗
        if (collision.gameObject.CompareTag("Bicycle"))
        {
            hasLaunched = false;
            isGrounded = true;

            // 失敗処理を実行
            FinishJumpAsFailure();
            return;
        }

        // 地面との衝突判定
        bool isGroundLayer =
            (groundLayer.value &
            (1 << collision.gameObject.layer)) != 0;

        if (isGroundLayer)
        {
            FinishJump();
        }
    }

    private void FinishJumpAsFailure()
    {
        // 通常時の画像に戻す
        if (spriteRenderer != null && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }

        // ゲーム管理クラスがなければ終了
        if (GameManager.Instance == null ||
            GameManager.Instance.GameEnded)
        {
            return;
        }

        // 失敗として処理する
        GameManager.Instance.OnJumpResult(false);

        // ゲーム終了前ならプレイヤーを戻す
        if (!GameManager.Instance.GameEnded)
        {
            ResetPlayer();
        }
    }
    /// <summary>
    /// 着地時に成功・失敗を判定する。
    /// </summary>
    private void FinishJump()
    {
        // ジャンプ状態を解除する
        hasLaunched = false;
        isGrounded = true;

        // 通常時の画像に戻す
        if (spriteRenderer != null && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }

        // ゲーム管理クラスがない場合は終了する
        if (GameManager.Instance == null ||
            GameManager.Instance.GameEnded)
        {
            return;
        }

        // 最後の自転車を越えていれば成功
        bool success =
            GameManager.Instance.bicycleManager != null &&
            transform.position.x >
            GameManager.Instance.bicycleManager.LastBicycleX + 0.5f;

        // 成功・失敗の結果をゲーム管理クラスに渡す
        GameManager.Instance.OnJumpResult(success);
        // ゲームが終了していなければプレイヤーをリスポーンさせる
        if (!GameManager.Instance.GameEnded)
        {
            ResetPlayer();
        }
    }

    /// <summary>
    /// 次の日にプレイヤーをリスポーンさせる。
    /// </summary>
    public void ResetPlayer()
    {
        // リスポーン時は通常画像に戻す
        if (spriteRenderer != null && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }
        // ドラッグとジャンプの状態をリセットする
        isDragging = false;
        hasLaunched = false;
        isGrounded = true;

        // ドラッグ線を非表示にする
        if (dragLine != null)
            dragLine.enabled = false;

        // 物理速度をリセットする
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // 設定した位置に戻す
        transform.position = new Vector3(
            respawnPosition.x,
            respawnPosition.y,
            transform.position.z
        );
    }
}