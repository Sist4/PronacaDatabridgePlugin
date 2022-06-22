using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PronacaPlugin
{
    class ExcepcionSQL : Exception
    {
        public ExcepcionSQL(string message) : base(message)
        {
        }
        public ExcepcionSQL(string message, Exception innerException) : base(message, innerException)
        {
        }
        protected ExcepcionSQL(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
