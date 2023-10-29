using Akka.Cluster.Sharding;
using RastreamentoCorreios.Domain.Common;

namespace RastreamentoCorreios.Domain.PackageTracking;

public sealed class PackageMessageExtractor : HashCodeMessageExtractor
{
    public PackageMessageExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
    {
    }

    public override string EntityId(object message)
    {
        if (message is IWithTrackingCode tc) return tc.TrackingCode;
        return null!;
    }
}