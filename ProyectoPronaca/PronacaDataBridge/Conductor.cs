using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PronacaPlugin
{
    public class Conductor
    {
        public Conductor()
        {
            Id = 0;
            Fecha = DateTime.Now;
            Dispositivo = "";
            Planta = "";
            Cedula = "";
            Nombre = "";
            Estado = "";
        }

        public Conductor(int id, DateTime fecha, string dispositivo, string planta, string cedula, string nombre, string estado)
        {
            Id = id;
            Fecha = fecha;
            Dispositivo = dispositivo;
            Planta = planta;
            Cedula = cedula;
            Nombre = nombre;
            Estado = estado;
        }

        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Dispositivo { get; set; }
        public string Planta { get; set; }
        public string Cedula { get; set; }
        public string Nombre { get; set; }
        public string Estado { get; set; }
    }
}
