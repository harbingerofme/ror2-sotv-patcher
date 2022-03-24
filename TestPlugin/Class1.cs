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

    [NetworkSettings]
    public class Class2 : NetworkBehaviour
    {
        public NetworkAnimator _Animator;


        public NetworkAnimator blah(NetworkBehaviour networkBehaviour)
        {
            var x = new NetworkClient();

            var y = NetworkError.BadMessage;

            var z = x.connection;
            var xa = UnityEngine.Input.compass;

            Class1._Logger.LogDebug(x);
            Class1._Logger.LogDebug(y);
            Class1._Logger.LogDebug(z);
            Class1._Logger.LogDebug(xa);

            return null;
        }
    }
}
