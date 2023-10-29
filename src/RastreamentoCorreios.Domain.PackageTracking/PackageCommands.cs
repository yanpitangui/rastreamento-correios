using RastreamentoCorreios.Domain.Common;

namespace RastreamentoCorreios.Domain.PackageTracking;

public static class PackageCommands
{
    public sealed record Track(string TrackingCode) : IWithTrackingCode;
}