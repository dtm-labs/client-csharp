using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dtmcli
{
    public class TccGlobalTransaction
    {
        public string dtm;
        private HttpClient httpClient;

        public TccGlobalTransaction(string dtmUrl)
        {
            this.dtm = dtmUrl;
        }

        public async Task<string> Excecute(Action<Tcc> tcc_cb)
        {
            var tcc = new Tcc(this.dtm, await this.GenGid());

            var tbody = new TccBody
            { 
                Gid = tcc.Gid,
                Trans_Type ="tcc"
            };
            var content = new StringContent(JsonSerializer.Serialize(tbody));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(this.dtm);

            try
            {
                var response = await httpClient.PostAsync("/prepare", content);
                CheckStatus(response.StatusCode);
                tcc_cb(tcc);
                response = await httpClient.PostAsync("/submit", content);
                CheckStatus(response.StatusCode);
            }
            catch(Exception ex)
            {
                var response = await httpClient.PostAsync("/abort", content);
                CheckStatus(response.StatusCode);
                return string.Empty;
            }
            return tcc.Gid;
        }

        public async Task<string> GenGid()
        {
            httpClient = new HttpClient();
            var response = await httpClient.GetAsync(this.dtm + "/newGid");
            CheckStatus(response.StatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        public void CheckStatus(HttpStatusCode status)
        {
            if (status != HttpStatusCode.OK)
            {
                throw new Exception($"bad http response status: {status}");
            }
        }
    }
}
