using System;
using System.Collections.Generic;
using MythMerge.Core;
using UnityEngine;

namespace MythMerge
{
    public class PoolModule : Singleton<PoolModule>, IModule
    {
        protected readonly Dictionary<string, PoolBase> pools = new Dictionary<string, PoolBase>();
        private float updateTime;
        public void InitializeModule() { updateTime = 0; }
        public void OnStartGameLogic() => UpdateScheduler.HookUpdate(Update);
        public void OnShutDown()
        {
            UpdateScheduler.UnhookUpdate(Update);
            foreach (var pool in pools.Values) pool.OnShutDown();
            pools.Clear();
            GameObjectPool.DestroyRoot();
        }
        public void CreatePool<T>(string name, int count) where T : PoolBase
        {
            if (pools.TryGetValue(name, out var existing))
            {
                if (existing is T) return;
                throw new InvalidOperationException($"Pool {name} already uses {existing.GetType().Name}.");
            }
            var pool = Activator.CreateInstance<T>();
            pool.InitPool(name, count);
            pools.Add(name, pool);
        }
        public void Update()
        {
            updateTime += Time.unscaledDeltaTime;
            if (updateTime < 5f) return;
            updateTime = 0;
            foreach (var pool in pools.Values) pool.Update();
        }
        public T Spawn<T>(string name) where T : class => GetPool<PoolBase<T>>(name)?.Spawn();
        public void DeSpawn<T>(string name, T obj) where T : class => GetPool<PoolBase<T>>(name)?.DeSpawn(obj);
        public T GetPool<T>(string name) where T : PoolBase
        {
            if (!pools.TryGetValue(name, out var pool)) return null;
            return pool as T ?? throw new InvalidOperationException($"Pool {name} is not {typeof(T).Name}.");
        }
    }
}
