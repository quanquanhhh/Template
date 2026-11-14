// **********************************************
// 
// Author : xuejie.guo
// Date : 2023/06/25/12:03
// Ver : 1.0.0
// Description : Module.cs
// ChangeLog :
// **********************************************

namespace MythMerge
{
    public interface IModule
    {
        public void InitializeModule();
        
        public void OnStartGameLogic();
         
        public void OnShutDown();
        
    }
}
