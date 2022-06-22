using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PronacaPlugin
{
    class ExcepcionNegocio : Exception
    {
        public ExcepcionNegocio(string message) : base(message)
        {
        }
        public ExcepcionNegocio(string message, Exception innerException) : base(message, innerException)
        {
        }
        protected ExcepcionNegocio(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
