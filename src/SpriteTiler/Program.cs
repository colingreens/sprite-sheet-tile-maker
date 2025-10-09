using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tomlyn;
using SpritesheetCollector.Models;

namespace SpritesheetCollector;

internal class Program
{
    private static void Main(string[] args)
    {
        // --- MAIN LOGIC ---
        var tomlContent = File.ReadAllText("../../config.toml");
        var config = Toml.ToModel<RootConfig>(tomlContent);
        var targetSize = new Size(config.Target_Frame_Width, config.Target_Frame_Height);

        Console.WriteLine("Configuration loaded. Processing sheets...");

        // --- 1. SLICE AND PROCESS FRAMES, GROUPING BY SHEET ---
        foreach (var sheetInfo in config.Sheets)
        {
            Console.WriteLine($"Processing {sheetInfo.Path}...");
            using (Image sourceSheet = Image.Load(sheetInfo.Path))
            {
                int cols = sourceSheet.Width / sheetInfo.Frame_Width;
                int rows = sourceSheet.Height / sheetInfo.Frame_Height;

                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < cols; x++)
                    {
                        var sliceRect = new Rectangle(x * sheetInfo.Frame_Width, y * sheetInfo.Frame_Height, sheetInfo.Frame_Width, sheetInfo.Frame_Height);
                        using (Image frame = sourceSheet.Clone(ctx => ctx.Crop(sliceRect)))
                        {
                            var standardizedFrame = new Image<Rgba32>(targetSize.Width, targetSize.Height);
                            var pastePoint = new Point((targetSize.Width - frame.Width) / 2, (targetSize.Height - frame.Height) / 2);
                            standardizedFrame.Mutate(ctx => ctx.DrawImage(frame, pastePoint, 1f));

                            // Add the processed frame to its own sheet's list
                            sheetInfo.ProcessedFrames.Add(standardizedFrame);
                        }
                    }
                }
            }
        }

        Console.WriteLine("All frames processed. Assembling final spritesheet...");

        // --- 2. ASSEMBLE THE FINAL SPRITESHEET WITH ONE SHEET PER LINE ---
        var sheetsWithFrames = config.Sheets.Where(s => s.ProcessedFrames.Any()).ToList();
        if (sheetsWithFrames.Any())
        {
            // The width of our final image is determined by the sheet with the most frames.
            int maxWidthInFrames = sheetsWithFrames.Max(s => s.ProcessedFrames.Count);
            int finalWidth = maxWidthInFrames * targetSize.Width;

            // The height is the number of sheets multiplied by the frame height.
            int finalHeight = sheetsWithFrames.Count * targetSize.Height;

            using (var finalSheet = new Image<Rgba32>(finalWidth, finalHeight))
            {
                // Loop through each sheet to create each row.
                for (int sheetIndex = 0; sheetIndex < sheetsWithFrames.Count; sheetIndex++)
                {
                    var sheet = sheetsWithFrames[sheetIndex];
                    // Loop through the frames in the current sheet.
                    for (int frameIndex = 0; frameIndex < sheet.ProcessedFrames.Count; frameIndex++)
                    {
                        // Calculate where to paste the frame.
                        int pasteX = frameIndex * targetSize.Width;
                        int pasteY = sheetIndex * targetSize.Height;
                        var pastePoint = new Point(pasteX, pasteY);

                        finalSheet.Mutate(ctx => ctx.DrawImage(sheet.ProcessedFrames[frameIndex], pastePoint, 1f));
                    }
                }

                // --- 3. SAVE THE RESULT ---
                finalSheet.Save(config.Output_File);
                Console.WriteLine("Final spritesheet saved as output_spritesheet.png");
            }
        }

        // Clean up memory
        foreach (var sheet in config.Sheets)
        {
            sheet.ProcessedFrames.ForEach(img => img.Dispose());
        }
    }
}