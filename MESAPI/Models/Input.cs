namespace MES.Models
{
    /// <summary>
    /// List input parameter
    /// </summary>
    public class Input
    {
        /// <summary>
        /// DB info (It's need same with data in Web.config of MES API)
        /// </summary>
        public string IN_DB { get; set; }

        /// <summary>
        /// Stored Procedures name (SFIS1.SMTLOADING)
        /// </summary>
        public string IN_SP { get; set; }

        /// <summary>
        /// Event name in SP
        /// </summary>
        public string IN_EVENT{ get; set; }

        /// <summary>
        /// Data Input
        /// </summary>
        public string IN_DATA { get; set; }
    }
}