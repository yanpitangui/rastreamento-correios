using System.Text.RegularExpressions;
using HtmlAgilityPack;
using RastreamentoCorreios.Domain.Common;

namespace RastreamentoCorreios.Domain.Scraping;

internal static partial class StatusEntryParser
{
    [GeneratedRegex(@"\d{1,2}/\d{1,2}/\d{4}", RegexOptions.Compiled)]
    private static partial Regex DateRegex();
    

    [GeneratedRegex(@"(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]", RegexOptions.Compiled)]
    private static partial Regex TimeRegex();
    
    
    private const string DatePattern = "dd/MM/yyyy";

    public static List<StatusEntry> Parse(HtmlDocument doc)
    {
        var notFound = doc.DocumentNode.SelectSingleNode("//*[@id=\"page\"]/main/div[4]/div/div/p");

        if (notFound != null)
        {
            return new List<StatusEntry>();
        }
            
        var list = doc.DocumentNode
            .SelectSingleNode("//*[@id=\"page\"]/main/div[4]/div/div/div[3]");

        var statuses = list.SelectNodes("ul");


        return ParseStatuses(statuses);
    }
    
    private static List<StatusEntry> ParseStatuses(HtmlNodeCollection nodes)
    {

        var list = new List<StatusEntry>();
        foreach (var node in nodes)
        {
            
            list.Add(ParseStatus(node));
        }

        return list;
    }

    private static StatusEntry ParseStatus(HtmlNode node)
    {
        var status = node.SelectSingleNode("li[1]").InnerText.AsSpan(7).Trim();
        var datetime = ParseDateTime(node.SelectSingleNode("li[2]").InnerText);
        var place = node.SelectSingleNode("li[3]").InnerText.AsSpan(6).Trim();

        return new StatusEntry
        {
            Date = datetime.date,
            Time = datetime.time,
            Status = status.ToString(),
            Place = place.ToString()
        };
    }
    
    private static (DateOnly date, TimeOnly time) ParseDateTime(string datetime)
    {
        var dateMatch = DateRegex().Match(datetime).ToString();
        var timeMatch = TimeRegex().Match(datetime).ToString();
        
        return (DateOnly.ParseExact(dateMatch, DatePattern), TimeOnly.Parse(timeMatch));
    }

}