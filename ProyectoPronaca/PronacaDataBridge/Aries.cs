using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PronacaPlugin
{
    internal class Aries
    {
        public Aries()
        {
            Id = "";
            IdTicket = "";
            Fecha = DateTime.Now;
            EstatusRecibido = 0;
            MensajeRecibido = "";
            XmlEnviado = "";
            XmlRecibido = "";
            Estado = "";
        }

        public string Id { get; set; }
        public string IdTicket { get; set; }
        public DateTime Fecha { get; set; }
        public int EstatusRecibido { get; set; }
        public string MensajeRecibido { get; set; }
        public string XmlEnviado { get; set; }
        public string XmlRecibido { get; set; }
        public string Estado { get; set; }
    }
}
