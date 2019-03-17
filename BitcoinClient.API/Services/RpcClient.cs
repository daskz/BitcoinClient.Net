using System.Collections;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BitcoinClient.API.Services
{
    public class RpcClient
    {
        public RpcResult Invoke(string sMethod, string walletId = null, params object[] parameters)
        {
            var url = walletId != null ? "http://localhost:28331/wallet/" + walletId : "http://localhost:28331";
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Credentials = new NetworkCredential("rpc", "rpc");

            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";

            JObject joe = new JObject();
            joe["jsonrpc"] = "1.0";
            joe["id"] = "1";
            joe["method"] = sMethod;

            if (parameters != null)
            {
                if (parameters.Length > 0)
                {
                    JArray props = new JArray();
                    foreach (var p in parameters)
                    {
                        if (p.GetType().IsGenericType && p is IEnumerable)
                        {
                            JArray l = new JArray();
                            foreach (var i in (IEnumerable)p)
                            {
                                l.Add(i);
                            }
                            props.Add(l);
                        }
                        else
                        {
                            props.Add(p);
                        }
                    }
                    joe.Add(new JProperty("params", props));
                }
            }

            string s = JsonConvert.SerializeObject(joe);
            // serialize json for the request
            byte[] byteArray = Encoding.UTF8.GetBytes(s);
            webRequest.ContentLength = byteArray.Length;
            ServicePointManager.ServerCertificateValidationCallback = new
                RemoteCertificateValidationCallback
                (
                    delegate { return true; }
                );

            using (Stream dataStream = webRequest.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (Stream str = webResponse.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(str))
                    {
                        return new RpcResult {Result = JsonConvert.DeserializeObject<JObject>(sr.ReadToEnd()), IsSuccessful = true};
                    }
                }
            }
        }

        public class RpcResult
        {
            public JObject Result { get; set; }
            public bool IsSuccessful { get; set; }
            public string Error { get; set; }
        }
    }
}
