using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class SimpleRelayConnector
{
#if UNITY_WEBGL && !UNITY_EDITOR
    private const string ConnType = "wss";  // WebGL/HTTPSはWSS必須
#else
    private const string ConnType = "dtls"; // 通常は DTLS を推奨（udp も可）
#endif

    public async Task<string> StartHostAsync(int maxConnections = 3)
    {
        await UgsBootstrap.InitAndSignInAsync();

        // ホスト用アロケーションと JoinCode
        Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

        // UTP に Relay サーバーデータを設定
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetRelayServerData(AllocationUtils.ToRelayServerData(alloc, ConnType));

#if UNITY_WEBGL && !UNITY_EDITOR
        utp.UseWebSockets = true; // WebGL では WSS を使う
#endif

        NetworkManager.Singleton.StartHost();
        return joinCode;
    }

    public async Task<bool> StartClientAsync(string joinCode)
    {
        await UgsBootstrap.InitAndSignInAsync();

        JoinAllocation join = await RelayService.Instance.JoinAllocationAsync(joinCode);
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetRelayServerData(AllocationUtils.ToRelayServerData(join, ConnType));

#if UNITY_WEBGL && !UNITY_EDITOR
        utp.UseWebSockets = true;
#endif
        return NetworkManager.Singleton.StartClient();
    }
}
