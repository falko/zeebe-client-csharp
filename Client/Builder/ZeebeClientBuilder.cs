using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Grpc.Auth;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Zeebe.Client.Builder
{
    public class ZeebeClientBuilder : IZeebeClientBuilder, IZeebeClientTransportBuilder
    {
        private ILoggerFactory LoggerFactory { get; set; }
        private string GatewayAddress { get; set; }

        public IZeebeClientBuilder UseLoggerFactory(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
            return this;
        }

        public IZeebeClientTransportBuilder UseGatewayAddress(string gatewayAddress)
        {
            GatewayAddress = gatewayAddress;
            return this;
        }

        public IZeebeSecureClientBuilder UseTransportEncryption(string rootCertificatePath)
        {
            return new ZeebeSecureClientBuilder(GatewayAddress, rootCertificatePath, LoggerFactory);
        }

        public IZeebeSecureClientBuilder UseTransportEncryption()
        {
            return new ZeebeSecureClientBuilder(GatewayAddress, LoggerFactory);
        }

        public IZeebeClientFinalBuildStep UsePlainText()
        {
            return new ZeebePlainClientBuilder(GatewayAddress, LoggerFactory);
        }
    }

    internal class ZeebePlainClientBuilder : IZeebeClientFinalBuildStep
    {
        private readonly ILoggerFactory loggerFactory;

        private string Address { get; }

        public ZeebePlainClientBuilder(string address, ILoggerFactory loggerFactory = null)
        {
            Address = address;
            this.loggerFactory = loggerFactory;
        }

        public IZeebeClient Build()
        {
            return new ZeebeClient(Address, loggerFactory);
        }
    }

    internal class ZeebeSecureClientBuilder : IZeebeSecureClientBuilder
    {
        private readonly ILoggerFactory loggerFactory;

        private string Address { get; }

        private ChannelCredentials Credentials { get; set; }

        public ZeebeSecureClientBuilder(string address, string certificatePath, ILoggerFactory loggerFactory = null)
        {
            Address = address;
            this.loggerFactory = loggerFactory;
            Credentials = new SslCredentials(File.ReadAllText(certificatePath));
        }

        public ZeebeSecureClientBuilder(string address, ILoggerFactory loggerFactory = null)
        {
            Address = address;
            this.loggerFactory = loggerFactory;
            Credentials = new SslCredentials();
        }

        public IZeebeClientFinalBuildStep UseAccessToken(string accessToken)
        {
            Credentials = ChannelCredentials.Create(Credentials, GoogleGrpcCredentials.FromAccessToken(accessToken));
            return this;
        }

        public IZeebeClientFinalBuildStep UseAccessTokenSupplier(IAccessTokenSupplier supplier)
        {
            Credentials = ChannelCredentials.Create(Credentials, supplier.ToCallCredentials());
            return this;
        }

        public IZeebeClient Build()
        {
            return new ZeebeClient(Address, Credentials, loggerFactory);
        }
    }
}