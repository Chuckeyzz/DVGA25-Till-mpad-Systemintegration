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
        public async Task PublishToQueue(string fileName, string queue, string routing_key)
        {
            try
            {
                ConnectionFactory factory = new ConnectionFactory();
                factory.UserName = "peremil100";
                factory.Password = "peremil100_rmq_password";
                factory.VirtualHost = "vh_peremil100";
                factory.HostName = "vortex.cse.kau.se";

                IConnection conn = await factory.CreateConnectionAsync();

                IChannel channel = await conn.CreateChannelAsync();
                string exchange_name = "peremil100_ex";
                //ToDO fixa Exchange, kö och binding
                
                string message = File.ReadAllText(@fileName);
                var props = new BasicProperties();
                props.ContentType = "text/plain";

                byte[] messageBodyBytes = System.Text.Encoding.UTF8.GetBytes(message);
                await channel.BasicPublishAsync(exchange_name, routing_key, false, props, messageBodyBytes);

                await channel.CloseAsync();
                await conn.CloseAsync();
                await channel.DisposeAsync();
                await conn.DisposeAsync();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                throw;
            }
        }
    }
}
