using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVGA25_Datatransformer
{
    internal class Producer
	{
		private IConnection? _connection;
		private IChannel? _channel;

        public async Task InitializeQueue(){
			ConnectionFactory factory = new ConnectionFactory();
			factory.UserName = "peremil100";
			factory.Password = "peremil100_rmq_password";
			factory.VirtualHost = "vh_peremil100";
			factory.HostName = "vortex.cse.kau.se";

			_connection = await factory.CreateConnectionAsync();
			_channel = await _connection.CreateChannelAsync();
		}
		public async Task PublishToQueue(string fileName, string routing_key)
		{//changed q-function to not include q-name since it works on the exchange and gets correctly forwarded on routing key
			try
            {
				if(_channel == null) {
					throw new InvalidOperationException("Producer is null");
				}

                string exchange_name = "peremil100_ex";                
                string message = File.ReadAllText(@fileName);
                var props = new BasicProperties();
                props.ContentType = "text/plain";

                byte[] messageBodyBytes = System.Text.Encoding.UTF8.GetBytes(message);
                await _channel.BasicPublishAsync(exchange_name, routing_key, false, props, messageBodyBytes);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Error: " + ex.Message);
                throw;
            }

        }

		public async ValueTask DisposeAsync()
		{
			if (_channel != null)
			{
				await _channel.CloseAsync();
				await _channel.DisposeAsync();
			}

			if (_connection != null)
			{
				await _connection.CloseAsync();
				await _connection.DisposeAsync();
			}
		}
	}
}
