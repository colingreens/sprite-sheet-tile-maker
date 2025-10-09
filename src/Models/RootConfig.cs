namespace SpritesheetCollector.Models;

public record RootConfig
{
    public string Output_File { get; set; } = "";
    public int Target_Frame_Width { get; set; }
    public int Target_Frame_Height { get; set; }
    public List<SpriteSheetInfo> Sheets { get; set; } = new();
}