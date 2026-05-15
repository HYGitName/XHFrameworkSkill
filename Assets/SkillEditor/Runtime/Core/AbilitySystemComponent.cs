using System;
using SkillEditor.Data;

namespace SkillEditor.Runtime
{
    /// <summary>
    /// 技能系统组件 - GAS的核心实现
    /// 管理技能、效果、属性、标签的中枢组件
    /// </summary>
    public class AbilitySystemComponent
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// 属性容器
        /// </summary>
        public AttributeSetContainer Attributes { get; private set; }

        /// <summary>
        /// 标签容器
        /// </summary>
        public GameplayTagContainer OwnedTags { get; private set; }

        /// <summary>
        /// 技能容器
        /// </summary>
        public AbilityContainer Abilities { get; private set; }

        /// <summary>
        /// 效果容器（别名，方便访问）
        /// </summary>
        public GameplayEffectContainer EffectContainer{ get; private set; }

        /// <summary>
        /// 所属的GameObject
        /// </summary>
        public UnityEngine.GameObject Owner { get; private set; }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; }

        // ============ 事件 ============

        public event Action<GameplayAbilitySpec> OnAbilityActivated;
        public event Action<GameplayAbilitySpec, bool> OnAbilityEnded;
        public event Action<GameplayEffectSpec, AbilitySystemComponent> OnEffectApplied;
        public event Action<GameplayEffectSpec> OnEffectRemoved;
        public event Action<GameplayTag, bool> OnTagChanged;

        public event Action<GameplayEventType> OnTGameplayEvent;
        // ============ 构造函数 ============

        public AbilitySystemComponent(UnityEngine.GameObject owner = null)//构造函数
        {
            Id = Guid.NewGuid().ToString();//生成唯一ID
            Owner = owner;//设置所属对象

            // 初始化容器
            Attributes = new AttributeSetContainer();//初始化属性容器
            OwnedTags = new GameplayTagContainer();//初始化标签容器
            Abilities = new AbilityContainer(this);//初始化技能容器
            EffectContainer = new GameplayEffectContainer(this);//初始化效果容器

            // 订阅事件
            SubscribeEvents();//订阅事件

            IsInitialized = true;//设置为已初始化

            // 注册到GASHost
            GASHost.Instance.Register(this);//注册到GASHost
        }

        /// <summary>
        /// 订阅内部事件
        /// </summary>
        private void SubscribeEvents()
        {
            // 标签变化事件
            OwnedTags.OnTagsChanged += () =>
            {
                // 可以在这里处理标签变化的全局逻辑
            };

            Attributes.OnAnyAttributeChanged += (Attribute, before, after) =>//属性变化事件
            {
                if (Attribute.AttrType == AttrType.Health)
                {
                    if (after < before)
                    {
                        OnTGameplayEvent?.Invoke(GameplayEventType.OnTakeDamage);
                    }
                }
            };
        }

        // ============ 技能相关 ============

        /// <summary>
        /// 授予技能
        /// </summary>
        public GameplayAbilitySpec GrantAbility(SkillGraphData abilityData)
        {
            if (abilityData == null)
                return null;

            return Abilities.GrantAbility(abilityData);
        }

        /// <summary>
        /// 授予技能并设置技能ID
        /// </summary>
        public GameplayAbilitySpec GrantAbility(SkillGraphData abilityData, int skillId)
        {
            if (abilityData == null)
                return null;

            return Abilities.GrantAbility(abilityData, skillId);
        }

        /// <summary>
        /// 移除技能
        /// </summary>
        public bool RemoveAbility(GameplayAbilitySpec spec)
        {
            return Abilities.RemoveAbility(spec);
        }

        /// <summary>
        /// 尝试激活技能
        /// </summary>
        public bool TryActivateAbility(GameplayAbilitySpec spec, AbilitySystemComponent target = null)
        {
            if (spec == null)
                return false;

            bool success = Abilities.TryActivateAbility(spec, target);//尝试激活技能

            if (success)
            {
                OnAbilityActivated?.Invoke(spec);//触发技能激活事件

                // 订阅技能结束事件
                spec.OnEnded += HandleAbilityEnded;//订阅技能结束事件
            }

            return success;//返回是否成功
        }

        /// <summary>
        /// 取消技能
        /// </summary>
        public void CancelAbility(GameplayAbilitySpec spec)
        {
            Abilities.CancelAbility(spec);//取消技能
        }

        /// <summary>
        /// 结束技能
        /// </summary>
        public void EndAbility(GameplayAbilitySpec spec, bool wasCancelled = false)
        {
            Abilities.EndAbility(spec, wasCancelled);
        }

        /// <summary>
        /// 处理技能结束
        /// </summary>
        private void HandleAbilityEnded(GameplayAbilitySpec spec, bool wasCancelled)
        {
            spec.OnEnded -= HandleAbilityEnded;//取消订阅技能结束事件
            OnAbilityEnded?.Invoke(spec, wasCancelled);//订阅技能结束事件
        }

        // ============ 效果相关 ============

        /// <summary>
        /// 移除效果
        /// </summary>
        public bool RemoveActiveEffect(GameplayEffectSpec effectSpec)
        {
            return EffectContainer.RemoveEffect(effectSpec);
        }

        /// <summary>
        /// 移除带有指定标签的所有效果
        /// </summary>
        public int RemoveActiveEffectsWithTags(GameplayTagSet tags)
        {
            return EffectContainer.RemoveEffectsWithTags(tags);
        }

        // ============ 标签相关 ============

        /// <summary>
        /// 检查是否拥有标签
        /// </summary>
        public bool HasTag(GameplayTag tag)
        {
            return OwnedTags.HasTag(tag);
        }

        /// <summary>
        /// 检查是否拥有所有标签
        /// </summary>
        public bool HasAllTags(GameplayTagSet tags)
        {
            return OwnedTags.HasAllTags(tags);
        }

        /// <summary>
        /// 检查是否拥有任意标签
        /// </summary>
        public bool HasAnyTags(GameplayTagSet tags)
        {
            return OwnedTags.HasAnyTags(tags);
        }

        /// <summary>
        /// 检查是否不拥有任何指定标签
        /// </summary>
        public bool HasNoneTags(GameplayTagSet tags)
        {
            return OwnedTags.HasNoneTags(tags);
        }

        // ============ 更新 ============

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Tick(float deltaTime)
        {
            // 更新技能
            Abilities.Tick(deltaTime);

            // 更新效果
            EffectContainer.Tick(deltaTime);
        }

        // ============ 清理 ============

        /// <summary>
        /// 销毁ASC
        /// </summary>
        public void Destroy()
        {
            // 从GASHost注销
            GASHost.Instance.Unregister(this);

            // 清理所有技能和效果
            Abilities.Clear();
            EffectContainer.Clear();
            OwnedTags.Clear();

            IsInitialized = false;
        }
        
    }
}
