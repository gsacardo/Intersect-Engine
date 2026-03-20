using Intersect.Extensions;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Crafting;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Mapping.Tilesets;
using Intersect.Framework.Core.GameObjects.Maps.MapList;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.Framework.Core.GameObjects.Resources;
using Intersect.Framework.Core.GameObjects.Variables;
using Intersect.GameObjects;
using Intersect.Core;
using Intersect.Server.Database.GameData.Migrations;
using Intersect.Server.Maps;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Intersect.Server.Database.GameData;

/// <summary>
/// <see cref="DbContext"/> implementation that contains static game content descriptors.
/// </summary>
public abstract partial class GameContext : IntersectDbContext<GameContext>, IGameContext
{
    /// <inheritdoc />
    protected GameContext(DatabaseContextOptions databaseContextOptions) : base(databaseContextOptions) { }

    //Animations
    public DbSet<AnimationDescriptor> Animations { get; set; }

    //Crafting
    public DbSet<CraftingRecipeDescriptor> Crafts { get; set; }

    public DbSet<CraftingTableDescriptor> CraftingTables { get; set; }

    //Classes
    public DbSet<ClassDescriptor> Classes { get; set; }

    //Events
    public DbSet<EventDescriptor> Events { get; set; }

    //Items
    public DbSet<ItemDescriptor> Items { get; set; }

    //Equipment Properties of Items
    public DbSet<EquipmentProperties> Items_EquipmentProperties { get; set; }

    //Maps
    public DbSet<MapController> Maps { get; set; }

    public DbSet<MapList> MapFolders { get; set; }

    //NPCs
    public DbSet<NPCDescriptor> Npcs { get; set; }

    //Projectiles
    public DbSet<ProjectileDescriptor> Projectiles { get; set; }

    //Quests
    public DbSet<QuestDescriptor> Quests { get; set; }

    //Resources
    public DbSet<ResourceDescriptor> Resources { get; set; }

    //Shops
    public DbSet<ShopDescriptor> Shops { get; set; }

    //Spells
    public DbSet<SpellDescriptor> Spells { get; set; }

    //Variables
    public DbSet<PlayerVariableDescriptor> PlayerVariables { get; set; }

    public DbSet<ServerVariableDescriptor> ServerVariables { get; set; }

    public DbSet<GuildVariableDescriptor> GuildVariables { get; set; }

    public DbSet<UserVariableDescriptor> UserVariables { get; set; }

    //Tilesets
    public DbSet<TilesetDescriptor> Tilesets { get; set; }

    //Time
    public DbSet<DaylightCycleDescriptor> Time { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EquipmentProperties>()
            .HasOne(property => property.Descriptor)
            .WithOne(item => item.EquipmentProperties)
            .HasForeignKey<EquipmentProperties>(property => property.DescriptorId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public override void OnSchemaMigrationsProcessed(string[] migrations)
    {
        if (migrations.IndexOf("20201004032158_EnablingCerasVersionTolerance") > -1)
        {
            CerasVersionToleranceMigration.Run(this);
        }

        if (migrations.IndexOf("20210512071349_BoundItemExtension") > -1)
        {
            BoundItemExtensionMigration.Run(this);
        }

        if (migrations.IndexOf("20211031200145_FixQuestTaskCompletionEvents") > -1)
        {
            FixQuestTaskCompletionEventsMigration.Run(this);
        }

        EnsureClassHairColumns();
        EnsureItemHideHairColumn();
    }

    private void EnsureItemHideHairColumn()
    {
        var databaseType = DatabaseType;
        if (ColumnExists("Items", "HideHair"))
        {
            return;
        }

        ApplicationContext.Context.Value?.Logger.LogInformation(
            "Applying schema compatibility patch for Items.HideHair on {DatabaseType}.",
            databaseType
        );

        ExecuteNonQuery(
            databaseType switch
            {
                Intersect.Config.DatabaseType.Sqlite => "ALTER TABLE \"Items\" ADD COLUMN \"HideHair\" INTEGER NOT NULL DEFAULT 0;",
                Intersect.Config.DatabaseType.MySql => "ALTER TABLE `Items` ADD COLUMN `HideHair` tinyint(1) NOT NULL DEFAULT 0;",
                _ => throw new DatabaseTypeInvalidException(databaseType),
            }
        );
    }

    private void EnsureClassHairColumns()
    {
        var databaseType = DatabaseType;
        if (ColumnExists("Classes", "Hairs"))
        {
            return;
        }

        ApplicationContext.Context.Value?.Logger.LogInformation(
            "Applying schema compatibility patch for Classes.Hairs on {DatabaseType}.",
            databaseType
        );

        ExecuteNonQuery(
            databaseType switch
            {
                Intersect.Config.DatabaseType.Sqlite => "ALTER TABLE \"Classes\" ADD COLUMN \"Hairs\" TEXT NULL;",
                Intersect.Config.DatabaseType.MySql => "ALTER TABLE `Classes` ADD COLUMN `Hairs` longtext NULL;",
                _ => throw new DatabaseTypeInvalidException(databaseType),
            }
        );

        ExecuteNonQuery(
            databaseType switch
            {
                Intersect.Config.DatabaseType.Sqlite => "UPDATE \"Classes\" SET \"Hairs\" = '[]' WHERE \"Hairs\" IS NULL;",
                Intersect.Config.DatabaseType.MySql => "UPDATE `Classes` SET `Hairs` = '[]' WHERE `Hairs` IS NULL;",
                _ => throw new DatabaseTypeInvalidException(databaseType),
            }
        );
    }

    private bool ColumnExists(string tableName, string columnName)
    {
        var databaseType = DatabaseType;
        Database.OpenConnection();
        using var command = Database.GetDbConnection().CreateCommand();
        command.CommandText = databaseType switch
        {
            Intersect.Config.DatabaseType.Sqlite => $"PRAGMA table_info(\"{tableName}\");",
            Intersect.Config.DatabaseType.MySql => $"SHOW COLUMNS FROM `{tableName}` LIKE '{columnName}';",
            _ => throw new DatabaseTypeInvalidException(databaseType),
        };

        using var reader = command.ExecuteReader();
        if (databaseType == Intersect.Config.DatabaseType.Sqlite)
        {
            while (reader.Read())
            {
                if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        return reader.Read();
    }

    private void ExecuteNonQuery(string commandText)
    {
        Database.OpenConnection();
        using var command = Database.GetDbConnection().CreateCommand();
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }

    internal static partial class Queries
    {
        internal static readonly Func<Guid, ServerVariableDescriptor> ServerVariableById =
            (Guid id) => (ServerVariableDescriptor)ServerVariableDescriptor.Lookup.FirstOrDefault(variable => variable.Key == id).Value;

        internal static readonly Func<string, ServerVariableDescriptor> ServerVariableByName =
            (string name) => (ServerVariableDescriptor)ServerVariableDescriptor.Lookup.FirstOrDefault(variable => string.Equals(variable.Value.Name, name, StringComparison.OrdinalIgnoreCase)).Value;

        internal static readonly Func<int, int, IEnumerable<ServerVariableDescriptor>> ServerVariables =
            (int page, int count) => ServerVariableDescriptor.Lookup.Select(v => (ServerVariableDescriptor)v.Value).OrderBy(v => v.Id.ToString()).Skip(page * count).Take(count);
    }
}
