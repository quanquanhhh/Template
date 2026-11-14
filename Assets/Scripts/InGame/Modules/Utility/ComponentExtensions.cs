using UnityEngine;

namespace MythMerge
{
    public static class ComponentExtensions
    {
        public static T Bind<T>(this Transform transform, bool forceCreate = false) where T : Component
        {
            var component = transform.GetComponent<T>();
            return component != null || !forceCreate ? component : transform.gameObject.AddComponent<T>();
        }

        public static bool HasState(this Animator animator, string state, int layer = 0)
        {
            return animator != null && animator.runtimeAnimatorController != null && animator.HasState(layer, Animator.StringToHash(state));
        }
    }
}
