using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace zeR0Tsu.Editor
{
    /// <summary>
    /// 在 Hierarchy 层级列表中为网格物体显示类型标识。
    ///
    /// [M] 绿色 - 普通网格 (MeshRenderer)
    /// [S] 蓝色 - 蒙皮网格 (SkinnedMeshRenderer)，点击可聚焦到骨骼
    ///
    /// 性能说明：hierarchyWindowItemOnGUI 会对每个可见物体、每一帧重绘都回调一次。
    /// 为避免场景展开后卡顿，这里把所有识别结果按 GameObject 缓存：
    ///   - 每项每帧退化为 O(1) 的字典查找 + 一次按钮绘制，不再反复做组件查询；
    ///   - 骨骼 BoneWeight 分析只在鼠标悬停到 [S] 标签时才懒计算一次并缓存；
    ///   - 所有 onGUI 事件都处理，确保 [S] 按钮能收到点击（点击/悬停均基于缓存，开销为 O(1)）；
    ///   - 场景新增物体 走缓存未命中逻辑，只会被单独扫描；
    ///   - 组件增删时只使对应物体的缓存失效（避免漏扫/误扫其他物体）。
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyMeshLabel
    {
        private static readonly Color MeshColor = new Color(0.3f, 0.8f, 0.3f);
        private static readonly Color SkinnedMeshColor = new Color(0.3f, 0.6f, 1.0f);

        private static GUIStyle _labelStyle;
        private static Texture2D _hoverTex;
        private static Texture2D _activeTex;

        // 按实例ID缓存识别结果，避免每个可见物体每帧做组件查询 / 骨骼扫描。
        private static readonly Dictionary<int, CacheItem> _cache = new Dictionary<int, CacheItem>();

        private sealed class CacheItem
        {
            public bool IsSkinned;
            public bool IsMesh;
            public string Tooltip; // null 表示尚未在悬停时算过；算过后缓存为具体文案
        }

        static HierarchyMeshLabel()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            // 结构变动（增删物体、改名、父子关系）时清理指向已销毁物体的缓存项。
            // 新增物体本身走缓存未命中逻辑，只会被单独扫描，不会触发全量重扫。
            EditorApplication.hierarchyChanged += PruneDeadEntries;

#if UNITY_2021_2_OR_NEWER
            // 组件增加时只使对应物体缓存失效，让新增/改动的物体被单独扫描
            //（该版本引擎没有 componentWasRemoved 事件，组件删除由 PruneDeadEntries 兜底）
            ObjectFactory.componentWasAdded += OnComponentAdded;
#endif
        }

        private static void OnComponentAdded(Component component)
        {
            if (component == null) return;
            _cache.Remove(component.gameObject.GetInstanceID());
        }

        private static void PruneDeadEntries()
        {
            if (_cache.Count == 0) return;

            List<int> dead = null;
            foreach (var kvp in _cache)
            {
                if (EditorUtility.InstanceIDToObject(kvp.Key) == null)
                    (dead = dead ?? new List<int>()).Add(kvp.Key);
            }

            if (dead != null)
                for (int i = 0; i < dead.Count; i++)
                    _cache.Remove(dead[i]);
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            // 注意：不能只处理 Repaint。GUI.Button 的点击是在 MouseUp 等事件里回调的，
            // 若早期 return 掉非 Repaint 事件，按钮就永远收不到点击、无法聚焦骨骼。
            // 识别结果已缓存，各事件下每项开销都是 O(1)。
            if (Event.current == null) return;
            if (!MeshTypeIdentifierConfig.SkinnedLabelEnabled && !MeshTypeIdentifierConfig.MeshLabelEnabled) return;

            InitStyles();

            if (!_cache.TryGetValue(instanceID, out var item))
            {
                var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                if (go == null) return; // 该实例不存在，不做任何处理

                item = new CacheItem();
                if (go.TryGetComponent<SkinnedMeshRenderer>(out _))
                {
                    item.IsSkinned = true;
                }
                else if (go.TryGetComponent<MeshRenderer>(out _) && go.TryGetComponent<MeshFilter>(out _))
                {
                    item.IsMesh = true;
                }

                _cache[instanceID] = item;
            }

            DrawLabel(instanceID, selectionRect, item);
        }

        private static void InitStyles()
        {
            if (_labelStyle != null) return;

            _labelStyle = new GUIStyle
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                padding = new RectOffset(0, 2, 0, 0)
            };

            // 悬浮背景：半透蓝色
            _hoverTex = new Texture2D(1, 1);
            _hoverTex.SetPixel(0, 0, new Color(0.3f, 0.6f, 1.0f, 0.2f));
            _hoverTex.Apply();

            // 点击背景：稍强蓝色
            _activeTex = new Texture2D(1, 1);
            _activeTex.SetPixel(0, 0, new Color(0.3f, 0.6f, 1.0f, 0.35f));
            _activeTex.Apply();

            _labelStyle.hover.background = _hoverTex;
            _labelStyle.active.background = _activeTex;
        }

        private static void DrawLabel(int instanceID, Rect selectionRect, CacheItem item)
        {
            const float labelWidth = 22f;
            float rightMargin = MeshTypeIdentifierConfig.HierarchyRightMargin;
            var labelRect = new Rect(selectionRect.xMax - rightMargin - labelWidth,
                                     selectionRect.y, labelWidth, selectionRect.height);

            if (item.IsSkinned)
            {
                if (!MeshTypeIdentifierConfig.SkinnedLabelEnabled) return;

                _labelStyle.normal.textColor = SkinnedMeshColor;
                _labelStyle.hover.textColor = SkinnedMeshColor;
                _labelStyle.active.textColor = SkinnedMeshColor;

                string tooltip = item.Tooltip;
                if (tooltip == null)
                {
                    // 只在鼠标悬停到标签时才做骨骼分析并缓存，平时不扫 mesh
                    if (labelRect.Contains(Event.current.mousePosition))
                    {
                        tooltip = BuildBoneTooltip(instanceID);
                        item.Tooltip = tooltip; // 可能是 "未绑定骨骼"，同样缓存
                    }
                    else
                    {
                        tooltip = ""; // 未悬停：本次不计算，也不落缓存
                    }
                }

                var content = new GUIContent("[S]", tooltip);
                if (GUI.Button(labelRect, content, _labelStyle))
                    SelectNearestBone(instanceID);
            }
            else if (item.IsMesh)
            {
                if (!MeshTypeIdentifierConfig.MeshLabelEnabled) return;

                _labelStyle.normal.textColor = MeshColor;
                _labelStyle.hover.textColor = MeshColor;
                _labelStyle.active.textColor = MeshColor;

                GUI.Button(labelRect, new GUIContent("[M]"), _labelStyle);
            }
        }

        private static string BuildBoneTooltip(int instanceID)
        {
            var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null || !go.TryGetComponent<SkinnedMeshRenderer>(out var smr))
                return "未绑定骨骼";

            var nearest = FindNearestBone(go, smr);
            int boneCount = smr.bones != null ? smr.bones.Length : 0;
            int vertCount = smr.sharedMesh != null ? smr.sharedMesh.vertexCount : 0;

            if (nearest != null)
                return $"控制骨骼: {nearest.name}  (顶点: {vertCount}, 骨骼: {boneCount})\n点击聚焦到该骨骼";

            return "未绑定骨骼";
        }

        /// <summary>
        /// 从 SkinnedMeshRenderer 的 sharedMesh 中读取 BoneWeight 数据，
        /// 对所有有顶点权重的骨骼按「权重占比 / depth^2」综合评分。
        ///
        /// 如果该网格使用的骨骼数不足总骨骼的 10%（独立部位，如头、尾巴），
        /// 则排除根骨骼（移动它全身都动，没有意义），在部位内选最佳骨骼。
        /// 否则为全身网格，保留根骨骼参与评分。
        ///
        /// 评分 = 权重占比 / depth^2：权重越高越好，depth 越深惩罚越大。
        /// </summary>
        private static Transform FindNearestBone(GameObject go, SkinnedMeshRenderer smr)
        {
            if (smr.bones == null || smr.bones.Length == 0)
                return smr.rootBone;

            var mesh = smr.sharedMesh;
            if (mesh == null)
                return smr.rootBone;

            var boneWeights = mesh.boneWeights;
            if (boneWeights == null || boneWeights.Length == 0)
                return smr.rootBone;

            if (mesh.bindposes == null || mesh.bindposes.Length != smr.bones.Length)
                return smr.rootBone;

            // 统计每根骨骼的权重总和，收集有影响的骨骼索引
            float[] totalWeight = new float[smr.bones.Length];
            var usedIndices = new System.Collections.Generic.HashSet<int>();

            foreach (var bw in boneWeights)
            {
                if (bw.weight0 > 0) { totalWeight[bw.boneIndex0] += bw.weight0; usedIndices.Add(bw.boneIndex0); }
                if (bw.weight1 > 0) { totalWeight[bw.boneIndex1] += bw.weight1; usedIndices.Add(bw.boneIndex1); }
                if (bw.weight2 > 0) { totalWeight[bw.boneIndex2] += bw.weight2; usedIndices.Add(bw.boneIndex2); }
                if (bw.weight3 > 0) { totalWeight[bw.boneIndex3] += bw.weight3; usedIndices.Add(bw.boneIndex3); }
            }

            if (usedIndices.Count == 0)
                return smr.rootBone;

            float grandTotal = 0;
            foreach (var w in totalWeight) grandTotal += w;
            if (grandTotal <= 0)
                return smr.rootBone;

            // 定位根骨骼在 bones[] 中的索引
            int rootBoneIndex = -1;
            for (int i = 0; i < smr.bones.Length; i++)
            {
                if (smr.bones[i] == smr.rootBone)
                {
                    rootBoneIndex = i;
                    break;
                }
            }

            // ── 判断是否为独立部位 ────────────────────
            // 如果该网格使用的骨骼数不到总骨骼的 10%，说明它是一个独立部位
            //（如头、尾巴、独立配件），此时排除根骨骼，在部位内选最佳骨骼。
            // 如果使用了很多骨骼（如全身衣服 body_base），保留根骨骼。
            float usedRatio = (float)usedIndices.Count / smr.bones.Length;
            bool excludeRoot = usedRatio < 0.1f;

            // ── 评分 ──────────────────────────────────
            Transform bestBone = smr.rootBone;
            float bestScore = -1f;

            foreach (int idx in usedIndices)
            {
                if (idx < 0 || idx >= smr.bones.Length) continue;
                var bone = smr.bones[idx];
                if (bone == null) continue;

                // 独立部位模式下排除根骨骼
                if (excludeRoot && idx == rootBoneIndex) continue;

                float weightRatio = totalWeight[idx] / grandTotal;
                int depth = GetHierarchyDepth(bone);

                // 评分 = 权重占比 / depth^2
                float score = weightRatio / (depth * depth);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestBone = bone;
                }
            }

            // 回退：只有当真的一根骨骼都没评上分时（bestScore < 0），才取第一个非根骨骼
            if (bestScore < 0 && usedIndices.Count > 1)
            {
                foreach (int idx in usedIndices)
                {
                    if (idx >= 0 && idx < smr.bones.Length && smr.bones[idx] != null && idx != rootBoneIndex)
                    {
                        bestBone = smr.bones[idx];
                        break;
                    }
                }
            }

            return bestBone;
        }

        private static int GetHierarchyDepth(Transform t)
        {
            int depth = 0;
            while (t.parent != null)
            {
                depth++;
                t = t.parent;
            }
            return depth;
        }

        private static void SelectNearestBone(int instanceID)
        {
            var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null || !go.TryGetComponent<SkinnedMeshRenderer>(out var smr)) return;

            var bone = FindNearestBone(go, smr);
            if (bone != null)
            {
                Selection.activeGameObject = bone.gameObject;
                EditorGUIUtility.PingObject(bone.gameObject);
            }
        }
    }
}
