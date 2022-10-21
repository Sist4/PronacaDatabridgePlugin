using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PronacaPlugin
{
    class Log
    {
        public static string operadorLog;
        public static void Estado(Ticket ticket,string pesoBascula,string evento,int bascula)
        {
            string path = @"C:\Program Files (x86)\METTLER TOLEDO\DataBridge\Log-Estados.txt";
            if (!File.Exists(path))
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine("-------------------------------------------------");
                    sw.WriteLine("Fecha y hora: "+DateTime.Now);
                    sw.WriteLine("Número de transacción: "+ticket.Numero);
                    sw.WriteLine("Vehiculo: " + ticket.PlacaVehiculo);
                    sw.WriteLine("Conductor ID: " + ticket.CedulaConductor);
                    sw.WriteLine("Peso enviado: " + ticket.PesoEnviado);
                    sw.WriteLine("Báscula: " + bascula);
                    sw.WriteLine("Peso en Báscula: " + pesoBascula);
                    sw.WriteLine("Evento: " + evento);
                    sw.Close();
                }
            }
            else
            {
                using (StreamWriter sw = new StreamWriter(path, true))
                {
                    sw.WriteLine("-------------------------------------------------");
                    sw.WriteLine("Fecha y hora: " + DateTime.Now);
                    sw.WriteLine("Número de transacción: " + ticket.Numero);
                    sw.WriteLine("Vehiculo: " + ticket.PlacaVehiculo);
                    sw.WriteLine("Conductor ID: " + ticket.CedulaConductor);
                    sw.WriteLine("Peso enviado: " + ticket.PesoEnviado);
                    sw.WriteLine("Báscula: " + bascula);
                    sw.WriteLine("Peso en Báscula: " + pesoBascula);
                    sw.WriteLine("Evento: " + evento);
                    sw.Close();
                }
            }
        }

        public static void Mensajes(string mensaje,string metodo)
        {
            string path = @"C:\Program Files (x86)\METTLER TOLEDO\DataBridge\Log-Mensajes.txt";
            if (!File.Exists(path))
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine("-------------------------------------------------");
                    sw.WriteLine("Fecha y hora: " + DateTime.Now);
                    sw.WriteLine("Método: " + metodo);
                    sw.WriteLine("Mensaje: " + mensaje);
                    sw.Close();
                }
            }
            else
            {
                using (StreamWriter sw = new StreamWriter(path, true))
                {
                    sw.WriteLine("-------------------------------------------------");
                    sw.WriteLine("Fecha y hora: " + DateTime.Now);
                    sw.WriteLine("Método: " + metodo);
                    sw.WriteLine("Mensaje: " + mensaje);
                    sw.Close();
                }
            }
        }

        public static void operadorActual(string operador)
        {
            operadorLog = operador;
        }

    }
}
