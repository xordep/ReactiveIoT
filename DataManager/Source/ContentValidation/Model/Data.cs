using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataManager.Model
{
    public class Data
    {
        public Guid Id { get; set; }
        public int IdTransaction { get; set; }
        public string Token { get; set; }
        public int Value { get; set; }

    }
}
