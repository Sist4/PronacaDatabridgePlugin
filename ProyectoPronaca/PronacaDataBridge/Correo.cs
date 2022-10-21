using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace PronacaPlugin
{
    public class Correo
    {
        public Correo()
        {
            Id = "";
            Fecha = DateTime.Now;
            Tipo = "";
            Placa ="";
            Pin ="";
            Asunto = "";
            Remitente ="";
            Destinatarios ="";
            RutaImagen1 = "";
            RutaImagen2 = "";

        }

        public string Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; }
        public string Placa { get; set; }
        public string Pin { get; set; }
        public string Asunto { get; set; }
        public string Remitente { get; set; }
        public string Destinatarios { get; set; }
        public string RutaImagen1 { get; set; }
        public string RutaImagen2 { get; set; }


    }
}
