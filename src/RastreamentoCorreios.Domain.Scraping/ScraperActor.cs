using Akka.Actor;
using HtmlAgilityPack;

namespace RastreamentoCorreios.Domain.Scraping;

public sealed class ScraperActor : ReceiveActor
{
    private const string Url = "https://www.linkcorreios.com.br/?id={0}";

    public ScraperActor()
    {
        HtmlWeb html = new();

        Receive<ScraperCommands.ScrapePackage>((msg) =>
        {
            var doc = html.Load(string.Format(Url, msg.TrackingCode));

            var statuses = StatusEntryParser.Parse(doc);
            
            Sender.Tell(statuses);
        });
        
    }
    
 
}