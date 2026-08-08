using Microsoft.Data.Sqlite;

namespace ProjectEve.Core.Database;

public static class EveDb
{
    // CHANGE THIS to your real Project Eve DB path
    public static string DbPath { get; set; } =
    @"C:\Users\ryans\source\repos\RedTagFiber\ProjectEve2026\ProjectEve\bin\Debug\net10.0\project_eve.db";

    public static SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        return conn;
    }

    public static string LoadEvePersonality()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Name, Age, Location, PersonalityContext, Goal, Need, Fear, Want
            FROM Characters
            WHERE Id = 1
            """;

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return "You are Eve Sinclair, 25, coffee shop manager. Never say you are an AI.";

        var name = reader.GetString(0);
        var age = reader.GetInt32(1);
        var location = reader.IsDBNull(2) ? "Ohio" : reader.GetString(2);
        var personality = reader.IsDBNull(3) ? "" : reader.GetString(3);
        var goal = reader.IsDBNull(4) ? "" : reader.GetString(4);
        var need = reader.IsDBNull(5) ? "" : reader.GetString(5);
        var fear = reader.IsDBNull(6) ? "" : reader.GetString(6);
        var want = reader.IsDBNull(7) ? "" : reader.GetString(7);

        return $"""
        You are {name}, age {age}, living around {location}.
        PersonalityContext: {personality}
        Goal: {goal}
        Need: {need}
        Fear: {fear}
        Want: {want}
        You are texting from your phone. Never say you are an AI.
        """;
    }

    public static void SaveMemory(string summary, string category = "phone_chat", int importance = 1)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Memories (NpcId, Summary, Category, Importance, Timestamp)
            VALUES ($NpcId, $Summary, $Category, $Importance, $Timestamp)
            """;
        cmd.Parameters.AddWithValue("$NpcId", 1);
        cmd.Parameters.AddWithValue("$Summary", summary);
        cmd.Parameters.AddWithValue("$Category", category);
        cmd.Parameters.AddWithValue("$Importance", importance);
        cmd.Parameters.AddWithValue("$Timestamp", DateTime.Now.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public static List<string> LoadRecentMemories(int count = 8)
    {
        var list = new List<string>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Summary FROM Memories
            WHERE NpcId = 1
            ORDER BY Timestamp DESC
            LIMIT $count
            """;
        cmd.Parameters.AddWithValue("$count", count);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(reader.GetString(0));

        list.Reverse();
        return list;
    }
}