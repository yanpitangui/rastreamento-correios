using Akka.Actor;
using HtmlAgilityPack;
using RastreamentoCorreios.Domain.Common;

namespace RastreamentoCorreios.Domain.Scraping;

public sealed class ScraperActor : ReceiveActor
{
    private const string Url = "https://www.linkcorreios.com.br/?id={0}";

    public ScraperActor()
    {
        HtmlWeb html = new();

        Receive<ScraperCommands.ScrapePackage>((msg) =>
        {
            try
            {
                var doc = html.Load(string.Format(Url, msg.TrackingCode));

                var statuses = StatusEntryParser.Parse(doc);
                Sender.Tell(statuses);

                if (statuses.Count > 0)
                {
                    Sender.Tell(new PackageCommands.SendSuccessfulScrape(msg.TrackingCode, statuses));
                }
                else
                {
                    Sender.Tell(new PackageCommands.SendNotFoundScrape(msg.TrackingCode));
                }
            }
            catch (Exception ex)
            {
                Sender.Tell(new PackageCommands.SendErrorScrape(msg.TrackingCode, ex.Message));
            }
            
            
        });
        
    }
    
 
}