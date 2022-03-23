using BepInEx;
using BepInEx.Logging;
using UnityEngine.Networking;

namespace TestPlugin
{
    [BepInPlugin("testplugin", "test plugin, should throw", "0.0.1")]
    public class Class1 : BaseUnityPlugin
    {
        public static ManualLogSource _Logger;

        public void Awake()
        {
            _Logger = Logger;
            new Class2();
        }
    }

    public class Class2 : NetworkBehaviour
    {

    }
}
