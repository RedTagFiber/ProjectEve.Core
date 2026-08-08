using Microsoft.Data.Sqlite;

namespace ProjectEve.Core.Database;

public static class LocationDb
{
    public static void EnsureTables()
    {
        using var conn = EveDb.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Locations (
                Key TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                ImagePath TEXT,
                Prompt TEXT,
                Light TEXT,
                Smell TEXT,
                Mood TEXT,
                DefaultNarration TEXT,
                IsGenerated INTEGER NOT NULL DEFAULT 0,
                UpdatedAt TEXT
            );

            CREATE TABLE IF NOT EXISTS NpcCurrentLocation (
                NpcId INTEGER PRIMARY KEY,
                LocationKey TEXT NOT NULL,
                ArrivedAt TEXT
            );

            CREATE TABLE IF NOT EXISTS NpcLocationVisits (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NpcId INTEGER NOT NULL,
                LocationKey TEXT NOT NULL,
                FirstVisitAt TEXT,
                LastVisitAt TEXT,
                VisitCount INTEGER NOT NULL DEFAULT 1,
                Notes TEXT
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public static void SeedDefaults()
    {
        EnsureTables();
        using var conn = EveDb.Open();

        UpsertLocation(conn, new LocationRecord(
            Key: "eve-apartment",
            Name: "Eve’s apartment",
            ImagePath: "/images/scenes/eve-apartment.png",
            Prompt: "cozy small apartment living room, warm lamp light, couch, soft evening mood",
            Light: "Warm lamp",
            Smell: "Coffee + lotion",
            Mood: "Private",
            DefaultNarration: "She’s on the couch when you come in, one leg tucked under her, phone face-down on the cushion.",
            IsGenerated: false
        ));

        UpsertLocation(conn, new LocationRecord(
            Key: "coffee-shop",
            Name: "Coffee shop",
            ImagePath: "/images/scenes/coffee-shop.png",
            Prompt: "warm modern coffee shop interior, wooden counter, morning window light, espresso machine",
            Light: "Morning window light",
            Smell: "Espresso and pastry",
            Mood: "Public face",
            DefaultNarration: "Steam lifts from the machine. She’s behind the counter, apron on, already watching the door.",
            IsGenerated: false
        ));

        UpsertLocation(conn, new LocationRecord(
            Key: "ryans-house",
            Name: "Ryan’s house",
            ImagePath: "/images/scenes/ryans-house.png",
            Prompt: "small clean living room in Ohio house, desk with computer, warm indoor light",
            Light: "Warm indoor light",
            Smell: "Clean laundry + PC heat",
            Mood: "Familiar",
            DefaultNarration: "The house is quiet. Her shoes are by the door like she already decided to stay a while.",
            IsGenerated: false
        ));

        // Eve defaults to coffee shop as her work world start, or apartment if you prefer
        SetNpcLocation(conn, npcId: 1, locationKey: "eve-apartment");
    }

    public static LocationRecord? GetLocation(string key)
    {
        using var conn = EveDb.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Key, Name, ImagePath, Prompt, Light, Smell, Mood, DefaultNarration, IsGenerated
            FROM Locations
            WHERE Key = $key
            """;
        cmd.Parameters.AddWithValue("$key", key);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new LocationRecord(
            Key: reader.GetString(0),
            Name: reader.GetString(1),
            ImagePath: reader.IsDBNull(2) ? null : reader.GetString(2),
            Prompt: reader.IsDBNull(3) ? null : reader.GetString(3),
            Light: reader.IsDBNull(4) ? null : reader.GetString(4),
            Smell: reader.IsDBNull(5) ? null : reader.GetString(5),
            Mood: reader.IsDBNull(6) ? null : reader.GetString(6),
            DefaultNarration: reader.IsDBNull(7) ? null : reader.GetString(7),
            IsGenerated: reader.GetInt32(8) == 1
        );
    }

    public static LocationRecord? GetNpcCurrentLocation(int npcId)
    {
        using var conn = EveDb.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT LocationKey
            FROM NpcCurrentLocation
            WHERE NpcId = $id
            """;
        cmd.Parameters.AddWithValue("$id", npcId);

        var key = cmd.ExecuteScalar() as string;
        if (string.IsNullOrWhiteSpace(key))
            return null;

        return GetLocation(key);
    }

    public static void MoveNpcToLocation(int npcId, string locationKey, string? notes = null)
    {
        using var conn = EveDb.Open();
        SetNpcLocation(conn, npcId, locationKey);

        var now = DateTime.Now.ToString("o");

        using var find = conn.CreateCommand();
        find.CommandText = """
            SELECT Id, VisitCount FROM NpcLocationVisits
            WHERE NpcId = $npc AND LocationKey = $key
            """;
        find.Parameters.AddWithValue("$npc", npcId);
        find.Parameters.AddWithValue("$key", locationKey);

        using var reader = find.ExecuteReader();
        if (reader.Read())
        {
            var id = reader.GetInt32(0);
            var count = reader.GetInt32(1) + 1;
            reader.Close();

            using var update = conn.CreateCommand();
            update.CommandText = """
                UPDATE NpcLocationVisits
                SET LastVisitAt = $last, VisitCount = $count, Notes = COALESCE($notes, Notes)
                WHERE Id = $id
                """;
            update.Parameters.AddWithValue("$last", now);
            update.Parameters.AddWithValue("$count", count);
            update.Parameters.AddWithValue("$notes", (object?)notes ?? DBNull.Value);
            update.Parameters.AddWithValue("$id", id);
            update.ExecuteNonQuery();
        }
        else
        {
            reader.Close();
            using var insert = conn.CreateCommand();
            insert.CommandText = """
                INSERT INTO NpcLocationVisits
                (NpcId, LocationKey, FirstVisitAt, LastVisitAt, VisitCount, Notes)
                VALUES ($npc, $key, $first, $last, 1, $notes)
                """;
            insert.Parameters.AddWithValue("$npc", npcId);
            insert.Parameters.AddWithValue("$key", locationKey);
            insert.Parameters.AddWithValue("$first", now);
            insert.Parameters.AddWithValue("$last", now);
            insert.Parameters.AddWithValue("$notes", (object?)notes ?? DBNull.Value);
            insert.ExecuteNonQuery();
        }
    }

    private static void SetNpcLocation(SqliteConnection conn, int npcId, string locationKey)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO NpcCurrentLocation (NpcId, LocationKey, ArrivedAt)
            VALUES ($npc, $key, $at)
            ON CONFLICT(NpcId) DO UPDATE SET
                LocationKey = $key,
                ArrivedAt = $at
            """;
        cmd.Parameters.AddWithValue("$npc", npcId);
        cmd.Parameters.AddWithValue("$key", locationKey);
        cmd.Parameters.AddWithValue("$at", DateTime.Now.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    private static void UpsertLocation(SqliteConnection conn, LocationRecord loc)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Locations
            (Key, Name, ImagePath, Prompt, Light, Smell, Mood, DefaultNarration, IsGenerated, UpdatedAt)
            VALUES ($key, $name, $image, $prompt, $light, $smell, $mood, $narr, $gen, $updated)
            ON CONFLICT(Key) DO UPDATE SET
                Name = $name,
                ImagePath = $image,
                Prompt = $prompt,
                Light = $light,
                Smell = $smell,
                Mood = $mood,
                DefaultNarration = $narr,
                UpdatedAt = $updated
            """;
        cmd.Parameters.AddWithValue("$key", loc.Key);
        cmd.Parameters.AddWithValue("$name", loc.Name);
        cmd.Parameters.AddWithValue("$image", (object?)loc.ImagePath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$prompt", (object?)loc.Prompt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$light", (object?)loc.Light ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$smell", (object?)loc.Smell ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$mood", (object?)loc.Mood ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$narr", (object?)loc.DefaultNarration ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$gen", loc.IsGenerated ? 1 : 0);
        cmd.Parameters.AddWithValue("$updated", DateTime.Now.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public record LocationRecord(
        string Key,
        string Name,
        string? ImagePath,
        string? Prompt,
        string? Light,
        string? Smell,
        string? Mood,
        string? DefaultNarration,
        bool IsGenerated
    );
}