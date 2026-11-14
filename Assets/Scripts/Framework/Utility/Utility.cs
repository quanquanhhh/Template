using UnityEngine;

namespace MythMerge.LauncherEngine
{
    /// <summary>
    /// 实用函数集。
    /// </summary>
    public static partial class Utility
    {
        public static T Bind<T>(this Transform t, bool forceCreate = false) where T : Component
        {
            T component = null;

            component = t.GetComponent<T>();

            if (component == null && forceCreate)
            {
                component = t.gameObject.AddComponent<T>();
            }

            return component;
        }
        
        private const string DEFAULT_SHADER_NAME = "Sprites/Default";
        
        private static Material _spriteDefaultMat;
        public static void ReplaceDefaultShader(SpriteRenderer spRender)
        {
            if (_spriteDefaultMat == null)
            {
                _spriteDefaultMat = new Material(Shader.Find(DEFAULT_SHADER_NAME));
#if UNITY_EDITOR
                _spriteDefaultMat.name = "ReplacedMaterial";
#endif
            }
        
            if (spRender.sharedMaterial.shader.name == DEFAULT_SHADER_NAME)
            {
                spRender.sharedMaterial = _spriteDefaultMat;
            }
        }
        
        /// <summary>
        /// 获取指定子节点上的指定组件 特别当 path 为空的时候子节点即根结点<para>
        /// 如果子节点不存在 返回 null</para>
        /// 如果子节点不存在该组件 则添加该组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">根节点</param>
        /// <param name="path">子节点相对根结点的路径</param>
        public static T GetComponentDefault<T>(this Component component, string path = null) where T : Component
        {
            if (!component) return null;
            return _GetComponentDefault<T>(component.transform, path);
        }
        private static T _GetComponentDefault<T>(this Transform transform, string path = null) where T : Component
        {
            var node = string.IsNullOrEmpty(path) ? transform : transform.Find(path);
            if (!node) return null;
            var comp = node.GetComponent<T>();
            return comp ? comp : node.gameObject.AddComponent<T>();
        }
    }
}
