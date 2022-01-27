using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PronacaPlugin
{
   public class Ticket
    {
        public Ticket()
        {
            Numero = "";
            Estado = "";
            BasculaEntrada = 0;
            BasculaSalida = 0;
            Conductor = "";
            FechaEntrada = DateTime.Now;
            FechaSalida = DateTime.Now;
            PesoEntrada = "";
            PesoSalida = "";
        }

        public Ticket(string ti_Numero, int ti_basculaEntrada, int ti_basculaSalida, string ti_Placa, string ti_Chofer,
            string ti_Peso_Ingreso, string ti_Peso_Salida, DateTime ti_Fecha_Ingreso, DateTime ti_Fecha_Salida, string ti_Estado)
        {
            this.Numero = ti_Numero;
            this.BasculaEntrada = ti_basculaEntrada;
            this.BasculaSalida = ti_basculaSalida;
            this.PlacaVehiculo = ti_Placa;
            this.Conductor = ti_Chofer;
            this.PesoEntrada = ti_Peso_Ingreso;
            this.PesoSalida = ti_Peso_Salida;
            this.FechaEntrada = ti_Fecha_Ingreso;
            this.FechaSalida = ti_Fecha_Salida;
            this.Estado = ti_Estado;
        }

        public string Numero { get; set; }
        public int BasculaEntrada { get;  set; }
        public int BasculaSalida { get; set; }
        public string PlacaVehiculo { get; set; }
        public string Conductor { get; set; }
        public string PesoEntrada { get; set; }
        public string PesoSalida { get; set; }
        public DateTime FechaEntrada { get; set; }
        public DateTime FechaSalida { get; set; }
        public string Estado { get; set; }

    }
}
