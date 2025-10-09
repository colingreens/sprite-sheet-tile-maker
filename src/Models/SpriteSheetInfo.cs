using System.Xml.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SpritesheetCollector.Models;

public record SpriteSheetInfo
{
    public string Path { get; set; } = "";
    public int Frame_Width { get; set; }
    public int Frame_Height { get; set; }
    [XmlIgnore]
    public List<Image<Rgba32>> ProcessedFrames { get; set; } = new();
}