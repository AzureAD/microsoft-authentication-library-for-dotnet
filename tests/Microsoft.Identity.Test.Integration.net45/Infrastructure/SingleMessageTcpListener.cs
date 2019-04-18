using Microsoft.Identity.Client.UI;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    /// <summary>
    /// This object is responsible for listening to a single TCP request, on localhost:port, 
    /// extracting the uri, parsing 
    /// </summary>
    /// <remarks>
    /// The underlying TCP listener might capture multiple requests, but only the first one is handled.
    /// </remarks>
    internal class SingleMessageTcpListener : IDisposable
    {
        private readonly int _port;
        private readonly System.Net.Sockets.TcpListener _tcpListener;

        public SingleMessageTcpListener(int port)
        {
            if (port < 1 || port == 80)
            {
                throw new ArgumentOutOfRangeException("Expected a valid port number, > 0, not 80");
            }

            _port = port;
            _tcpListener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, _port);
            _tcpListener.Start();

        }

        public async Task ListenToSingleRequestAndRespondAsync(
            Func<Uri, string> responseProducer,
            CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => _tcpListener.Stop());

            TcpClient tcpClient = null;
            try
            {
                tcpClient =
                    await AcceptTcpClientAsync(_tcpListener, cancellationToken)
                    .ConfigureAwait(false);

                await ExtractUriAndRespondAsync(tcpClient, responseProducer, cancellationToken).ConfigureAwait(false);

            }
            finally
            {
                tcpClient?.Close();
                tcpClient?.Dispose();
            }

        }

        /// <summary>
        /// AcceptTcpClientAsync does not natively support cancellation, so use this wrapper. Make sure
        /// the cancellation token is registered to stop the listener.
        /// </summary>
        /// <remarks>See https://stackoverflow.com/questions/19220957/tcplistener-how-to-stop-listening-while-awaiting-accepttcpclientasync</remarks>
        private static async Task<TcpClient> AcceptTcpClientAsync(TcpListener listener, CancellationToken token)
        {
            try
            {
                return await listener.AcceptTcpClientAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (token.IsCancellationRequested)
            {
                throw new OperationCanceledException("Cancellation was requested while awaiting TCP client connection.", ex);
            }
        }

        private async Task ExtractUriAndRespondAsync(
            TcpClient tcpClient,
            Func<Uri, string> responseProducer,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string httpRequest = await GetTcpResponseAsync(tcpClient, cancellationToken).ConfigureAwait(false);
            Uri uri = ExtractUriFromHttpRequest(httpRequest);

            // AuthorizationResult authenticationResult = new AuthorizationResult(AuthorizationStatus.Success, uri);

            // write an "OK, please close the browser message" 
            await WriteResponseAsync(responseProducer(uri), tcpClient.GetStream(), cancellationToken)
                .ConfigureAwait(false);
        }

#pragma warning disable CS1570 // XML comment has badly formed XML
        /// <summary>
        /// Example TCP response:
        /// 
        /// {GET /?code=OAQABAAIAAAC5una0EUFgTIF8ElaxtWjTl5wse5YHycjcaO_qJukUUexKz660btJtJSiQKz1h4b5DalmXspKis-bS6Inu8lNs4CpoE4FITrLv00Mr3MEYEQzgrn6JiNoIwDFSl4HBzHG8Kjd4Ho65QGUMVNyTjhWyQDf_12E8Gw9sll_sbOU51FIreZlVuvsqIWBMIJ8mfmExZBSckofV6LbcKJTeEZKaqjC09x3k1dpsCNJAtYTQIus5g1DyhAW8viDpWDpQJlT55_0W4rrNKY3CSD5AhKd3Ng4_ePPd7iC6qObfmMBlCcldX688vR2IghV0GoA0qNalzwqP7lov-yf38uVZ3ir6VlDNpbzCoV-drw0zhlMKgSq6LXT7QQYmuA4RVy_7TE9gjQpW-P0_ZXUHirpgdsblaa3JUq4cXpbMU8YCLQm7I2L0oCkBTupYXKLoM2gHSYPJ5HChhj1x0pWXRzXdqbx_TPTujBLsAo4Skr_XiLQ4QPJZpkscmXezpPa5Z87gDenUBRBI9ppROhOksekMbvPataF0qBaM38QzcnzeOCFyih1OjIKsq3GeryChrEtfY9CL9lBZ6alIIQB4thD__Tc24OUmr04hX34PjMyt1Z9Qvr76Pw0r7A52JvqQLWupx8bqok6AyCwqUGfLCPjwylSLA7NYD7vScAbfkOOszfoCC3ff14Dqm3IAB1tUJfCZoab61c6Mozls74c2Ujr3roHw4NdPuo-re5fbpSw5RVu8MffWYwXrO3GdmgcvIMkli2uperucLldNVIp6Pc3MatMYSBeAikuhtaZiZAhhl3uQxzoMhU-MO9WXuG2oIkqSvKjghxi1NUhfTK4-du7I5h1r0lFh9b3h8kvE1WBhAIxLdSAA&state=b380f309-7d24-4793-b938-e4a512b2c7f6&session_state=a442c3cd-a25e-4b88-8b33-36d194ba11b2 HTTP/1.1
        /// Host: localhost:9001
        /// Accept-Language: en-GB,en;q=0.9,en-US;q=0.8,ro;q=0.7,fr;q=0.6
        /// Connection: keep-alive
        /// Upgrade-Insecure-Requests: 1
        /// User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.102 Safari/537.36
        /// Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8
        /// Accept-Encoding: gzip, deflate, br
        /// </summary>
        /// <returns>http://localhost:9001/?code=foo&session_state=bar</returns>
        private Uri ExtractUriFromHttpRequest(string httpRequest)
#pragma warning restore CS1570 // XML comment has badly formed XML
        {
            string regexp = @"GET \/\?(.*) HTTP";
            string getQuery = null;
            Regex r1 = new Regex(regexp);
            Match match = r1.Match(httpRequest);
            if (!match.Success)
            {
                throw new InvalidOperationException("Not a GET query");
            }

            getQuery = match.Groups[1].Value;
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Query = getQuery;
            uriBuilder.Port = _port;

            return uriBuilder.Uri;
        }

        private static async Task<string> GetTcpResponseAsync(TcpClient client, CancellationToken cancellationToken)
        {
            NetworkStream networkStream = client.GetStream();

            byte[] readBuffer = new byte[1024];
            StringBuilder stringBuilder = new StringBuilder();
            int numberOfBytesRead = 0;

            // Incoming message may be larger than the buffer size. 
            do
            {
                numberOfBytesRead = await networkStream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken)
                    .ConfigureAwait(false);

                string s = Encoding.ASCII.GetString(readBuffer, 0, numberOfBytesRead);
                stringBuilder.Append(s);

            }
            while (networkStream.DataAvailable);

            return stringBuilder.ToString();
        }

        private async Task WriteResponseAsync(
            string message,
            NetworkStream stream,
            CancellationToken cancellationToken)
        {
            //string message = null;
            //switch (result.Status)
            //{
            //    case AuthorizationStatus.Success:
            //        message = CloseWindowSuccessHtml;
            //        break;
            //    default:
            //        message = CloseWindowFailureHtml;
            //        break;
            //}

            string fullResponse = $"HTTP/1.1 200 OK\r\n\r\n{message}";
            var response = Encoding.ASCII.GetBytes(fullResponse);
            await stream.WriteAsync(response, 0, response.Length, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _tcpListener?.Stop();
        }
    }

}
