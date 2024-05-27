using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPTrans.StatusCartaoPersonalizado.Configurations.Factories;
using RabbitMQ.Client.Events;
using SPTrans.StatusCartaoPersonalizado.Domain.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Newtonsoft.Json;
using System.Dynamic;

namespace SPTrans.StatusCartaoPersonalizado.Domain.Services
{
    public interface IMessagingService
    {
        AsyncEventHandler<BasicDeliverEventArgs> AsyncDequeue(CancellationToken cancellationToken, Func<string, Task> callback);
        void Queue(string exchange, string routingKey, string message, Dictionary<string, object> headers = null, string type = ExchangeType.Topic);
        void Queue<T>(IEnumerable<T> messages, Dictionary<string, object> headers = null, string type = ExchangeType.Topic);
        void Queue<T>(string exchange, string routingKey, IEnumerable<T> messages, Dictionary<string, object> headers = null, string type = ExchangeType.Topic);
    }

    public class MessagingService : IMessagingService
    {
        private readonly IMessagingFactory _messagingFactory;
        private readonly Messaging _messaging;
        private readonly ILogger<MessagingService> _logger;

        public MessagingService(
            IMessagingFactory messagingFactory,
            IOptions<Messaging> messaging,
            ILogger<MessagingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messagingFactory = messagingFactory ?? throw new ArgumentNullException(nameof(messagingFactory));
            _messaging = messaging.Value ?? throw new ArgumentNullException(nameof(messaging));
        }

        public AsyncEventHandler<BasicDeliverEventArgs> AsyncDequeue(CancellationToken cancellationToken, Func<string, Task> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var channel = _messagingFactory.ConfigureConsumer();

            return async (model, ea) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("MESSAGING | RECEIVING NEW MESSAGE");

                var body = ea.Body.ToArray();
                string raw = Encoding.UTF8.GetString(body);
                var retries = 0;

                _logger.LogDebug($"MESSAGING | RAW MESSAGE: { raw }");

                try
                {
                    retries = Retries(ea);

                    _logger.LogDebug($"MESSAGING | THIS MESSAGE HAS BEEN PROCESSED { retries } TIMES");

                    if (_messaging.Retries > retries)
                    {
                        _logger.LogDebug($"MESSAGING | MESSAGE WAS SUCCESSFULLY DESERIALIZE");

                        await callback.Invoke(raw);
                    }
                    else
                    {
                        _logger.LogDebug($"MESSAGING | MESSAGE WAS PROCESSED TOO MANY TIMES, PUSHING TO ERROR QUEUE");

                        var headers = new Dictionary<string, object>
                        {
                            { "queue", _messaging.Consuming.Queue }
                        };

                        Queue(_messaging.Error.Exchange, _messaging.Error.Routingkey, raw, headers, ExchangeType.Direct);
                    }

                    _logger.LogDebug($"MESSAGING | MESSAGE HAS BEEN ACKED");

                    channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"MESSAGING | SOMETHING HAPPENED WHEN PROCESSING THE MESSAGE: { ex }");

                    if (_messaging.Retries > retries)
                    {
                        channel.BasicNack(ea.DeliveryTag, false, false);

                        _logger.LogDebug($"MESSAGING | MESSAGE HAS BEEN NACKED");
                    }
                    else
                    {
                        _logger.LogDebug($"MESSAGING | MESSAGE WAS PROCESSED TOO MANY TIMES, PUSHING TO ERROR QUEUE");

                        var headers = new Dictionary<string, object>
                        {
                            { "queue", _messaging.Consuming.Queue }
                        };

                        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(raw);
                        obj.Exception = ex;
                        var rawEx = JsonConvert.SerializeObject(obj);

                        Queue(_messaging.Error.Exchange, _messaging.Error.Routingkey, rawEx, headers, ExchangeType.Direct);

                        channel.BasicAck(ea.DeliveryTag, false);
                    }
                }
            };
        }
        
        public void Queue<T>(IEnumerable<T> messages, Dictionary<string, object> headers = null, string type = ExchangeType.Topic)
        {
            Queue(_messaging.Publishing.Exchange, _messaging.Publishing.Routingkey, messages, headers, type);
        }

        public void Queue(string exchange, string routingKey, string message, Dictionary<string, object> headers = null, string type = ExchangeType.Topic)
        {
            Queue(exchange, routingKey, new List<string>() { message }, headers, type);
        }

        public void Queue<T>(string exchange, string routingKey, IEnumerable<T> objects, Dictionary<string, object> headers = null, string type = ExchangeType.Topic) 
        {
            var channel = _messagingFactory.ConfigureProducer(exchange, type);

            var properties = channel.CreateBasicProperties();
            properties.Headers = headers;
            properties.Persistent = true;
            properties.ContentType = "application/json;charset=utf-8";

            foreach (var obj in objects)
            {
                string message;

                if (obj is string)
                    message = obj as string;
                else
                    message = Newtonsoft.Json.JsonConvert.SerializeObject(obj);

                _logger.LogDebug($"MESSAGING | PUSHING '{ message }' TO ROUTING KEY '{ routingKey }' ON '{ exchange }' EXCHANGE");
                channel.BasicPublish(exchange, routingKey, false, properties, Encoding.UTF8.GetBytes(message));
            }

            _logger.LogDebug($"MESSAGING | MESSAGE WAS SUCCESSFULLY PUSHED");
            if (channel.IsOpen) 
                channel.Close();
        }

        private int Retries(BasicDeliverEventArgs ea)
        {
            int count = 0;

            try
            {
                if (ea.BasicProperties.Headers is Dictionary<string, object> dic && dic.ContainsKey("x-death"))
                {
                    if (ea.BasicProperties.Headers["x-death"] is List<object> xdeath)
                    {
                        if (xdeath.FirstOrDefault() is Dictionary<string, object> headers)
                        {
                            count = Convert.ToInt32(headers["count"]);
                        }
                    }
                }
            }
            catch
            {
                count = 1;
            }

            return ++count;
        }
    }
}