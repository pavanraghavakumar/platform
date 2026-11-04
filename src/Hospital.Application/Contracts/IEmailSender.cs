namespace Hospital.Application.Contracts;

public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string textBody, CancellationToken cancellationToken = default);
}
