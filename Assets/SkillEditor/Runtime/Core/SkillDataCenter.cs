using System.Collections.Generic;
using SkillEditor.Data;
using UnityEngine;

namespace SkillEditor.Runtime
{
    /// <summary>
    /// 技能静态数据中心 - 集中管理所有技能图表数据
    /// 提供节点查询和连接关系查询功能
    /// </summary>
    public class SkillDataCenter
    {
        /// <summary>
        /// 单例实例
        /// </summary>
        private static SkillDataCenter _instance;
        public static SkillDataCenter Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SkillDataCenter();
                return _instance;
            }
        }

        /// <summary>
        /// 已注册的技能图表数据 (graphDataName -> SkillGraphData)
        /// </summary>
        private readonly Dictionary<string, SkillGraphData> _skillGraphs = new Dictionary<string, SkillGraphData>();

        /// <summary>
        /// 节点数据缓存 (graphDataName:nodeGuid -> NodeData)
        /// </summary>
        private readonly Dictionary<string, NodeData> _nodeCache = new Dictionary<string, NodeData>();

        /// <summary>
        /// 连接关系缓存 (graphDataName:nodeGuid:portName -> List<ConnectionData>)
        /// </summary>
        private readonly Dictionary<string, List<ConnectionData>> _connectionCache = new Dictionary<string, List<ConnectionData>>();

        /// <summary>
        /// Ability节点缓存 (graphDataName -> AbilityNodeData)
        /// </summary>
        private readonly Dictionary<string, AbilityNodeData> _abilityNodeCache = new Dictionary<string, AbilityNodeData>();

        // ============ 注册/注销 ============

        /// <summary>
        /// 注册技能图表数据
        /// </summary>
        public void RegisterSkillGraph(SkillGraphData graphData)
        {
            if (graphData == null)
                return;

            // 使用ScriptableObject的name作为graphDataName
            string graphDataName = graphData.name;
            if (string.IsNullOrEmpty(graphDataName))
                return;

            if (_skillGraphs.ContainsKey(graphDataName))
                return;

            _skillGraphs[graphDataName] = graphData;
            BuildCache(graphData, graphDataName);
        }

        /// <summary>
        /// 注销技能图表数据
        /// </summary>
        public void UnregisterSkillGraph(string graphDataName)
        {
            if (string.IsNullOrEmpty(graphDataName))
                return;

            if (_skillGraphs.Remove(graphDataName))
            {
                ClearCache(graphDataName);
            }
        }

        /// <summary>
        /// 构建缓存
        /// </summary>
        private void BuildCache(SkillGraphData graphData, string graphDataName)
        {
            // 构建节点缓存
            if (graphData.nodes != null)
            {
                foreach (var node in graphData.nodes)
                {
                    if (node == null || string.IsNullOrEmpty(node.guid))
                        continue;

                    string nodeKey = GetNodeKey(graphDataName, node.guid);
                    _nodeCache[nodeKey] = node;

                    // 缓存Ability节点
                    if (node is AbilityNodeData abilityNode)
                    {
                        _abilityNodeCache[graphDataName] = abilityNode;
                    }
                }
            }

            // 构建连接缓存
            if (graphData.connections != null)
            {
                foreach (var connection in graphData.connections)
                {
                    if (connection == null)
                        continue;

                    string connectionKey = GetConnectionKey(graphDataName, connection.outputNodeGuid, connection.outputPortName);

                    if (!_connectionCache.TryGetValue(connectionKey, out var connections))
                    {
                        connections = new List<ConnectionData>();
                        _connectionCache[connectionKey] = connections;
                    }

                    connections.Add(connection);
                }
            }
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        private void ClearCache(string graphDataName)
        {
            // 清除节点缓存
            var nodeKeysToRemove = new List<string>();
            foreach (var key in _nodeCache.Keys)
            {
                if (key.StartsWith(graphDataName + ":"))
                    nodeKeysToRemove.Add(key);
            }
            foreach (var key in nodeKeysToRemove)
            {
                _nodeCache.Remove(key);
            }

            // 清除连接缓存
            var connectionKeysToRemove = new List<string>();
            foreach (var key in _connectionCache.Keys)
            {
                if (key.StartsWith(graphDataName + ":"))
                    connectionKeysToRemove.Add(key);
            }
            foreach (var key in connectionKeysToRemove)
            {
                _connectionCache.Remove(key);
            }

            // 清除Ability节点缓存
            _abilityNodeCache.Remove(graphDataName);
        }

        // ============ 查询方法 ============

        /// <summary>
        /// 获取技能图表数据
        /// </summary>
        public SkillGraphData GetSkillGraph(string graphDataName)
        {
            return _skillGraphs.TryGetValue(graphDataName, out var graph) ? graph : null;
        }

        /// <summary>
        /// 获取节点数据
        /// </summary>
        public NodeData GetNodeData(string graphDataName, string nodeGuid)
        {
            string key = GetNodeKey(graphDataName, nodeGuid);
            return _nodeCache.TryGetValue(key, out var node) ? node : null;
        }

        /// <summary>
        /// 获取节点数据（泛型版本）
        /// </summary>
        public T GetNodeData<T>(string graphDataName, string nodeGuid) where T : NodeData
        {
            return GetNodeData(graphDataName, nodeGuid) as T;
        }

        /// <summary>
        /// 获取Ability节点数据
        /// </summary>
        public AbilityNodeData GetAbilityNodeData(string graphDataName)
        {
            return _abilityNodeCache.TryGetValue(graphDataName, out var node) ? node : null;
        }

        /// <summary>
        /// 获取指定端口连接的所有节点数据
        /// </summary>
        public List<NodeData> GetConnectedNodes(string graphDataName, string nodeGuid, string outputPortName)
        {
            var result = new List<NodeData>();
            string connectionKey = GetConnectionKey(graphDataName, nodeGuid, outputPortName);
            
            if (!_connectionCache.TryGetValue(connectionKey, out var connections))
                return result;

            foreach (var connection in connections)
            {
                var nodeData = GetNodeData(graphDataName, connection.inputNodeGuid);
                if (nodeData != null)
                {
                    result.Add(nodeData);
                }
            }

            return result;
        }

        /// <summary>
        /// 获取指定端口的连接数据
        /// </summary>
        public List<ConnectionData> GetConnections(string graphDataName, string nodeGuid, string outputPortName)
        {
            string connectionKey = GetConnectionKey(graphDataName, nodeGuid, outputPortName);
            return _connectionCache.TryGetValue(connectionKey, out var connections)
                ? new List<ConnectionData>(connections)
                : new List<ConnectionData>();
        }

        /// <summary>
        /// 检查是否有指定端口的连接
        /// </summary>
        public bool HasConnection(string graphDataName, string nodeGuid, string outputPortName)
        {
            string connectionKey = GetConnectionKey(graphDataName, nodeGuid, outputPortName);
            return _connectionCache.TryGetValue(connectionKey, out var connections) && connections.Count > 0;
        }

        // ============ 辅助方法 ============

        private string GetNodeKey(string graphDataName, string nodeGuid)
        {
            return $"{graphDataName}:{nodeGuid}";
        }

        private string GetConnectionKey(string graphDataName, string nodeGuid, string portName)
        {
            return $"{graphDataName}:{nodeGuid}:{portName ?? "output"}";
        }

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear()
        {
            _skillGraphs.Clear();
            _nodeCache.Clear();
            _connectionCache.Clear();
            _abilityNodeCache.Clear();
        }

        /// <summary>
        /// 获取已注册的技能数量
        /// </summary>
        public int RegisteredCount => _skillGraphs.Count;
    }
}
