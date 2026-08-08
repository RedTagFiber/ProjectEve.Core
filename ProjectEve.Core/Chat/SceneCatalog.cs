namespace ProjectEve.PhoneOS.Services;

public class SceneCatalog
{
    public SceneData GetDefault() => new(
        Location: "Eve’s apartment",
        Light: "Warm lamp",
        Smell: "Coffee + lotion",
        Mood: "Private",
        Narration: "She’s on the couch when you come in, one leg tucked under her, phone face-down on the cushion.",
        SceneImageUrl: "/images/scenes/eve-apartment.jpg",
        PortraitImageUrl: "/images/portraits/eve-hoodie.jpg",
        PortraitName: "Eve",
        PortraitOutfit: "hoodie"
    );

    public SceneData? FromPlayerText(string input)
    {
        input = (input ?? "").ToLowerInvariant();

        if (input.Contains("coffee") || input.Contains("work") || input.Contains("shop"))
        {
            return new SceneData(
                Location: "Coffee shop",
                Light: "Morning window light",
                Smell: "Espresso and pastry",
                Mood: "Public face",
                Narration: "Steam lifts from the machine. She’s behind the counter, apron on, already watching the door.",
                SceneImageUrl: "/images/scenes/coffee-shop.jpg",
                PortraitImageUrl: "/images/portraits/eve-apron.jpg",
                PortraitName: "Eve",
                PortraitOutfit: "apron"
            );
        }

        if (input.Contains("apartment") || input.Contains("couch") || input.Contains("home"))
            return GetDefault();

        return null;
    }

    public record SceneData(
        string Location,
        string Light,
        string Smell,
        string Mood,
        string Narration,
        string SceneImageUrl,
        string PortraitImageUrl,
        string PortraitName,
        string PortraitOutfit
    );
}