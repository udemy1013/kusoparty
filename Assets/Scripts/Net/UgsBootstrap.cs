using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Core.Environments; // SetEnvironmentName 用
using Unity.Services.Authentication;

public static class UgsBootstrap
{
    public static async Task InitAndSignInAsync(string environmentName = "dev")
    {
        // 環境（dev/production など）を明示
        var options = new InitializationOptions();
        options.SetEnvironmentName(environmentName);
        await UnityServices.InitializeAsync(options);

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            // 必要に応じてイベント購読：セッション失効通知など
            // AuthenticationService.Instance.Expired += () => ...
        }
    }
}
