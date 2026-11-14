// **********************************************
// 
// Author : xuejie.guo
// Date : 2024/05/17/17:58
// Ver : 0.0.1
// Description : Singleton.cs
// ChangeLog :
// **********************************************

namespace MythMerge.Core
{
    public class Singleton<T> where T : new()
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (null == _instance)
                {
                    _instance = new T();
                }

                return _instance;
            }
        }
    }
}
