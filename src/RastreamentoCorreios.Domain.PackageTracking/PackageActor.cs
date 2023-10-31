using Akka.Actor;
using Akka.Persistence;
using RastreamentoCorreios.Domain.Common;

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
        
        
        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is PackageState state)
            {
                _state = state;
            }
        });

        Recover<IPackageEvent>(productEvent =>
        {
            _state = _state.ProcessEvent(productEvent);
        });
        
        
        Command<PackageQueries.GetPackage>(t =>
        {
            Sender.Tell(_state);

            if (_state.Status is PackageStatus.NotTracked
                || 
                // Allow to "force" sync again after 10 min of last request
                (_state.LastSyncRequest is not null
                && _state.LastSyncRequest.Value.AddMinutes(10) < DateTimeOffset.Now)
                && _state.Status is not (PackageStatus.Delivered or PackageStatus.Lost))
            {
                Self.Tell(new PackageCommands.Track(_state.PackageCode));
            }
        });

        
        Command<IPackageCommand>(cmd =>
        {
            var response = _state.ProcessCommand(cmd, scraperWorkerPool);

            if (response.Events.Count > 0)
            {
                PersistAll(response.Events, packageEvent =>
                {
                    _state = _state.ProcessEvent(packageEvent);
                
                    if(LastSequenceNr % 10 == 0)
                        SaveSnapshot(_state);
                });
            }
        });
        
        Command<SaveSnapshotSuccess>(success =>
        {
            
        });
    }
    
    public static Props Props(string packageCode, IActorRef scraperWorkerPool) => Akka.Actor.Props.Create<PackageActor>(packageCode, scraperWorkerPool);

    public ITimerScheduler Timers { get; set; }
}

