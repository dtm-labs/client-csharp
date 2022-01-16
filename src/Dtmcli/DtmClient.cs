using Dtmcli.DtmImp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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

        public async Task<bool> TransCallDtm(TransBase tb, object body, string operation, CancellationToken cancellationToken)
        {
            var url = string.Concat(Constant.Request.URLBASE_PREFIX, operation);

            var content = new StringContent(JsonSerializer.Serialize(body, options));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(Constant.Request.CONTENT_TYPE);

            var response = await httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
            var dtmcontent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var dtmResult = JsonSerializer.Deserialize<DtmResult>(dtmcontent, options);
            CheckStatus(response.StatusCode, dtmResult);
            return dtmResult.Success;
        }

        public async Task<bool> TransRegisterBranch(TransBase tb, Dictionary<string, string> added, string operation, CancellationToken cancellationToken)
        {
            var dict = new Dictionary<string, string>
            {
                { Constant.Request.GID, tb.Gid},
                { Constant.Request.TRANS_TYPE, tb.TransType},
            };

            foreach (var item in added ?? new Dictionary<string, string>())
            {
                dict[item.Key] = item.Value;
            }

            return await TransCallDtm(tb, dict, operation, cancellationToken);
        }

        public async Task<HttpResponseMessage> TransRequestBranch(TransBase tb, object body, string branchID, string op, string url, CancellationToken cancellationToken)
        {
            var queryParams = string.Format(
                "dtm={0}&gid={1}&trans_type={2}&branch_id={3}&op={4}",
                string.Concat(httpClient.BaseAddress.ToString().Trim('/'), Constant.Request.URLBASE_PREFIX),
                tb.Gid,
                tb.TransType,
                branchID,
                op);

            var client = new HttpClient();
            var content = new StringContent(JsonSerializer.Serialize(body, options));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(Constant.Request.CONTENT_TYPE);

            foreach (var item in tb.BranchHeaders ?? new Dictionary<string, string>())
            {
                content.Headers.TryAddWithoutValidation(item.Key, item.Value);
            }

            if (url.Contains("?")) url = string.Concat(url, "&" ,queryParams);
            else url = string.Concat(url, "?", queryParams);

            var response = await client.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
            return response;
        }
    }
}
