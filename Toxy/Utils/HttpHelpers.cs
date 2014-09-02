using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Toxy.Common;

namespace Toxy.Utils
{
    public static class HttpHelpers
    {
        public static bool IsImageUrl(string URL)
        {
            try
            {
                var uriResult = default(Uri);
                bool result = Uri.TryCreate(URL, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (result)
                {
                    var req = (HttpWebRequest)HttpWebRequest.Create(URL);
                    req.Method = "HEAD";
                    using (var resp = req.GetResponse())
                    {
                        return resp.ContentType.ToLower(CultureInfo.InvariantCulture).StartsWith("image/");
                    }
                }
            }
            catch (WebException ex)
            {
                //user is offline
                return false;
            }
            catch(Exception ex)
            {
                Logger.LogException(ex);
            }
            return false;
        }
    }
}
