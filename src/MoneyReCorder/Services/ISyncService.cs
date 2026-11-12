namespace MoneyReCorder.Services;

/// <summary>
/// 云端同步服务接口（预留，当前版本未实现）。
/// 当需要实现云同步时，实现此接口即可，不影响现有架构。
/// </summary>
public interface ISyncService
{
    /// <summary>是否已启用同步</summary>
    bool IsEnabled { get; }

    /// <summary>最后同步时间</summary>
    DateTime? LastSyncTime { get; }

    /// <summary>执行数据同步</summary>
    Task<bool> SyncAsync();

    /// <summary>启用同步</summary>
    Task EnableAsync(string endpoint, string? token = null);

    /// <summary>禁用同步</summary>
    Task DisableAsync();
}
