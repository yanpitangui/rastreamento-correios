using Akka.Actor;
using RastreamentoCorreios.Domain.Common;
using RastreamentoCorreios.Domain.Scraping;

namespace RastreamentoCorreios.Domain.PackageTracking;


public enum PackageStatus
{
    NotTracked,
    Tracking,
    Delivered,
    Lost,
    NotFound,
    Error,
}

public record PackageState
{
    public PackageStatus Status { get; init; }
    public required string PackageCode { get; init; }
    public List<StatusEntry> History { get; init; } = new();
    
    public DateTimeOffset? LastSyncRequest { get; init; }
    public DateTimeOffset? LastSyncResponse { get; init; }

    public StatusEntry? LastStatus => History.FirstOrDefault();
}

public static class PackageStateExtensions
{
    public static PackageCommandResponse ProcessCommand(this PackageState state, IPackageCommand command,
        IActorRef scraperWorkerPool)
    {
        switch (command)
        {
            case PackageCommands.Track track:
                var packageTracked = new PackageTracked(track.TrackingCode, DateTimeOffset.Now);
                var response = new PackageCommandResponse(track.TrackingCode, new IPackageEvent[]
                {
                    packageTracked
                });
                scraperWorkerPool.Tell(new ScraperCommands.ScrapePackage(state.PackageCode));
                return response;
            case PackageCommands.SendSuccessfulScrape successfulScrape:
                var packageUpdated = new PackageUpdated(command.TrackingCode, DateTimeOffset.Now,
                    successfulScrape.Entries);
                return new PackageCommandResponse(command.TrackingCode, new IPackageEvent[]
                {
                    packageUpdated
                });            
            case PackageCommands.SendNotFoundScrape:
                var packageNotFound = new PackageNotFound(command.TrackingCode, DateTimeOffset.Now);
                return new PackageCommandResponse(command.TrackingCode, new IPackageEvent[]
                {
                    packageNotFound
                });             
            case PackageCommands.SendErrorScrape:
                var packageErroed = new PackageError(command.TrackingCode, DateTimeOffset.Now);
                return new PackageCommandResponse(command.TrackingCode, new IPackageEvent[]
                {
                    packageErroed
                });
            default:
                return new PackageCommandResponse(command.TrackingCode, Array.Empty<IPackageEvent>(), false,
                    $"Package with [Id={command.TrackingCode}] is not ready to process command [{command}]");
        }
    }
    public static PackageState ProcessEvent(this PackageState state, IPackageEvent packageEvent)
    {
        switch (packageEvent)
        {
            case PackageTracked packageTracked:
                state = state with
                {
                    Status = PackageStatus.Tracking,
                    LastSyncRequest = packageTracked.Timestamp
                };
                break;
            
            case PackageUpdated packageUpdated:
                state = state with
                {
                    History = packageUpdated.Entries,
                    LastSyncResponse = packageUpdated.Timestamp
                };
                break;
            
            case PackageError error:
                state = state with
                {
                    Status = PackageStatus.Error,
                    LastSyncResponse = error.Timestamp
                };
                break;
            case PackageNotFound notFound:
                state = state with
                {
                    Status = PackageStatus.NotFound,
                    LastSyncResponse = notFound.Timestamp
                };
                break;
        }

        return state;
    }
    
}