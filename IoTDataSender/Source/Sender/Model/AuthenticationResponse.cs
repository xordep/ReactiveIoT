using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sender.Model
{
    public class AuthenticationResponse
    {
        public Guid Id { get; set; }
        public string Token { get; set; }
    }
}
