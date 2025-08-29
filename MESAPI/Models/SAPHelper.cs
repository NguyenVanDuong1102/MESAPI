using SAPTableFactoryCtrl;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MES.Models
{
    /// <summary>
    /// Param input
    /// </summary>
    public class SAPParameter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public SAPParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }
        public string Name { get; set; }
        public object Value { get; set; }
    }
    /// <summary>
    /// Output
    /// </summary>
    public class SAPOutPut
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public SAPOutPut(string name)
        {
            Name = name;
            Value = "";
        }
        public SAPOutPut(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SAPHelper
    {
        /// <summary>
        /// 
        /// </summary>
        public SAPHelper()
        {
            this.sUser = "NSGBG";
            this.sPass = "MESEDICU";
            this.sClient = "800";
            this.sServer = ConfigurationSettings.AppSettings["SAP_PRIMARY_SERVER"];
            this.sGroupName = "CNSBG_800";
            this.sSystem = "CNP";
            this.sLanguage = "EN";
            this.sSystemNumber = 00;
            this.sException = "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sUser"></param>
        /// <param name="sPass"></param>
        /// <param name="sClient"></param>
        /// <param name="sServer"></param>
        /// <param name="sGroupName"></param>
        /// <param name="sSystem"></param>
        /// <param name="sLanguage"></param>
        /// <param name="sSystemNumber"></param>
        public SAPHelper(string sUser, string sPass, string sClient, string sServer, string sGroupName, string sSystem, string sLanguage, int sSystemNumber)
        {
            this.sUser = sUser;
            this.sPass = sPass;
            this.sClient = sClient;
            this.sServer = sServer;
            this.sGroupName = sGroupName;
            this.sSystem = sSystem;
            this.sLanguage = sLanguage;
            this.sSystemNumber = sSystemNumber;
            this.sException = "";
        }

        public string sUser { get; set; }
        public string sPass { get; set; }
        public string sClient { get; set; }
        public string sServer { get; set; }
        public string sGroupName { get; set; }
        public string sSystem { get; set; }
        public string sLanguage { get; set; }
        public int sSystemNumber { get; set; }
        public string sException { get; set; }

        /// <summary>
        /// Conver to DataTable
        /// </summary>
        /// <param name="itab"></param>
        /// <param name="colIndex"></param>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public static DataTable ToDataTable(SAPTableFactoryCtrl.Table itab, int colIndex = 0, string searchString = "")
        {
            DataTable dt = new DataTable();

            // Header
            for (int col = 1; col <= itab.ColumnCount; col++)
            {
                dt.Columns.Add(itab.ColumnName[col], typeof(string));
            }

            // Line items
            if (colIndex == 0)
            {
                for (int row = 1; row <= itab.RowCount; row++)
                {
                    DataRow dr = dt.NewRow();
                    for (int col = 1; col <= itab.ColumnCount; col++)
                    {
                        dr[col - 1] = itab.get_Cell(row, col);
                    }
                    dt.Rows.Add(dr);
                }
            }
            else
            {
                for (int row = 1; row <= itab.RowCount; row++)
                {
                    try
                    {
                        if (itab.get_Cell(row, colIndex).ToString().Equals(searchString) || itab.get_Cell(row, colIndex).ToString().Equals(searchString.PadLeft(12, '0')))
                        {
                            DataRow dr = dt.NewRow();
                            for (int col = 1; col <= itab.ColumnCount; col++)
                            {
                                dr[col - 1] = itab.get_Cell(row, col);
                            }
                            dt.Rows.Add(dr);
                        }
                    }
                    catch
                    {
                        DataRow dr = dt.NewRow();
                        for (int col = 1; col <= itab.ColumnCount; col++)
                        {
                            dr[col - 1] = itab.get_Cell(row, col);
                        }
                        dt.Rows.Add(dr);
                    }
                }
            }


            return dt;
        }

        /// <summary>
        /// Download data from SAP
        /// </summary>
        /// <param name="function"></param>
        /// <param name="tableName"></param>
        /// <param name="listParams"></param>
        /// <returns></returns>
        public DataTable DownloadFromSAP(string function, string tableName, List<SAPParameter> listParams, string in_db)
        {
            DataTable dtInput = null;
            int colIndex = 0;
            string searchString = "";
            DataTable dt = new DataTable();
            this.sException = "";
            SAPLogonCtrl.Connection connSAP = null;
            try
            {
                SAPLogonCtrl.SAPLogonControlClass login = new SAPLogonCtrl.SAPLogonControlClass();
                login.User = sUser;
                login.Password = sPass;
                login.Client = sClient;
                login.Language = sLanguage;
                login.GroupName = sGroupName;
                login.System = sSystem;
                login.SystemNumber = sSystemNumber;
                if (in_db.ToUpper().Contains("TEST"))
                {
                    login.ApplicationServer = ConfigurationSettings.AppSettings["SAP_TEST_SERVER"];
                }
                else
                {
                    login.MessageServer = sServer;
                }

                connSAP = (SAPLogonCtrl.Connection)login.NewConnection();
                bool IsLogin = connSAP.Logon(null, true);

                if (IsLogin)
                {
                    SAPFunctionsOCX.SAPFunctionsClass func = new SAPFunctionsOCX.SAPFunctionsClass();
                    func.Connection = connSAP;
                    SAPFunctionsOCX.IFunction ifunc = (SAPFunctionsOCX.IFunction)func.Add(function);

                    foreach (SAPParameter param in listParams)
                    {
                        SAPFunctionsOCX.IParameter sapParam = (SAPFunctionsOCX.IParameter)ifunc.get_Exports(param.Name);
                        sapParam.Value = param.Value;
                    }

                    if (dtInput != null && !string.IsNullOrEmpty(dtInput.TableName) && dtInput.Rows.Count > 0)
                    {
                        SAPTableFactoryCtrl.Tables tables = (SAPTableFactoryCtrl.Tables)ifunc.Tables;
                        SAPTableFactoryCtrl.Table tb = (SAPTableFactoryCtrl.Table)tables.get_Item(dtInput.TableName);
                        foreach (DataRow dr in dtInput.Rows)
                        {
                            tb.AppendGridData(1, 1, 1, dr[0].ToString());
                        }
                    }

                    ifunc.Call();

                    SAPTableFactoryCtrl.Tables resTables = (SAPTableFactoryCtrl.Tables)ifunc.Tables;
                    SAPTableFactoryCtrl.Table resTable = (SAPTableFactoryCtrl.Table)resTables.get_Item(tableName);

                    dt = ToDataTable(resTable, colIndex, searchString);
                }
                else
                {
                    this.sException = "Login fail";
                }
            }
            catch (Exception ex)
            {
                this.sException = $"Error: {ex.Message}\nStackTrace: {ex.StackTrace}";
            }
            finally
            {
                if (connSAP != null)
                {
                    connSAP.Logoff();  // Đảm bảo đóng kết nối
                }
            }
            return dt;
        }

        /// <summary>
        /// Upload data to SAP
        /// </summary>
        /// <param name="function"></param>
        /// <param name="listParams"></param>
        /// <param name="listOutput"></param>
        /// <param name="in_db"></param>
        /// <returns></returns>
        public bool UploadToSAP(string function, List<SAPParameter> listParams, ref List<SAPOutPut> listOutput, string in_db)
        {
            this.sException = "";
            try
            {
                SAPLogonCtrl.SAPLogonControlClass login = new SAPLogonCtrl.SAPLogonControlClass();
                login.User = sUser;
                login.Password = sPass;
                login.Client = sClient;
                login.Language = sLanguage;
                login.GroupName = sGroupName;
                login.System = sSystem;
                login.SystemNumber = sSystemNumber;
                if (in_db.ToUpper().Contains("TEST"))
                {
                    login.ApplicationServer = ConfigurationSettings.AppSettings["SAP_TEST_SERVER"];
                }
                else
                {
                    login.MessageServer = sServer;
                }

                SAPLogonCtrl.Connection connSAP = (SAPLogonCtrl.Connection)login.NewConnection();
                bool Islogin = connSAP.Logon(null, true);
                if (Islogin)
                {
                    SAPFunctionsOCX.SAPFunctionsClass func = new SAPFunctionsOCX.SAPFunctionsClass();
                    func.Connection = connSAP;
                    SAPFunctionsOCX.IFunction ifunc = (SAPFunctionsOCX.IFunction)func.Add(function);

                    foreach (SAPParameter param in listParams)
                    {
                        SAPFunctionsOCX.IParameter sapParam = (SAPFunctionsOCX.IParameter)ifunc.get_Exports(param.Name);
                        sapParam.Value = param.Value;
                    }

                    ifunc.Call();

                    foreach (SAPOutPut output in listOutput)
                    {
                        SAPFunctionsOCX.IParameter outputParams = (SAPFunctionsOCX.IParameter)ifunc.get_Imports(output.Name);
                        output.Value = outputParams.Value.ToString();
                    }

                    return true;
                }
                else
                {
                    this.sException = "SAP Login fail";
                }
            }
            catch (Exception ex)
            {
                this.sException = ex.Message;
            }
            return false;
        }

        public bool UploadToSAP(string function, List<SAPParameter> listParams, DataTable dataTable, ref List<SAPOutPut> listOutput, string in_db)
        {
            this.sException = "";
            try
            {
                SAPLogonCtrl.SAPLogonControlClass login = new SAPLogonCtrl.SAPLogonControlClass();
                login.User = sUser;
                login.Password = sPass;
                login.Client = sClient;
                login.Language = sLanguage;
                login.GroupName = sGroupName;
                login.System = sSystem;
                login.SystemNumber = sSystemNumber;
                if (in_db.ToUpper().Contains("TEST"))
                {
                    login.ApplicationServer = ConfigurationSettings.AppSettings["SAP_TEST_SERVER"];
                }
                else
                {
                    login.MessageServer = sServer;
                }

                SAPLogonCtrl.Connection connSAP = (SAPLogonCtrl.Connection)login.NewConnection();
                bool Islogin = connSAP.Logon(null, true);
                if (Islogin)
                {
                    SAPFunctionsOCX.SAPFunctionsClass func = new SAPFunctionsOCX.SAPFunctionsClass();
                    func.Connection = connSAP;
                    SAPFunctionsOCX.IFunction ifunc = (SAPFunctionsOCX.IFunction)func.Add(function);

                    foreach (SAPParameter param in listParams)
                    {
                        SAPFunctionsOCX.IParameter sapParam = (SAPFunctionsOCX.IParameter)ifunc.get_Exports(param.Name);
                        sapParam.Value = param.Value;
                    }
                    Tables tables = (Tables)ifunc.Tables;
                    Table options = (Table)tables.get_Item("IN_TAB");
                    try
                    {
                        if (dataTable != null && dataTable.Rows.Count > 0)
                        {
                            for (int i = 0; i < dataTable.Rows.Count; i++)
                            {
                                foreach (DataColumn column in dataTable.Columns)
                                {
                                    options.AppendGridData(1, 1, Convert.ToInt32(column.ColumnName), dataTable.Rows[i][column.ColumnName].ToString());
                                }
                            }
                        }
                    }
                    catch
                    {
                        foreach (SAPOutPut output in listOutput)
                        {
                            output.Value = "IN_DATA_TABLE format is not right";
                        }
                        return false;
                    }

                    ifunc.Call();

                    foreach (SAPOutPut output in listOutput)
                    {
                        SAPFunctionsOCX.IParameter outputParams = (SAPFunctionsOCX.IParameter)ifunc.get_Imports(output.Name);
                        output.Value = outputParams.Value.ToString();
                    }

                    return true;
                }
                else
                {
                    this.sException = "SAP Login fail";
                }
            }
            catch (Exception ex)
            {
                this.sException = ex.Message;
            }
            return false;
        }
    }
}