using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using RastreamentoCorreios.Domain.Common;
using RastreamentoCorreios.Domain.Scraping;

namespace RastreamentoCorreios.Domain.PackageTracking;

public sealed class PackageActor : ReceivePersistentActor, IWithTimers
{
    public override string PersistenceId { get; }

    private PackageState _state;

    public PackageActor(string packageCode, IActorRef scraperWorkerPool)
    {

        PersistenceId = $"package-{packageCode}";
        _state = new()
        {
            PackageCode = packageCode,
            Status = PackageStatus.NotTracked
        };
        
        Command<PackageQueries.GetPackage>(t =>
        {
            Sender.Tell(_state);

            if (_state.Status is PackageStatus.NotTracked
                || 
                // Allow to "force" sync again after 10 min of last request
                (_state.LastSyncRequest is not null
                && _state.LastSyncRequest.Value.AddMinutes(10) > DateTimeOffset.Now)
                && _state.Status is not (PackageStatus.Delivered or PackageStatus.Lost))
            {
                Self.Tell(new PackageCommands.Track(_state.PackageCode));
                _state = _state with { LastSyncRequest = DateTimeOffset.Now, Status = PackageStatus.Tracking };
            }
        });

        
        Command<PackageCommands.Track>(t =>
        {
            scraperWorkerPool.Tell(new ScraperCommands.ScrapePackage(t.TrackingCode));
        });

        Command<List<StatusEntry>>(p =>
        {
            _state = _state with
            {
                History = p.ToList(),
                LastSyncResponse = DateTimeOffset.Now
            };
        });
    }
    
    public static Props Props(string packageCode, IActorRef scraperWorkerPool) => Akka.Actor.Props.Create<PackageActor>(packageCode, scraperWorkerPool);

    public ITimerScheduler Timers { get; set; }
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

public enum PackageStatus
{
    NotTracked,
    Tracking,
    Delivered,
    Lost,
    NotFound
}