using System.Collections.Generic;
using UnityEngine;

namespace SkillEditor.Data
{
    [CreateAssetMenu(fileName = "SkillGraph", menuName = "SkillEditor/SkillGraph")]
    public class SkillGraphData : ScriptableObject
    {
        //[SerializeReference] + ScriptableObject是目前 Unity 官方支持“父类保存子类数据”的唯一稳定方案。所以这里使用ScriptableObject来保存NodeData。
        [SerializeReference]
        public List<NodeData> nodes = new List<NodeData>();
        public List<ConnectionData> connections = new List<ConnectionData>();
    }
}
