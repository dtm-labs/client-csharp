using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Dtmcli
{
    public class DtmClient : IDtmClient
    {
        private HttpClient httpClient;

        public DtmClient(HttpClient httpclient)
        {
            this.httpClient = httpclient;
        }

        public async Task<bool> RegisterTccBranch(RegisterTccBranch registerTcc, CancellationToken cancellationToken = default)
        {
            var content = new StringContent(JsonSerializer.Serialize(registerTcc));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await httpClient.PostAsync("/registerTccBranch", content);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"bad http response status: {response.StatusCode}");
            }

            return true;
        }


        public async Task<bool> TccPrepare(TccBody tccBody, CancellationToken cancellationToken)
        {
            var content = new StringContent(JsonSerializer.Serialize(tccBody));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var response = await httpClient.PostAsync("/prepare", content);
            CheckStatus(response.StatusCode);
            return true;
        }

        public async Task<bool> TccSubmit(TccBody tccBody, CancellationToken cancellationToken)
        {
            var content = new StringContent(JsonSerializer.Serialize(tccBody));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await httpClient.PostAsync("/submit", content);
            CheckStatus(response.StatusCode);

            return true;
        }

        public async Task<bool> TccAbort(TccBody tccBody, CancellationToken cancellationToken)
        {
            var content = new StringContent(JsonSerializer.Serialize(tccBody));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await httpClient.PostAsync("/abort", content);
            CheckStatus(response.StatusCode);
            return true;
        }

        public async Task<string> GenGid(CancellationToken cancellationToken)
        {
            var response = await httpClient.GetAsync("/newGid");
            CheckStatus(response.StatusCode);
            return await response.Content.ReadAsStringAsync();  
        }

        private void CheckStatus(HttpStatusCode status)
        {
            if (status != HttpStatusCode.OK)
            {
                throw new Exception($"bad http response status: {status}");
            }
        }


        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.httpClient?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
