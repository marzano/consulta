using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPTrans.StatusCartaoPersonalizado.Domain.Models.Configuration;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace SPTrans.StatusCartaoPersonalizado.Configurations.Factories
{
    public interface IMessagingFactory
    {
        IModel ConfigureConsumer();
        IModel ConfigureProducer(string exchange, string type);
    }

    public class MessagingFactory : IMessagingFactory
    {
        private readonly IConnectionFactory _connectionFactory;

        private IConnection _consumerConnection;
        private IConnection _producerConnection;


        private IModel _consumerChannel;

        private readonly Messaging _messaging;
        private readonly ILogger<MessagingFactory> _logger;

        public MessagingFactory(
            IOptions<Messaging> messaging,
            ILogger<MessagingFactory> logger)
        {
            _messaging = messaging.Value ?? throw new ArgumentNullException(nameof(messaging));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _connectionFactory = new ConnectionFactory()
            {
                HostName = _messaging.Host,
                Port = _messaging.Port,
                UserName = _messaging.User,
                Password = _messaging.Password,
                VirtualHost = _messaging.Consuming.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                RequestedChannelMax = 2000
            };

        }

        public IModel ConfigureConsumer()
        {
            if (_consumerChannel != null)
            {
                return _consumerChannel;
            }

            _logger.LogInformation($"RABBITMQ | CREATING CONSUMER CONNECTION");
            _consumerConnection = _connectionFactory.CreateConnection();

            _logger.LogInformation($"RABBITMQ | CREATING CONSUMER MODEL");
            _consumerChannel = _consumerConnection.CreateModel();

            // Creating of error queues, when the microservice can't process a message for an determined amount of tries, it goes to this queue
            _logger.LogInformation($"RABBITMQ | CREATING ERROR EXCHANGE: {_messaging.Error.Exchange}");
            _consumerChannel.ExchangeDeclare(_messaging.Error.Exchange, ExchangeType.Direct, true);

            _logger.LogInformation($"RABBITMQ | CREATING ERROR QUEUE: {_messaging.Error.Queue}");
            _consumerChannel.QueueDeclare(_messaging.Error.Queue, true, false, false, null);

            _logger.LogInformation($"RABBITMQ | BINDING ERROR EXCHANGE AND QUEUE");
            _consumerChannel.QueueBind(_messaging.Error.Queue, _messaging.Error.Exchange, _messaging.Error.Routingkey);

            // The deadletter of this microservices' queue, when it can't process a message, that message goes to deadletter, after an amount of seconds, that message comes back to the original queue to be reprocessed
            _logger.LogInformation($"RABBITMQ | CREATING DEADLETTER EXCHANGE: {_messaging.Consuming.Deadletter.Exchange}");
            _consumerChannel.ExchangeDeclare(_messaging.Consuming.Deadletter.Exchange, ExchangeType.Topic, true);

            _logger.LogInformation($"RABBITMQ | CREATING DEADLETTER QUEUE: {_messaging.Consuming.Deadletter.Queue}");
            _consumerChannel.QueueDeclare(_messaging.Consuming.Deadletter.Queue, true, false, false, new Dictionary<string, object>()
            {
                { "x-dead-letter-exchange", _messaging.Consuming.Exchange },
                { "x-dead-letter-routing-key", _messaging.Consuming.Bindingkey },
                { "x-message-ttl", _messaging.TTL }
            });

            _logger.LogInformation($"RABBITMQ | BINDING DEADLETTER EXCHANGE AND QUEUE");
            _consumerChannel.QueueBind(_messaging.Consuming.Deadletter.Queue, _messaging.Consuming.Deadletter.Exchange, _messaging.Consuming.Deadletter.Routingkey);

            // The queue this microservices will watch for new messages
            _logger.LogInformation($"RABBITMQ | CREATING CONSUMING EXCHANGE: {_messaging.Consuming.Exchange}");
            _consumerChannel.ExchangeDeclare(_messaging.Consuming.Exchange, ExchangeType.Topic, true);

            _logger.LogInformation($"RABBITMQ | CREATING CONSUMING QUEUE: {_messaging.Consuming.Queue}");
            _consumerChannel.QueueDeclare(_messaging.Consuming.Queue, true, false, false, new Dictionary<string, object>()
            {
                { "x-dead-letter-exchange", _messaging.Consuming.Deadletter.Exchange },
                { "x-dead-letter-routing-key", _messaging.Consuming.Deadletter.Routingkey }
            });

            _consumerChannel.BasicQos(0, 50, true);

            _logger.LogInformation($"RABBITMQ | BINDING CONSUMING EXCHANGE AND QUEUE");
            _consumerChannel.QueueBind(_messaging.Consuming.Queue, _messaging.Consuming.Exchange, _messaging.Consuming.Bindingkey);

            return _consumerChannel;
        }

        public IModel ConfigureProducer(string exchange, string type)
        {
            if (_producerConnection == null || _producerConnection.IsOpen == false)
            {
                _producerConnection = _connectionFactory.CreateConnection();
                _logger.LogInformation($"RABBITMQ | CREATING PRODUCER CONNECTION");
            }

            _logger.LogInformation($"RABBITMQ | CREATING CHANNEL PRODUCER CONNECTION");
            var producerChannel = _producerConnection.CreateModel();

            _logger.LogInformation($"RABBITMQ | CREATING POSTING EXCHANGE: {exchange}");
            producerChannel.ExchangeDeclare(exchange, type, true);

            return producerChannel;
        }
    }
}