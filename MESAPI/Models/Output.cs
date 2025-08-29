namespace MES.Models
{
    public class Output
    {
        //Code = 1 <=> OK
        //Code = 0 <=> Exception
        public string Code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}