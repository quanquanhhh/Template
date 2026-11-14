using MythMerge.LauncherEngine;
// Author : xuejie.guo
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythMerge.Save
{
    [Serializable]
    public class StorageDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public StorageDictionary() : this(false) { }

        public StorageDictionary(bool syncForce)
        {
            SyncForce = syncForce;
        }
        bool SyncForce { get; set; }

        void OnStorageChanged()
        {
            StorageManager.Instance.LocalVersion++;
            if (SyncForce)
            {
                StorageManager.Instance.SyncForce = true;
            }

        }

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
            OnStorageChanged();
        }

        public new void Clear()
        {
            Log.Warning("You are deleting some of the contents of the storage, please double check if you really want to do this");
            base.Clear();
            OnStorageChanged();
        }

        public new bool Remove(TKey key)
        {
            Log.Warning("You are deleting some of the contents of the storage, please double check if you really want to do this");
            OnStorageChanged();
            return base.Remove(key);
        }

        public new TValue this[TKey key]
        {
            get
            {
                return base[key];
            }
            set
            {
                if (!base.ContainsKey(key) || !EqualityComparer<TValue>.Default.Equals(value, base[key]))
                {
                    base[key] = value;
                    OnStorageChanged();
                }
            }
        }
    }
}