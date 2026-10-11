namespace Bloxstrap.Models.Persistable
{
    public class WindowPermissions
    {
        public List<long> Allowed { get; set; } = new();

        public List<long> Blocked { get; set; } = new();

        public List<long> Used { get; set; } = new();

        public Dictionary<long, List<string>> Disabled { get; set; } = new();
    }
}
