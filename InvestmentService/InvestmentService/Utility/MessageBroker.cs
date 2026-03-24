using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

using InvestmentService.Models;
using InvestmentService.Data;

namespace InvestmentService.Utility{
    public class MessageBroker{
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

        public async Task PublishEvent(string Exchange, string RoutingKey, string Message){

            using var connection = await _factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            var body = System.Text.Encoding.UTF8.GetBytes(Message);

            await channel.ExchangeDeclareAsync(
                exchange: Exchange,
                type: "direct");


            await channel.BasicPublishAsync(
                exchange: Exchange,
                routingKey: RoutingKey,
                body: body);
        }

        public async Task ConsumeUserCreatedEvent(string exchange,string routingKey, string _queue){

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
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                var investmentEvent = JsonSerializer.Deserialize<InvestmentRespondEvent>(message);

                using var scope = _serviceProvider.CreateScope();
                var investmentService = scope.ServiceProvider.GetRequiredService<InvestmentServicex>();

                await investmentService.HandlePaymentServiceResponse(investmentEvent!);

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            await channel.BasicConsumeAsync(
                queue: _queue,
                autoAck: false,
                consumer: consumer);
        }
    }

    public class RabbitMQConsumerService : BackgroundService{
        private readonly IServiceProvider _serviceProvider;

        public RabbitMQConsumerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var broker = new MessageBroker(scope.ServiceProvider);
                Console.WriteLine("running rabbitmq consumer");
                await broker.ConsumeUserCreatedEvent("payment.events", "payment.respond", "investment.payment.response.q");
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
        }
    }
}