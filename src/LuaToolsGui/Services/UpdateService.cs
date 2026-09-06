using Velopack;
using Velopack.Sources;

namespace LuaToolsGui.Services;

/// <summary>
/// Silent background auto-update via Velopack + GitHub Releases.
/// Updates are downloaded only from the MLK3 Tools repository.
/// </summary>
public class UpdateService
{
    private readonly UpdateManager _manager =
        new UpdateManager(
            new GithubSource(
                AppConfig.GithubReleasesRepos[0],
                accessToken: null,
                prerelease: false,
                downloader: new ProxiedFileDownloader()));

    private UpdateInfo? _staged;

    /// <summary>
    /// Raised when an update has finished downloading and is ready to apply.
    /// </summary>
    public event Action? UpdateReady;

    /// <summary>
    /// True when an update has been downloaded and is waiting to be applied.
    /// </summary>
    public bool HasStagedUpdate => _staged is not null;

    /// <summary>
    /// Checks for and stages an update from the MLK3 Tools GitHub repository.
    /// </summary>
    public async Task CheckAndStageAsync()
    {
        if (!_manager.IsInstalled)
            return;

        try
        {
            var info = await _manager.CheckForUpdatesAsync();

            if (info is null)
                return;

            await _manager.DownloadUpdatesAsync(info);

            _staged = info;

            UpdateReady?.Invoke();
        }
        catch
        {
            // Fail silently and retry on the next launch.
        }
    }

    /// <summary>
    /// Applies the staged update immediately and restarts the application.
    /// </summary>
    public void ApplyAndRestart(string[]? restartArgs = null)
    {
        if (_staged is not null)
        {
            _manager.ApplyUpdatesAndRestart(
                _staged,
                restartArgs);
        }
    }

    /// <summary>
    /// Applies the staged update after the application exits.
    /// </summary>
    public void ApplyOnExit()
    {
        if (_staged is not null)
        {
            _manager.WaitExitThenApplyUpdates(
                _staged,
                silent: true,
                restart: false);
        }
    }
}
