using OneSignalSDK.DotNet;
using OneSignalSDK.DotNet.Core.User.Subscriptions;

namespace Mascotared.Services
{
    public static class OneSignalService
    {
        private const string OneSignalAppId = "0dca4c23-7771-4adb-8a81-6491ed2c3425";

        public static void Inicializar()
        {
#if ANDROID || IOS
            OneSignal.Initialize(OneSignalAppId);
            OneSignal.Notifications.RequestPermissionAsync(true);

            OneSignal.User.PushSubscription.Changed += async (sender, args) =>
            {
                var playerId = OneSignal.User.PushSubscription.Id;
                if (!string.IsNullOrEmpty(playerId))
                    await GuardarPlayerIdAsync(playerId);
            };
#endif
        }

        private static async Task GuardarPlayerIdAsync(string playerId)
        {
            try
            {
                var api = new ApiService();
                await api.GuardarPlayerIdAsync(playerId);
            }
            catch { }
        }
    }
}