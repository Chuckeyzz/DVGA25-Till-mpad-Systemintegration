using DATATRANFORMERSERVICE;
using DVGA25_Datatransformer;

internal class Worker : BackgroundService
{
	private readonly ILogger<Worker> _logger;
	private readonly Consumer _consumer;
	private readonly Producer _producer;
	private readonly dataTransformerService _service;


	public Worker(ILogger<Worker> logger, Consumer consumer, dataTransformerService service, Producer producer)
	{
		_logger = logger;
		_consumer = consumer;
		_producer = producer;
		_service = service;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await _producer.InitializeQueue();
		_logger.LogInformation("Producer queueu initialized");
		_consumer.MessageProcessed += messageConsumed;
		await _consumer.ReceiveAsync();
	}

	private async void messageConsumed()
	{
		try
		{
			_logger.LogInformation("Message processed by consumer");
			await _service.automaticTransformation();
			_logger.LogInformation("Message transformed");
			//messages never arrive at queues
			await _service.ExportToQueue();
			_logger.LogInformation("Message exported to queue");
		}
		catch (Exception e){
			_logger.LogError(e, "Error during datatransofmration");
		}
	}
}