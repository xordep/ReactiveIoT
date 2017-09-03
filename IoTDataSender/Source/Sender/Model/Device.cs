using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sender.Model
{
    public class Device
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Enum.DeviceType Type { get; set; }
    }
}
