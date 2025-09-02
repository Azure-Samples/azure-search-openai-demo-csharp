using Azure.Messaging.ServiceBus;

namespace MinimalApi.Services;

public interface IServiceBusSender : IAsyncDisposable
{
    Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default);
}

internal sealed class ServiceBusSenderAdapter(ServiceBusSender inner) : IServiceBusSender
{
    public Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default) =>
        inner.SendMessageAsync(message, cancellationToken);

    public ValueTask DisposeAsync() => inner.DisposeAsync();
}
