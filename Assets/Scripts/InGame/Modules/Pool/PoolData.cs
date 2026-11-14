// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/27/11:32
// Ver : 1.0.0
// Description : PoolData.cs
// ChangeLog :
// **********************************************
using UnityEngine;

namespace MythMerge
{
    public class PoolData<T> where T : class
    {
        public T poolObj = null;

        public float useTime = 0f;

        public float timeOutTime = 2 * 60;

        public PoolData(float inTimeOutTime)
        {
            timeOutTime = inTimeOutTime;
        }

        public bool IsTimeOut()
        {
            return Time.realtimeSinceStartup - useTime > timeOutTime;
        }

        public void UpdateUseTime()
        {
            useTime = Time.realtimeSinceStartup;
        }

        public void Update()
        {
        }
    }
}
