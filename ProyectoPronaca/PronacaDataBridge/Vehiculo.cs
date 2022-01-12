using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PronacaPlugin
{
   public class Vehiculo
    {
        public int Veh_Bascula { get; set; }
        public string Veh_Placa { get; set; }
        public string Veh_Chofer { get; set; }
        public string Veh_Peso_Ingreso { get; set; }
        public string Veh_Peso_Salida { get; set; }
        public string Veh_Ticket { get; set; }
        public DateTime Veh_Fecha_Ingreso { get; set; }
        public DateTime Veh_Fecha_Salida { get; set; }
        public string Veh_Estado { get; set; }
    }
}
