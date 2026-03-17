using Prevly.WorkerService.Models;

namespace Prevly.WorkerService.Interfaces;

public interface IEmailContentParserService
{
    ParsedMonitoredEmailData Parse(EmailMessageInfo message);
}
