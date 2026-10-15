using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// •¡”‚ÌƒXƒLƒ‹‚ğ‚Ü‚Æ‚ß‚ÄŠÇ—‚·‚é
/// </summary>
[CreateAssetMenu(
    fileName = "SkillTree",
    menuName = "BikeJump/Skill Tree")]
public class SkillTreeData : ScriptableObject
{
    public List<SkillData> skills = new List<SkillData>();
}