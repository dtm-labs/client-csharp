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
        private JsonSerializerOptions options;

        public DtmClient(HttpClient httpclient)
        {
            this.httpClient = httpclient;
            options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

        }

        public async Task<bool> RegisterTccBranch(RegisterTccBranch registerTcc, CancellationToken cancellationToken = default)
        {
            var content = new StringContent(JsonSerializer.Serialize(registerTcc,options));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await httpClient.PostAsync("/api/dtmsvr/registerTccBranch", content);
            var dtmcontent = await response.Content.ReadAsStringAsync();
            var dtmResult = JsonSerializer.Deserialize<DtmResult>(dtmcontent, options);
            CheckStatus(response.StatusCode, dtmResult);
            return dtmResult.Success;
        }


        public async Task<bool> TccPrepare(TccBody tccBody, CancellationToken cancellationToken)
        {
            var content = new StringContent(JsonSerializer.Serialize(tccBody,options));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var response = await httpClient.PostAsync("/api/dtmsvr/prepare", content);
            var dtmcontent = await response.Content.ReadAsStringAsync();
            var dtmResult = JsonSerializer.Deserialize<DtmResult>(dtmcontent, options);
            CheckStatus(response.StatusCode, dtmResult);
            return dtmResult.Success;
        }

        public async Task<bool> TccSubmit(TccBody tccBody, CancellationToken cancellationToken)
        {
            var content = new StringContent(JsonSerializer.Serialize(tccBody, options));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await httpClient.PostAsync("/api/dtmsvr/submit", content);
            var dtmcontent = await response.Content.ReadAsStringAsync();
            var dtmResult = JsonSerializer.Deserialize<DtmResult>(dtmcontent, options);
            CheckStatus(response.StatusCode, dtmResult);
            return dtmResult.Success;
        }

        public async Task<bool> TccAbort(TccBody tccBody, CancellationToken cancellationToken)
        {
            var content = new StringContent(JsonSerializer.Serialize(tccBody, options));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await httpClient.PostAsync("/api/dtmsvr/abort", content);
            var dtmcontent = await response.Content.ReadAsStringAsync();
            var dtmResult = JsonSerializer.Deserialize<DtmResult>(dtmcontent, options);
            CheckStatus(response.StatusCode, dtmResult);
            return dtmResult.Success;
        }

        public async Task<string> GenGid(CancellationToken cancellationToken)
        {
            var response = await httpClient.GetAsync("/api/dtmsvr/newGid");
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"bad http response status: {response.StatusCode}");
            }
            var content = await response.Content.ReadAsStringAsync();
            
            var dtmgid = JsonSerializer.Deserialize<DtmGid>(content, options);
            return dtmgid.Gid;
        }

        private void CheckStatus(HttpStatusCode status, DtmResult dtmResult)
        {
            if (status != HttpStatusCode.OK || dtmResult.Success != true)
            {
                throw new Exception($"http response status: {status}, Message :{ dtmResult.Message }");
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
