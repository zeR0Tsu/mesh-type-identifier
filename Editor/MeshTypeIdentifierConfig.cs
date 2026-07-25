using UnityEditor;

namespace zeR0Tsu.Editor
{
    /// <summary>
    /// 网格类型识别插件配置
    /// 使用 EditorPrefs 持久化存储设置。
    /// </summary>
    public static class MeshTypeIdentifierConfig
    {
        private const string Prefix = "zeR0Tsu_MeshTypeID_";

        // ── Hierarchy 标签启用开关 ──────────────────
        private const string MeshLabelKey = Prefix + "MeshLabelEnabled";
        private const bool MeshLabelDefault = true;

        public static bool MeshLabelEnabled
        {
            get => EditorPrefs.GetBool(MeshLabelKey, MeshLabelDefault);
            set => EditorPrefs.SetBool(MeshLabelKey, value);
        }

        private const string SkinnedLabelKey = Prefix + "SkinnedLabelEnabled";
        private const bool SkinnedLabelDefault = true;

        public static bool SkinnedLabelEnabled
        {
            get => EditorPrefs.GetBool(SkinnedLabelKey, SkinnedLabelDefault);
            set => EditorPrefs.SetBool(SkinnedLabelKey, value);
        }

        // ── Hierarchy 标签右边距 ────────────────────
        // 值越大标签越靠左（距离 Hierarchy 右边缘的像素数）
        private const string RightMarginKey = Prefix + "HierarchyRightMargin";
        private const float RightMarginDefault = 4f;
        private const float RightMarginMin = 0f;
        private const float RightMarginMax = 100f;

        public static float HierarchyRightMargin
        {
            get => EditorPrefs.GetFloat(RightMarginKey, RightMarginDefault);
            set => EditorPrefs.SetFloat(RightMarginKey,
                UnityEngine.Mathf.Clamp(value, RightMarginMin, RightMarginMax));
        }
    }
}
