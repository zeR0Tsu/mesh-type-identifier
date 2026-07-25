using System.Collections.Generic;
using UnityEngine;

namespace zeR0Tsu
{
    /// <summary>
    /// 场景网格扫描器
    /// 遍历场景中所有 Renderer，识别网格类型并生成 MeshInfo 列表。
    /// 
    /// 扫描流程：
    ///   1. Object.FindObjectsByType&lt;Renderer&gt;() 统一获取所有 Renderer
    ///   2. 判断 Renderer 实际类型
    ///   3. 根据类型创建对应的 MeshInfo
    /// </summary>
    public static class MeshScanner
    {
        /// <summary>
        /// 扫描当前场景中所有网格，返回 MeshInfo 列表。
        /// </summary>
        public static List<MeshInfo> ScanAll()
        {
            var results = new List<MeshInfo>();

            // 使用 FindObjectsByType 统一获取所有 Renderer
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);

            foreach (var renderer in renderers)
            {
                // 优先判断 SkinnedMeshRenderer
                if (renderer is SkinnedMeshRenderer smr)
                {
                    var info = MeshInfo.FromSkinnedMeshRenderer(smr);
                    if (info != null)
                        results.Add(info);
                    continue;
                }

                // 再判断 MeshRenderer
                if (renderer is MeshRenderer mr)
                {
                    var info = MeshInfo.FromMeshRenderer(mr);
                    if (info != null)
                        results.Add(info);
                    continue;
                }

                // 其他 Renderer（如 ParticleSystemRenderer、LineRenderer 等）忽略
            }

            return results;
        }

        /// <summary>
        /// 扫描当前场景中所有网格，按类型分组。
        /// </summary>
        public static void ScanByType(out List<MeshInfo> meshes, out List<MeshInfo> skinnedMeshes)
        {
            var all = ScanAll();
            meshes = new List<MeshInfo>();
            skinnedMeshes = new List<MeshInfo>();

            foreach (var info in all)
            {
                if (info.meshType == MeshType.Mesh)
                    meshes.Add(info);
                else
                    skinnedMeshes.Add(info);
            }
        }

        /// <summary>
        /// 获取指定 GameObject 的网格信息。
        /// </summary>
        public static MeshInfo GetMeshInfo(GameObject go)
        {
            if (go == null) return null;

            // 优先判断 SkinnedMeshRenderer
            if (go.TryGetComponent<SkinnedMeshRenderer>(out var smr))
                return MeshInfo.FromSkinnedMeshRenderer(smr);

            // 再判断 MeshRenderer
            if (go.TryGetComponent<MeshRenderer>(out var mr))
                return MeshInfo.FromMeshRenderer(mr);

            return null;
        }
    }
}
