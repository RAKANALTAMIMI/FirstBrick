using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

using PaymentService.Models;
using PaymentService.Data;

namespace PaymentService.Utility
{
    // ✅ ADDED: plain model, no "required" keyword (breaks System.Text.Json)
    public class InvestmentCreatedEvent
    {
        public int investmentid { get; set; }
        public int userid { get; set; }
        public decimal amount { get; set; }
    }

    public class MessageBroker
    {
        private readonly ConnectionFactory _factory;
        private readonly IServiceProvider _serviceProvider;

        public MessageBroker(IServiceProvider serviceProvider)
        {
            _factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            _serviceProvider = serviceProvider;
        }

        public async Task PublishEvent(string Exchange, string RoutingKey, string Message)
        {
            using var connection = await _factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            var body = Encoding.UTF8.GetBytes(Message);

            await channel.ExchangeDeclareAsync(
                exchange: Exchange,
                type: "direct");

            await channel.BasicPublishAsync(
                exchange: Exchange,
                routingKey: RoutingKey,
                body: body);
        }

        public async Task ConsumeInvestmentCreatedEvent(string exchange, string routingKey, string _queue)
        {
            var connection = await _factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: "direct");

            await channel.QueueDeclareAsync(
                queue: _queue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await channel.QueueBindAsync(
                queue: _queue,
                exchange: exchange,
                routingKey: routingKey);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    Console.WriteLine($"Raw message: {message}");

                    var investmentEvent = JsonSerializer.Deserialize<InvestmentCreatedEvent>(message);

                    if (investmentEvent == null)
                    {
                        Console.WriteLine("Deserialization returned null");
                        await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                        return;
                    }

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var paymentService = scope.ServiceProvider.GetRequiredService<PaymentServicex>();
                        await paymentService.HandleInvestments(investmentEvent.userid, investmentEvent.amount, investmentEvent.investmentid);
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Consumer failed: {ex.Message}");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
            };

            await channel.BasicConsumeAsync(
                queue: _queue,
                autoAck: false,
                consumer: consumer);
        }

        public async Task ConsumeUserCreatedEvent(string exchange, string routingKey, string _queue)
        {
            var connection = await _factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: "direct");

            await channel.QueueDeclareAsync(
                queue: _queue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await channel.QueueBindAsync(
                queue: _queue,
                exchange: exchange,
                routingKey: routingKey);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    // var userId = int.Parse(message);

                    // Deserialize the event if needed

                    if (!int.TryParse(message, out int userId))
                    {
                        Console.WriteLine("Deserialization returned null");
                        await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                        return;
                    }

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var paymentService = scope.ServiceProvider.GetRequiredService<PaymentServicex>();
                        await paymentService.CreateWallet(userId);
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Consumer failed: {ex.Message}");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
            };

            await channel.BasicConsumeAsync(
                queue: _queue,
                autoAck: false,
                consumer: consumer);
        }
    }

    public class RabbitMQConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public RabbitMQConsumerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken){
        using (var scope = _serviceProvider.CreateScope())
        {
            var broker = new MessageBroker(_serviceProvider);
            Console.WriteLine("running rabbitmq consumers");

            var investmentConsumer = broker.ConsumeInvestmentCreatedEvent(
                "investment.events", "investment.created", "payment.investment.created.q");

            var userConsumer = broker.ConsumeUserCreatedEvent(
                "user.events", "user.created", "user.created.q");

            await Task.WhenAll(investmentConsumer, userConsumer);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }    
    
    }
}