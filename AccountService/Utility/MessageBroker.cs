using RabbitMQ.Client;


namespace AccountService.Utility{
    public class MessageBroker
    {
        private readonly ConnectionFactory _factory;

        public MessageBroker()
        {
            _factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };
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
    }

}