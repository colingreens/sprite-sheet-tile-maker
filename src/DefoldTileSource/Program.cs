using SixLabors.ImageSharp;
using System.Text;
using Tomlyn;
using SpritesheetCollector.Models;

namespace DefoldTileSource;

internal class Program
{
    private record SheetFrameData(string AnimationId, int FrameCount);

    private static void Main(string[] args)
    {
        Console.WriteLine("--- Defold TileSource Generator ---");

        var tomlPath = "../../config.toml";
        if (!File.Exists(tomlPath))
        {
            Console.WriteLine($"Error: Configuration file not found at '{tomlPath}'");
            return;
        }
        var tomlContent = File.ReadAllText(tomlPath);
        var config = Toml.ToModel<RootConfig>(tomlContent);
        Console.WriteLine("Configuration loaded successfully.");

        var frameDataList = new List<SheetFrameData>();
        int maxWidthInFrames = 0;

        Console.WriteLine("\nAnalyzing source spritesheets...");
        foreach (var sheet in config.Sheets)
        {
            if (!File.Exists(sheet.Path))
            {
                Console.WriteLine($"  - WARNING: Skipping missing file: {sheet.Path}");
                continue;
            }

            using (var image = Image.Load(sheet.Path))
            {
                int cols = image.Width / sheet.Frame_Width;
                int rows = image.Height / sheet.Frame_Height;
                int frameCount = cols * rows;

                string animId = Path.GetFileNameWithoutExtension(sheet.Path);
                frameDataList.Add(new SheetFrameData(animId, frameCount));

                if (frameCount > maxWidthInFrames)
                {
                    maxWidthInFrames = frameCount;
                }
                Console.WriteLine($"  - Found '{animId}' with {frameCount} frames.");
            }
        }
        Console.WriteLine($"\nCalculated atlas width: {maxWidthInFrames} frames per row.");

        var sb = new StringBuilder();

        string imagePathForDefold = "/asset" + config.Output_File.Split(new[] { "asset" }, StringSplitOptions.None).LastOrDefault()?.Replace('\\', '/');

        sb.AppendLine($"image: \"{imagePathForDefold}\"");
        sb.AppendLine($"tile_width: {config.Target_Frame_Width}");
        sb.AppendLine($"tile_height: {config.Target_Frame_Height}");
        sb.AppendLine("extrude_borders: 2");
        sb.AppendLine("collision_groups: \"default\"");

        int currentRowStartTile = 1;
        foreach (var data in frameDataList)
        {
            if (data.FrameCount == 0)
            {
                currentRowStartTile += maxWidthInFrames;
                continue;
            }

            int startTile = currentRowStartTile;
            int endTile = startTile + data.FrameCount - 1;

            sb.AppendLine("animations {");
            sb.AppendLine($"  id: \"{data.AnimationId}\"");
            sb.AppendLine($"  start_tile: {startTile}");
            sb.AppendLine($"  end_tile: {endTile}");
            sb.AppendLine("  fps: 12");
            sb.AppendLine("  playback: PLAYBACK_LOOP_FORWARD");
            sb.AppendLine("}");

            currentRowStartTile += maxWidthInFrames;
        }

        string tilesourcePath = Path.ChangeExtension(config.Output_File, ".tilesource");
        File.WriteAllText(tilesourcePath, sb.ToString());

        Console.WriteLine($"\n✅ Success! File generated at:");
        Console.WriteLine(tilesourcePath);
    }
}