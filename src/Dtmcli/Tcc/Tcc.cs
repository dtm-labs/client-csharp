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
        private IDtmClient dtmClient;

        public Tcc(IDtmClient dtmHttpClient, string gid)
        {
            this.dtmClient = dtmHttpClient;
            this.Gid = gid;
            this.idGen = new IdGenerator();
        }

        public string Gid { get => gid; set => gid = value; }

        public async Task<string> CallBranch(object body, string tryUrl, string confirmUrl, string cancelUrl, CancellationToken cancellationToken = default)
        {
            var branchId = this.idGen.NewBranchId();
            var registerTccBranch = new RegisterTccBranch()
            {
                Branch_id = branchId,
                Cancel = cancelUrl,
                Confirm = confirmUrl,
                Status = "prepared",
                Trans_type = "tcc",
                Gid = this.Gid,
                Try = tryUrl,
                Data = JsonSerializer.Serialize(body)
            };
            
            await dtmClient.RegisterTccBranch(registerTccBranch,cancellationToken);

            var tryHttpClient = new HttpClient();
            var tryContent = new StringContent(JsonSerializer.Serialize(body));
            tryContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            tryUrl = $"{tryUrl}?gid={this.Gid}&trans_type=tcc&branch_id={branchId}&branch_type=try";
 
            var response = await tryHttpClient.PostAsync(tryUrl, tryContent);
            return await response.Content.ReadAsStringAsync();
        }
 
    }
}
