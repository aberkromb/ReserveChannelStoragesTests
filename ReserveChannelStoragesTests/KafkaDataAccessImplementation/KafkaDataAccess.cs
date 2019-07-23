using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Generator;
using ReserveChannelStoragesTests.BinarySerializers;
using ReserveChannelStoragesTests.JsonSerializers;
using static ReserveChannelStoragesTests.Condition;
using static ReserveChannelStoragesTests.Telemetry.TelemetryService;

namespace ReserveChannelStoragesTests.KafkaDataAccessImplementation
{
    //docker run -p 2181:2181 -p 9092:9092 --env ADVERTISED_HOST=`docker-machine ip \`docker-machine active\`` --env ADVERTISED_PORT=9092 spotify/kafka
    public class KafkaDataAccess : IDataAccess<KafkaDataObject, Unit>, IDisposable
    {
        private ProducerConfig producerConfig = new ProducerConfig { BootstrapServers = "localhost:9092" };
        private ConsumerConfig consumerConfig = new ConsumerConfig { GroupId = "test-consumer-group", BootstrapServers = "localhost:9092" , AutoOffsetReset = AutoOffsetReset.Earliest};

        private readonly IProducer<Null, string> producer;
        private readonly IConsumer<Null, string> consumer;
        private readonly IJsonService jsonService;
        private const string topicName = "reserve-channel";


        public KafkaDataAccess(IJsonService jsonService)
        {
            this.jsonService = jsonService;

            producer = new ProducerBuilder<Null, string>(this.producerConfig)
//                       .SetValueSerializer(new KafkaValueSerializer<MessageData>())
                .Build();

            consumer = new ConsumerBuilder<Null, string>(this.consumerConfig)
//                       .SetValueDeserializer(new KafkaValueSerializer<MessageData>())
                .Build();
        }


        public async Task<Unit> Add(KafkaDataObject @object, CancellationToken token)
        {
            Task<DeliveryResult<Null, string>> Func() => this.producer.ProduceAsync(topicName, new Message<Null, string> { Value = this.jsonService.Serialize(@object.Data) });

            await MeasureIt(Func);

            return Unit.Value;
        }


        public Task<KafkaDataObject> Get(Unit key, CancellationToken token)
        {
            this.consumer.Subscribe(topicName);
            try
            {
                var consumeResult = this.consumer.Consume(token);
                return Task.FromResult(new KafkaDataObject { Data = this.jsonService.Deserialize<MessageData>(consumeResult.Value) });
            }
            catch (ConsumeException e)
            {
                Console.WriteLine(e);
            }

            return null;
        }


        public Task<List<KafkaDataObject>> GetAll(Unit key, CancellationToken token)
        {
            var result = new List<KafkaDataObject>();

            this.consumer.Subscribe(topicName);

            var isEnd = false;
            while (!isEnd)
            {
                try
                {
                    var consumeResult = this.consumer.Consume(TimeSpan.FromSeconds(1));
                    
                    if(consumeResult is null) continue;

                    isEnd = consumeResult.IsPartitionEOF;
                    result.Add(new KafkaDataObject { Data = this.jsonService.Deserialize<MessageData>(consumeResult.Value) });
                }
                catch (ConsumeException e)
                {
                    Console.WriteLine(e);
                    isEnd = true;
                }
            }

            return Task.FromResult(result);
        }


        public Task<bool> Delete(Unit key, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }


        public Task<List<KafkaDataObject>> GetAllByCondition(Unit key, CancellationToken token)
        {
            var result = new List<KafkaDataObject>();

            this.consumer.Subscribe(topicName);

            var isEnd = false;
            while (!isEnd)
            {
                try
                {
                    var consumeResult = this.consumer.Consume(TimeSpan.FromSeconds(1));
                    isEnd = consumeResult.IsPartitionEOF;

                    var data = this.jsonService.Deserialize<MessageData>(consumeResult.Value);

                    if (Predicate(data))
                        result.Add(new KafkaDataObject { Data = data });
                }
                catch (ConsumeException e)
                {
                    Console.WriteLine(e);
                    isEnd = true;
                }
            }

            return Task.FromResult(result);
        }


        public void Dispose()
        {
            this.producer?.Dispose();
            this.consumer?.Dispose();
        }
    }

    public class KafkaValueSerializer<T> : ISerializer<T>, IDeserializer<T> where T : MessageData
    {
        private static readonly IBinarySerializer Serializer;

        static KafkaValueSerializer() => Serializer = new ZeroFormatterWrapper();

        public byte[] Serialize(T data, SerializationContext context) => Serializer.Serialize(data);

        public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) => isNull ? default : Serializer.Deserialize<T>(data.ToArray());
    }
}