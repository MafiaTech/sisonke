using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

/// <summary>
/// Portable distributed lock (works identically on SQL Server and SQLite — no sp_getapplock, no
/// native SEQUENCE) — see Services/Jobs/IDistributedJobLock.cs. One row per named job; acquired
/// via a conditional UPDATE, released by clearing LockToken, self-healing via LockExpiresAt if a
/// holder crashes without releasing.
/// </summary>
public class JobExecutionLock
{
    [MaxLength(100)]
    public string JobName { get; set; } = string.Empty;

    public Guid? LockToken { get; set; }

    public DateTime? LockedAt { get; set; }

    public DateTime? LockExpiresAt { get; set; }
}
