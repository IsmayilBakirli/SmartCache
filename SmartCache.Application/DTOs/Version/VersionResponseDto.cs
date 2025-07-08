namespace SmartCache.Application.DTOs.Version
{
    public class VersionResponseDto
    {
        public string Module { get; set; }
        public bool HasChanged { get; set; }
        public int Version { get; set; } 
    }
}
