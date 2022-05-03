﻿#if NET5_0_OR_GREATER
using Grpc.Net.Client;
#else
using Grpc.Core;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.DevelopmentKit.Common;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArmoniK.DevelopmentKit.GridServer.Client
{
  /// <summary>
  ///   The main object to communicate with the control Plane from the client side
  ///   The class will connect to the control plane to createSession, SubmitTask,
  ///   Wait for result and get the result.
  ///   See an example in the project ArmoniK.Samples in the sub project
  ///   https://github.com/aneoconsulting/ArmoniK.Samples/tree/main/Samples/GridServerLike
  ///   Samples.ArmoniK.Sample.SymphonyClient
  /// </summary>
  [MarkDownDoc]
  public class ArmonikDataSynapseClientService
  {
    private readonly Properties properties_;
    private ILogger<ArmonikDataSynapseClientService> Logger { get; set; }
    private Submitter.SubmitterClient ControlPlaneService { get; set; }


    /// <summary>
    /// Set or Get TaskOptions with inside MaxDuration, Priority, AppName, VersionName and AppNamespace
    /// </summary>
    private TaskOptions TaskOptions { get; set; }

    private ILoggerFactory LoggerFactory { get; set; }

    /// <summary>
    /// The ctor with IConfiguration and optional TaskOptions
    /// 
    /// </summary>
    /// <param name="loggerFactory">The factory to create the logger for clientService</param>
    /// <param name="properties">Properties containing TaskOption and connection string to the control plane</param>
    public ArmonikDataSynapseClientService(ILoggerFactory loggerFactory, Properties properties)
    {
      properties_   = properties;
      LoggerFactory = loggerFactory;
      Logger        = loggerFactory.CreateLogger<ArmonikDataSynapseClientService>();

      TaskOptions = properties_.TaskOptions;
    }

    /// <summary>
    /// Create the session to submit task
    /// </summary>
    /// <param name="taskOptions">Optional parameter to set TaskOptions during the Session creation</param>
    /// <returns></returns>
    public SessionService CreateSession(TaskOptions taskOptions = null)
    {
      if (taskOptions != null) TaskOptions = taskOptions;

      ControlPlaneConnection();

      Logger.LogDebug("Creating Session... ");

      return new SessionService(LoggerFactory,
                                ControlPlaneService,
                                TaskOptions);
    }

    private void ControlPlaneConnection()
    {
      //AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
      //                     true);

      var uri  = new Uri(properties_.ConnectionString);
      var conf = properties_.Configuration;
      Logger.LogInformation($"Connecting to armoniK  : {uri} port : {uri.Port}");
      Logger.LogInformation($"HTTPS Activated: {uri.Scheme == Uri.UriSchemeHttps}");


      var credentials = uri.Scheme == Uri.UriSchemeHttps ? new SslCredentials() : ChannelCredentials.Insecure;

#if NET5_0_OR_GREATER
      HttpClientHandler httpClientHandler  = null;
      var               controlPlanAddress = conf.GetSection("Grpc");

      if (conf.GetSection("Grpc").Exists())
      {
        httpClientHandler = new HttpClientHandler();


        if (controlPlanAddress.GetSection("SSLValidation").Exists() && controlPlanAddress["SSLValidation"] == "disable")
        {
          httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        var clientCertFilename = "";
        var clientKeyFilename  = "";

        if (controlPlanAddress.GetSection("ClientCert").Exists())
          clientCertFilename = controlPlanAddress["ClientCert"];

        if (controlPlanAddress.GetSection("ClientKey").Exists())
          clientKeyFilename = controlPlanAddress["ClientKey"];

        if (!(string.IsNullOrEmpty(clientKeyFilename) && string.IsNullOrEmpty(clientKeyFilename)))
        {
          var clientCertPem = File.ReadAllText(clientCertFilename);
          var clientKeyPem  = File.ReadAllText(clientKeyFilename);

          var cert = X509Certificate2.CreateFromPem(clientCertPem,
                                                    clientKeyPem);

          httpClientHandler.ClientCertificates.Add(cert);
        }
      }

      var channelOptions = new GrpcChannelOptions()
      {
        Credentials   = uri.Scheme == Uri.UriSchemeHttps ? new SslCredentials() : ChannelCredentials.Insecure,
        HttpHandler   = httpClientHandler,
        LoggerFactory = LoggerFactory,
      };

      var channel = GrpcChannel.ForAddress(controlPlanAddress["Endpoint"],
                                           channelOptions);

#else
      Environment.SetEnvironmentVariable("GRPC_DNS_RESOLVER",
                                         "native");

      var channel = new Channel($"{uri.Host}:{uri.Port}",
                                credentials);
#endif
      ControlPlaneService ??= new Submitter.SubmitterClient(channel);
    }

    /// <summary>
    /// Set connection to an already opened Session
    /// </summary>
    /// <param name="sessionId">SessionId previously opened</param>
    /// <param name="clientOptions"></param>
    public SessionService OpenSession(string sessionId, IDictionary<string, string> clientOptions = null)
    {
      ControlPlaneConnection();

      return new SessionService(LoggerFactory,
                                ControlPlaneService,
                                new Session()
                                {
                                  Id = sessionId,
                                },
                                clientOptions);
    }

    /// <summary>
    /// This method is creating a default taskOptions initialization where
    /// MaxDuration is 40 seconds, MaxRetries = 2 The app name is ArmoniK.DevelopmentKit.GridServer
    /// The version is 1.0.0 the namespace ArmoniK.DevelopmentKit.GridServer and simple service FallBackServerAdder 
    /// </summary>
    /// <returns>Return the default taskOptions</returns>
    public static TaskOptions InitializeDefaultTaskOptions()
    {
      TaskOptions taskOptions = new()
      {
        MaxDuration = new Duration
        {
          Seconds = 40,
        },
        MaxRetries = 2,
        Priority   = 1,
      };

      taskOptions.Options.Add(AppsOptions.EngineTypeNameKey,
                              EngineType.DataSynapse.ToString());

      taskOptions.Options.Add(AppsOptions.GridAppNameKey,
                              "ArmoniK.DevelopmentKit.GridServer");

      taskOptions.Options.Add(AppsOptions.GridAppVersionKey,
                              "1.X.X");

      taskOptions.Options.Add(AppsOptions.GridAppNamespaceKey,
                              "ArmoniK.DevelopmentKit.GridServer");

      taskOptions.Options.Add(AppsOptions.GridServiceNameKey,
                              "FallBackServerAdder");

      return taskOptions;
    }
  }
}