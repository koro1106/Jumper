using UnityEngine;
/// <summary>
/// スキル1つ分の設定を定義
/// </summary>
public enum SkillType
{
    JumpPower,
    JumpDistance,
    MoneyReward,
    SubscriberReward
}

[CreateAssetMenu(
    fileName = "Skill",
    menuName = "BikeJump/Skill"
)]
public class SkillData : ScriptableObject
{
    [Header("基本情報")]
    public string skillId;
    public string skillName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("スキル効果")]
    public SkillType skillType;

    [Tooltip("1レベルごとの加算倍率")]
    public float valuePerLevel = 0.15f;

    [Header("購入価格")]
    public long baseCost = 100;

    [Tooltip("レベルごとの価格上昇率")]
    public float costGrowth = 1.6f;

    [Header("スキルツリー")]
    public SkillData prerequisite;
    public int maxLevel = 10;

    public long GetCost(int currentLevel)
    {
        double cost = baseCost *
            System.Math.Pow(costGrowth, currentLevel);

        return cost >= long.MaxValue
            ? long.MaxValue
            : System.Math.Max(1L, (long)cost);
    }
}
