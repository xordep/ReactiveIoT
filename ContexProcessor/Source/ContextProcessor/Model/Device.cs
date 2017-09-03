using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContextProcessor.Model
{
    public class Device
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Enum.DeviceType Type { get; set; }
        public string Salt { get; set; }
        public string Token { get; set; }
    }
}
