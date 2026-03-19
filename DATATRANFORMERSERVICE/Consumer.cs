using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVGA25_Datatransformer
{
	internal class Consumer
	{
		public Consumer() { }
		public Consumer(string name) { }
		public event Action? MessageProcessed;

		public async Task ReceiveAsync()
		{
			try
			{
				ConnectionFactory factory = new ConnectionFactory();
				//LAB3: använd dina egna uppgifter:
				factory.UserName = "peremil100";
				factory.Password = "peremil100_rmq_password";
				factory.VirtualHost = "vh_peremil100";
				factory.HostName = "vortex.cse.kau.se";
				//skapa connection:
				IConnection conn = await factory.CreateConnectionAsync();
				//skapa channel:
				IChannel channel = await conn.CreateChannelAsync();
				//LAB3: använd dina egna uppgifter:
				string exchange_name = "peremil100_ex";
				string queue = "peremil100_MASTER";
				string routing_key = "MASTER";

				//LAB3: skapa exchange, kö och binding:
				//skriv rätt anrop till funktionerna nedan! ******
				await channel.ExchangeDeclareAsync(exchange_name, ExchangeType.Direct);

				//parameters from queue declaration in order is
				//queue name, durable (Q doesn't survive restart), exclusive (Q is not restricted to this connection),
				//autoDelete (RabbitMQ will not delete Q after last consumer unsubscribes), arguments (such as TTL, max_len, dead-letter exchange)
				await channel.QueueDeclareAsync(queue, false, false, false, null);

				//bind queue and exchange together
				await channel.QueueBindAsync(queue, exchange_name, routing_key, null);
				//************************************************

				var consumer = new AsyncEventingBasicConsumer(channel);
				var message = String.Empty;
				consumer.ReceivedAsync += async (model, ea) =>
				{
					byte[] body = ea.Body.ToArray();
					message = Encoding.UTF8.GetString(body);

					try
					{
						//save new "database":
						StreamWriter sw = new StreamWriter(@"Data\updatedMaster.csv");
						sw.Write(message);
						sw.Close();
						MessageProcessed?.Invoke();
					}
					catch (Exception e)
					{
						//MessageBox.Show("Error:" + e.Message);
					}

					await channel.BasicAckAsync(ea.DeliveryTag, false);

				};

				await channel.BasicConsumeAsync(queue: queue, autoAck: false, consumer: consumer);
			}

			catch (RabbitMQClientException e)
			{

				//MessageBox.Show("Error: " + e.Message.ToString());
				throw;
			}
		}
	}

}