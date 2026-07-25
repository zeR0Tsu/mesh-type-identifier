using System;
using UnityEngine;

namespace zeR0Tsu
{
    /// <summary>
    /// 核心网格信息数据对象
    /// 封装单一网格的所有相关信息，供后续功能统一使用。
    /// 
    /// 普通网格：
    ///   bones = null
    ///   rootBone = null
    /// 
    /// 蒙皮网格：
    ///   bones != null
    ///   rootBone != null
    /// </summary>
    [Serializable]
    public class MeshInfo : IMeshInfo
    {
        [SerializeField] private GameObject _gameObject;
        [SerializeField] private Mesh _mesh;
        [SerializeField] private MeshType _meshType;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Transform[] _bones;
        [SerializeField] private Transform _rootBone;

        // ──────────────────────────────────────────────
        //  IMeshInfo 接口实现
        // ──────────────────────────────────────────────

        public Mesh Mesh => _mesh;
        public MeshType Type => _meshType;
        public GameObject Owner => _gameObject;

        // ──────────────────────────────────────────────
        //  属性
        // ──────────────────────────────────────────────

        public GameObject gameObject => _gameObject;
        public Mesh mesh => _mesh;
        public MeshType meshType => _meshType;
        public Renderer renderer => _renderer;
        public Transform[] bones => _bones;
        public Transform rootBone => _rootBone;

        // ──────────────────────────────────────────────
        //  统计信息（方便后续统计、批量处理及调试）
        // ──────────────────────────────────────────────

        /// <summary>顶点数</summary>
        public int VertexCount => _mesh != null ? _mesh.vertexCount : 0;

        /// <summary>三角面数</summary>
        public int TriangleCount => _mesh != null ? _mesh.triangles.Length / 3 : 0;

        /// <summary>SubMesh 数量</summary>
        public int SubMeshCount => _mesh != null ? _mesh.subMeshCount : 0;

        /// <summary>材质数量</summary>
        public int MaterialCount => _renderer != null ? _renderer.sharedMaterials.Length : 0;

        /// <summary>骨骼数量（仅蒙皮网格）</summary>
        public int BoneCount => _bones != null ? _bones.Length : 0;

        /// <summary>BlendShape 数量（仅蒙皮网格）</summary>
        public int BlendShapeCount
        {
            get
            {
                if (_mesh == null) return 0;
                int count = 0;
                for (int i = 0; i < _mesh.blendShapeCount; i++)
                    count++;
                return count;
            }
        }

        // ──────────────────────────────────────────────
        //  构造
        // ──────────────────────────────────────────────

        public MeshInfo() { }

        public MeshInfo(GameObject gameObject, Mesh mesh, MeshType meshType, Renderer renderer,
                        Transform[] bones = null, Transform rootBone = null)
        {
            _gameObject = gameObject;
            _mesh = mesh;
            _meshType = meshType;
            _renderer = renderer;
            _bones = bones;
            _rootBone = rootBone;
        }

        // ──────────────────────────────────────────────
        //  工厂方法：从 MeshRenderer 创建
        // ──────────────────────────────────────────────

        public static MeshInfo FromMeshRenderer(MeshRenderer renderer)
        {
            if (renderer == null) return null;

            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return null;

            return new MeshInfo
            {
                _gameObject = renderer.gameObject,
                _mesh = filter.sharedMesh,
                _meshType = MeshType.Mesh,
                _renderer = renderer,
                _bones = null,
                _rootBone = null
            };
        }

        // ──────────────────────────────────────────────
        //  工厂方法：从 SkinnedMeshRenderer 创建
        // ──────────────────────────────────────────────

        public static MeshInfo FromSkinnedMeshRenderer(SkinnedMeshRenderer renderer)
        {
            if (renderer == null || renderer.sharedMesh == null) return null;

            return new MeshInfo
            {
                _gameObject = renderer.gameObject,
                _mesh = renderer.sharedMesh,
                _meshType = MeshType.SkinnedMesh,
                _renderer = renderer,
                _bones = renderer.bones,
                _rootBone = renderer.rootBone
            };
        }

        // ──────────────────────────────────────────────
        //  辅助
        // ──────────────────────────────────────────────

        public override string ToString()
        {
            var info = $"[{_meshType}] {_gameObject?.name}\n"
                     + $"  Mesh: {(_mesh != null ? _mesh.name : "null")}\n"
                     + $"  Renderer: {(_renderer != null ? _renderer.GetType().Name : "null")}\n"
                     + $"  Vertices: {VertexCount}, Triangles: {TriangleCount}\n"
                     + $"  SubMeshes: {SubMeshCount}, Materials: {MaterialCount}";

            if (_meshType == MeshType.SkinnedMesh)
            {
                info += $"\n  Bones: {BoneCount}, BlendShapes: {BlendShapeCount}";
                if (_rootBone != null)
                    info += $"\n  RootBone: {_rootBone.name}";
            }

            return info;
        }
    }
}
