using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PronacaPlugin
{
    public class Ticket
    {

        private Ticket()
        {
            BasculaSalida = 0;
            Bascula = 0;
            Placa = "";
            Chofer = "";
            PesoIngreso = "0";
            PesoSalida = "0";
            NumeroTicket = "0";
            FechaIngreso = DateTime.Now;
            FechaSalida = DateTime.Now;
            OperadorIngreso = "";
            OperadorSalida = "";
            PinEntrada = "";
            PinSalida = "";
            PesosObtenidos = "";
            Estado = "";
            Val1 = "";
            Val2 = "";
            Val3 = "";
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
            BasculaSalida = 0;
            Bascula = 0;
            Placa = "";
            Chofer = "";
            PesoIngreso = "0";
            PesoSalida = "0";
            NumeroTicket = "0";
            FechaIngreso = DateTime.Now;
            FechaSalida = DateTime.Now;
            OperadorIngreso = "";
            OperadorSalida = "";
            PinEntrada = "";
            PinSalida = "";
            PesosObtenidos = "";
            Estado = "";
            Val1 = "";
            Val2 = "";
            Val3 = "";
        }

        public int Bascula { get; set; }
        public int BasculaSalida { get; set; }
        public string Placa{ get; set; }
        public string Chofer { get; set; }
        public string PesoIngreso { get; set; }
        public string PesoSalida { get; set; }
        public string NumeroTicket { get; set; }
        public DateTime FechaIngreso { get; set; }
        public DateTime FechaSalida { get; set; }
        public string OperadorIngreso { get; set; }
        public string OperadorSalida { get; set; }
        public string PinEntrada { get; set; }
        public string PinSalida { get; set; }
        public string PesosObtenidos { get; set; }
        public string  Estado { get; set; }
        public string Val1  { get; set; }
        public string Val2 { get; set; }
        public string Val3 { get; set; }
    }
}
