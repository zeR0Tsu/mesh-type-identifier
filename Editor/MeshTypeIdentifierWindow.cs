using System.Text;
using UnityEditor;
using UnityEngine;

namespace zeR0Tsu.Editor
{
    /// <summary>
    /// 网格类型识别插件设置窗口
    /// </summary>
    public class MeshTypeIdentifierWindow : EditorWindow
    {
        private static bool _meshLabelEnabled;
        private static bool _skinnedLabelEnabled;
        private static float _hierarchyMarginCache;

        [MenuItem("zeR0Tsu/网格识别设置")]
        private static void Open()
        {
            var window = GetWindow<MeshTypeIdentifierWindow>("网格识别设置");
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        private void OnEnable()
        {
            _meshLabelEnabled = MeshTypeIdentifierConfig.MeshLabelEnabled;
            _skinnedLabelEnabled = MeshTypeIdentifierConfig.SkinnedLabelEnabled;
            _hierarchyMarginCache = MeshTypeIdentifierConfig.HierarchyRightMargin;
        }

        private void OnGUI()
        {
            DrawHierarchySection();
            EditorGUILayout.Space(10);
            DrawDebugSection();
        }

        // ──────────────────────────────────────────────
        //  Hierarchy 标签设置
        // ──────────────────────────────────────────────

        private void DrawHierarchySection()
        {
            EditorGUILayout.LabelField("Hierarchy 标签", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _meshLabelEnabled = EditorGUILayout.ToggleLeft("[M] 普通网格 (MeshRenderer)", _meshLabelEnabled);
            if (EditorGUI.EndChangeCheck())
            {
                MeshTypeIdentifierConfig.MeshLabelEnabled = _meshLabelEnabled;
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUI.BeginChangeCheck();
            _skinnedLabelEnabled = EditorGUILayout.ToggleLeft("[S] 蒙皮网格 (SkinnedMeshRenderer)", _skinnedLabelEnabled);
            if (EditorGUI.EndChangeCheck())
            {
                MeshTypeIdentifierConfig.SkinnedLabelEnabled = _skinnedLabelEnabled;
                EditorApplication.RepaintHierarchyWindow();
            }

            if (_meshLabelEnabled || _skinnedLabelEnabled)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("右边距", GUILayout.Width(60));

                EditorGUI.BeginChangeCheck();
                _hierarchyMarginCache = EditorGUILayout.Slider(_hierarchyMarginCache, 0f, 100f);
                if (EditorGUI.EndChangeCheck())
                {
                    MeshTypeIdentifierConfig.HierarchyRightMargin = _hierarchyMarginCache;
                    EditorApplication.RepaintHierarchyWindow();
                }

                EditorGUILayout.LabelField($"{_hierarchyMarginCache:F0}px", GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();
            }
        }

        // ──────────────────────────────────────────────
        //  调试工具
        // ──────────────────────────────────────────────

        private Vector2 _debugScroll;
        private string _debugOutput = "";
        private bool _debugFoldout = true;

        private void DrawDebugSection()
        {
            _debugFoldout = EditorGUILayout.Foldout(_debugFoldout, "调试工具", true);

            if (!_debugFoldout) return;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("输出选中网格的权重分布", GUILayout.Height(24)))
            {
                RunWeightDebug();
            }
            if (GUILayout.Button("清空输出", GUILayout.Height(24)))
            {
                _debugOutput = "";
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_debugOutput))
            {
                _debugScroll = EditorGUILayout.BeginScrollView(_debugScroll, GUILayout.Height(200));
                EditorGUILayout.TextArea(_debugOutput, GUI.skin.textArea);
                EditorGUILayout.EndScrollView();
            }
        }

        private void RunWeightDebug()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                _debugOutput = "请先在 Hierarchy 中选中要分析的网格物体";
                return;
            }

            var sb = new StringBuilder();

            foreach (var go in selected)
            {
                if (!go.TryGetComponent<SkinnedMeshRenderer>(out var smr))
                {
                    sb.AppendLine($"{go.name} 没有 SkinnedMeshRenderer，跳过");
                    continue;
                }
                AppendWeightReport(sb, smr);
            }

            _debugOutput = sb.ToString();
        }

        private void AppendWeightReport(StringBuilder sb, SkinnedMeshRenderer smr)
        {
            var go = smr.gameObject;
            var mesh = smr.sharedMesh;
            if (mesh == null) { sb.AppendLine($"{go.name}: mesh 为空"); return; }

            var boneWeights = mesh.boneWeights;
            if (boneWeights == null || boneWeights.Length == 0)
            { sb.AppendLine($"{go.name}: 没有 BoneWeight 数据"); return; }

            var bones = smr.bones;
            float[] totalWeight = new float[bones.Length];
            int[] vertexCount = new int[bones.Length];

            foreach (var bw in boneWeights)
            {
                if (bw.weight0 > 0) { totalWeight[bw.boneIndex0] += bw.weight0; vertexCount[bw.boneIndex0]++; }
                if (bw.weight1 > 0) { totalWeight[bw.boneIndex1] += bw.weight1; vertexCount[bw.boneIndex1]++; }
                if (bw.weight2 > 0) { totalWeight[bw.boneIndex2] += bw.weight2; vertexCount[bw.boneIndex2]++; }
                if (bw.weight3 > 0) { totalWeight[bw.boneIndex3] += bw.weight3; vertexCount[bw.boneIndex3]++; }
            }

            var list = new System.Collections.Generic.List<(int idx, float weight, int verts, int depth, string name)>();
            float grandTotal = 0;

            for (int i = 0; i < bones.Length; i++)
            {
                if (totalWeight[i] <= 0) continue;
                var bone = bones[i];
                if (bone == null) continue;
                grandTotal += totalWeight[i];
                int depth = GetDepth(bone);
                list.Add((i, totalWeight[i], vertexCount[i], depth, bone.name));
            }

            list.Sort((a, b) => a.depth.CompareTo(b.depth));

            sb.AppendLine($"===== {go.name} =====");
            sb.AppendLine($"顶点: {(int)grandTotal}, 影响骨骼: {list.Count}/{bones.Length}, RootBone: {smr.rootBone?.name}");
            sb.AppendLine($"{"深度",-5} {"骨骼名",-30} {"权重",-8} {"占比",-8} {"评分(depth^2)",-14}");
            sb.AppendLine(new string('-', 70));

            bool excludeRoot = (float)list.Count / bones.Length < 0.1f;
            int rootIdx = -1;
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] == smr.rootBone) { rootIdx = i; break; }

            float bestScore = -1;
            string bestBone = smr.rootBone?.name ?? "null";

            foreach (var s in list)
            {
                float ratio = s.weight / grandTotal;
                float score = ratio / (s.depth * s.depth);

                bool excluded = excludeRoot && s.idx == rootIdx;
                string marker = excluded ? " [排除]" : "";

                sb.AppendLine($"depth={s.depth,-2}  {s.name,-30} {s.weight,7:F1}  {ratio,6:P1}  {score,10:F6}{marker}");

                if (!excluded && score > bestScore)
                {
                    bestScore = score;
                    bestBone = s.name;
                }
            }

            sb.AppendLine($"\n策略: {(excludeRoot ? "独立部位(排除根)" : "全身网格(保留根)")}  选中: {bestBone}  评分: {bestScore:F6}");
            sb.AppendLine("========================\n");
        }

        private static int GetDepth(Transform t)
        {
            int d = 0;
            while (t.parent != null) { d++; t = t.parent; }
            return d;
        }
    }
}
