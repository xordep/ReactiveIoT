using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sender.Model
{
    public class Authentication
    {
        public Guid Id { get; set; }
        public string Password { get; set; }
        public string BaseString { get; set; }
    }
}
