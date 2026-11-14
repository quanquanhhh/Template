using System;
using Newtonsoft.Json;

namespace MythMerge.Save
{
    [Serializable]
    public class StorageCommon : StorageBase
    {
        //------------用户信息-------
        [JsonProperty] private ulong playerId;

        [JsonIgnore]
        public ulong PlayerId
        {
            get { return playerId; }
            set
            {
                if (playerId != value)
                {
                    playerId = value;
                    StorageManager.Instance.LocalVersion++;
                    StorageManager.Instance.SyncForce = true;
                }
            }
        }
        
        //------------多语言相关------
        [JsonProperty] private string locale = "";

        [JsonIgnore]
        public string Locale
        {
            get { return locale; }
            set
            {
                if (locale != value)
                {
                    locale = value;
                    StorageManager.Instance.LocalVersion++;
                }
            }
        }
        
        
        [JsonProperty] private string oriLocale = "";
        [JsonIgnore]
        public string OriLocale
        {
            get
            {
                return oriLocale;
            }
            set
            {
                if (oriLocale != value)
                {
                    oriLocale = value;
                    StorageManager.Instance.LocalVersion++;
                }
            }
        }
    }
}