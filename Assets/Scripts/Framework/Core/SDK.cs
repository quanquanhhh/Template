using System;
using System.Collections.Generic;

namespace MythMerge.Core
{
    // 保留旧项目的服务访问写法；只注册本地框架服务，不加载商业 SDK。
    public static class SDK
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        public static void Install<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            Services[typeof(T)] = service;
        }

        internal static T Get<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out var service)) return (T)service;
            throw new InvalidOperationException($"Service {typeof(T).FullName} has not been installed.");
        }

        internal static bool Contains<T>() where T : class => Services.ContainsKey(typeof(T));
        public static void Uninstall<T>() where T : class => Services.Remove(typeof(T));
    }

    public static class SDK<T> where T : class
    {
        public static T Instance => SDK.Get<T>();
        public static bool CanGet() => SDK.Contains<T>();
    }

    public readonly struct SDKRef<T> where T : class
    {
        public T Instance => SDK<T>.Instance;
    }

    public class SDKException : Exception
    {
        public SDKException(string message) : base(message) { }
    }
}
