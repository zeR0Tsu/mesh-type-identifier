using UnityEngine;

namespace zeR0Tsu
{
    /// <summary>
    /// 统一网格信息接口
    /// 屏蔽 MeshRenderer 与 SkinnedMeshRenderer 的差异，
    /// 为后续功能提供一致的访问方式。
    /// </summary>
    public interface IMeshInfo
    {
        /// <summary>网格数据 (UnityEngine.Mesh)</summary>
        Mesh Mesh { get; }

        /// <summary>网格类型 (Mesh / SkinnedMesh)</summary>
        MeshType Type { get; }

        /// <summary>所属 GameObject</summary>
        GameObject Owner { get; }
    }
}
