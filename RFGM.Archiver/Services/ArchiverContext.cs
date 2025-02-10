using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using RFGM.Archiver.Models.Db;

namespace RFGM.Archiver.Services;

public class ArchiverContext(ILogger<ArchiverContext> log, ILoggerFactory loggerFactory) : DbContext
{
    public DbSet<ContainerEntry> Containers { get; set; }
    public DbSet<PrimitiveEntry> Primitives { get; set; }
    public DbSet<MetadataEntry> Metadata { get; set; }
    public DbSet<FileEntry> Files { get; set; }
    public DbSet<AsmEntry> Asm { get; set; }
    public DbSet<PrimitiveLocation> PrimitiveLocations { get; set; }
    public DbSet<DbPrimitive> PrimitiveIndex { get; set; }
    public DbSet<DbFile> FileIndex { get; set; }
    public DbSet<DbAsmUpdate> AsmUpdates { get; set; }


    public string DbPath { get; } = Path.Combine(AppContext.BaseDirectory, ".rfgm.db");

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options
            .UseSqlite($"Data Source={DbPath}")
            .UseCamelCaseNamingConvention()
            .UseLoggerFactory(loggerFactory)
            .EnableSensitiveDataLogging()
        ;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelBuilder.Entity<Metadata>().OwnsOne(x => x.Properties, b => b.ToJson());
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Drops and recreates single table in sqlite database, ignoring others
    /// </summary>
    [SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Parameter is inferred from type name")]
    public async Task RebuildTable<T>(DbSet<T> table, CancellationToken token)
        where T : class
    {
        var name = table.EntityType.GetTableName();
        await Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS \"{name}\";", token);

        var databaseCreator = this.GetService<IRelationalDatabaseCreator>();
        var script = databaseCreator.GenerateCreateScript().Replace("CREATE TABLE", "CREATE TABLE if not exists");
        log.LogTrace("Applying SQL script to init schema:\n{sql}", script);
        await Database.ExecuteSqlRawAsync(script, token);
    }

}
