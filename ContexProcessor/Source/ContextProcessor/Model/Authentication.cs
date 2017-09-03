using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContextProcessor.Model
{
    public class Authentication
    {
        public Guid Id { get; set; }
        public string Password { get; set; }
        public string BaseString { get; set; }
        public String Salt { get; set; }
        public String Token { get; set; }
    }
}
