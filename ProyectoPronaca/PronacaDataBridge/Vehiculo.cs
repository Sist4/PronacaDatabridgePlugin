using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PronacaPlugin
{
    public class Vehiculo
    {
        public Vehiculo()
        {
            Id = 0;
            Camara = "";
            Fecha = DateTime.Now;
            Placa = "";
            Estado = "";
        }

        public Vehiculo(int id, string camara, DateTime fecha, string placa, string estado)
        {
            Id = id;
            Camara = camara;
            Fecha = fecha;
            Placa = placa;
            Estado = estado;
        }

        public int Id { get; set; }
        public string Camara { get; set; }

        public DateTime Fecha{ get; set; }
        public string Placa { get; set; }
        public string Estado { get; set; }

    }
}
