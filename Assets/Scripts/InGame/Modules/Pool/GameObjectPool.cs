using System;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MythMerge
{
    public class GameObjectPool : PoolBase<GameObject>
    {
        private GameObject rootTemplate;
        private Transform _root;
        private bool _ownsTemplate;
        protected static GameObject PoolRoot;

        public void InitPoolRoot()
        {
            if (PoolRoot == null)
            {
                PoolRoot = new GameObject("PoolRoot");
                Object.DontDestroyOnLoad(PoolRoot);
                PoolRoot.SetActive(false);
            }
            if (_root == null)
            {
                var go = new GameObject(poolName);
                go.transform.SetParent(PoolRoot.transform, false);
                _root = go.transform;
            }
        }
        public void InitializeWithPrefab(GameObject prefab, string name, int count)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            rootTemplate = prefab;
            InitPool(name, count);
        }
        public override void InitPool(string name, int count)
        {
            poolName = name;
            InitPoolRoot();
            base.InitPool(name, count);
        }
        protected override PoolData<GameObject> SpawnPoolData()
        {
            if (rootTemplate == null)
            {
                rootTemplate = AssetModule.Instance.LoadAsset<GameObject>(poolName, _root);
                rootTemplate.SetActive(false);
                _ownsTemplate = true;
            }
            var go = Object.Instantiate(rootTemplate, _root);
            go.SetActive(false);
            return new PoolData<GameObject>(120) { poolObj = go };
        }
        public override GameObject Spawn()
        {
            // 外部提前销毁的实例不能再返回给调用方。
            for (int i = freeQueue.Count - 1; i >= 0; i--) if (freeQueue[i].poolObj == null) freeQueue.RemoveAt(i);
            var go = base.Spawn();
            go.transform.SetParent(null, false);
            go.SetActive(true);
            foreach (var component in go.GetComponentsInChildren<MonoBehaviour>(true)) if (component is IPoolable reset) reset.OnSpawn();
            return go;
        }
        public override PoolData<GameObject> DeSpawn(GameObject obj)
        {
            if (obj == null) return null;
            var data = base.DeSpawn(obj);
            if (data == null) return null;
            foreach (var component in obj.GetComponentsInChildren<MonoBehaviour>(true)) if (component is IPoolable reset) reset.OnDeSpawn();
            foreach (var transform in obj.GetComponentsInChildren<Transform>(true)) transform.DOKill(false);
            foreach (var particle in obj.GetComponentsInChildren<ParticleSystem>(true)) particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var audio in obj.GetComponentsInChildren<AudioSource>(true)) audio.Stop();
            obj.SetActive(false);
            obj.transform.SetParent(_root, false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            return data;
        }
        public override void ReleasePoolData(PoolData<GameObject> data)
        {
            if (data.poolObj != null)
            {
                foreach (var transform in data.poolObj.GetComponentsInChildren<Transform>(true)) transform.DOKill(false);
                Object.Destroy(data.poolObj);
            }
            data.poolObj = null;
        }
        public override void OnShutDown()
        {
            base.OnShutDown();
            if (_ownsTemplate && rootTemplate != null) Object.Destroy(rootTemplate);
            rootTemplate = null;
            if (_root != null) Object.Destroy(_root.gameObject);
            _root = null;
        }
        internal static void DestroyRoot()
        {
            if (PoolRoot != null) Object.Destroy(PoolRoot);
            PoolRoot = null;
        }
    }
}
