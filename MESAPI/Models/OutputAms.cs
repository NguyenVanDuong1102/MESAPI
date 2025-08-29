using System.Collections.Generic;

namespace MES.Models
{
    public class OutputAmsMessage
    {
        public string Message { get; set; }
    }

    public class OutputAmsEmp
    {
        public string ApplicationID { get; set; }
        public object EmpList { get; set; }
    }

    public class OutputAmsRole
    {
        public string ApplicationID { get; set; }
        public object RoleList { get; set; }
    }
}