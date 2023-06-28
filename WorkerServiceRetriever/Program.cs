using WorkerServiceRetriever;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddTransient<IVaultClient, VaultClient>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
