using glove_e.Models;
using SQLite;

namespace glove_e.Services;

public interface IDatabaseService
{
    Task SaveReadingAsync(DistanceReading reading);
    Task<List<DistanceReading>> GetReadingsAsync(int limit = 200);
    Task ClearReadingsAsync();
    Task SaveAlertAsync(AlertEvent alert);
    Task<List<AlertEvent>> GetAlertsAsync();
}

/// <summary>Persistencia local con SQLite (sqlite-net-pcl).</summary>
public class DatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection? _db;

    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db is not null)
            return _db;

        var path = Path.Combine(FileSystem.AppDataDirectory, "glove_e.db3");
        _db = new SQLiteAsyncConnection(path,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        await _db.CreateTableAsync<DistanceReading>();
        await _db.CreateTableAsync<AlertEvent>();
        return _db;
    }

    public async Task SaveReadingAsync(DistanceReading reading)
    {
        var db = await GetDbAsync();
        await db.InsertAsync(reading);
    }

    public async Task<List<DistanceReading>> GetReadingsAsync(int limit = 200)
    {
        var db = await GetDbAsync();
        return await db.Table<DistanceReading>()
                       .OrderByDescending(r => r.Id)
                       .Take(limit)
                       .ToListAsync();
    }

    public async Task ClearReadingsAsync()
    {
        var db = await GetDbAsync();
        await db.DeleteAllAsync<DistanceReading>();
    }

    public async Task SaveAlertAsync(AlertEvent alert)
    {
        var db = await GetDbAsync();
        await db.InsertAsync(alert);
    }

    public async Task<List<AlertEvent>> GetAlertsAsync()
    {
        var db = await GetDbAsync();
        return await db.Table<AlertEvent>()
                       .OrderByDescending(a => a.Id)
                       .ToListAsync();
    }
}
