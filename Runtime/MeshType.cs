namespace zeR0Tsu
{
    /// <summary>
    /// 网格类型枚举
    /// 区分普通网格 (Mesh) 与蒙皮网格 (SkinnedMesh)
    /// </summary>
    public enum MeshType
    {
        /// <summary>普通网格，由 MeshFilter + MeshRenderer 渲染</summary>
        Mesh,

        /// <summary>蒙皮网格，由 SkinnedMeshRenderer 渲染</summary>
        SkinnedMesh
    }
}
