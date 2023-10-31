using RastreamentoCorreios.Domain.Common;

namespace RastreamentoCorreios.Domain.PackageTracking;

public record PackageCommandResponse(string TrackingCode, IReadOnlyCollection<IPackageEvent> Events, bool Success = true, string Message = "") : IWithTrackingCode;