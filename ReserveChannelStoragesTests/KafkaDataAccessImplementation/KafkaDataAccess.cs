using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Generator;
using ReserveChannelStoragesTests.BinarySerializers;
using ReserveChannelStoragesTests.JsonSerializers;
using static ReserveChannelStoragesTests.Filters;
using static ReserveChannelStoragesTests.Telemetry.TelemetryService;

namespace ReserveChannelStoragesTests.KafkaDataAccessImplementation
{
    //docker run -p 2181:2181 -p 9092:9092 --env ADVERTISED_HOST=`localhost` --env ADVERTISED_PORT=9092 spotify/kafka
    // TODO https://github.com/confluentinc/confluent-kafka-dotnet/blob/master/examples/Consumer/Program.cs
    public class KafkaDataAccess : IDataAccess<MessageData, Unit, Unit>, IDisposable
    {
        private ProducerConfig producerConfig = new ProducerConfig { BootstrapServers = "localhost:9092" };

        private ConsumerConfig consumerConfig = new ConsumerConfig
                                                {
                                                    GroupId = "test-consumer-group",
                                                    BootstrapServers = "localhost:9092", 
                                                    AutoOffsetReset = AutoOffsetReset.Earliest,
                                                    EnablePartitionEof = true,
                                                    
                                                };

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


        public async Task<Unit> Add(MessageData @object, CancellationToken token)
        {
            async Task<Unit> Func()
            {
                await this.producer.ProduceAsync(topicName, new Message<Null, string> { Value = this.jsonService.Serialize(@object) });
                return Unit.Value;
            }

            return await MeasureIt(Func);
        }


        public Task<MessageData> Get(Unit key, CancellationToken token)
        {
            this.consumer.Subscribe(topicName);
            try
            {
                var consumeResult = this.consumer.Consume(token);
                this.consumer.Commit(consumeResult);
                return Task.FromResult(this.jsonService.Deserialize<MessageData>(consumeResult.Value));
            }
            catch (ConsumeException e)
            {
                Console.WriteLine(e);
            }

            return null;
        }


        public Task<List<MessageData>> GetBatch(int count, CancellationToken token)
        {
            Task<List<MessageData>> Func() => this.GetAllInternal(count);
            return MeasureIt(Func);
        }


        public Task<List<MessageData>> GetAll(Unit key, CancellationToken token)
        {
            Task<List<MessageData>> Func() => this.GetAllInternal();
            return MeasureIt(Func);
        }


        private Task<List<MessageData>> GetAllInternal(int? count = null)
        {
            var result = new List<MessageData>();

            this.consumer.Subscribe(topicName);

            while (true)
            {
                try
                {
                    var consumeResult = this.consumer.Consume(TimeSpan.FromSeconds(1));

                    if (consumeResult is null) continue;
                    if (consumeResult.IsPartitionEOF) break;

                    result.Add(this.jsonService.Deserialize<MessageData>(consumeResult.Value));

                    if (count.HasValue && result.Count >= count) break;
                }
                catch (ConsumeException e)
                {
                    Console.WriteLine(e);
                    break;
                }
            }

            return Task.FromResult(result);
        }


        public Task<bool> Delete(Unit key, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }


        public Task<bool> DeleteBatch(IEnumerable<Unit> keys, CancellationToken token)
        {
            throw new NotImplementedException();
        }


        public Task<List<MessageData>> GetAllByCondition(Unit key, CancellationToken token)
        {
            var result = new List<MessageData>();

            this.consumer.Subscribe(topicName);

            var isEnd = false;
            while (!isEnd)
            {
                try
                {
                    var consumeResult = this.consumer.Consume(TimeSpan.FromSeconds(1));
                    isEnd = consumeResult.IsPartitionEOF;

                    var data = this.jsonService.Deserialize<MessageData>(consumeResult.Value);

                    if (IsFiltersPassed(data))
                        result.Add(data);
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