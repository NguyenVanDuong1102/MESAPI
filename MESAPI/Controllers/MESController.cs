using JackieLib;
using MES.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OracleClient;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace MES.Controllers
{
    /// <summary>
    /// MES API for all system
    /// </summary>
    public class MESController : ApiController
    {
        public LogFile log = new LogFile();
        public NotesSendMail notesmail;
        /*------------Created by Jackie Than----------------*
        inputJson = {
                        "IN_DB": "SFC",
                        "IN_SP": "SFIS1.SMTLOADING",
                        "IN_EVENT": "TEST",
                        "IN_DATA": [
                            {
                                "KEY1": "VALUE1",
                                "KEY2": "VALUE2",
                                "KEY3": "VALUE3"
                            }
                        ]
                    }

        output =    {
                        "Code": "1",
                        "Message": "TEST",
                        "Data": [
                            {
                                "MESSAGE": "NO TABLE RETURN"
                            }
                        ]
                    }
        => Code = 1 -> API execute normal 
           Code = 0 -> API execute happen Exception
        *----------------------------------------------------*/
        /// <summary>
        /// API connect to Oracle function
        /// </summary>
        /// <param name="inputJson">Input Json</param>
        /// <returns>Data Output: Code = 1 -> API execute normal / Code = 0 -> API execute happen Exception</returns>
        public IHttpActionResult CallAPI(JObject inputJson) //Don't need declare Models => We can using dynamic or object or JObject
        {
            Hashtable htSP     = new Hashtable();
            DataTable dt       = new DataTable();
            string strIPClient = HttpContext.Current.Request.UserHostAddress == "::1" ? "Debug" : HttpContext.Current.Request.UserHostAddress;
            string strDB       = string.Empty;
            string strSP       = string.Empty;
            string strIN_EVENT = string.Empty;
            string strIN_DATA  = string.Empty;
            string strRES      = string.Empty;
            string strSchema   = string.Empty;
            string strConn     = string.Empty;
            try
            {
                Hashtable ht = JsonConvert.DeserializeObject<Hashtable>(Convert.ToString(inputJson));
                if (String.IsNullOrEmpty(strIPClient))
                {
                    strRES = "Can't get IP client";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                try
                {
                    strIN_EVENT = ht["IN_EVENT"].ToString().Trim();
                    if (String.IsNullOrEmpty(strIN_EVENT))
                    {
                        strRES = "IN_EVENT is null";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                }
                catch
                {
                    strRES = "IN_EVENT not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                if (strIN_EVENT.Trim() == "GET_IP")
                {
                    return Ok(new Output { Code = "1", Message = strIPClient });
                }

                try
                {
                    strIN_DATA = ht["IN_DATA"].ToString().Replace("\\", "\\\\");
                    if (String.IsNullOrEmpty(strIN_DATA))
                    {
                        strRES = "IN_DATA is null";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                }
                catch
                {
                    strRES = "IN_DATA not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                if (strIN_DATA.Substring(0, 1) == "[")
                {
                    strIN_DATA = strIN_DATA.Replace("[", "").Replace("]", "").Trim();
                }

                if (strIN_EVENT.Trim() == "DNS_GET_TIME")
                {
                    DNSTimeClient client = new DNSTimeClient(strIN_DATA, "123");
                    client.Connect();
                    DateTime dateNTP = client.ReceiveTimestamp;
                    strRES = dateNTP.ToString("yyyy/MM/dd HH:mm:ss");
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                if (strIN_EVENT.Trim() == "CHECK_PING")
                {
                    try
                    {
                        Network network = new Network();
                        if (network.CheckPing(strIN_DATA.Trim()))
                        {
                            return Ok(new Output { Code = "1", Message = "OK" });
                        }
                        else
                        {
                            return Ok(new Output { Code = "0", Message = "NG" });
                        }
                    }
                    catch (Exception ex)
                    {
                        return Ok(new Output { Code = "0", Message = "Exception: " + ex.Message });
                    }
                }

                if (strIN_EVENT.Trim() == "CHECK_TELNET")
                {
                    try
                    {
                        Network network = new Network();
                        string ip = strIN_DATA.Trim().Split(',')[0].ToString().Trim();
                        int port = Convert.ToInt32(strIN_DATA.Trim().Split(',')[1].ToString().Trim());
                        if (network.CheckTelnet(ip, port))
                        {
                            return Ok(new Output { Code = "1", Message = "OK" });
                        }
                        else
                        {
                            return Ok(new Output { Code = "0", Message = "NG" });
                        }
                    }
                    catch (Exception ex)
                    {
                        return Ok(new Output { Code = "0", Message = "Exception: " + ex.Message });
                    }
                }

                try
                {
                    strDB = ht["IN_DB"].ToString().Trim();
                    if (String.IsNullOrEmpty(strDB))
                    {
                        strRES = "IN_DB is null";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                }
                catch
                {
                    strRES = "IN_DB not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                try
                {
                    strSP = ht["IN_SP"].ToString();
                    if (String.IsNullOrEmpty(strSP))
                    {
                        strRES = "IN_SP is null";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                }
                catch
                {
                    strRES = "IN_SP not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                if (strSP.IndexOf(".") == -1)
                {
                    strRES = "IN_SP error: The SP: (" + strSP.ToUpper() + ") format error. Please call IT check!";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                try
                {
                    strConn = ConfigurationManager.ConnectionStrings[strDB].ConnectionString.Trim();
                }
                catch
                {
                    strRES = "IN_DB error: The Connection String (" + strDB + ") not exist in Web.config of API. Please call IT check!";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

               // string query = "SELECT ID ,DATE_TIME,PUSH_STATUS FROM TIMEKEEPING_DATA where PUSH_STATUS is null";
               
                SqlDB sqlDB = new SqlDB(strConn);
                //dt = sqlDB.ExecuteDataTable(query);
              
                //strSchema = (strDB.Substring(0, 2) == "AP") ? "MES4" : "SFISM4";
                //Assembly assembly = Assembly.GetExecutingAssembly();
                //FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                //float versionOnWeb = Convert.ToInt64(fvi.FileVersion.Replace(".", ""));
                //dt = sqlDB.ExecuteDataTable("SELECT AP_VERSION FROM " + strSchema + ".AMS_AP WHERE AP_NAME = 'MESAPI'");
                //if (dt.Rows.Count == 0)
                //{
                //    strRES = "The MESAPI not exist on AMS system. Please call IT check!";
                //    return Ok(new Output { Code = "1", Message = strRES });
                //}

                //float versionOnDB = Convert.ToInt64(dt.Rows[0][0].ToString().Replace(".", ""));
                //if (versionOnWeb < versionOnDB)
                //{
                //    strRES = "MESAPI version on server (" + fvi.FileVersion + ") < version on AMS system (" + dt.Rows[0][0].ToString() + "). Please call IT to update MESAPI latest version!";
                //    return Ok(new Output { Code = "1", Message = strRES });
                //}

                dt = sqlDB.ExecuteDataTable("SELECT *FROM sys.objects where schema_id='5' and name= '" + strSP.Split('.')[1] + "' and type_desc='SQL_STORED_PROCEDURE'");
                if (dt.Rows.Count == 0)
                {
                    strRES = "IN_SP error: The SP: (" + strSP.ToUpper() + ") not exist on DB. Please call IT check!";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
               
                htSP.Add("@IN_EVENT", strIN_EVENT);
                htSP.Add("@IN_DATA", strIN_DATA);
                htSP.Add("@RES", "");
                htSP.Add("@RES_TABLE", "");
                htSP = sqlDB.ExecuteSPReturnHashtable(strSP, htSP);
                strRES = htSP["@RES"].ToString();
                return Ok(new Output { Code = "1", Message = strRES, Data = (DataTable)htSP["@RES_TABLE"] });
            }
            catch (Exception ex)
            {
                strRES = "Exception on MESAPI CallAPI: " + ex.Message;
                return Ok(new Output { Code = "0", Message = strRES });
            }
            finally
            {
                /*-----------------------Get log by IP client-----------------------*/
                try
                {
                    if(strIN_EVENT.ToUpper().Contains("SCAN_ACTION") || strIN_EVENT.ToUpper().Contains("SCAN_DATA"))
                    {
                        if (strIN_DATA.ToUpper().Contains("IP"))
                        {
                            dt = new DataTable();
                            dt = JsonConvert.DeserializeObject<DataTable>(strIN_DATA.Trim().Replace("{", "[{").Replace("}", "}]"));
                            if (dt.Rows.Count > 0)
                            {
                                strIPClient = dt.Rows[0]["IP"].ToString().Trim();
                            }
                        }
                        else
                        {
                            strIPClient = strIN_DATA.Substring(0, 20).Trim();
                        }
                    }
                }
                catch
                {

                }
                /*----------------------/Get log by IP client-----------------------*/

                /*-----------------------Save log-----------------------------------*/
                if (strIN_EVENT.Trim() != "CHECK_PING" && strIN_EVENT.Trim() != "CHECK_TELNET" && strIN_EVENT.Trim() != "GET_IP" && strIN_EVENT.Trim() != "DNS_GET_TIME" && !strIN_EVENT.Contains("NO_LOG"))
                {
                    log.WriteLog(HttpContext.Current.Server.MapPath("~") + @"\MESAPI_LOG\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ": " + strSP +
                                   "\r\n- IN_EVENT: " + strIN_EVENT +
                                   "\r\n- IN_DATA: " + strIN_DATA +
                                   "\r\n=> RES: " + strRES +
                                   "\r\n-------------------------------------\r\n");
                }
                /*----------------------/Save log-----------------------------------*/

                /*-----------------------Warning mail when SP have issue-----------------------*/
                if (strRES.ToUpper().Contains("EXCEPTION") || strRES.ToUpper().Contains("ORA-"))
                {
                    try
                    {
                        strIN_DATA = strIPClient.PadRight(20, ' ') + strSP.PadRight(50, ' ') + HttpContext.Current.Request.Url.ToString().PadRight(200, ' ') + strConn.Split(';')[0].Split('=')[1].Trim().PadRight(200, ' ') + strRES.PadRight(1000, ' ') + Convert.ToString(inputJson);
                        SqlDB sqlDB_error = new SqlDB(strConn);
                        try
                        {
                            strSP = strSP.Split('.')[0].Trim() + "." + ConfigurationSettings.AppSettings["SP_EXCEPTION"].Trim();
                            dt = sqlDB_error.ExecuteDataTable("SELECT * FROM ALL_OBJECTS WHERE OBJECT_TYPE = 'PROCEDURE' AND OWNER = '" + strSP.Split('.')[0] + "' AND OBJECT_NAME = '" + strSP.Split('.')[1] + "'");
                            if (dt.Rows.Count == 0)
                            {
                                strRES = "IN_SP error: The SP: (" + strSP.ToUpper() + ") not exist on DB. Please call IT check!";
                            }
                            else
                            {
                                if (dt.Rows[0]["STATUS"].ToString().ToUpper() != "VALID")
                                {
                                    strRES = "IN_SP error: The SP: (" + strSP.ToUpper() + ") invalid. Please call IT check!";
                                }
                                else
                                {
                                    Hashtable htSP_error = new Hashtable();
                                    htSP_error.Add("IN_EVENT", strIN_EVENT);
                                    htSP_error.Add("IN_DATA", strIN_DATA);
                                    htSP_error.Add("RES", "");
                                    htSP_error.Add("RES_TABLE", "");
                                    htSP_error = sqlDB_error.ExecuteSPReturnHashtable(strSP, htSP_error);
                                    strRES = "OK executed " + strSP;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            strRES = strSP + " Exception: " + ex.Message;
                        }
                    }
                    catch (Exception ex)
                    {
                        strRES = "Exception on MESAPI CallAPI (" + strSP + "): " + ex.Message;
                    }
                    finally
                    {
                        log.WriteLog(HttpContext.Current.Server.MapPath("~") + @"\MESAPI_LOG\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ": " + strSP +
                                   "\r\n- IN_EVENT: " + strIN_EVENT +
                                   "\r\n- IN_DATA: " + strIN_DATA +
                                   "\r\n=> RES: " + strRES +
                                   "\r\n-------------------------------------\r\n");
                    }
                }
                /*----------------------/Warning mail when SP have issue-----------------------*/
            }
        }

        /// <summary>
        /// API connect to SQL Server function
        /// </summary>
        /// <param name="inputJson">Input Json</param>
        /// <returns>Data Output: Code = 1 -> API execute normal / Code = 0 -> API execute happen Exception</returns>
        public IHttpActionResult CallAPISQL([FromBody] Input inputJson) //Don't need declare Models => We can using dynamic or object or JObject
        {
            Hashtable htSP     = new Hashtable();
            DataTable dt       = new DataTable();
            string strIPClient = HttpContext.Current.Request.UserHostAddress == "::1" ? "Debug" : HttpContext.Current.Request.UserHostAddress;
            string strDB       = string.Empty;
            string strSP       = string.Empty;
            string strIN_EVENT = string.Empty;
            string strIN_DATA  = string.Empty;
            string strRES      = string.Empty;
            string strConn     = string.Empty;
            try
            {
                strDB       = inputJson.IN_DB;
                strSP       = inputJson.IN_SP;
                strIN_EVENT = inputJson.IN_EVENT;
                strIN_DATA  = inputJson.IN_DATA;
                if (String.IsNullOrEmpty(strIN_EVENT))
                {
                    strRES = "IN_EVENT is null";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                if (strIN_EVENT.Trim() == "GET_IP")
                {
                    return Ok(new Output { Code = "1", Message = strIPClient });
                }
                if (String.IsNullOrEmpty(strIN_DATA))
                {
                    strRES = "IN_DATA is null";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                if (strIN_EVENT.Trim() == "DNS_GET_TIME")
                {
                    DNSTimeClient client = new DNSTimeClient(strIN_DATA, "123");
                    client.Connect();
                    DateTime dateNTP = client.ReceiveTimestamp;
                    strRES = dateNTP.ToString("yyyy/MM/dd HH:mm:ss");
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                if (strIN_EVENT.Trim() == "CHECK_PING")
                {
                    try
                    {
                        Network network = new Network();
                        if (network.CheckPing(strIN_DATA.Trim()))
                        {
                            return Ok(new Output { Code = "1", Message = "OK" });
                        }
                        else
                        {
                            return Ok(new Output { Code = "0", Message = "NG" });
                        }
                    }
                    catch (Exception ex)
                    {
                        return Ok(new Output { Code = "0", Message = "Exception: " + ex.Message });
                    }
                }
                if (strIN_EVENT.Trim() == "CHECK_TELNET")
                {
                    try
                    {
                        Network network = new Network();
                        string ip = strIN_DATA.Trim().Split(',')[0].ToString().Trim();
                        int port = Convert.ToInt32(strIN_DATA.Trim().Split(',')[1].ToString().Trim());
                        if (network.CheckTelnet(ip, port))
                        {
                            return Ok(new Output { Code = "1", Message = "OK" });
                        }
                        else
                        {
                            return Ok(new Output { Code = "0", Message = "NG" });
                        }
                    }
                    catch (Exception ex)
                    {
                        return Ok(new Output { Code = "0", Message = "Exception: " + ex.Message });
                    }
                }
                if (String.IsNullOrEmpty(strDB))
                {
                    strRES = "IN_DB is null";
                    return Ok(new Output { Code = "1", Message = strRES });
                }  
                if (String.IsNullOrEmpty(strSP))
                {
                    strRES = "IN_SP is null";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                if (strSP.IndexOf(".") == -1)
                {
                    strRES = "IN_SP error: The SP: (" + strSP.ToUpper() + ") format error. Please call IT check!";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                try
                {
                    strConn = ConfigurationManager.ConnectionStrings[strDB].ConnectionString.Trim();
                }
                catch
                {
                    strRES = "IN_DB error: The Connection String (" + strDB + ") not exist in Web.config of API. Please call IT check!";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                SqlDB sqlDB = new SqlDB(strConn);
                dt = sqlDB.ExecuteDataTable("SELECT * FROM SYS.OBJECTS WHERE NAME = '" + strSP.Split('.')[1] + "'");
                if (dt.Rows.Count == 0)
                {
                    strRES = "IN_SP error: The SP: (" + strSP.ToUpper() + ") not exist on DB. Please call IT check!";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                htSP.Add("@IN_EVENT", strIN_EVENT);
                htSP.Add("@IN_DATA", strIN_DATA);
                htSP.Add("@RES", "");
                htSP.Add("@RES_TABLE", "");
                htSP = sqlDB.ExecuteSPReturnHashtable(strSP, htSP);
                strRES = htSP["@RES"].ToString();
                return Ok(new Output { Code = "1", Message = strRES, Data = (DataTable)htSP["@RES_TABLE"] });
            }
            catch (Exception ex)
            {
                strRES = "Exception on MESAPI CallAPISQL: " + ex.Message;
                return Ok(new Output { Code = "0", Message = strRES });
            }
            finally
            {
                /*-----------------------Get log by IP client-----------------------*/
                try
                {
                    if (strIN_DATA.Contains("IP"))
                    {
                        dt = new DataTable();
                        dt = JsonConvert.DeserializeObject<DataTable>(strIN_DATA.Trim().Replace("{", "[{").Replace("}", "}]"));
                        if (dt.Rows.Count > 0)
                        {
                            strIPClient = dt.Rows[0]["IP"].ToString().Trim();
                        }
                    }
                }
                catch
                {

                }
                /*----------------------/Get log by IP client-----------------------*/

                /*-----------------------Save log-----------------------------------*/
                if (strIN_EVENT.Trim() != "CHECK_PING" && strIN_EVENT.Trim() != "CHECK_TELNET" && strIN_EVENT.Trim() != "GET_IP" && strIN_EVENT.Trim() != "DNS_GET_TIME" && !strIN_EVENT.Contains("NO_LOG"))
                {
                    log.WriteLog(HttpContext.Current.Server.MapPath("~") + @"\MESAPI_LOG\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ": " + strSP +
                                   "\r\n- IN_EVENT: " + strIN_EVENT +
                                   "\r\n- IN_DATA: " + strIN_DATA +
                                   "\r\n=> RES: " + strRES +
                                   "\r\n-------------------------------------\r\n");
                }
                /*----------------------/Save log-----------------------------------*/
            }
        }

        /// <summary>
        /// API AOI auto pass station
        /// </summary>
        /// <param name="inputJson">Input Json</param>
        /// <returns>Data Output: Code = 1 -> API execute normal / Code = 0 -> API execute happen Exception</returns>
        public IHttpActionResult AoiAutoPassStation(dynamic inputJson) //Don't need declare Models => We can using dynamic or object or JObject
        {
            Hashtable ht       = JsonConvert.DeserializeObject<Hashtable>(Convert.ToString(inputJson));
            Hashtable htSP     = new Hashtable();
            DataTable dt       = new DataTable();
            string strIPClient = HttpContext.Current.Request.UserHostAddress == "::1" ? "Debug" : HttpContext.Current.Request.UserHostAddress;
            string strDB       = string.Empty;
            string strSP       = string.Empty;
            string strIN_EVENT = string.Empty;
            string strIN_DATA  = string.Empty;
            string strRES      = string.Empty;
            string strSchema   = string.Empty;
            string strConn     = string.Empty;
            try
            {
                try
                {
                    strSP = ConfigurationSettings.AppSettings["SP_AOI_APS"].Trim();
                }
                catch
                {
                    strRES = "The MESAPI not config SP (key = SP_AOI_APS) in Web.config file";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                if (String.IsNullOrEmpty(strIPClient))
                {
                    strRES = "IN_IP is null";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                try
                {
                    strDB = ht["IN_DB"].ToString().Trim();
                }
                catch
                {
                    strRES = "IN_DB not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                if (String.IsNullOrEmpty(strSP))
                {
                    strRES = "IN_SP is null";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                try
                {
                    strIN_EVENT = ht["IN_EVENT"].ToString().Trim();
                }
                catch
                {
                    strRES = "IN_EVENT not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                if (strIN_EVENT.Trim() == "GET_IP")
                {
                    return Ok(new Output { Code = "1", Message = strIPClient });
                }
                try
                {
                    strIN_DATA = ht["IN_DATA"].ToString();
                }
                catch
                {
                    strRES = "IN_DATA not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                if (strSP.IndexOf(".") == -1)
                {
                    strRES = "IN_SP error: The SP: (" + strSP.ToUpper() + ") format error. Please call IT check!";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                try
                {
                    strConn = ConfigurationManager.ConnectionStrings[strDB].ConnectionString.Trim();
                }
                catch
                {
                    strRES = "IN_DB error: The Connection String (" + strDB + ") not exist in Web.config of API. Please call IT check!";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                SqlDB sqlDB = new SqlDB(strConn);
                strSchema = (strDB.Substring(0, 2) == "AP") ? "MES4" : "SFISM4";
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                float versionOnWeb = Convert.ToInt64(fvi.FileVersion.Replace(".", ""));
                dt = sqlDB.ExecuteDataTable("SELECT AP_VERSION FROM " + strSchema + ".AMS_AP WHERE AP_NAME = 'MESAPI'");
                if (dt.Rows.Count == 0)
                {
                    strRES = "The MESAPI not exist on AMS system. Please call IT check!";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                float versionOnDB = Convert.ToInt64(dt.Rows[0][0].ToString().Replace(".", ""));
                if (versionOnWeb < versionOnDB)
                {
                    strRES = "MES API version on server (" + fvi.FileVersion + ") < version on AMS system (" + dt.Rows[0][0].ToString() + "). Please call IT to update MESAPI latest version!";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                dt = sqlDB.ExecuteDataTable("SELECT * FROM ALL_OBJECTS WHERE OBJECT_TYPE = 'PROCEDURE' AND OWNER = '" + strSP.Split('.')[0] + "' AND OBJECT_NAME = '" + strSP.Split('.')[1] + "'");
                if (dt.Rows.Count == 0)
                {
                    strRES = "IN_SP error: The SP: (" + strSP.ToUpper() + ") not exist on DB. Please call IT check!";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                if (dt.Rows[0]["STATUS"].ToString().ToUpper() != "VALID")
                {
                    strRES = "IN_SP error: The SP: (" + strSP.ToUpper() + ") invalid. Please call IT check!";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                htSP.Add("IN_EVENT", strIN_EVENT);
                htSP.Add("IN_DATA", strIN_DATA);
                htSP.Add("RES", "");
                htSP.Add("RES_TABLE", "");
                htSP = sqlDB.ExecuteSPReturnHashtable(strSP, htSP);
                strRES = htSP["RES"].ToString();
                return Ok(new Output { Code = "1", Message = strRES, Data = (DataTable)htSP["RES_TABLE"] });
            }
            catch (Exception ex)
            {
                strRES = "Exception on MESAPI CallAPI: " + ex.Message;
                return Ok(new Output { Code = "0", Message = strRES });
            }
            finally
            {
                log.WriteLog(HttpContext.Current.Server.MapPath("~") + @"\MESAPI_LOG\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ": " + strSP +
                               "\r\n- IN_EVENT: " + strIN_EVENT +
                               "\r\n- IN_DATA: " + strIN_DATA +
                               "\r\n=> RES: " + strRES +
                               "\r\n-------------------------------------\r\n");
            }
        }

        public IHttpActionResult APICallAPI(dynamic inputJson) // We can using dynamic or object or JObject
        {
            Hashtable ht = JsonConvert.DeserializeObject<Hashtable>(Convert.ToString(inputJson));
            string strRES = "OK";
            string strURL = string.Empty;
            dynamic strJSON = string.Empty;
            string strIPClient = HttpContext.Current.Request.UserHostAddress == "::1" ? "Debug" : HttpContext.Current.Request.UserHostAddress;
            try
            {
                try
                {
                    strURL = ht["IN_URL"].ToString().Trim();
                    if (strURL.Trim() == "DNS_GET_TIME")
                    {
                        try
                        {
                            strJSON = ht["IN_JSON"].ToString().Trim();
                        }
                        catch
                        {
                            strRES = "IN_JSON not exist in JSON input";
                            return Ok(new Output { Code = "1", Message = strRES });
                        }
                        DNSTimeClient client = new DNSTimeClient(strJSON, "123");
                        client.Connect();
                        DateTime dateNTP = client.ReceiveTimestamp;
                        strRES = dateNTP.ToString("yyyy/MM/dd HH:mm:ss");
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                }
                catch
                {
                    strRES = "IN_URL not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //ServicePointManager.ServerCertificateValidationCallback = (senderX, certificate, chain, sslPolicyErrors) => { return true; };
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true; //Allow URL is https
                using (var webclient = new WebClient())
                {
                    try
                    {
                        webclient.Headers.Add(HttpRequestHeader.Accept, "application/json");
                        webclient.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                        webclient.Encoding = System.Text.Encoding.UTF8;
                        if (strURL.Contains("?")) //GET
                        {
                            strRES = webclient.DownloadString(strURL);
                            return Ok(new Output { Code = "1", Message = "OK", Data = JsonConvert.DeserializeObject<dynamic>(strRES) });
                        }
                        else //POST
                        {
                            try
                            {
                                strJSON = ht["IN_JSON"].ToString().Trim();
                            }
                            catch
                            {
                                strRES = "IN_JSON not exist in JSON input";
                                return Ok(new Output { Code = "1", Message = strRES });
                            }

                            var responseBody = webclient.UploadString(strURL, strJSON);
                            try
                            {
                                ht = JsonConvert.DeserializeObject<Hashtable>(Convert.ToString(responseBody));
                                return Ok(new Output { Code = "1", Message = strRES, Data = ht });
                            }
                            catch
                            {
                                strRES = Convert.ToString(responseBody).Replace("\"", "");
                                return Ok(new Output { Code = "1", Message = strRES });
                            }
                        }  
                    }
                    catch (Exception ex)
                    {
                        strRES = ex.Message;
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                }
            }
            catch (Exception ex)
            {
                strRES = "Exception on MESAPI APICallAPI: " + ex.Message;
                return Ok(new Output { Code = "0", Message = strRES });
            }
            finally
            {
                log.WriteLog(HttpContext.Current.Server.MapPath("~") + @"\MESAPI_LOG\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ": API Call API" +
                               "\r\n- IN_URL: " + strURL +
                               "\r\n- IN_JSON: " + strJSON +
                               "\r\n=> RES: " + strRES +
                               "\r\n-------------------------------------\r\n");
            }
        }

        public IHttpActionResult SendMail(JObject inputJson) // We can using dynamic or object or JObject
        {
            Hashtable ht = JsonConvert.DeserializeObject<Hashtable>(Convert.ToString(inputJson));
            string strRES         = "OK";
            string mailSubject    = string.Empty;
            string mailTo         = string.Empty;
            string mailCc         = string.Empty;
            string mailBcc        = string.Empty;
            //string mailAttachment = string.Empty;
            string mailContent    = string.Empty;
            string strIPClient = HttpContext.Current.Request.UserHostAddress == "::1" ? "Debug" : HttpContext.Current.Request.UserHostAddress;
            string date_time = "[" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "]";
            try
            {
                try
                {
                    mailSubject = date_time + " " + ht["MAIL_SUBJECT"].ToString().Trim();
                }
                catch
                {
                    strRES = "MAIL_SUBJECT not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                try
                {
                    mailTo = ht["MAIL_TO"].ToString().Trim();
                }
                catch
                {
                    strRES = "MAIL_TO not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                try
                {
                    mailCc = ht["MAIL_CC"].ToString().Trim();
                }
                catch
                {
                    strRES = "MAIL_CC not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                try
                {
                    mailBcc = ht["MAIL_BCC"].ToString().Trim();
                }
                catch
                {
                    strRES = "MAIL_BCC not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                try
                {
                    mailContent = ht["MAIL_CONTENT"].ToString().Trim();
                }
                catch
                {
                    strRES = "MAIL_CONTENT not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                notesmail = new NotesSendMail(ConfigurationSettings.AppSettings["Mail_Password"].Trim(), ConfigurationSettings.AppSettings["Mail_Path"].Trim());
                notesmail.mailSubject = mailSubject;
                notesmail.mailTo = mailTo;
                notesmail.mailCc = mailCc;
                notesmail.mailBcc = mailBcc;
                notesmail.mailAttachment = "";
                notesmail.mailBody = mailContent;
                notesmail.SendMail();
                return Ok(new Output { Code = "1", Message = strRES });
            }
            catch (Exception ex)
            {
                strRES = "Exception on MESAPI SendMail: " + ex.Message;
                return Ok(new Output { Code = "0", Message = strRES });
            }
            finally
            {
                log.WriteLog(HttpContext.Current.Server.MapPath("~") + @"\MESAPI_LOG\SEND_MAIL\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ": " + date_time +
                               "\r\n- MAIL_SUBJECT: " + mailSubject +
                               "\r\n- MAIL_TO: " + mailTo +
                               "\r\n- MAIL_CC: " + mailCc +
                               "\r\n- MAIL_BCC: " + mailBcc +
                               "\r\n=> RES: " + strRES +
                               "\r\n-------------------------------------\r\n");
            }
        }

        public IHttpActionResult SaveImages(JObject inputJson) // We can using dynamic or object or JObject
        {
            Hashtable ht = JsonConvert.DeserializeObject<Hashtable>(Convert.ToString(inputJson));
            string strRES = "OK Save Image";
            string empNo = string.Empty;
            string orderNo = string.Empty;
            string imageBase64 = string.Empty;
            string strIPClient = HttpContext.Current.Request.UserHostAddress == "::1" ? "Debug" : HttpContext.Current.Request.UserHostAddress;
            string imageName = string.Empty;
            string strDB = string.Empty;
            string strConn = string.Empty;
            try
            {
                try
                {
                    empNo = ht["EMP_NO"].ToString().Trim();
                }
                catch
                {
                    strRES = "EMP_NO not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                try
                {
                    orderNo = ht["ORDER_NO"].ToString().Trim();
                }
                catch
                {
                    strRES = "ORDER_NO not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                try
                {
                    imageName = ht["IMAGE_NAME"].ToString().Trim();
                }
                catch
                {
                    strRES = "IMAGE_NAME not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                try
                {
                    imageBase64 = ht["IMAGE_BASE64"].ToString().Trim();
                }
                catch
                {
                    strRES = "IMAGE_BASE64 not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                try
                {
                    strDB = ht["IN_DB"].ToString().Trim();
                }
                catch
                {
                    strRES = "IN_DB not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                strConn = ConfigurationManager.ConnectionStrings[strDB].ConnectionString.Trim();
                byte[] imageBytes = Convert.FromBase64String(imageBase64);
                string strSql = "INSERT INTO SFCRUNTIME.CARIMAGE(CARNO, FILENAME, LASTEDITDT, FILEATTACH1, DATA1, DATA2) VALUES(:orderNo, :imageName, SYSDATE, :fileAttach, :empNo, 'TEMP')";
                using (OracleConnection conn = new OracleConnection(strConn))
                {
                    using (OracleCommand cmd = new OracleCommand(strSql, conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("orderNo", orderNo));
                        cmd.Parameters.Add(new OracleParameter("imageName", imageName));
                        cmd.Parameters.Add(new OracleParameter("fileAttach", OracleType.Blob)).Value = imageBytes;
                        cmd.Parameters.Add(new OracleParameter("empNo", empNo));
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new Output { Code = "1", Message = strRES });
            }
            catch (Exception ex)
            {
                strRES = "Exception on MESAPI SaveImages: " + ex.Message;
                return Ok(new Output { Code = "0", Message = strRES });
            }
            finally
            {
                log.WriteLog(HttpContext.Current.Server.MapPath("~") + @"\MESAPI_LOG\SAVE_IMAGES\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ": SaveImages" +
                               "\r\n- EMP_NO: " + empNo +
                               "\r\n- ORDER_NO: " + orderNo +
                               "\r\n- IMAGE_NAME: " + imageName +
                               "\r\n=> RES: " + strRES +
                               "\r\n-------------------------------------\r\n");
            }
        }
        public async Task<IHttpActionResult> UploadFile()
        {
            string strIPClient = HttpContext.Current.Request.UserHostAddress == "::1" ? "Debug" : HttpContext.Current.Request.UserHostAddress;
            string strRES = string.Empty;
            string strFilePath = string.Empty;
            string strFunction = string.Empty;
            string map_path = HttpContext.Current.Server.MapPath("~");
            try
            {
                if (!Request.Content.IsMimeMultipartContent())
                {
                    strRES = "Unsupported media type";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                var provider = new MultipartFormDataStreamProvider(HttpContext.Current.Server.MapPath("~/App_Data"));
                await Request.Content.ReadAsMultipartAsync(provider);
                string folderPath = map_path + @"\UPLOAD_FILES\" + provider.FormData["folder_path"];
                string fileName = provider.FormData["file_name"];
                strFunction = provider.FormData["Function"];

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                foreach (var file in provider.FileData)
                {
                    if (fileName == null)
                    {
                        fileName = Path.GetFileName(file.Headers.ContentDisposition.FileName.Trim('"'));
                    }
                    strFilePath = Path.Combine(folderPath, fileName);
                    if (File.Exists(strFilePath))
                    {
                        strRES = "File already exists";
                    }
                    else
                    {
                        File.Move(file.LocalFileName, strFilePath);
                        strRES = "OK Uploaded success";
                    }
                }
                return Ok(new Output { Code = "1", Message = strRES });
            }
            catch (Exception ex)
            {
                strRES = "Exception on MESAPI UploadFile: " + ex.Message;
                return Ok(new Output { Code = "0", Message = strRES });
            }
            finally
            {
                log.WriteLog(map_path + @"\MESAPI_LOG\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ": UPLOAD FILE" +
                            "\r\n- IN_EVENT: " + strFunction +
                            "\r\n- IN_FILE: " + strFilePath +
                            "\r\n=> RES: " + strRES +
                            "\r\n-------------------------------------\r\n");
            }
        }

        public IHttpActionResult DeleteFile(JObject inputJson)
        {
            string strRES = string.Empty;
            string strIN_EVENT = string.Empty;
            string strFileName = string.Empty;
            string strIPClient = HttpContext.Current.Request.UserHostAddress == "::1" ? "Debug" : HttpContext.Current.Request.UserHostAddress;
            try
            {
                Hashtable ht = JsonConvert.DeserializeObject<Hashtable>(Convert.ToString(inputJson));
                try
                {
                    strIN_EVENT = ht["IN_EVENT"].ToString().Trim();
                    if (String.IsNullOrEmpty(strIN_EVENT))
                    {
                        strRES = "IN_EVENT is null";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                }
                catch
                {
                    strRES = "IN_EVENT not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                try
                {
                    strFileName = ht["IN_FILE_NAME"].ToString().Trim();
                    if (String.IsNullOrEmpty(strFileName))
                    {
                        strRES = "IN_PATH is null";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                }
                catch
                {
                    strRES = "IN_PATH not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                strFileName = HttpContext.Current.Server.MapPath("~") + @"\UPLOAD_FILES\" + strFileName;
                if (File.Exists(strFileName))
                {
                    File.Delete(strFileName);
                    strRES = "File delete success!";
                    return Ok(new { Code = "1", message = strRES });
                }
                else
                {
                    strRES = "File not found";
                    return Ok(new { Code = "1", message = strRES });
                }
            }
            catch (Exception ex)
            {
                strRES = "Exception on MESAPI DeleteFile: " + ex.Message;
                return Ok(new Output { Code = "0", Message = strRES });
            }
            finally
            {
                log.WriteLog(HttpContext.Current.Server.MapPath("~") + @"\MESAPI_LOG\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ": DELETE FILE" +
                               "\r\n- IN_EVENT: " + strIN_EVENT +
                               "\r\n- IN_FILE_NAME: " + strFileName +
                               "\r\n=> RES: " + strRES +
                               "\r\n-------------------------------------\r\n");
            }
        }

        public async Task<IHttpActionResult> ManageFile()
        {
            string strIPClient = HttpContext.Current.Request.UserHostAddress == "::1" ? "Debug" : HttpContext.Current.Request.UserHostAddress;
            string strRES = string.Empty;
            string inputJson = string.Empty;
            string strIN_EVENT = string.Empty;
            string strIN_PATH_FILE = string.Empty;
            string path_folder = string.Empty;
            string file_name = string.Empty;
            string file_name_new = string.Empty;
            string map_path = HttpContext.Current.Server.MapPath("~");
            object labelInfo = new
            {
                FormVariables = "",
                FreeVariables = "",
                Formulas = "",
                Barcodes = "",
                Texts = ""
            };

            try
            {
                if (Request.Content.IsMimeMultipartContent()) // kiểm tra xem nội dung của yêu cầu (request) có phải là dạng multipart/form-data hay không
                {
                    var provider = new MultipartFormDataStreamProvider(map_path + @"\UPLOAD_FILES");
                    await Request.Content.ReadAsMultipartAsync(provider);
                    inputJson = provider.FormData["DATA_INPUT"];
                    if (string.IsNullOrEmpty(inputJson))
                    {
                        strRES = "DATA_INPUT is null";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    Hashtable ht = JsonConvert.DeserializeObject<Hashtable>(inputJson);

                    try
                    {
                        strIN_EVENT = ht["IN_EVENT"].ToString().Trim();
                        if (String.IsNullOrEmpty(strIN_EVENT))
                        {
                            strRES = "IN_EVENT is null";
                            return Ok(new Output { Code = "1", Message = strRES });
                        }
                    }
                    catch
                    {
                        strRES = "IN_EVENT not exist in JSON input";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    try
                    {
                        strIN_PATH_FILE = ht["IN_PATH_FILE"].ToString().Trim();
                        if (String.IsNullOrEmpty(strIN_PATH_FILE))
                        {
                            strRES = "IN_PATH_FILE is null";
                            return Ok(new Output { Code = "1", Message = strRES });
                        }
                    }
                    catch
                    {
                        strRES = "IN_PATH_FILE not exist in JSON input";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    path_folder = map_path + @"\UPLOAD_FILES\" + strIN_PATH_FILE;

                    if (strIN_EVENT == "UPLOAD")
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(path_folder)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(path_folder));
                        }
                        foreach (var file in provider.FileData)
                        {
                            if (File.Exists(path_folder))
                            {
                                strRES = "File already exist";
                                return Ok(new Output { Code = "1", Message = strRES });
                            }
                            else
                            {
                                File.Move(file.LocalFileName, path_folder);
                                File.Delete(file.LocalFileName);
                                strRES = "OK Uploaded success";
                            }
                        }
                    }
                }
                else
                {
                    inputJson = await Request.Content.ReadAsStringAsync();
                    Hashtable ht = JsonConvert.DeserializeObject<Hashtable>(inputJson);

                    try
                    {
                        strIN_EVENT = ht["IN_EVENT"].ToString().Trim();
                        if (String.IsNullOrEmpty(strIN_EVENT))
                        {
                            strRES = "IN_EVENT is null";
                            return Ok(new Output { Code = "1", Message = strRES });
                        }
                    }
                    catch
                    {
                        strRES = "IN_EVENT not exist in JSON input";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    try
                    {
                        strIN_PATH_FILE = ht["IN_PATH_FILE"].ToString().Trim();
                        if (String.IsNullOrEmpty(strIN_PATH_FILE))
                        {
                            strRES = "IN_PATH_FILE is null";
                            return Ok(new Output { Code = "1", Message = strRES });
                        }
                    }
                    catch
                    {
                        strRES = "IN_PATH_FILE not exist in JSON input";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    path_folder = map_path + @"\UPLOAD_FILES\" + strIN_PATH_FILE;

                    if (!File.Exists(path_folder))
                    {
                        strRES = "The path file (" + path_folder + ") not exist.";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    if (strIN_EVENT == "DELETE")
                    {
                        File.Delete(path_folder);
                        strRES = "OK delete file success!";
                    }
                    else if (strIN_EVENT == "CHECK_FILE")
                    {
                        try
                        {
                            labelInfo = CheckLabel.GetInfo(path_folder);
                            strRES = "OK check file success!";
                        }
                        catch (Exception ex)
                        {
                            strRES = "Codesoft not install => Exception: " + ex.Message;
                        }
                    }
                    else if (strIN_EVENT == "RENAME")
                    {
                        try
                        {
                            file_name_new = ht["FILE_NAME_NEW"].ToString().Trim();
                            if (String.IsNullOrEmpty(file_name_new))
                            {
                                strRES = "FILE_NAME_NEW is null";
                                return Ok(new Output { Code = "1", Message = strRES });
                            }
                        }
                        catch
                        {
                            strRES = "FILE_NAME_NEW not exist in JSON input";
                            return Ok(new Output { Code = "1", Message = strRES });
                        }

                        string path_file_new = Path.Combine(Path.GetDirectoryName(path_folder), file_name_new);
                        File.Move(path_folder, path_file_new);
                        strRES = "OK rename file success!";
                    }
                    else
                    {
                        strRES = "IN_EVENT invalid";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                }

                return Ok(new Output { Code = "1", Message = strRES, Data = labelInfo });
            }
            catch (Exception ex)
            {
                strRES = "Exception on MESAPI ManageFile: " + ex.Message;
                return Ok(new Output { Code = "0", Message = strRES });
            }
            finally
            {
                log.WriteLog(map_path + @"\MESAPI_LOG\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy / MM / dd HH: mm: ss") + ": MANAGE FILE" +
                            "\r\n- IN_EVENT: " + strIN_EVENT +
                            "\r\n- IN_FILE: " + path_folder + 
                            (strIN_EVENT == "RENAME" ? "\r\n- FILE_NAME_NEW: " + file_name_new : "") +
                            "\r\n=> RES: " + strRES +
                            "\r\n-------------------------------------\r\n");
            }
        }

        public IHttpActionResult SvnAPI(JObject inputJson)
        {
            string strIPClient = HttpContext.Current.Request.UserHostAddress == "::1" ? "Debug" : HttpContext.Current.Request.UserHostAddress;
            string strIN_EVENT = string.Empty;
            string strIN_DATA = string.Empty;
            string strRES = string.Empty;
            string path_root = HttpContext.Current.Server.MapPath("~");

            try
            {
                Hashtable ht = new Hashtable();
                DataTable dt = new DataTable();
                string svn_user = string.Empty;
                string svn_pwd = string.Empty;
                string local_folder = string.Empty;
                string svn_url = string.Empty;
                string svn_path = string.Empty;
                string local_path = string.Empty;
                string comment = string.Empty;
                string svn_path_file = string.Empty;
                string local_path_file = string.Empty;
                ht = JsonConvert.DeserializeObject<Hashtable>(Convert.ToString(inputJson));
                if (String.IsNullOrEmpty(strIPClient))
                {
                    strRES = "Can't get IP client";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                try
                {
                    strIN_EVENT = ht["IN_EVENT"].ToString().Trim();
                    if (String.IsNullOrEmpty(strIN_EVENT))
                    {
                        strRES = "IN_EVENT is null";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                }
                catch
                {
                    strRES = "IN_EVENT not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                if (strIN_EVENT.Trim() == "GET_IP")
                {
                    return Ok(new Output { Code = "1", Message = strIPClient });
                }

                try
                {
                    strIN_DATA = ht["IN_DATA"].ToString();
                    if (String.IsNullOrEmpty(strIN_DATA))
                    {
                        strRES = "IN_DATA is null";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                }
                catch
                {
                    strRES = "IN_DATA not exist in JSON input";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                try
                {
                    dt = JsonConvert.DeserializeObject<DataTable>(strIN_DATA.Trim());
                    if (strIN_DATA.Substring(0, 1) != "[")
                    {
                        strRES = "IN_DATA is not JSON format";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                }
                catch
                {
                    strRES = "IN_DATA is not JSON format";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                try
                {
                    svn_user = dt.Rows[0]["SVN_USER"].ToString();
                    if (String.IsNullOrEmpty(svn_user))
                    {
                        strRES = "SVN_USER is null";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                }
                catch
                {
                    strRES = "SVN_USER not exist in JSON IN_DATA";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                try
                {
                    svn_pwd = dt.Rows[0]["SVN_PWD"].ToString();
                    if (String.IsNullOrEmpty(svn_pwd))
                    {
                        strRES = "SVN_PWD is null";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }
                    strIN_DATA = strIN_DATA.Replace(svn_pwd, "Password");
                }
                catch
                {
                    strRES = "SVN_PWD not exist in JSON IN_DATA";
                    return Ok(new Output { Code = "1", Message = strRES });
                }

                SVNClient svn = new SVNClient(svn_user, svn_pwd);

                if (strIN_EVENT == "CHECK_EXIST_LOCAL_FOLDER")
                {
                    try
                    {
                        local_folder = dt.Rows[0]["LOCAL_FOLDER"].ToString();
                    }
                    catch
                    {
                        strRES = "LOCAL_FOLDER not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    strRES = svn.CheckExistSvnLocalFolder(local_folder);
                }
                else if (strIN_EVENT == "CHECK_EXIST_URL")
                {
                    try
                    {
                        svn_url = dt.Rows[0]["SVN_URL"].ToString();
                    }
                    catch
                    {
                        strRES = "SVN_URL not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    strRES = svn.CheckExistSvnUrl(svn_url);
                }
                else if (strIN_EVENT == "GET_LOCAL_FOLDER_INFO")
                {
                    try
                    {
                        local_folder = dt.Rows[0]["LOCAL_FOLDER"].ToString();
                    }
                    catch
                    {
                        strRES = "LOCAL_FOLDER not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    strRES = svn.CheckExistSvnLocalFolder(local_folder);
                    if (strRES.ToUpper().Substring(0, 2) == "OK")
                    {
                        ht = svn.GetSvnLocalFolderInfo(local_folder);
                        strRES = ht["Status"].ToString().Trim();
                        return Ok(new Output { Code = "1", Message = strRES, Data = ht });
                    }
                }
                else if (strIN_EVENT == "GET_URL_INFO")
                {
                    try
                    {
                        svn_url = dt.Rows[0]["SVN_URL"].ToString();
                    }
                    catch
                    {
                        strRES = "SVN_URL not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    strRES = svn.CheckExistSvnUrl(svn_url);
                    if (svn.CheckExistSvnUrl(svn_url).ToUpper().Substring(0, 2) == "OK")
                    {
                        ht = svn.GetSvnUrlInfo(svn_url);
                        strRES = ht["Status"].ToString().Trim();
                        return Ok(new Output { Code = "1", Message = strRES, Data = ht });
                    }
                }
                else if (strIN_EVENT == "IMPORT_FOLDER")
                {
                    try
                    {
                        svn_path = dt.Rows[0]["SVN_PATH"].ToString();
                    }
                    catch
                    {
                        strRES = "SVN_PATH not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    try
                    {
                        local_path = dt.Rows[0]["LOCAL_PATH"].ToString();
                    }
                    catch
                    {
                        strRES = "LOCAL_PATH not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    try
                    {
                        comment = dt.Rows[0]["COMMENT"].ToString();
                    }
                    catch
                    {
                        strRES = "COMMENT not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    if (Directory.Exists(local_path))
                    {
                        strRES = svn.SvnImportFolder(svn_path, local_path, comment);
                    }
                    else
                    {
                        strRES = "The local path (" + local_path + ") not exist";
                    }
                }
                else if (strIN_EVENT == "IMPORT_FILE")
                {
                    try
                    {
                        svn_path_file = dt.Rows[0]["SVN_PATH_FILE"].ToString();
                    }
                    catch
                    {
                        strRES = "SVN_PATH_FILE not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    try
                    {
                        local_path_file = dt.Rows[0]["LOCAL_PATH_FILE"].ToString();
                    }
                    catch
                    {
                        strRES = "LOCAL_PATH_FILE not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    try
                    {
                        comment = dt.Rows[0]["COMMENT"].ToString();
                    }
                    catch
                    {
                        strRES = "COMMENT not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    if (File.Exists(local_path_file))
                    {
                        strRES = svn.SvnImportFile(svn_path_file, local_path_file, comment);
                    }
                    else
                    {
                        strRES = "The local path file (" + local_path_file + ") not exist";
                    }
                }
                else if (strIN_EVENT == "CHECKOUT")
                {
                    try
                    {
                        svn_path = dt.Rows[0]["SVN_PATH"].ToString();
                    }
                    catch
                    {
                        strRES = "SVN_PATH not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    try
                    {
                        local_path = dt.Rows[0]["LOCAL_PATH"].ToString();
                    }
                    catch
                    {
                        strRES = "LOCAL_PATH not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    strRES = svn.CheckExistSvnUrl(svn_path);
                    if (strRES.ToUpper().Substring(0, 2) == "OK")
                    {
                        strRES = svn.SvnCheckOut(svn_path, local_path);
                    }
                }
                else if (strIN_EVENT == "UPDATE")
                {
                    try
                    {
                        local_path = dt.Rows[0]["LOCAL_PATH"].ToString();
                    }
                    catch
                    {
                        strRES = "LOCAL_PATH not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    strRES = svn.CheckExistSvnLocalFolder(local_path);
                    if (strRES.ToUpper().Substring(0, 2) == "OK")
                    {
                        strRES = svn.SvnUpdate(local_path);
                    }
                }
                else if (strIN_EVENT == "COMMIT")
                {
                    try
                    {
                        local_path = dt.Rows[0]["LOCAL_PATH"].ToString();
                    }
                    catch
                    {
                        strRES = "LOCAL_PATH not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    try
                    {
                        comment = dt.Rows[0]["COMMENT"].ToString();
                    }
                    catch
                    {
                        strRES = "COMMENT not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    strRES = svn.CheckExistSvnLocalFolder(local_path);
                    if (strRES.ToUpper().Substring(0, 2) == "OK")
                    {
                        strRES = svn.SvnCommit(local_path, comment);
                    }
                }
                else if (strIN_EVENT == "DELETE")
                {
                    try
                    {
                        svn_url = dt.Rows[0]["SVN_URL"].ToString();
                    }
                    catch
                    {
                        strRES = "SVN_PATH_FILE not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    try
                    {
                        comment = dt.Rows[0]["COMMENT"].ToString();
                    }
                    catch
                    {
                        strRES = "COMMENT not exist in JSON IN_DATA";
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    strRES = svn.CheckExistSvnUrl(svn_url);
                    if (strRES.ToUpper().Substring(0, 2) == "OK")
                    {
                        strRES = svn.SvnDelete(svn_url, comment);
                    }
                }
                else
                {
                    strRES = "IN_EVENT (" + strIN_EVENT + ") not exist";
                }

                return Ok(new Output { Code = "1", Message = strRES });
            }
            catch (Exception ex)
            {
                strRES = "Exception on MESAPI SVNClient: " + ex.Message;
                return Ok(new Output { Code = "0", Message = strRES });
            }
            finally
            {
                if (!strIN_EVENT.Contains("NO_LOG"))
                {
                    log.WriteLog(path_root + @"\MESAPI_LOG\SVN\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ": SVN_Client" +
                                   "\r\n- IN_EVENT: " + strIN_EVENT +
                                   "\r\n- IN_DATA: " + strIN_DATA +
                                   "\r\n=> RES: " + strRES +
                                   "\r\n-------------------------------------\r\n");
                }
            }
        }
    }
}