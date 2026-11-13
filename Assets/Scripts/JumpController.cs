using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum JumpMode
{
    Charge,   // 長押しでパワーが往復 → 離してジャンプ（角度固定）
    Drag      // 引っ張った方向と距離でジャンプ（従来方式）
}

/// <summary>
/// プレイヤーのジャンプ操作と着地判定を管理する。
/// PlayerにRigidbody2DとLineRendererを付けて使用する。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JumpController : MonoBehaviour
{
    [Header("ジャンプ方式")]
    public JumpMode jumpMode = JumpMode.Charge;

    [Header("ジャンプ設定（共通）")]

    [Tooltip("ジャンプ力の最大値")]
    public float maxLaunchPower = 18f;

    [Tooltip("ジャンプ力の最小値")]
    public float minLaunchPower = 2f;

    [Header("チャージ式の設定")]

    [Tooltip("パワーが0→1になるまでの秒数（片道）")]
    public float chargeTime = 0.75f;

    [Tooltip("チャージ式ジャンプの角度（度）")]
    public float chargeAngle = 55f;

    [Tooltip("チャージ中の照準線の最大の長さ")]
    public float chargeLineLength = 2f;

    [Tooltip("チャージ量のゲージ（任意。Slider の Min=0 Max=1）")]
    [SerializeField] private Slider chargeGauge;

    [Header("ドラッグ式の設定")]

    [Tooltip("引っ張る距離をジャンプ力に変換する倍率")]
    public float powerMultiplier = 5f;

    [Tooltip("引っ張れる最大距離")]
    public float maxPullDistance = 2f;

    [Header("リスポーン設定")]

    [Tooltip("リスポーンする位置")]
    public Vector2 respawnPosition = new Vector2(-11f, 0f);

    [Header("地面判定")]

    [Tooltip("地面として扱うレイヤー")]
    public LayerMask groundLayer;

    [Header("照準線設定")]

    [Tooltip("照準線の太さ")]
    public float dragLineWidth = 0.08f;

    [Tooltip("照準線の色")]
    public Color dragLineColor = Color.yellow;

    [Header("プレイヤー画像設定")]

    [Tooltip("通常時の画像")]
    public Sprite normalSprite;

    [Tooltip("ジャンプ中の画像")]
    public Sprite jumpSprite;

    [Header("UI設定")]
    [SerializeField] private GameObject shopUI;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private LineRenderer dragLine;

    // ドラッグ式
    private Vector2 startPosition;
    private Vector2 dragPosition;
    private bool isDragging;

    // チャージ式
    private bool isCharging;
    private float chargeValue;   // 0?1
    private float chargeDir = 1f;

    private bool isGrounded;
    private bool hasLaunched;

    private JumpEffects Fx =>
        GameManager.Instance != null ? GameManager.Instance.jumpEffects : null;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        dragLine = GetComponent<LineRenderer>();

        if (dragLine == null)
        {
            dragLine = gameObject.AddComponent<LineRenderer>();
        }

        dragLine.useWorldSpace = true;
        dragLine.positionCount = 2;
        dragLine.startWidth = dragLineWidth;
        dragLine.endWidth = dragLineWidth;
        dragLine.startColor = dragLineColor;
        dragLine.endColor = dragLineColor;
        dragLine.enabled = false;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }

        if (chargeGauge != null) chargeGauge.gameObject.SetActive(false);
    }

    private void Start()
    {
        isGrounded = true;
    }

    private void Update()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.GameEnded)
            return;

        // ショップが開いている間は操作禁止
        if (shopUI != null && shopUI.activeInHierarchy)
        {
            CancelInput();
            return;
        }

        if (Camera.main == null)
            return;

        if (jumpMode == JumpMode.Charge) UpdateCharge();
        else UpdateDrag();
    }

    private void CancelInput()
    {
        isDragging = false;
        isCharging = false;
        if (dragLine != null) dragLine.enabled = false;
        if (chargeGauge != null) chargeGauge.gameObject.SetActive(false);
    }

    // ========================================
    // チャージ式
    // ========================================

    private void UpdateCharge()
    {
        bool down = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        bool held = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
        bool up = Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space);

        // チャージ開始
        if (!hasLaunched && !isCharging && isGrounded && down)
        {
            if (Input.GetMouseButtonDown(0) &&
                EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return;

            startPosition = rb.position;
            chargeValue = 0f;
            chargeDir = 1f;
            isCharging = true;

            dragLine.enabled = true;
            if (chargeGauge != null)
            {
                chargeGauge.gameObject.SetActive(true);
                chargeGauge.value = 0f;
            }

            if (Fx != null) Fx.OnChargeStart();
            UpdateChargeLine();
        }

        // チャージ中：0→1→0 を往復
        if (isCharging && held)
        {
            chargeValue += chargeDir * Time.deltaTime / Mathf.Max(0.01f, chargeTime);

            if (chargeValue >= 1f) { chargeValue = 1f; chargeDir = -1f; }
            if (chargeValue <= 0f) { chargeValue = 0f; chargeDir = 1f; }

            if (chargeGauge != null) chargeGauge.value = chargeValue;
            UpdateChargeLine();
        }

        // 離したらジャンプ
        if (isCharging && up)
        {
            isCharging = false;
            dragLine.enabled = false;
            if (chargeGauge != null) chargeGauge.gameObject.SetActive(false);

            LaunchCharge();
        }
    }

    private Vector2 ChargeDirection()
    {
        float rad = chargeAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private void UpdateChargeLine()
    {
        Vector3 start = new Vector3(rb.position.x, rb.position.y, transform.position.z);
        Vector3 end = start + (Vector3)(ChargeDirection() * chargeLineLength * chargeValue);

        dragLine.SetPosition(0, start);
        dragLine.SetPosition(1, end);
    }

    private void LaunchCharge()
    {
        float power = Mathf.Lerp(minLaunchPower, maxLaunchPower, chargeValue);
        ApplyLaunch(ChargeDirection(), power);
    }

    // ========================================
    // ドラッグ式（従来方式）
    // ========================================

    private void UpdateDrag()
    {
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

            if (Fx != null) Fx.OnChargeStart();
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
            dragPosition = GetMouseWorldPosition();
            UpdateDragLine();

            isDragging = false;
            dragLine.enabled = false;

            LaunchDrag();
        }
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector3 screenPosition = Input.mousePosition;
        screenPosition.z = Mathf.Abs(
            Camera.main.transform.position.z - transform.position.z
        );

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        return new Vector2(worldPosition.x, worldPosition.y);
    }

    private void UpdateDragLine()
    {
        Vector2 pull = Vector2.ClampMagnitude(startPosition - dragPosition, maxPullDistance);
        Vector2 aimDirection = pull.normalized;

        Vector3 lineStart = new Vector3(startPosition.x, startPosition.y, transform.position.z);
        Vector3 lineEnd = lineStart + new Vector3(
            aimDirection.x * pull.magnitude,
            aimDirection.y * pull.magnitude,
            0f
        );

        dragLine.SetPosition(0, lineStart);
        dragLine.SetPosition(1, lineEnd);
    }

    private void LaunchDrag()
    {
        Vector2 pull = Vector2.ClampMagnitude(startPosition - dragPosition, maxPullDistance);

        float power = Mathf.Clamp(
            pull.magnitude * powerMultiplier,
            minLaunchPower,
            maxLaunchPower
        );

        // 左へ引っ張ったときは右方向、上方向の力は最低限確保する
        float horizontal = Mathf.Max(0.2f, pull.x);
        float vertical = Mathf.Max(0.2f, pull.y);

        ApplyLaunch(new Vector2(horizontal, vertical).normalized, power);
    }

    // ========================================
    // 共通：ジャンプの実行
    // ========================================

    private void ApplyLaunch(Vector2 direction, float power)
    {
        float jumpMultiplier = 1f;
        float distanceMultiplier = 1f;

        if (GameManager.Instance.skillManager != null)
        {
            jumpMultiplier =
                GameManager.Instance.skillManager.GetMultiplier(SkillType.JumpPower);

            distanceMultiplier =
                GameManager.Instance.skillManager.GetMultiplier(SkillType.JumpDistance);
        }

        rb.linearVelocity = Vector2.zero;

        // JumpDistanceは横方向の力にだけ適用する（正規化し直さない）
        Vector2 launchForce = new Vector2(
            direction.x * distanceMultiplier,
            direction.y
        );

        rb.AddForce(launchForce * power * jumpMultiplier, ForceMode2D.Impulse);

        hasLaunched = true;
        isGrounded = false;

        if (spriteRenderer != null && jumpSprite != null)
        {
            spriteRenderer.sprite = jumpSprite;
        }

        if (Fx != null) Fx.OnLaunch(transform.position);
    }

    // ========================================
    // 着地判定
    // ========================================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!hasLaunched)
            return;

        Vector2 contact = collision.contactCount > 0
            ? collision.GetContact(0).point
            : (Vector2)transform.position;

        // 自転車にぶつかったら失敗
        if (collision.gameObject.CompareTag("Bicycle"))
        {
            hasLaunched = false;
            isGrounded = true;

            FinishJumpAsFailure(contact);
            return;
        }

        bool isGroundLayer =
            (groundLayer.value & (1 << collision.gameObject.layer)) != 0;

        if (isGroundLayer)
        {
            FinishJump(contact);
        }
    }

    private void FinishJumpAsFailure(Vector2 contactPoint)
    {
        if (spriteRenderer != null && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }

        if (GameManager.Instance == null || GameManager.Instance.GameEnded)
        {
            return;
        }

        if (Fx != null) Fx.OnLand(contactPoint, false, false);

        GameManager.Instance.OnJumpResult(false);

        if (!GameManager.Instance.GameEnded)
        {
            ResetPlayer();
        }
    }

    private void FinishJump(Vector2 contactPoint)
    {
        hasLaunched = false;
        isGrounded = true;

        if (spriteRenderer != null && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }

        if (GameManager.Instance == null || GameManager.Instance.GameEnded)
        {
            return;
        }

        BicycleManager bikes = GameManager.Instance.bicycleManager;

        // 最後の自転車を越えていれば成功
        bool success =
            bikes != null &&
            transform.position.x > bikes.LastBicycleX + 0.5f;

        // PERFECTマーカーの近くに着地していれば PERFECT
        bool perfect = success && bikes.IsPerfect(transform.position.x);

        if (Fx != null) Fx.OnLand(contactPoint, success, perfect);

        GameManager.Instance.OnJumpResult(success, perfect);

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
        if (spriteRenderer != null && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }

        CancelInput();
        hasLaunched = false;
        isGrounded = true;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        transform.position = new Vector3(
            respawnPosition.x,
            respawnPosition.y,
            transform.position.z
        );
    }
}