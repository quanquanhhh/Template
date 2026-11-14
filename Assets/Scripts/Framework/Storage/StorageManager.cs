// Author : xuejie.guo
using MythMerge.LauncherEngine;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MythMerge.Save
{
    // TM5 StorageManager：保留本地序列化、版本和保存流程。
    public class StorageManager : Manager<StorageManager>
    {
        const string storageKey = "StorageData";
        const string localVersionKey = "StorageVersion";
        const string runOnceKey = "RunOnce";
        public static System.Action onRebuild;
        static readonly Dictionary<System.Type, string> gType2Name = new Dictionary<System.Type, string>();
        Dictionary<string, StorageBase> storageMap = new Dictionary<string, StorageBase>();

        float LocalTickTime
        {
            get; set;
        }
        float localInterval = 1.0f;
        ulong _localVersion;
        public ulong LocalVersion
        {
            get
            {
                return _localVersion;
            }
            set
            {
                _localVersion = value;
                //Log.Info("[storage] changed local_version to {0}", value);
            }
        }
        
        ulong lastSavedLocalVersion;
        bool isFullInitialized;
        string keyPrefix = "MM1.";
        public bool IsFullInitialized => Inited && isFullInitialized;

        // 默认隔离 MM1 的键；测试或分槽需要时，在 Init 之前配置。
        public void ConfigureKeyPrefix(string prefix)
        {
            if (Inited) throw new System.InvalidOperationException("Configure keys before Init.");
            if (string.IsNullOrWhiteSpace(prefix)) throw new System.ArgumentException("A nonempty key prefix is required.", nameof(prefix));
            keyPrefix = prefix;
        }

        public T GetStorage<T>() where T : StorageBase
        {
            if (!Inited) throw new System.InvalidOperationException("Initialize StorageManager before GetStorage.");
            System.Type storage_type = typeof(T);
            if (!gType2Name.TryGetValue(storage_type, out string name))
            {
                name = storage_type.Name;
                gType2Name[storage_type] = name;
            }

            return (T)storageMap[name];            
        }



        public bool SyncForce
        {
            get;
            set;
        }

        public float LocalInterval
        {
            get
            {
                return localInterval;
            }
            set
            {
                if (value > 0.0f)
                {
                    localInterval = value;
                }
            }
        }

        public bool Inited
        {
            get;
            private set;
        }

        public void SaveToLocal()
        {
            if (!Inited || !isFullInitialized)
            {
                return;
            }
            
            //fixed:ArgumentNullException: Value cannot be null. Parameter name: obj
            JsonSerializerSettings setting = new JsonSerializerSettings();
            setting.NullValueHandling = NullValueHandling.Ignore;
            string jsonData = JsonConvert.SerializeObject(storageMap,setting);
            byte[] encryptData = RijndaelManager.Instance.EncryptStringToBytes(jsonData);
            PlayerPrefs.SetString(keyPrefix + storageKey, System.Convert.ToBase64String(encryptData));
            PlayerPrefs.SetString(keyPrefix + localVersionKey, System.Convert.ToBase64String(RijndaelManager.Instance.EncryptStringToBytes(LocalVersion.ToString())));

            //Log.Info(" save local version : " + LocalVersion);
            //Log.Info(" save local storage json : " + jsonData);
            PlayerPrefs.Save();
            lastSavedLocalVersion = LocalVersion;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(storageMap);
        }

        public void FromJson(string jsonData)
        {
            JObject jObj = JObject.Parse(jsonData);
            foreach (var type in storageMap.Keys)
            {
                var token = jObj[type];
                if (token == null)
                {
                    continue;
                }

                var str = token.ToString();
                JsonSerializerSettings setting = new JsonSerializerSettings();

                setting.ObjectCreationHandling = ObjectCreationHandling.Replace;
                setting.NullValueHandling = NullValueHandling.Ignore;
                JsonConvert.PopulateObject(str, storageMap[type], setting);
            }

            onRebuild?.Invoke();
        }

        void ReadFromLocal()
        {
            // 读取本地存档
            string jsonData = "{}";
            if (PlayerPrefs.HasKey(keyPrefix + storageKey))
            {
                byte[] encryptData = System.Convert.FromBase64String(PlayerPrefs.GetString(keyPrefix + storageKey));
                jsonData = RijndaelManager.Instance.DecryptStringFromBytes(encryptData);
#if !DISABLE_STORAGE_LOG
                Log.Info(" read storage json from local : " + jsonData);
#endif
                FromJson(jsonData);
            }
            else
            {
#if !DISABLE_STORAGE_LOG
                Log.Info("No local storage data can read! ");
#endif
            }

            // 读取本地存档版本
            if (PlayerPrefs.HasKey(keyPrefix + localVersionKey))
            {
                string strVersion = RijndaelManager.Instance.DecryptStringFromBytes(System.Convert.FromBase64String(PlayerPrefs.GetString(keyPrefix + localVersionKey)));
                LocalVersion = ulong.Parse(strVersion);
#if !DISABLE_STORAGE_LOG
                Log.Info(" read local version : " + LocalVersion);
#endif
            }
            else
            {
                LocalVersion = 0;
#if !DISABLE_STORAGE_LOG
                Log.Info("No local storage version can read! ");
#endif
            }
            lastSavedLocalVersion = LocalVersion;

        }

        void Update()
        {
            if (!Inited || !isFullInitialized)
                return;

            LocalTickTime += Time.unscaledDeltaTime;
            if (SyncForce || (LocalTickTime > localInterval && LocalVersion > lastSavedLocalVersion))
            {
                LocalTickTime = 0.0f;
                SaveToLocal();
                SyncForce = false;
            }

        }

        public void Init(List<StorageBase> storages, bool fullInitialized = true)
        {
            if (storages == null) throw new System.ArgumentNullException(nameof(storages));
            if (!Inited || !isFullInitialized)
            {
                storageMap = new Dictionary<string, StorageBase>();
                foreach (var storage in storages)
                {
                    var type = storage.GetType().Name;
                    if (storageMap.ContainsKey(type)) throw new System.InvalidOperationException("Duplicate storage type name: " + type);
                    storageMap[type] = storage;
                }
                
                Inited = true;
                
                isFullInitialized = fullInitialized;
                
                LocalTickTime = 0;
                SyncForce = false;
                try { ReadFromLocal(); }
                catch { Inited = isFullInitialized = false; throw; }
            }
            else
            {
                throw new System.InvalidOperationException("StorageManager is already fully initialized.");
            }
        }

        public void Reload()
        {
            if (Inited) return;
            Inited = true;
            try
            {
                Clear();
                ReadFromLocal();
                SyncForce = false;
            }
            catch { Inited = isFullInitialized = false; throw; }
        }

        public void Release()
        {
            if (Inited)
            {
                SaveToLocal();
                Inited = false;
            }
        }

        public bool RunOnce(System.Action cb)
        {
            if (PlayerPrefs.HasKey(keyPrefix + runOnceKey))
            {
                return false;
            }

            if (!Inited || !isFullInitialized) throw new System.InvalidOperationException("Fully initialize StorageManager before RunOnce.");
            cb?.Invoke();
            PlayerPrefs.SetString(keyPrefix + runOnceKey, true.ToString());
            SaveToLocal();
            return true;
        }

        void Clear()
        {
            foreach (var key in storageMap.Keys)
            {
                storageMap[key].Clear();
            }
        }

        // 只删除本存档的键，不调用 PlayerPrefs.DeleteAll。
        public void ClearAll()
        {
            if (!IsFullInitialized) 
                throw new System.InvalidOperationException("Fully initialize StorageManager before ClearAll.");
            Clear();
            LocalVersion = 0;
            SyncForce = false;
            PlayerPrefs.DeleteKey(keyPrefix + runOnceKey);
            SaveToLocal();
            onRebuild?.Invoke();
        }

        void OnApplicationPause(bool paused) { if (paused) SaveToLocal(); }
        void OnApplicationQuit() { Release(); }
        void OnDestroy() { Release(); }
    }
}
