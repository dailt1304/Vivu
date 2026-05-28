namespace Vivu.Application.DTOs.Responses.Statistics;

public class RevenueStatsDto
{
    public decimal RevenueMTD { get; set; }
    public decimal RevenuePreviousMonth { get; set; }
    public int SuccessfulTransactions { get; set; }
    public int PendingTransactions { get; set; }
}
