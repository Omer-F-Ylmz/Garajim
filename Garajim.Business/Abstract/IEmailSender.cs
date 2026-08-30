namespace Garajim.Business.Abstract
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string body);
    }
}
