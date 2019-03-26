using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyAuditApplication
{
    public class Object
    {
        public string Id { get; set; }
        public double Timestamp { get; set; }
        public string Type { get; set; }
        public string Changes { get; set; }
    }
}