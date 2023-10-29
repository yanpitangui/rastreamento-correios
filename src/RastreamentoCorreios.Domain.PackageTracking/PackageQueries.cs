using RastreamentoCorreios.Domain.Common;

namespace RastreamentoCorreios.Domain.PackageTracking;

public static class PackageQueries
{
    public record GetPackage(string TrackingCode) : IWithTrackingCode;
}