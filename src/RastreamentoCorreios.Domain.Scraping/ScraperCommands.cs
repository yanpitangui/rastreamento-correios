using RastreamentoCorreios.Domain.Common;

namespace RastreamentoCorreios.Domain.Scraping;

public static class ScraperCommands
{
    public record ScrapePackage(string TrackingCode) : IWithTrackingCode;
}