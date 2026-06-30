namespace ViabilityIQ.Web.Services
{
    public sealed class CanvasRequest
    {
        public string? HeaderIcon { get; init; }
        public string Title { get; set; } = string.Empty;
        public Type ComponentType { get; set; } = default;
        public Dictionary<string, object>? Parameters { get; set; } 
        public int Width { get; set; } = 700;
        public string? CssClass { get; init; }
    }
}
