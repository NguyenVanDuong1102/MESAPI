using Newtonsoft.Json;
using System;
using System.Web.Http;
using MES.Models;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Reflection;
using System.Diagnostics;
using JackieLib;
using System.Web;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Data.OracleClient;
using System.Collections.Generic;
using System.Linq;

namespace AMS.Controllers
{
    public class EMP
    {
        public string EMPNO { get; set; }
    }
    /// <summary>
    /// MES API for all system
    /// </summary>
    public class AMSController : ApiController
    {
        public LogFile log = new LogFile();
        public NotesSendMail notesmail;
        /*------------Created by Jackie Than----------------*
        inputJson = {
                        "ApplicationID": "SFC_VNFH_BTS",
                        "EmpList": [
                            {
                                "EMPNO": "ALL"
                            }
                        ],
                        "Token": "@Token"
                    }

        inputJson = {
                        "ApplicationID": "SFC_VNFH_BTS",
                        "RoleList": [
                            "ALL"
                        ],
                        "Token": "@Token"
                    }

        output =    {
                        "ApplicationID": "SFC_VNFH_BTS",
                        "EmpList": [
                            {
                                "USERID": "V0980352",
                                "EMPNO": "V0980352",
                                "NAME": "申文權",
                                "GroupName": "IT",
                                "GroupRemark": "IT",
                                "EffectTime": "12/17/2024 2:11:00 PM"
                            }
                        ]
                    }

        output =    {
                        "ApplicationID": "SFC_VNFH_BTS",
                        "RoleList": [
                            {
                                "RoleName": "IT",
                                "RoleExplain": "",
                                "APOwner": "V1048123",
                                "RoleOwner": ""
                            }
                        ]
                    }
        *----------------------------------------------------*/
        /// <summary>
        /// API connect to Oracle function
        /// </summary>
        /// <param name="inputJson">Input Json</param>
        /// <returns>Data Output: Code = 1 -> API execute normal / Code = 0 -> API execute happen Exception</returns>
        public IHttpActionResult CallAPI(object inputJson) //Don't need declare Models => We can using dynamic or object or JObject
        {
            Hashtable htSP     = new Hashtable();
            DataTable dt       = new DataTable();
            string strIPClient = HttpContext.Current.Request.UserHostAddress == "::1" ? "Debug" : HttpContext.Current.Request.UserHostAddress;
            string strIN_APPLICATIONID = string.Empty;
            string strDB       = string.Empty;
            string strSP       = string.Empty;
            string strIN_EVENT = string.Empty;
            string strIN_DATA  = string.Empty;
            string strEmplist  = string.Empty;
            string strRoleList = string.Empty;
            string strToken    = string.Empty;
            string strRES      = string.Empty;
            string strConn     = string.Empty;
            try
            {
                Hashtable ht = JsonConvert.DeserializeObject<Hashtable>(Convert.ToString(inputJson));
                if (String.IsNullOrEmpty(strIPClient))
                {
                    strRES = "Can't get IP client";
                    return Ok(new OutputAmsMessage { Message = strRES });
                }

                try
                {
                    strIN_APPLICATIONID = ht["ApplicationID"].ToString().Trim();
                    if (String.IsNullOrEmpty(strIN_APPLICATIONID))
                    {
                        strRES = "ApplicationID is null";
                        return Ok(new OutputAmsMessage { Message = strRES });
                    }
                }
                catch
                {
                    strRES = "An error has occurred.";
                    return Ok(new OutputAmsMessage { Message = strRES });
                }

                try
                {
                    strToken = ht["Token"].ToString().Trim();
                }
                catch
                {
                    strToken = "@Token";
                }

                try
                {
                    strEmplist = ht["EmpList"].ToString().Trim();
                }
                catch
                {
                    strEmplist = string.Empty;
                }

                try
                {
                    strRoleList = ht["RoleList"].ToString().Trim();
                }
                catch
                {
                    strRoleList = string.Empty;
                }

                if (!String.IsNullOrEmpty(strEmplist))
                {
                    strIN_EVENT = "API_GET_EMP";
                    strIN_DATA = string.Join(",", JsonConvert.DeserializeObject<List<EMP>>(strEmplist).Select(e => e.EMPNO).ToList());
                }
                else if (!String.IsNullOrEmpty(strRoleList))
                {
                    strIN_EVENT = "API_GET_ROLE";
                    strIN_DATA = string.Join(",", JsonConvert.DeserializeObject<List<string>>(strRoleList));
                }
                else
                {
                    strRES = "An error has occurred.";
                    return Ok(new OutputAmsMessage { Message = strRES });
                }

                try
                {
                    try
                    {
                        strDB = ConfigurationSettings.AppSettings[strIN_APPLICATIONID].Trim();
                    }
                    catch
                    {
                        strRES = "APPLICATION_ID error: The AppSettings (" + strIN_APPLICATIONID + ") not exist in Web.config of API. Please call IT check!";
                        return Ok(new OutputAmsMessage { Message = strRES });
                    }
                    strConn = ConfigurationManager.ConnectionStrings[strDB].ConnectionString.Trim();
                }
                catch
                {
                    strRES = "IN_DB error: The Connection String (" + strDB + ") not exist in Web.config of API. Please call IT check!";
                    return Ok(new OutputAmsMessage { Message = strRES });
                }

                try
                {
                    strSP = (strDB.Substring(0, 2) == "AP" ? "MES1." : "SFIS1.") + ConfigurationSettings.AppSettings["SP_AMS_LOG"].Trim();
                    if (String.IsNullOrEmpty(strSP))
                    {
                        strRES = "IN_SP is null";
                        return Ok(new OutputAmsMessage { Message = strRES });
                    }
                }
                catch
                {
                    strRES = "SP_AMS_LOG error: The AppSettings (SP_AMS_LOG) not exist in Web.config of API. Please call IT check!";
                    return Ok(new OutputAmsMessage { Message = strRES });
                }

                SqlDB sqlDB = new SqlDB(strConn);
                dt = sqlDB.ExecuteDataTable("SELECT * FROM ALL_OBJECTS WHERE OBJECT_TYPE = 'PROCEDURE' AND OWNER = '" + strSP.Split('.')[0] + "' AND OBJECT_NAME = '" + strSP.Split('.')[1] + "'");
                if (dt.Rows.Count == 0)
                {
                    strRES = "IN_SP error: The SP: (" + strSP.ToUpper() + ") not exist on DB. Please call IT check!";
                    return Ok(new OutputAmsMessage { Message = strRES });
                }
                if (dt.Rows[0]["STATUS"].ToString().ToUpper() != "VALID")
                {
                    strRES = "IN_SP error: The SP: (" + strSP.ToUpper() + ") invalid. Please call IT check!";
                    return Ok(new OutputAmsMessage { Message = strRES });
                }

                htSP.Add("IN_EVENT", strIN_EVENT);
                htSP.Add("IN_DATA", strIN_DATA);
                htSP.Add("RES", "");
                htSP.Add("RES_TABLE", "");
                htSP = sqlDB.ExecuteSPReturnHashtable(strSP, htSP);
                strRES = htSP["RES"].ToString();
                if (strIN_EVENT == "API_GET_EMP")
                {
                    return Ok(new OutputAmsEmp { ApplicationID = strIN_APPLICATIONID, EmpList = (DataTable)htSP["RES_TABLE"] });
                }
                else if (strIN_EVENT == "API_GET_ROLE")
                {
                    return Ok(new OutputAmsRole { ApplicationID = strIN_APPLICATIONID, RoleList = (DataTable)htSP["RES_TABLE"] });
                }
                else
                {
                    return Ok(new OutputAmsMessage { Message = strRES });
                }
            }
            catch (Exception ex)
            {
                strRES = "Exception on AMSAPI CallAPI: " + ex.Message;
                return Ok(new OutputAmsMessage { Message = strRES });
            }
            finally
            {
                /*----------------------------------Save log-----------------------------------*/
                log.WriteLog(HttpContext.Current.Server.MapPath("~") + @"\MESAPI_LOG\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ": " + strSP +
                                   "\r\n- ApplicationID: " + strIN_APPLICATIONID +
                                   "\r\n- IN_EVENT: " + strIN_EVENT +
                                   "\r\n- IN_DATA: " + strIN_DATA +
                                   "\r\n=> RES: " + strRES +
                                   "\r\n-------------------------------------\r\n");
                /*---------------------------------/Save log-----------------------------------*/

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
                                    htSP_error.Add("IN_EVENT", strIN_APPLICATIONID);
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
                                   "\r\n- ApplicationID: " + strIN_APPLICATIONID +
                                   "\r\n- IN_EVENT: " + strIN_EVENT +
                                   "\r\n- IN_DATA: " + strIN_DATA +
                                   "\r\n=> RES: " + strRES +
                                   "\r\n-------------------------------------\r\n");
                    }
                }
                /*----------------------/Warning mail when SP have issue-----------------------*/
            }
        }
    }
}