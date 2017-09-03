using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.Threading.Tasks;

namespace Discovery.Model
{
    public class Device
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Enum.DeviceType Type { get; set; }
        public String Salt { get; set; }
    }
}
