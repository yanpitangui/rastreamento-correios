namespace RastreamentoCorreios.Domain.Common;

public record StatusEntry
{
    public DateOnly Date { get; init; }
    
    public TimeOnly Time { get; init; }

    public string Status { get; init; } = null!;
    
    public string? Place { get; init; }
}