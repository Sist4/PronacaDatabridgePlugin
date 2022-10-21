using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PronacaPlugin
{
    public class Secuencia
    {
        public Secuencia()
        {
            Id = "";
            IdCorreo = "";
            Fecha = DateTime.Now;
            Operador = "";
            Razon = "";
            Bascula = 0;
            PesoObtenido = "0";
            PesoBascula ="0";
        }

        public string Id { get; set; }
        public string IdCorreo { get; set; }
        public DateTime Fecha { get; set; }
        public string Operador { get; set; }
        public string Razon { get; set; }
        public int Bascula { get; set; }
        public string PesoObtenido { get; set; }
        public string PesoBascula { get; set; }

    }
}
