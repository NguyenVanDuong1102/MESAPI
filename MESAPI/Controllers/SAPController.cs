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
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace MES.Controllers
{
    /// <summary>
    /// MES API for all system
    /// </summary>
    public class SAPController : ApiController
    {
        public LogFile log = new LogFile();
        public IHttpActionResult SAPConnection(object inputJson)
        {
            string strIPClient = HttpContext.Current.Request.UserHostAddress == "::1" ? "Debug" : HttpContext.Current.Request.UserHostAddress;
            string strIN_EVENT = string.Empty;
            string strIN_DATA = string.Empty;
            string strRES = string.Empty;
            string strIN_RFC = string.Empty;
            try
            {
                ConvertData convertData = new ConvertData();
                DataTable dt = new DataTable();
                Hashtable ht = JsonConvert.DeserializeObject<Hashtable>(Convert.ToString(inputJson));
                SAPConnection sap = new SAPConnection("NSGBG", "MESEDICU", "800", "10.134.108.111", "CNSBG_800", "CNP", "EN", 00);
                List<string> listTables = new List<string>();
                List<SAPInput> listInputs = new List<SAPInput>();
                List<SAPOutput> listOutputs = new List<SAPOutput>();

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

                DataTable dt_params = new DataTable();
                DataTable dataTable = new DataTable();
                if (strIN_EVENT == "WHS_INVENTORY")
                {
                    /*--------------------JSON FORMAT---------------------*
                    inputJson = {
                                    "IN_EVENT": "WHS_INVENTORY",
                                    "IN_DATA": [
                                        {
                                            "PLANT": "VNEB",
                                            "LGORT": "B24F"
                                        },
                                        {
                                            "PLANT": "VNER",
                                            "LGORT": "BGFG"
                                        },
                                        {
                                            "PLANT": "VNEB",
                                            "LGORT": "BGFG"
                                        }
                                    ]
                                }

                    output =    {
                                    "Code": "1",
                                    "Message": "OK",
                                    "Data": [
                                        {
                                            "MESSAGE": "NO TABLE RETURN"
                                        }
                                    ]
                                }
                    => Code = 1 -> API execute normal 
                       Code = 0 -> API execute happen Exception
                    *--------------------/JSON FORMAT---------------------*/
                    strIN_RFC = "ZCMM_NSBG_0025";
                    //Add Input parameters
                    dt_params = JsonConvert.DeserializeObject<DataTable>(strIN_DATA);
                    listInputs.Clear();
                    DataTable dt_temp = new DataTable();
                    for (int i = 0; i < dt_params.Rows.Count; i++)
                    {
                        listInputs.Clear();
                        listInputs.Add(new SAPInput(dt_params.Columns[0].Caption, dt_params.Rows[i][0].ToString()));

                        dataTable = new DataTable();
                        dataTable.Columns.Add(dt_params.Columns[1].Caption, typeof(string));
                        dataTable.Rows.Add(dataTable.NewRow()[dt_params.Columns[1].Caption] = dt_params.Rows[i][1].ToString());
                        dataTable.TableName = "IN_LOC";

                        Thread thread = new Thread(() =>
                        {
                            try
                            {
                                dt_temp = new DataTable();
                                dt_temp = sap.SAPDownload(strIN_RFC, "OUT_STOCK", listInputs, dataTable);
                                if (i == 0)
                                {
                                    dt = dt_temp;
                                }
                                else
                                {
                                    for (int j = 0; j < dt_temp.Rows.Count; j++)
                                    {
                                        dt.Rows.Add(dt_temp.Rows[j].ItemArray);
                                    }
                                }
                            }
                            catch
                            {

                            }
                        });
                        thread.SetApartmentState(ApartmentState.STA);
                        thread.Start();
                        thread.Join();
                    }
                    strRES = "OK";
                    /*SAPConnection sap = new SAPConnection("NSGBG", "MESEDICU", "800", "10.134.108.111", "CNSBG_800", "CNP", "EN", 00);
                    DataTable dt = new DataTable();
                    List<string> listTables = new List<string>();
                    List<SAPInput> listInputs = new List<SAPInput>();
                    List<SAPOutput> listOutputs = new List<SAPOutput>();
                    //Add Input parameters
                    listInputs.Clear();
                    listInputs.Add(new SAPInput("PLANT", "VNEB"));
                    DataTable dt_input = new DataTable("IN_LOC");
                    dt_input.Columns.Add("LGORT", typeof(string));
                    dt_input.Rows.Add(dt_input.NewRow()["LGORT"] = "B24F");
                    Thread thread = new Thread(() =>
                    {
                        try
                        {
                            dt = sap.SAPDownload("ZCMM_NSBG_0025", "OUT_STOCK", listInputs, dt_input);
                        }
                        catch
                        {
                        
                        }
                    });
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();*/
                }
                else
                {
                    strRES = "The IN_EVENT (" + strIN_EVENT + ") not exist";
                }
                return Ok(new Output { Code = "1", Message = strRES, Data = dt });
            }
            catch (Exception ex)
            {
                strRES = "Exception on MESAPI CallAPI: " + ex.Message;
                return Ok(new Output { Code = "0", Message = strRES });
            }
            finally
            {
                log.WriteLog(HttpContext.Current.Server.MapPath("~") + @"\MESAPI_LOG\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ": " + strIN_RFC +
                               "\r\n- IN_EVENT: " + strIN_EVENT +
                               "\r\n- IN_RFC: " + strIN_RFC +
                               "\r\n=> RES: " + strRES +
                               "\r\n-------------------------------------\r\n");
            }
        }
        public IHttpActionResult CallAPI(object inputJson)
        {
            SAPHelper sapHelper = new SAPHelper();
            Hashtable ht = JsonConvert.DeserializeObject<Hashtable>(Convert.ToString(inputJson));
            string strRES = "OK";
            string strIN_DB = string.Empty;
            string strIN_RFC = string.Empty;
            string strIN_TABLE = string.Empty;
            string strIN_DATA = string.Empty;
            string strIN_DATA_TABLE = string.Empty;
            string strIPClient = HttpContext.Current.Request.UserHostAddress == "::1" ? "Debug" : HttpContext.Current.Request.UserHostAddress;

            try
            {
                strIN_DB = string.IsNullOrWhiteSpace(ht["IN_DB"]?.ToString().Trim()) ? throw new ArgumentException("IN_DB is missing or empty") : ht["IN_DB"].ToString().Trim();
                strIN_RFC = string.IsNullOrWhiteSpace(ht["IN_RFC"]?.ToString().Trim()) ? throw new ArgumentException("IN_RFC is missing or empty") : ht["IN_RFC"].ToString().Trim();
                strIN_DATA = string.IsNullOrWhiteSpace(ht["IN_DATA"]?.ToString().Trim()) ? throw new ArgumentException("IN_DATA is missing or empty") : ht["IN_DATA"].ToString().Trim();
                var inJsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(strIN_DATA);
                List<SAPParameter> listParams = inJsonData.Select(entry => new SAPParameter(entry.Key, entry.Value.ToString())).ToList();

                if (ht["IN_EVENT"]?.ToString().Trim() == "DOWNLOAD")
                {
                    strIN_TABLE = string.IsNullOrWhiteSpace(ht["IN_TABLE"]?.ToString().Trim()) ? throw new ArgumentException("IN_TABLE is missing or empty") : ht["IN_TABLE"].ToString().Trim();
                    DataTable dt = new DataTable();
                    Thread thread = new Thread(() =>
                    {
                        dt = sapHelper.DownloadFromSAP(strIN_RFC, strIN_TABLE, listParams, strIN_DB);
                    });
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();

                    return Ok(new Output { Code = "1", Message = strRES, Data = dt });
                }
                else if (ht["IN_EVENT"]?.ToString().Trim() == "UPLOAD")
                {
                    DataTable dt = new DataTable();
                    if (ht.ContainsKey("IN_DATA_TABLE"))
                    {
                        try
                        {
                            strIN_DATA_TABLE = string.IsNullOrWhiteSpace(ht["IN_DATA_TABLE"]?.ToString().Trim()) ? throw new ArgumentException("IN_TABLE is missing or empty") : ht["IN_DATA_TABLE"].ToString().Trim();
                            dt = JsonConvert.DeserializeObject<DataTable>(strIN_DATA_TABLE);
                        }
                        catch
                        {
                            strRES = "IN_DATA_TABLE format is not right";
                            return Ok(new Output { Code = "0", Message = strRES });
                        }
                    }
                    List<SAPOutPut> listOutput = new List<SAPOutPut>
                    {
                        new SAPOutPut("O_FLAG"),
                        new SAPOutPut("O_MESSAGE")
                    };
                    bool uploadSuccess = true;
                    Thread thread = new Thread(() =>
                    {
                        if(dt.Rows.Count > 0)
                        {
                            uploadSuccess = sapHelper.UploadToSAP(strIN_RFC, listParams, dt, ref listOutput, strIN_DB);
                        }
                        else
                        {
                            uploadSuccess = sapHelper.UploadToSAP(strIN_RFC, listParams, ref listOutput, strIN_DB);
                        }
                    });
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();

                    if (!uploadSuccess)
                    {
                        if(listOutput[1].Value != null)
                        {
                            strRES = listOutput[1].Value;
                        }
                        else
                        {
                            strRES = "Upload to SAP failed!";
                        }
                        return Ok(new Output { Code = "1", Message = strRES });
                    }

                    strRES = listOutput[0].Value == "0" ? "OK " + listOutput[1].Value : "NG " + listOutput[1].Value;
                    return Ok(new Output { Code = "1", Message = strRES });
                }
                else
                {
                    strRES = "IN_EVENT not recognized";
                    return Ok(new Output { Code = "1", Message = strRES });
                }
            }
            catch (Exception ex)
            {
                strRES = "Exception: " + ex.Message;
                return Ok(new Output { Code = "0", Message = strRES });
            }
            finally
            {
                log.WriteLog(HttpContext.Current.Server.MapPath("~") + @"\MES_LOG\" + DateTime.Now.ToString("yyyyMMdd"), strIPClient + ".txt", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ": Call API SAP" +
                               "\r\n- IN_DB: " + strIN_DB +
                               "\r\n- IN_RFC: " + strIN_RFC +
                               "\r\n- IN_TABLE: " + strIN_TABLE +
                               "\r\n- IN_DATA: " + strIN_DATA +
                               "\r\n=> RES: " + strRES +
                               "\r\n-------------------------------------\r\n");
            }
        }
    }
}