using System.Collections.Generic;
using SkillEditor.Data;
using SkillEditor.Runtime;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public AbilitySystemComponent ownerASC;//技能系统组件

    [Header("单位配置")]
    public int id;//单位ID

    protected virtual void Awake()
    {
        ownerASC = new AbilitySystemComponent(this.gameObject);//创建技能系统组件

        UnitManager.Instance.Register(this);//注册到单位管理器

        InitFromTable();//初始化单位
    }

    protected virtual void OnDestroy()
    {
        UnitManager.Instance.Unregister(this);//注销到单位管理器
    }

    private void InitFromTable()
    {
        var data = LubanManager.Instance.Tables.TbUnit.GetOrDefault(id);//获取单位数据
        if (data == null)
        {
            Debug.LogWarning($"[Unit] TbUnit中找不到ID: {id}");
            return;
        }

        InitAttributes(data.InitialAttribute);//初始化属性
        GrantSkills(data.ActiveSkill);//授予主动技能
        GrantSkills(data.PassiveSkill);//授予被动技能
    }

    private void InitAttributes((int, int)[] attributes)
    {
        if (attributes == null) return;

        foreach (var (typeId, value) in attributes)//遍历属性
        {
            var attrType = (AttrType)typeId;//获取属性类型
            if (!ownerASC.Attributes.HasAttribute(attrType))//如果属性不存在，则添加属性    
                ownerASC.Attributes.AddAttribute(attrType, value);
        }
    }

    private void GrantSkills(int[] skillIds)
    {
        if (skillIds == null) return;

        var tbSkill = LubanManager.Instance.Tables.TbSkill;
        foreach (var skillId in skillIds)//遍历技能ID
        {
            var skillData = tbSkill.GetOrDefault(skillId);//获取技能数据
            if (skillData == null)
            {
                Debug.LogWarning($"[Unit] 技能表中找不到ID: {skillId}");
                continue;
            }

            var graphData = Resources.Load<SkillGraphData>(skillData.SkillGraphDataPath);//加载技能图表数据
            if (graphData == null)
            {
                Debug.LogWarning($"[Unit] 无法加载SkillGraphData: {skillData.SkillGraphDataPath}");
                continue;
            }

            ownerASC.GrantAbility(graphData, skillId);//授予技能
        }
    }
}
