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
        public IdGenerator IdGen;
        private readonly IDtmClient _dtmClient;

        public Tcc(IDtmClient dtmHttpClient, string gid)
        {
            this._dtmClient = dtmHttpClient;
            this.Gid = gid;
            this.IdGen = new IdGenerator();
        }

        public string Gid { get; set; }

        public async Task<string> CallBranch(object body, string tryUrl, string confirmUrl, string cancelUrl, CancellationToken cancellationToken = default)
        {
            var dataStr = JsonSerializer.Serialize(body);
            var branchId = this.IdGen.NewBranchId();
            var registerTccBranch = new RegisterTccBranch()
            {
                Branch_id = branchId,
                Cancel = cancelUrl,
                Confirm = confirmUrl,
                Status = "prepared",
                Trans_type = "tcc",
                Gid = this.Gid,
                Try = tryUrl,
                Data = dataStr
            };
            
            await _dtmClient.RegisterTccBranch(registerTccBranch,cancellationToken);

            var tryHttpClient = new HttpClient();
            var tryContent = new StringContent(dataStr);
            tryContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            tryUrl = $"{tryUrl}?gid={this.Gid}&trans_type=tcc&branch_id={branchId}&op=try";
 
            var response = await tryHttpClient.PostAsync(tryUrl, tryContent);
            var responseContent= await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"failed to request during try phase[{tryUrl}],httpStatus[{(int)response.StatusCode}],reasonPhrase[{response.ReasonPhrase}],responseContent[{responseContent}]");
            return responseContent;
        }
 
    }
}
