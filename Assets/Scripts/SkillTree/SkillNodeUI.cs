using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スキルツリー上の「1個のスキルアイコン」。
/// ボタンを押すと SkillTreeUI に選択されたことを伝える。
/// </summary>
public class SkillNodeUI : MonoBehaviour
{
    [Header("スキル")]
    [SerializeField] private SkillData skill;

    [Header("UI参照")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image frameImage;         // 枠（状態で色が変わる）
    [SerializeField] private TMP_Text levelText;       // 「2/5」「MAX」
    [SerializeField] private GameObject lockedOverlay; // 未解放のときに被せる暗い画像
    [SerializeField] private GameObject selectedMark;  // 選択中の強調表示

    [Header("ボタン本体の色")]
    [Tooltip("空ならButtonのTarget Graphic（Image）を自動で使う")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color unlockedButtonColor = Color.white;
    [SerializeField] private Color lockedButtonColor = new Color(0.4f, 0.4f, 0.4f, 1f); // 未解放：暗いグレー

    [Header("枠の色")]
    [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color availableColor = Color.white;                // 解放済み・未購入
    [SerializeField] private Color ownedColor = new Color(0.4f, 0.8f, 1f);      // 1Lv以上
    [SerializeField] private Color maxedColor = new Color(1f, 0.85f, 0.2f);     // MAX

    private SkillManager manager;
    private Action<SkillNodeUI> onSelected;

    public SkillData Data => skill;
    public RectTransform RectTransform => (RectTransform)transform;

    // SkillTreeUI から最初に1回呼ぶ
    public void Setup(SkillManager skillManager, Action<SkillNodeUI> selectedCallback)
    {
        manager = skillManager;
        onSelected = selectedCallback;

        if (iconImage != null && skill != null) iconImage.sprite = skill.icon;

        // 未指定ならボタン自身のImageを使う
        if (buttonImage == null) buttonImage = button.targetGraphic as Image;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelected?.Invoke(this));

        SetSelected(false);
    }

    // 現在のレベル・解放状況に合わせて見た目を更新する
    public void Refresh()
    {
        if (manager == null || skill == null) return;

        int level = manager.GetLevel(skill);
        bool unlocked = manager.IsUnlocked(skill);
        bool maxed = manager.IsMaxed(skill);

        if (levelText != null)
        {
            levelText.text = maxed ? "MAX" : $"{level}/{skill.maxLevel}";
        }

        if (lockedOverlay != null) lockedOverlay.SetActive(!unlocked);

        // 未解放ならボタン本体を暗いグレーにする
        if (buttonImage != null)
        {
            buttonImage.color = unlocked ? unlockedButtonColor : lockedButtonColor;
        }

        if (frameImage != null)
        {
            if (!unlocked) frameImage.color = lockedColor;
            else if (maxed) frameImage.color = maxedColor;
            else if (level > 0) frameImage.color = ownedColor;
            else frameImage.color = availableColor;
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedMark != null) selectedMark.SetActive(selected);
    }
}