using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCrawler
{
    class MessageRawData
    {
        public UInt64 msgid {get; set;}
        public UInt64 fakeid { get; set; }
        public string remark_name { get; set; }
        public string message_time { get; set; }
        public string message_content { get; set; }
        public string starred { get; set; }
    }
}
