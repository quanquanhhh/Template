using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythMerge
{
    public interface IPoolable
    {
        void OnSpawn();
        void OnDeSpawn();
    }
    public abstract class PoolBase
    {
        public string poolName = string.Empty;
        public int poolMaxCount;
        public virtual void InitPool(string inPoolName, int inPoolMaxCount)
        {
            if (inPoolMaxCount < 0) throw new ArgumentOutOfRangeException(nameof(inPoolMaxCount));
            poolName = inPoolName;
            poolMaxCount = inPoolMaxCount;
        }
        public abstract void Update();
        public abstract void OnShutDown();
    }
    public abstract class PoolBase<T> : PoolBase where T : class
    {
        public readonly List<PoolData<T>> freeQueue = new List<PoolData<T>>();
        public readonly Dictionary<T, PoolData<T>> useQueue = new Dictionary<T, PoolData<T>>();
        public override void InitPool(string name, int count)
        {
            base.InitPool(name, count);
            try { for (int i = 0; i < count; i++) freeQueue.Add(SpawnPoolData()); }
            catch { OnShutDown(); throw; }
        }
        protected abstract PoolData<T> SpawnPoolData();
        public virtual T Spawn()
        {
            PoolData<T> data;
            if (freeQueue.Count > 0) { data = freeQueue[0]; freeQueue.RemoveAt(0); }
            else data = SpawnPoolData();
            data.UpdateUseTime();
            useQueue.Add(data.poolObj, data);
            if (data.poolObj is IPoolable reset) reset.OnSpawn();
            return data.poolObj;
        }
        public virtual PoolData<T> DeSpawn(T obj)
        {
            if (ReferenceEquals(obj, null) || !useQueue.TryGetValue(obj, out var data)) return null;
            useQueue.Remove(obj);
            if (obj is IPoolable reset) reset.OnDeSpawn();
            data.UpdateUseTime();
            freeQueue.Add(data);
            return data;
        }
        public override void Update()
        {
            for (int i = freeQueue.Count - 1; i >= 0 && freeQueue.Count > poolMaxCount; i--)
            {
                if (!freeQueue[i].IsTimeOut()) continue;
                ReleasePoolData(freeQueue[i]);
                freeQueue.RemoveAt(i);
            }
        }
        public virtual void ReleasePoolData(PoolData<T> data) { data.poolObj = null; }
        public override void OnShutDown()
        {
            foreach (var data in freeQueue) ReleasePoolData(data);
            foreach (var data in useQueue.Values) ReleasePoolData(data);
            freeQueue.Clear();
            useQueue.Clear();
        }
    }
    public sealed class ObjectPool<T> : PoolBase<T> where T : class, new()
    {
        protected override PoolData<T> SpawnPoolData() => new PoolData<T>(120) { poolObj = new T() };
        public override void ReleasePoolData(PoolData<T> data)
        {
            if (data.poolObj is IDisposable disposable) disposable.Dispose();
            base.ReleasePoolData(data);
        }
    }
}
