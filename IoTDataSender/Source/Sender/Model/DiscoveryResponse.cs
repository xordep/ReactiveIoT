using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sender.Model
{
    public class DiscoveryResponse
    {
        public Guid Id { get; set; }
        public string Salt { get; set; }
    }
}
