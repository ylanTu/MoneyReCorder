namespace MoneyReCorder.Services;

/// <summary>
/// 云端同步服务桩实现（预留，待后续实现）。
/// 当前版本仅返回空操作，便于未来无缝接入。
/// </summary>
public class SyncService : ISyncService
{
    public bool IsEnabled => false;
    public DateTime? LastSyncTime => null;

    public Task<bool> SyncAsync()
    {
        // TODO: 实现云端同步逻辑
        return Task.FromResult(false);
    }

    public Task EnableAsync(string endpoint, string? token = null)
    {
        // TODO: 配置云同步
        return Task.CompletedTask;
    }

    public Task DisableAsync()
    {
        // TODO: 禁用云同步
        return Task.CompletedTask;
    }
}
