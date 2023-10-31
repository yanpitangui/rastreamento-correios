using RastreamentoCorreios.Domain.Common;

namespace RastreamentoCorreios.Domain.PackageTracking;

public interface IPackageEvent : IWithTrackingCode;

public record PackageTracked(string TrackingCode, DateTimeOffset Timestamp) : IPackageEvent;

public record PackageUpdated(string TrackingCode, DateTimeOffset Timestamp, List<StatusEntry> Entries) : IPackageEvent;

public record PackageNotFound(string TrackingCode, DateTimeOffset Timestamp) : IPackageEvent;

public record PackageError(string TrackingCode, DateTimeOffset Timestamp) : IPackageEvent;