using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Profile;

namespace PronacaPlugin
{
    public class Ticket
    {

        private Ticket()
        {
            Id = "";
            IdCorreo = "";
            PlacaVehiculo = "";
            CedulaConductor = "";
            Fecha = DateTime.Now;
            Hora = DateTime.Now;
            Numero = 0;
            Tipo = "";
            Bascula = 0;
            Operador = "";
            PesoEnviado = "0";
            PesosObtenidos = "";
            Estado = "";

        }

        private static Ticket _instance;
        public static Ticket GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Ticket();
            }
            return _instance;
        }
        public void Nuevo()
        {
            Id = "";
            IdCorreo = "";
            PlacaVehiculo = "";
            CedulaConductor = "";
            Fecha = DateTime.Now;
            Hora = DateTime.Now;
            Numero = 0;
            Tipo = "";
            Bascula = 0;
            Operador = "";
            PesoEnviado = "0";
            PesosObtenidos = "";
            Estado = "";
        }
        public string Id { get; set; }
        public string IdCorreo { get; set; }
        public string PlacaVehiculo { get; set; }
        public string CedulaConductor { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime Hora { get; set; }
        public int Numero { get; set; }
        public string Tipo { get; set; }
        public int Bascula { get; set; }
        public string Operador { get; set; }
        public string PesoEnviado { get; set; }
        public string PesosObtenidos { get; set; }
        public string Estado { get; set; }


    }
}
