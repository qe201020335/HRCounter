using System;
using System.Threading;
using System.Threading.Tasks;
using IPA.Logging;
using SiraUtil.Zenject;
using Zenject;

namespace HRCounter.Utils;

internal class UserInfoHelper : IAsyncInitializable
{
    private const int RETRY = 5;

    [Inject]
    private readonly Logger _logger = null!;

    [Inject]
    private readonly IPlatformUserModel _platformUserModel = null!;

    public UserInfo? UserInfo { get; private set; }

    Task IAsyncInitializable.InitializeAsync(CancellationToken token) => LoadUserInfo(token);

    private async Task LoadUserInfo(CancellationToken token)
    {
        for (var i = 0; i < RETRY; i++)
        {
            if (i > 0)
            {
                // exponential backoff
                var delay = 1000 * Math.Pow(2, i);
                await Task.Delay((int)delay);
            }

            try
            {
                var userInfo = await _platformUserModel.GetUserInfo(token);
                UserInfo = userInfo;
                return;
            }
            catch (OperationCanceledException)
            {
                // we exit
                _logger.Debug("LoadUserInfo cancelled");
                return;
            }
            catch (Exception e)
            {
                _logger.Warn($"Failed to load user info ({i + 1}/{RETRY}): {e.Message}");
                _logger.Warn(e);
            }
        }

        _logger.Warn("Failed to load user info after all retries");
    }
}
