using UnityEngine;
/// <summary>
/// スキルの購入・解放条件・効果倍率を管理します
/// </summary>
public class SkillManager : MonoBehaviour
{
    public SkillTreeData skillTree;
    public PlayerRuntimeData playerData;

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

        return true;
    }

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