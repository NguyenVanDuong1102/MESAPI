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
using System.Collections.Generic;

using System.Linq;

namespace APS.Controllers
{
    /// <summary>
    /// MES API for all system
    /// </summary>
    public class APSController : ApiController
    {
        public LogFile log = new LogFile();
        public IHttpActionResult CallAPI(JObject inputJson)
        {
            Hashtable ht = JsonConvert.DeserializeObject<Hashtable>(Convert.ToString(inputJson));
            Hashtable htSP = new Hashtable();
            DataTable dt = new DataTable();
            string strIPClient = HttpContext.Current.Request.UserHostAddress == "::1" ? "Debug" : HttpContext.Current.Request.UserHostAddress;
            string strSP = string.Empty;
            string strPlant = string.Empty;
            string product_list = string.Empty;
            string station_list = string.Empty;
            string strStartTime = string.Empty;
            string strEndTime = string.Empty;
            string strRES = string.Empty;
            string strConn = string.Empty;
            try
            {
                try
                {
                    strSP = ConfigurationSettings.AppSettings["SP_GET_APS"].Trim();
                    if (String.IsNullOrEmpty(strSP))
                    {
                        strRES = "IN_SP is null";
                        return Ok(new OutputAps { status = "1", message = strRES });
                    }
                }
                catch
                {
                    strRES = "The MESAPI not config SP (key = SP_AOI_APS) in Web.config file";
                    return Ok(new OutputAps { status = "1", message = strRES });
                }

                if (String.IsNullOrEmpty(strIPClient))
                {
                    strRES = "IN_IP is null";
                    return Ok(new OutputAps { status = "1", message = strRES });
                }

                if (strSP.IndexOf(".") == -1)
                {
                    strRES = "IN_SP error: The SP: (" + strSP.ToUpper() + ") format error. Please call IT check!";
                    return Ok(new OutputAps { status = "1", message = strRES });
                }
                try
                {
                    strConn = ConfigurationManager.ConnectionStrings["GET_APS"].ConnectionString.Trim();
                }
                catch
                {
                    strRES = "IN_DB error: The Connection String (GET_APS) not exist in Web.config of API. Please call IT check!";
                    return Ok(new OutputAps { status = "1", message = strRES });
                }
                SqlDB sqlDB = new SqlDB(strConn);
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                float versionOnWeb = Convert.ToInt64(fvi.FileVersion.Replace(".", ""));
                dt = sqlDB.ExecuteDataTable("SELECT AP_VERSION FROM SFISM4.AMS_AP WHERE AP_NAME = 'MESAPI'");
                if (dt.Rows.Count == 0)
                {
                    strRES = "The MESAPI not exist on AMS system. Please call IT check!";
                    return Ok(new OutputAps { status = "1", message = strRES });
                }

                float versionOnDB = Convert.ToInt64(dt.Rows[0][0].ToString().Replace(".", ""));
                if (versionOnWeb < versionOnDB)
                {
                    strRES = "MES API version on server (" + fvi.FileVersion + ") < version on AMS system (" + dt.Rows[0][0].ToString() + "). Please call IT to update MESAPI latest version!";
                    return Ok(new OutputAps { status = "1", message = strRES });
                }

                dt = sqlDB.ExecuteDataTable("SELECT * FROM ALL_OBJECTS WHERE OBJECT_TYPE = 'PROCEDURE' AND OWNER = '" + strSP.Split('.')[0] + "' AND OBJECT_NAME = '" + strSP.Split('.')[1] + "'");
                if (dt.Rows.Count == 0)
                {
                    strRES = "IN_SP error: The SP: (" + strSP.ToUpper() + ") not exist on DB. Please call IT check!";
                    return Ok(new OutputAps { status = "1", message = strRES });
                }
                if (dt.Rows[0]["STATUS"].ToString().ToUpper() != "VALID")
                {
                    strRES = "IN_SP error: The SP: (" + strSP.ToUpper() + ") invalid. Please call IT check!";
                    return Ok(new OutputAps { status = "1", message = strRES });
                }

                try
                {
                    strPlant = ht["plant"].ToString();
                    if (String.IsNullOrEmpty(strPlant))
                    {
                        strRES = "(plant) is null";
                        return Ok(new OutputAps { status = "1", message = strRES });
                    }
                }
                catch
                {
                    strRES = "(plant) not exist in JSON input";
                    return Ok(new OutputAps { status = "1", message = strRES });
                }

                try
                {
                    product_list = ht["product_list"].ToString();
                    if (String.IsNullOrEmpty(product_list) || product_list.Trim() == "[]")
                    {
                        strRES = "(product_list) is null";
                        return Ok(new OutputAps { status = "1", message = strRES });
                    }
                }
                catch
                {
                    strRES = "(product_list) not exist in JSON input";
                    return Ok(new OutputAps { status = "1", message = strRES });
                }

                try
                {
                    station_list = ht["station_list"].ToString();
                    if (String.IsNullOrEmpty(station_list))
                    {
                        strRES = "(station_list) is null";
                        return Ok(new OutputAps { status = "1", message = strRES });
                    }
                }
                catch
                {
                    strRES = "(station_list) not exist in JSON input";
                    return Ok(new OutputAps { status = "1", message = strRES });
                }

                try
                {
                    strStartTime = ht["start_time"].ToString();
                    if (String.IsNullOrEmpty(strStartTime))
                    {
                        strRES = "(start_time) is null";
                        return Ok(new OutputAps { status = "1", message = strRES });
                    }
                    else if (strStartTime.Trim().Length != 19)
                    {
                        strRES = "(start_time) format error";
                        return Ok(new OutputAps { status = "1", message = strRES });
                    }
                }
                catch
                {
                    strRES = "(start_time) not exist in JSON input";
                    return Ok(new OutputAps { status = "1", message = strRES });
                }

                try
                {
                    strEndTime = ht["end_time"].ToString();
                    if (String.IsNullOrEmpty(strEndTime))
                    {
                        strRES = "(end_time) is null";
                        return Ok(new OutputAps { status = "1", message = strRES });
                    }
                    else if (strEndTime.Trim().Length != 19)
                    {
                        strRES = "(end_time) format error";
                        return Ok(new OutputAps { status = "1", message = strRES });
                    }
                }
                catch
                {
                    strRES = "(end_time) not exist in JSON input";
                    return Ok(new OutputAps { status = "1", message = strRES });
                }

                List<string> products = JsonConvert.DeserializeObject<List<string>>(product_list);
                List<string> stations = JsonConvert.DeserializeObject<List<string>>(station_list);
                htSP.Add("IN_PLANT", strPlant);
                htSP.Add("IN_PRODUCT_LIST", string.Join(",", products));
                htSP.Add("IN_STATION_LIST", string.Join(",", stations));
                htSP.Add("IN_START_TIME", strStartTime.Replace("-", "").Replace(" ", "").Substring(0, 10));
                htSP.Add("IN_END_TIME", strEndTime.Replace("-", "").Replace(" ", "").Substring(0, 10));
                htSP.Add("RES", "");
                htSP.Add("RES_TABLE", "");
                htSP = sqlDB.ExecuteSPReturnHashtable(strSP, htSP);
                strRES = htSP["RES"].ToString();
                DataTable dataTable = new DataTable();
                dataTable = (DataTable)htSP["RES_TABLE"];
                
                Dictionary<string, List<ReportList>> ReportItem = new Dictionary<string, List<ReportList>>();
                var list_products = dataTable.AsEnumerable().GroupBy(q => q["MODEL_NAME"]).Select(group => group.Key.ToString()).ToList();
                foreach (var model in list_products)
                {
                    if (!ReportItem.ContainsKey(model))
                    {
                        ReportItem[model] = new List<ReportList>();
                    }
                    var model_detail = dataTable.AsEnumerable().Where(r => r["MODEL_NAME"].ToString() == model && r["MO_NUMBER"].ToString() == "TOTAL").Select(r => new
                    {
                        STEP = int.Parse(r["STEP"].ToString()),
                        MODEL_NAME = r["MODEL_NAME"].ToString(),
                        MO_NUMBER = r["MO_NUMBER"].ToString(),
                        GROUP_NAME = r["GROUP_NAME"].ToString(),
                        QTY = r["QTY"].ToString()
                    }).ToList();

                    var list_station = dataTable.AsEnumerable().Where(r => r["MODEL_NAME"].ToString() == model && r["MO_NUMBER"].ToString() != "TOTAL").Select(r => new
                    {
                        MODEL_NAME = r["MODEL_NAME"].ToString(),
                        MO_NUMBER = r["MO_NUMBER"].ToString(),
                        GROUP_NAME = r["GROUP_NAME"].ToString(),
                        QTY = r["QTY"].ToString()
                    }).ToList();

                    foreach (var station in list_station)
                    {
                        if (ReportItem[model].Where(r => r.station != null && r.station == station.GROUP_NAME).Any())
                        {
                            continue;
                        }
                        ReportList reportItem = new ReportList();
                        reportItem.serial_number = model_detail.AsEnumerable().Where(r => r.GROUP_NAME == station.GROUP_NAME).Select(r => r.STEP).FirstOrDefault();
                        reportItem.station = model_detail.AsEnumerable().Where(r => r.GROUP_NAME == station.GROUP_NAME).Select(r => r.GROUP_NAME).FirstOrDefault();
                        reportItem.total_quantity = int.Parse(model_detail.AsEnumerable().Where(r => r.GROUP_NAME == station.GROUP_NAME).Select(r => r.QTY).FirstOrDefault());

                        List<Detail> details = list_station.AsEnumerable().Where(r => r.GROUP_NAME == station.GROUP_NAME).Select(r => new Detail() { mo_no = r.MO_NUMBER, qty = int.Parse(r.QTY.ToString()) }).ToList();
                        reportItem.detail = details;
                        ReportItem[model].Add(reportItem);
                    }

                }
                Hashtable hashtable = new Hashtable();
                hashtable[strPlant] = ReportItem;
                return Ok(new OutputAps { status = "1", message = strRES, report_list = hashtable });
            }
            catch (Exception ex)
            {
                strRES = "Exception on MESAPI CallAPI: " + ex.Message;
                return Ok(new OutputAps { status = "0", message = strRES });
            }
            finally
            {
                log.WriteLog(HttpContext.Current.Server.MapPath("~") + @"\MESAPI_LOG\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ": " + strSP +
                               "\r\n- Plant: " + strPlant +
                               "\r\n- Product_List: " + product_list +
                               "\r\n- Station_List: " + station_list +
                               "\r\n- Start_Time: " + strStartTime +
                               "\r\n- End_Time: " + strEndTime +
                               "\r\n=> RES: " + strRES +
                               "\r\n-------------------------------------\r\n");
            }
        }
    }
}