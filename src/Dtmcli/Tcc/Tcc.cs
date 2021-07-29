using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dtmcli
{
    public class Tcc
    {
        public IdGenerator idGen;
        public string dtm;
        private string gid;
        private HttpClient httpClient;

        public Tcc(string dtmUrl, string gid)
        {
            this.dtm = dtmUrl;
            this.Gid = gid;
            this.idGen = new IdGenerator();
        }

        public string Gid { get => gid; set => gid = value; }

        public async Task<HttpResponseMessage> CallBranch(object body, string tryUrl, string confirmUrl, string cancelUrl)
        {
            var branchId = this.idGen.NewBranchId();
            var registerTccBranch = new RegisterTccBranch()
            {
                Branch_id = branchId,
                Cancel = cancelUrl,
                Confirm = confirmUrl,
                Gid = this.Gid,
                Try = tryUrl,
                Data = JsonSerializer.Serialize(body)
            };
            httpClient = new HttpClient();
            var content = new StringContent(JsonSerializer.Serialize(registerTccBranch));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await httpClient.PostAsync(this.dtm + "/registerTccBranch", content);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"bad http response status: {response.StatusCode}");
            }

            var tryContent = new StringContent(JsonSerializer.Serialize(body));
            tryContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            tryUrl = $"{tryUrl}?gid={this.Gid}&trans_type=tcc&branch_id={branchId}&branch_type=try";
            return await httpClient.PostAsync(tryUrl, tryContent); 
        }
 
    }
}
