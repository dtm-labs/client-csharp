using DtmCommon;
using Microsoft.Extensions.Options;
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
        private static readonly char Slash = '/';
        private static readonly string QueryStringFormat = "dtm={0}&gid={1}&trans_type={2}&branch_id={3}&op={4}";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DtmOptions _dtmOptions;
        private readonly JsonSerializerOptions _jsonOptions;
        
        public DtmClient(IHttpClientFactory httpClientFactory, IOptions<DtmOptions> optionsAccs)
        {
            this._httpClientFactory = httpClientFactory;
            this._dtmOptions = optionsAccs.Value;
            this._jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<string> GenGid(CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient(Constant.DtmClientHttpName);

            var response = await client.GetAsync($"{_dtmOptions.DtmUrl.TrimEnd(Slash)}{Constant.Request.URL_NewGid}", cancellationToken).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"bad http response status: {response.StatusCode}");
            }
            var content = await response.Content.ReadAsStringAsync();

            var dtmgid = JsonSerializer.Deserialize<DtmGid>(content, _jsonOptions);
            return dtmgid.Gid;
        }

        private void CheckStatus(HttpStatusCode status, DtmResult dtmResult)
        {
            if (status != HttpStatusCode.OK || dtmResult.Success != true)
            {
                throw new Exception($"http response status: {status}, Message :{ dtmResult.Message }");
            }
        }

        public async Task<bool> TransCallDtm(TransBase tb, object body, string operation, CancellationToken cancellationToken)
        {
            var url = string.Concat(_dtmOptions.DtmUrl.TrimEnd(Slash), Constant.Request.URLBASE_PREFIX, operation);

            var content = new StringContent(JsonSerializer.Serialize(body, _jsonOptions));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(Constant.Request.CONTENT_TYPE);

            var client = _httpClientFactory.CreateClient(Constant.DtmClientHttpName);
            var response = await client.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
            var dtmcontent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var dtmResult = JsonSerializer.Deserialize<DtmResult>(dtmcontent, _jsonOptions);
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

        public async Task<HttpResponseMessage> TransRequestBranch(TransBase tb, HttpMethod method, object body, string branchID, string op, string url, CancellationToken cancellationToken)
        {
            var queryParams = string.Format(
                QueryStringFormat,
                string.Concat(_dtmOptions.DtmUrl.TrimEnd(Slash), Constant.Request.URLBASE_PREFIX),
                tb.Gid,
                tb.TransType,
                branchID,
                op);

            var client = _httpClientFactory.CreateClient(Constant.BranchClientHttpName);
          
            if (url.Contains("?")) url = string.Concat(url, "&" ,queryParams);
            else url = string.Concat(url, "?", queryParams);

            var httpRequestMsg = new HttpRequestMessage(method, url);
            foreach (var item in tb.BranchHeaders ?? new Dictionary<string, string>())
            {
                httpRequestMsg.Headers.TryAddWithoutValidation(item.Key, item.Value);
            }

            if (body != null)
            {
                var content = new StringContent(JsonSerializer.Serialize(body, _jsonOptions));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(Constant.Request.CONTENT_TYPE);
                httpRequestMsg.Content = content;
            }

            var response = await client.SendAsync(httpRequestMsg, cancellationToken).ConfigureAwait(false);
            return response;
        }
    }
}
