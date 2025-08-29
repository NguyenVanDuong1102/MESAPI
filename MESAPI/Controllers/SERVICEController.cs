using System;
using System.Collections.Generic;
using System.Web;
using System.Net;
using System.Web.Http;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections;
using MES.Models;
using JackieLib;
using System.Xml;
using System.Data;

namespace MES.Controllers
{
    /// <summary>
    /// Call Webservices
    /// </summary>
    public class SERVICEController : ApiController
    {
        private HttpClient _httpClient;
        private LogFile log = new LogFile();

        /// <summary>
        /// Call Web Service
        /// </summary>
        /// <param name="inputJson"></param>
        /// <returns>Http result</returns>
        //[HttpPost]
        //[Route("api/webservice/service/{serviceName}")]
        public async Task<IHttpActionResult> CallService(JObject inputJson)
        {
            string strIPClient = HttpContext.Current.Request.UserHostAddress == "::1" ? "Debug" : HttpContext.Current.Request.UserHostAddress;
            string strRES = string.Empty;
            string strURL = string.Empty;
            string strCONTENT_TYPE = string.Empty;
            string strIN_DATA = string.Empty;
            try
            {
                Hashtable ht = JsonConvert.DeserializeObject<Hashtable>(Convert.ToString(inputJson));
                if (String.IsNullOrEmpty(strIPClient))
                {
                    strRES = "Can't get IP client";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                #region Get data JSON input
                try
                {
                    strURL = ht["URL"].ToString().Trim();
                    if (String.IsNullOrEmpty(strURL))
                    {
                        strRES = "URL is null";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                }
                catch
                {
                    strRES = "URL doesn't exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                try
                {
                    strCONTENT_TYPE = ht["CONTENT_TYPE"].ToString().Trim();
                    if (String.IsNullOrEmpty(strCONTENT_TYPE))
                    {
                        strRES = "CONTENT_TYPE is null";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                }
                catch
                {
                    strRES = "CONTENT_TYPE doesn't exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                try
                {
                    strIN_DATA = ht["IN_DATA"].ToString().Replace("\\", "\\\\");
                    if (String.IsNullOrEmpty(strIN_DATA))
                    {
                        strRES = "IN_DATA is null";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                    if (strIN_DATA.Substring(0, 1) == "[")
                    {
                        strIN_DATA = strIN_DATA.Replace("[", "").Replace("]", "").Trim();
                    }
                }
                catch
                {
                    strRES = "IN_DATA doesn't exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                #endregion Get data JSON input

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = (senderX, certificate, chain, sslPolicyErrors) => { return true; };
                _httpClient = new HttpClient();

                HttpResponseMessage response = new HttpResponseMessage();
                switch (strCONTENT_TYPE.ToUpper())
                {
                    case "FORM_URLENCODED":
                        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(strIN_DATA);
                        var formUrlEncoded = ToFormUrlEncoded(dictionary);
                        var contentForm = new StringContent(formUrlEncoded, Encoding.UTF8, "application/x-www-form-urlencoded");
                        response = await _httpClient.PostAsync(strURL, contentForm);
                        break;
                    default:
                        var contentJson = new StringContent(strIN_DATA, Encoding.UTF8, "application/json");
                        response = await _httpClient.PostAsync(strURL, contentJson);
                        break;
                }

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    strRES = "OK";
                    if (IsValidXml(result))
                    {
                        // Phân tích cú pháp XML
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);

                        XmlNodeList nodes = ht.ContainsKey("XML_NODE_NAME") ? xmlDoc.SelectNodes($"//{ht["XML_NODE_NAME"].ToString().Trim()}/*") : xmlDoc.DocumentElement.ChildNodes;
                        DataTable dt = ToDataTable(nodes);
                        return Ok(new Output { Code = "1", Message = strRES, Data = dt });
                    }
                    return Ok(new Output { Code = "1", Message = strRES, Data = result });
                }
                return StatusCode((HttpStatusCode)response.StatusCode);

            }
            catch (Exception ex)
            {
                strRES = "Exception on MESAPI CallWebService: " + ex.Message;
                return Ok(new Output { Code = "0", Message = strRES });
            }
            finally
            {
                string logDirect;
                if (HttpContext.Current != null)
                {
                    logDirect = HttpContext.Current.Server.MapPath("~");
                }
                else
                {
                    logDirect = AppDomain.CurrentDomain.BaseDirectory;
                }
                log.WriteLog(logDirect + @"\MESAPI_LOG\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ": CallWebService" +
                                       "\r\n- URL: " + strURL +
                                       "\r\n- CONTENT_TYPE: " + strCONTENT_TYPE +
                                       "\r\n- IN_DATA: " + strIN_DATA +
                                       "\r\n=> RES: " + strRES +
                                       "\r\n-------------------------------------\r\n");
            }
        }

        static string ToFormUrlEncoded(Dictionary<string, string> dictionary)
        {
            try
            {
                List<string> pairs = new List<string>();
                foreach (var kvp in dictionary)
                {
                    // Mã hóa khóa và giá trị
                    string encodedKey = WebUtility.UrlEncode(kvp.Key);
                    string encodedValue = WebUtility.UrlEncode(kvp.Value);
                    pairs.Add($"{encodedKey}={encodedValue}");
                }
                // Nối các cặp khóa-giá trị bằng dấu '&'
                return string.Join("&", pairs);
            }
            catch (Exception ex)
            {
                return "Exception: " + ex.Message;
            }
        }

        static bool IsValidXml(string xml)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml); // Nếu không hợp lệ, sẽ ném ra ngoại lệ
                return true;
            }
            catch (XmlException)
            {
                return false; // Nếu có ngoại lệ, chuỗi không phải là XML hợp lệ
            }
        }

        private DataTable ToDataTable(XmlNodeList nodes)
        {
            DataTable dataTable = new DataTable();
            try
            {
                if (nodes.Count > 0)
                {
                    // Thêm cột dựa trên các node con của node đầu tiên
                    foreach (XmlNode childNode in nodes[0].ChildNodes)
                    {
                        dataTable.Columns.Add(childNode.Name, typeof(string));
                    }

                    // Thêm hàng vào DataTable
                    foreach (XmlNode node in nodes)
                    {
                        DataRow row = dataTable.NewRow();
                        foreach (XmlNode childNode in node.ChildNodes)
                        {
                            row[childNode.Name] = string.IsNullOrEmpty(childNode.InnerText) ? (object)DBNull.Value : childNode.InnerText;
                        }
                        dataTable.Rows.Add(row);
                    }
                }
                return dataTable;
            }
            catch
            {
                dataTable = new DataTable();
                return dataTable;
            }
        }
    }
}