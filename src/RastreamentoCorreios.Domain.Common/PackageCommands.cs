namespace RastreamentoCorreios.Domain.Common;


public interface IPackageCommand : IWithTrackingCode
{
}
public static class PackageCommands
{
    public sealed record Track(string TrackingCode) : IPackageCommand;
    
    public sealed record SendSuccessfulScrape(string TrackingCode, List<StatusEntry> Entries) : IPackageCommand;
    
    public sealed record SendNotFoundScrape(string TrackingCode) : IPackageCommand;
    
    public sealed record SendErrorScrape(string TrackingCode, string Message) : IPackageCommand;
}