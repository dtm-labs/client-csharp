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
    public class Tcc
    {
        public IdGenerator idGen;
        public string dtm;
        private string gid;
        private IDtmClient httpClient;

        public Tcc(IDtmClient dtmHttpClient, string gid)
        {
            this.httpClient = dtmHttpClient;
            this.Gid = gid;
            this.idGen = new IdGenerator();
        }

        public string Gid { get => gid; set => gid = value; }

        public async Task<HttpResponseMessage> CallBranch(object body, string tryUrl, string confirmUrl, string cancelUrl, CancellationToken cancellationToken = default)
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
            
            await httpClient.RegisterTccBranch(registerTccBranch,cancellationToken);

            var tryHttpClient = new HttpClient();
            var tryContent = new StringContent(JsonSerializer.Serialize(body));
            tryContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            tryUrl = $"{tryUrl}?gid={this.Gid}&trans_type=tcc&branch_id={branchId}&branch_type=try";
 
            return await tryHttpClient.PostAsync(tryUrl, tryContent); 
        }
 
    }
}
