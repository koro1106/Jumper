using System;
using UnityEngine;

/// <summary>
/// 購入の可否の状態（UIの表示用）
/// </summary>
public enum SkillBuyState
{
    Ok,
    Maxed,           // 最大レベル
    Locked,          // 前提スキルが未習得
    NotEnoughMoney   // お金が足りない
}

/// <summary>
/// スキルの購入・解放条件・効果倍率を管理します
/// </summary>
public class SkillManager : MonoBehaviour
{
    public SkillTreeData skillTree;
    public PlayerRuntimeData playerData;

    // スキルを購入したときに通知（UIの更新用）
    public event Action<SkillData> OnSkillBought;

    // ========================================
    // 状態の取得（UI用に追加）
    // ========================================

    public int GetLevel(SkillData skill)
    {
        if (skill == null || playerData == null)
            return 0;

        return playerData.GetSkillLevel(skill.skillId);
    }

    public bool IsMaxed(SkillData skill)
    {
        return skill != null && GetLevel(skill) >= skill.maxLevel;
    }

    // 前提スキルが習得済み（Lv1以上）なら解放
    public bool IsUnlocked(SkillData skill)
    {
        if (skill == null)
            return false;

        if (skill.prerequisite == null)
            return true;

        return GetLevel(skill.prerequisite) > 0;
    }

    // 次のレベルに上げるコスト（最大レベルなら0）
    public long GetNextCost(SkillData skill)
    {
        if (skill == null || IsMaxed(skill))
            return 0;

        return skill.GetCost(GetLevel(skill));
    }

    public SkillBuyState GetBuyState(SkillData skill)
    {
        if (!IsUnlocked(skill)) return SkillBuyState.Locked;
        if (IsMaxed(skill)) return SkillBuyState.Maxed;
        if (playerData.money < GetNextCost(skill)) return SkillBuyState.NotEnoughMoney;
        return SkillBuyState.Ok;
    }

    // ========================================
    // 購入
    // ========================================

    public bool CanBuy(SkillData skill)
    {
        if (skill == null || playerData == null)
            return false;

        int level = playerData.GetSkillLevel(skill.skillId);

        if (level >= skill.maxLevel)
            return false;

        if (skill.prerequisite != null)
        {
            int requiredLevel = playerData.GetSkillLevel(
                skill.prerequisite.skillId
            );

            if (requiredLevel <= 0)
                return false;
        }

        return true;
    }

    public bool BuySkill(SkillData skill)
    {
        if (!CanBuy(skill))
            return false;

        int level = playerData.GetSkillLevel(skill.skillId);
        long cost = skill.GetCost(level);

        if (playerData.money < cost)
            return false;

        playerData.money -= cost;
        playerData.SetSkillLevel(skill.skillId, level + 1);

        OnSkillBought?.Invoke(skill);

        return true;
    }

    // ========================================
    // 効果倍率
    // ========================================

    public float GetMultiplier(SkillType type)
    {
        float multiplier = 1f;

        if (skillTree == null || playerData == null)
            return multiplier;

        foreach (SkillData skill in skillTree.skills)
        {
            if (skill == null || skill.skillType != type)
                continue;

            int level = playerData.GetSkillLevel(skill.skillId);

            multiplier += skill.valuePerLevel * level;
        }

        return multiplier;
    }
}