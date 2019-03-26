using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyAuditApplication.Entities
{
    public enum ResultCode { Success=1,Error=0, NoRecordFound=2};
    public class Result
    {
        public ResultCode Code { get; set; }
        public string Message { get; set; }
    }
}