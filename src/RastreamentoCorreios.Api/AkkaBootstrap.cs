﻿using System.Net;
using Akka.Cluster.Hosting;
using Akka.Configuration;
using Akka.Discovery.Config.Hosting;
using Akka.Hosting;
using Akka.Management;
using Akka.Management.Cluster.Bootstrap;
using Akka.Remote.Hosting;
using RastreamentoCorreios.Api.Config;
using Serilog.Core;
using ClusterOptions = RastreamentoCorreios.Api.Config.ClusterOptions;

namespace RastreamentoCorreios.Api;

public static class AkkaBootstrap
{
    public static AkkaConfigurationBuilder BootstrapNetwork(this AkkaConfigurationBuilder builder,
        IConfiguration configuration,
        string role, Logger logger)
    {
        #region Environment variable setup
        
        var options = GetEnvironmentVariables(configuration, logger);
        var remoteOptions = new RemoteOptions
        {
            HostName = "0.0.0.0",
            Port = 5213, 
        };
        var clusterOptions = new Akka.Cluster.Hosting.ClusterOptions
        {
            MinimumNumberOfMembers = 1,
            SeedNodes = new[] { $"akka.tcp://{role}@localhost:5213" },
            Roles = new[] { role }
        };
        var managementOptions = new AkkaManagementOptions
        {
            HostName = options.Ip ?? Dns.GetHostName(),
            Port = options.Discovery.ManagementPort,
        };
        
        var bootstrapOptions = new ClusterBootstrapOptions
        {
            ContactPointDiscovery =
            {
                ServiceName = options.Discovery.ServiceName, 
                PortName = options.Discovery.PortName, 
                StableMargin = TimeSpan.FromSeconds(5), 
                ContactWithAllContactPoints = true
            }
        };
        
        // Clear seed nodes if we're using Config or Kubernetes Discovery
        if (options.StartupMethod is StartupMethod.ConfigDiscovery or StartupMethod.KubernetesDiscovery )
        {
            clusterOptions.SeedNodes = null;
            options.Seeds = null;
        }
        
        // Setup remoting
        // Reads environment variable CLUSTER__PORT
        if (options.Port is not null)
        {
            logger.Information("From environment: PORT: {Port}", options.Port);
            remoteOptions.Port = options.Port;
        }
        else
        {
            logger.Information("From environment: PORT: NULL. Using tcp port: {Port}", remoteOptions.Port);
        }
        
        // Reads environment variable CLUSTER__IP
        if (options.Ip is not null)
        {
            var ip = options.Ip.Trim();
            remoteOptions.PublicHostName = ip;
            logger.Information("From environment: IP: {Ip}", ip);
        }
        else if (options.IsDocker)
        {
            var host = Dns.GetHostName();
            logger.Information("From environment: IP NULL, running in docker container, defaulting to: {Host}", host);
            remoteOptions.PublicHostName = host.ToHocon();
        }
        else
        {
            logger.Information("From environment: IP NULL, not running in docker container, defaulting to: localhost");
            remoteOptions.PublicHostName = "localhost";
        }
        
        if (options.Seeds is not null)
        {
            var seeds = string.Join(",", options.Seeds.Select(s => s.ToHocon()));
            clusterOptions.SeedNodes = options.Seeds;
            logger.Information("From environment: SEEDS: [{Seeds}]", seeds);
        }
        else
        {
            logger.Information("From environment: SEEDS: NULL, using seeds: [{Seeds}]", string.Join(", ", clusterOptions.SeedNodes ?? new []{ "" }));
        }
        
        #endregion
        
        switch (options.StartupMethod)
        {
            case StartupMethod.SeedNodes:
                return SeedNodes(builder, configuration, logger, remoteOptions, clusterOptions);
                
            case StartupMethod.ConfigDiscovery:
                ConfigDiscovery(builder, logger, options);
                break;
            default:
                throw new ConfigurationException($"From environment: Unknown startup method: {options.StartupMethod}");
        }

        builder
            .AddHocon(configuration.GetSection("Akka"), HoconAddMode.Prepend)
            .WithRemoting(remoteOptions)
            .WithClustering(clusterOptions)
            .WithAkkaManagement(managementOptions)
            // Add Akka.Management.Cluster.Bootstrap support
            .WithClusterBootstrap(bootstrapOptions, autoStart: true);
        
        return builder;
    }

    private static AkkaConfigurationBuilder SeedNodes(AkkaConfigurationBuilder builder, IConfiguration configuration,
        Logger logger, RemoteOptions remoteOptions, Akka.Cluster.Hosting.ClusterOptions clusterOptions)
    {
        // No need to setup seed based cluster
        logger.Information("From environment: Forming cluster using seed nodes");
        return builder
            .AddHocon(configuration.GetSection("Akka"), HoconAddMode.Prepend)
            .WithRemoting(remoteOptions)
            .WithClustering(clusterOptions);
    }

    private static void ConfigDiscovery(AkkaConfigurationBuilder builder, Logger logger, ClusterOptions options)
    {
        logger.Information("From environment: Forming cluster using Akka.Discovery.Config");

        if (options.Discovery.ConfigEndpoints is null)
            throw new ConfigurationException(
                "Cluster start up is set to configuration discovery but discovery endpoints is null");

        var endpoints = string.Join(',', options.Discovery.ConfigEndpoints.Select(s => s.ToHocon()));
        logger.Information("From environment: Using config based discovery endpoints: [{Endpoints}]", endpoints);

        builder.WithConfigDiscovery(new ConfigServiceDiscoveryOptions
        {
            Services = new List<Service>
            {
                new()
                {
                    Name = options.Discovery.ServiceName,
                    Endpoints = options.Discovery.ConfigEndpoints.ToArray()
                }
            }
        });


    }

    private static ClusterOptions GetEnvironmentVariables(IConfiguration configuration, Logger logger)
    {
        var section = configuration.GetSection("Cluster");
        if(!section.GetChildren().Any())
        {
            logger.Warning("Skipping environment variable bootstrap. No 'CLUSTER' section found");
            return new ClusterOptions();
        }
        
        var options = section.Get<ClusterOptions>();
        if (options is null)
        {
            logger.Warning($"Skipping environment variable bootstrap. Could not bind IConfiguration to '{nameof(ClusterOptions)}'");
            return new ClusterOptions();
        }
        
        return options;
    }
}