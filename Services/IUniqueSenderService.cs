using GmailHandler.Models;

namespace GmailHandler.Services
{
    public interface IUniqueSenderService
    {
        List<UniqueSenderModel> GetUniqueSenders(int max_length_message);
    }
}
