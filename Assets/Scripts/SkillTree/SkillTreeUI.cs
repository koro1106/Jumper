using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スキルツリー画面全体の管理。
/// ・スキルボタンで開く / 閉じるボタンで閉じる
/// ・ノード同士を前提関係に沿って線でつなぐ
/// ・ノードを選ぶと詳細を表示し、購入ボタンでレベルアップ
/// 演出を追加したい箇所には「【演出追加】」コメントを入れてある。
/// </summary>
public class SkillTreeUI : MonoBehaviour
{
    // ========================================
    // Inspector 設定項目
    // ========================================

    [Header("参照")]
    [SerializeField] private SkillManager skillManager;

    [Header("パネル")]
    [SerializeField] private GameObject panelRoot;   // スキルツリー画面全体
    [SerializeField] private Button openButton;      // 結果画面の「スキル」ボタン
    [SerializeField] private Button closeButton;     // 画面内の「閉じる」ボタン

    [Header("ツリー")]
    [Tooltip("空なら子オブジェクトの SkillNodeUI を自動で集める")]
    [SerializeField] private SkillNodeUI[] nodes;
    [Tooltip("線を生やす親。ノードより【前】(hierarchyの上)に置くと線がノードの裏に隠れる")]
    [SerializeField] private RectTransform lineParent;
    [Tooltip("線のひな形（Imageだけの非アクティブなオブジェクト）")]
    [SerializeField] private Image linePrefab;
    [SerializeField] private float lineThickness = 6f;
    [SerializeField] private Color lineLockedColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private Color lineUnlockedColor = Color.white;

    [Header("所持金")]
    [SerializeField] private TMP_Text moneyText;

    [Header("詳細パネル")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonLabel;

    // ========================================
    // 内部状態
    // ========================================

    private class LineEntry
    {
        public Image image;
        public SkillData prerequisite;
    }

    private readonly List<LineEntry> lines = new List<LineEntry>();
    private SkillNodeUI selectedNode;
    private bool linesBuilt = false;

    // ========================================
    // Unity イベント
    // ========================================

    private void Awake()
    {
        if (nodes == null || nodes.Length == 0)
        {
            nodes = GetComponentsInChildren<SkillNodeUI>(true);
        }

        foreach (SkillNodeUI node in nodes)
        {
            node.Setup(skillManager, OnNodeSelected);
        }

        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (buyButton != null) buyButton.onClick.AddListener(OnClickBuy);

        if (skillManager != null) skillManager.OnSkillBought += OnSkillBought;

        // 最初は閉じておく
        panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (skillManager != null) skillManager.OnSkillBought -= OnSkillBought;

        if (openButton != null) openButton.onClick.RemoveListener(Open);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (buyButton != null) buyButton.onClick.RemoveListener(OnClickBuy);
    }

    // ========================================
    // 開く / 閉じる
    // ========================================

    public void Open()
    {
        // 【演出追加】ボタンクリック時の演出（決定SEなど）
        // 【演出追加】スキルツリー画面 開演出（フェードイン、スライドインなど）

        panelRoot.SetActive(true);

        // 線の位置はレイアウト確定後に計算する
        Canvas.ForceUpdateCanvases();
        if (!linesBuilt)
        {
            BuildLines();
            linesBuilt = true;
        }

        if (selectedNode == null && nodes.Length > 0)
        {
            OnNodeSelected(nodes[0]);
        }

        RefreshAll();
    }

    public void Close()
    {
        // 【演出追加】ボタンクリック時の演出（キャンセルSEなど）
        // 【演出追加】スキルツリー画面 閉演出（フェードアウト、スライドアウトなど）

        panelRoot.SetActive(false);
    }

    // ========================================
    // 選択・購入
    // ========================================

    private void OnNodeSelected(SkillNodeUI node)
    {
        if (selectedNode != null) selectedNode.SetSelected(false);

        selectedNode = node;
        selectedNode.SetSelected(true);

        // 【演出追加】スキル選択時の演出（選択SEなど）

        RefreshDetail();
    }

    private void OnClickBuy()
    {
        if (selectedNode == null || skillManager == null) return;

        bool success = skillManager.BuySkill(selectedNode.Data);

        if (success)
        {
            // 【演出追加】購入成功の演出（レベルアップSE、ノードがキラッと光る演出など）
        }
        else
        {
            // 【演出追加】購入失敗の演出（エラーSE、ボタンを揺らすなど）
        }

        // 表示の更新は OnSkillBought 経由で行われる
    }

    private void OnSkillBought(SkillData skill)
    {
        if (panelRoot.activeInHierarchy) RefreshAll();
    }

    // ========================================
    // 表示の更新
    // ========================================

    // 所持金が変わったとき（ジャンプ後など）に外部から呼んでもOK
    public void RefreshMoney()
    {
        if (moneyText == null || skillManager == null || skillManager.playerData == null) return;
        moneyText.text = $"{skillManager.playerData.money}円";
    }

    private void RefreshAll()
    {
        foreach (SkillNodeUI node in nodes)
        {
            node.Refresh();
        }

        RefreshLines();
        RefreshDetail();
        RefreshMoney();
    }

    private void RefreshDetail()
    {
        if (selectedNode == null || skillManager == null)
        {
            nameText.text = "スキルを選択してください";
            descriptionText.text = "";
            levelText.text = "";
            effectText.text = "";
            costText.text = "";
            buyButton.interactable = false;
            buyButtonLabel.text = "購入";
            return;
        }

        SkillData skill = selectedNode.Data;
        int level = skillManager.GetLevel(skill);
        string typeName = GetTypeName(skill.skillType);

        nameText.text = skill.skillName;
        levelText.text = $"Lv {level} / {skill.maxLevel}";

        float current = skill.valuePerLevel * level * 100f;
        if (skillManager.IsMaxed(skill))
        {
            effectText.text = $"{typeName} +{current:0.#}%（最大）";
            costText.text = "-";
        }
        else
        {
            float next = skill.valuePerLevel * (level + 1) * 100f;
            effectText.text = $"{typeName} +{current:0.#}% → +{next:0.#}%";
            costText.text = $"コスト {skillManager.GetNextCost(skill)}円";
        }

        // 説明文（未解放なら解放条件も表示）
        StringBuilder desc = new StringBuilder(skill.description);
        if (!skillManager.IsUnlocked(skill) && skill.prerequisite != null)
        {
            desc.Append("\n\n<color=#FF6666>解放条件</color>\n");
            desc.Append($"・{skill.prerequisite.skillName} を習得");
        }
        descriptionText.text = desc.ToString();

        // 購入ボタン
        SkillBuyState state = skillManager.GetBuyState(skill);
        buyButton.interactable = (state == SkillBuyState.Ok);
        buyButtonLabel.text = GetBuyLabel(state);
    }

    private string GetBuyLabel(SkillBuyState state)
    {
        switch (state)
        {
            case SkillBuyState.Ok: return "購入";
            case SkillBuyState.Maxed: return "MAX";
            case SkillBuyState.Locked: return "未解放";
            case SkillBuyState.NotEnoughMoney: return "お金が足りません";
            default: return "購入";
        }
    }

    // UI表示用の日本語名
    private string GetTypeName(SkillType type)
    {
        switch (type)
        {
            case SkillType.JumpPower: return "ジャンプ力";
            case SkillType.JumpDistance: return "飛距離";
            case SkillType.MoneyReward: return "お金獲得量";
            case SkillType.SubscriberReward: return "登録者獲得量";
            default: return type.ToString();
        }
    }

    // ========================================
    // ノード間の線
    // ========================================

    private void BuildLines()
    {
        if (lineParent == null || linePrefab == null) return;

        foreach (SkillNodeUI node in nodes)
        {
            if (node.Data == null || node.Data.prerequisite == null) continue;

            SkillNodeUI fromNode = FindNode(node.Data.prerequisite);
            if (fromNode == null) continue;

            Image line = Instantiate(linePrefab, lineParent);
            line.gameObject.SetActive(true);

            // 2つのノード中心を、線の親のローカル座標に変換
            Vector3 a = lineParent.InverseTransformPoint(fromNode.RectTransform.position);
            Vector3 b = lineParent.InverseTransformPoint(node.RectTransform.position);
            a.z = 0f;
            b.z = 0f;
            Vector3 dir = b - a;

            RectTransform rt = line.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(dir.magnitude, lineThickness);
            rt.localPosition = (a + b) * 0.5f;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            lines.Add(new LineEntry { image = line, prerequisite = node.Data.prerequisite });
        }
    }

    // 前提を習得済みの線は明るく、未習得の線は暗くする
    private void RefreshLines()
    {
        if (skillManager == null) return;

        foreach (LineEntry entry in lines)
        {
            bool satisfied = skillManager.GetLevel(entry.prerequisite) > 0;
            entry.image.color = satisfied ? lineUnlockedColor : lineLockedColor;
        }
    }

    private SkillNodeUI FindNode(SkillData data)
    {
        if (data == null) return null;

        foreach (SkillNodeUI node in nodes)
        {
            if (node.Data == data) return node;
        }
        return null;
    }
}