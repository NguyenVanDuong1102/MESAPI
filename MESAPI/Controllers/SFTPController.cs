using System;
using System.Web.Http;
using MES.Models;
using System.Data;
using System.Net;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;
using System.Web.Http.Results;
using Renci.SshNet;

namespace MES.Controllers
{
    /// <summary>
    /// Working with SFTP files.
    /// By IT: V1017526
    /// </summary>
    public class SFTPController : ApiController
    {
        /*
         <remarks>
         Request Formats:
         Content-Type: application/json, text/json
         Example:
              {
                "IN_DB":"SFCHT"
                ,"IN_SP":"SFIS1.MES_APP"
                ,"IN_EVENT":"SFTP_ACCOUNT"
                ,"IN_DATA":"{\"FTP_NAME\":\"FTP_SERVER\",\"EMP_NO\":\"V1017526\",\"FILE_PATH\":\"Packing3_PALT//MES_P_EGA228BSAS.LAB\"}"
             }
         PROCEDURE OUTPUT TABLE example:
                        OPEN res_table FOR 
                        select VR_CLASS HOST, VR_ITEM PORT, VR_NAME USERNAME, VR_VALUE PASSWORD,'Packing3_PALT//MES_P_EGA228BSAS.LAB' FILE_PATH 
                        from sfis1.C_PARAMETER_INI WHERE prg_name LIKE 'FTP_SERVER' AND VR_DESC = 'FTP_SERVER';
        
         </remarks>
        */

        /// <summary>
        /// Download SFTP file.
        /// </summary>
        /// <param name="inputJson">API SAMPLE JSON.</param>
        /// <returns>FILE.</returns>
        public HttpResponseMessage DownloadFile(JObject inputJson)
        {
            SFTP_INFO SFTP;
            try
            {
                SFTP = LOAD_FTP_INFO(inputJson);
                if (string.IsNullOrEmpty(SFTP.FilePath))
                {
                    throw new MesException("FILE_PATH is null");
                }
                using (var client = new SftpClient(SFTP.Host, SFTP.Port, SFTP.Username, SFTP.Password))
                {
                    try
                    {
                        client.Connect();

                        using (var memoryStream = new MemoryStream())
                        {
                            client.DownloadFile(SFTP.FilePath, memoryStream);

                            memoryStream.Seek(0, SeekOrigin.Begin);

                            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                            {
                                Content = new ByteArrayContent(memoryStream.ToArray())
                            };
                            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                            {
                                FileName = SFTP.FilePath
                            };

                            return response;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new MesException($"Lỗi khi tải tệp: {ex.Message}");
                    }
                    finally
                    {
                        if (client.IsConnected)
                        {
                            client.Disconnect();
                        }
                    }
                }
            }
            catch (Exception EX)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, EX.Message);
            }
        }


        /// <summary>
        /// Delete SFTP file.
        /// </summary>
        /// <param name="inputJson">API DEFAULT JSON.</param>
        /// <returns>API DEFAULT JSON</returns>
        [HttpPost]
        public IHttpActionResult DeleteFile(JObject inputJson)
        {
            SFTP_INFO FTP;
            try
            {
                FTP = LOAD_FTP_INFO(inputJson);
                if (FTP.FilePath == "")
                {
                    throw new MesException("Không có dữ liệu FILE_PATH cần xóa");
                }
                using (var client = new SftpClient(FTP.Host, FTP.Port, FTP.Username, FTP.Password))
                {
                    client.Connect();
                    try
                    {
                        if (!client.Exists(FTP.FilePath))
                        {
                            client.Disconnect();
                            return Ok(new Output { Code = "1", Message = "OK Không tìm thấy file:" + FTP.FilePath + " hoặc file đã xóa" });
                        }
                        else
                        {
                            client.DeleteFile(FTP.FilePath);
                            client.Disconnect();
                            return Ok(new Output { Code = "1", Message = "OK" });
                        }
                    }
                    catch (Exception ex)
                    {
                        client.Disconnect();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(new Output { Code = "0", Message = ex.Message });
            }
        }
        /// <summary>
        /// Get SFTP connection account data
        /// </summary>
        /// <param name="inputJson"></param>
        /// <returns></returns>
        private SFTP_INFO LOAD_FTP_INFO(JObject inputJson)
        {
            try
            {
                MESController api = new MESController();
                IHttpActionResult actionResult = api.CallAPI(inputJson);
                var response_data = actionResult as OkNegotiatedContentResult<Output>;
                SFTP_INFO SFTP = new SFTP_INFO();
                if (response_data != null)
                {
                    Output result = response_data.Content;
                    if (result.Message.IndexOf("OK") == -1)
                    {
                        throw new MesException(result.Message);
                    }
                    else
                    {
                        DataTable table = (DataTable)result.Data;
                        if (table.Rows.Count != 1)
                        {
                            throw new MesException("CALL IT Không thể lấy dữ liệu tài khoản SFTP trong thủ tục");
                        }
                        SFTP.Host = table.Rows[0]["HOST"].ToString();
                        SFTP.Port = int.Parse(table.Rows[0]["PORT"].ToString());
                        SFTP.Username = table.Rows[0]["USERNAME"].ToString();
                        SFTP.Password = table.Rows[0]["PASSWORD"].ToString();
                        SFTP.FilePath = table.Rows[0]["FILE_PATH"].ToString();
                        return SFTP;
                    }
                }
                else
                {
                    throw new MesException("API Không trả về dữ liệu");
                }
            }
            catch(Exception ex)
            {
                throw new MesException("Exception: " + ex.Message);
            }
        }
    }

    public class MesException : Exception
    {
        public MesException(string Message) : base(Message)
        {

        }
    }

    public class SFTP_INFO
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FilePath { get; set; }
    }
}