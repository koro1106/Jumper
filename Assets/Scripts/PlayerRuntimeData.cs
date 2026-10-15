using System.Collections.Generic;
/// <summary>
/// プレイ中に変化するプレイヤーデータ
/// </summary>
[System.Serializable]
public class PlayerRuntimeData
{
    public int day;
    public long subscribers;
    public long money;
    public int bicycleCount;
    public long antiCount;

    // skillIdごとの現在のレベル
    public Dictionary<string, int> skillLevels =
        new Dictionary<string, int>();

    public void Initialize(GameConfig config)
    {
        day = 1;
        subscribers = config.initialSubscribers;
        money = config.initialMoney;
        bicycleCount = config.initialBicycles;
        antiCount = 0;

        skillLevels.Clear();
    }

    public int GetSkillLevel(string skillId)
    {
        if (skillLevels.TryGetValue(skillId, out int level))
            return level;

        return 0;
    }

    public void SetSkillLevel(string skillId, int level)
    {
        skillLevels[skillId] = level;
    }
}