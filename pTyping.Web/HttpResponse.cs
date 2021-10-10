using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace pTyping.Web {
    public class HttpResponse {
        public static string LineTerminator = "\r\n";

        public static string HttpVersion  = "HTTP/1.1";
        public        short  StatusCode   = 200;
        public        string ReasonPhrase = "OK";

        public Dictionary<string, string> Headers = new();

        public byte[] MessageBody;
        public string ContentType = "text/html; charset=UTF-8";

        public static string DatePattern = "{0:ddd,' 'dd' 'MMM' 'yyyy' 'HH':'mm':'ss' 'K}";

        public HttpResponse() {
            this.Headers.Add("date",   string.Format(DatePattern, DateTime.UtcNow));
            this.Headers.Add("server", HttpServer.Server);
        }

        private void UpdateHeaders() {
            this.Headers.Remove("content-length");
            this.Headers.Add("content-length", this.MessageBody.Length.ToString());

            this.Headers.Remove("content-type");
            this.Headers.Add("content-type", this.ContentType);
        }

        public byte[] GetBytes() {
            string finalString = $"{HttpVersion} {this.StatusCode} {this.ReasonPhrase}{LineTerminator}";
            this.UpdateHeaders();
            foreach ((string header, string data) in this.Headers)
                finalString += $"{header}: {data}{LineTerminator}";
            finalString += LineTerminator;

            byte[] finalBytes = Encoding.UTF8.GetBytes(finalString);
            finalBytes = finalBytes.Concat(this.MessageBody).ToArray();

            return finalBytes;
        }

        public static implicit operator byte[](HttpResponse response) => response.GetBytes();
    }
}
