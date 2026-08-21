using IdentityService.Api.Messaging;

namespace IdentityService.Api.Services;

public interface INotificationDeliveryService
{
    Task DeliverAsync(ReminderDueMessage message, CancellationToken cancellationToken);
}
