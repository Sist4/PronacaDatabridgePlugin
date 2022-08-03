using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PronacaPlugin
{
    class Log
    {
        public static void transacción(Ticket ticket,string pesoBascula,string evento,int bascula)
        {
            string path = @"C:\Program Files (x86)\METTLER TOLEDO\DataBridge\Log.txt";
            if (!File.Exists(path))
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine("-------------------------------------------------");
                    sw.WriteLine("Fecha y hora: "+DateTime.Now);
                    sw.WriteLine("Número de transacción: "+ticket.NumeroTicket);
                    sw.WriteLine("Vehiculo: " + ticket.Placa);
                    sw.WriteLine("Conductor: " + ticket.Chofer);
                    sw.WriteLine("Peso ingreso: " + ticket.PesoIngreso);
                    sw.WriteLine("Peso Salida: " + ticket.PesoSalida);
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
                    sw.WriteLine("Número de transacción: " + ticket.NumeroTicket);
                    sw.WriteLine("Vehiculo: " + ticket.Placa);
                    sw.WriteLine("Conductor: " + ticket.Chofer);
                    sw.WriteLine("Peso ingreso: " + ticket.PesoIngreso);
                    sw.WriteLine("Peso Salida: " + ticket.PesoSalida);
                    sw.WriteLine("Báscula: " + bascula);
                    sw.WriteLine("Peso en Báscula: " + pesoBascula);
                    sw.WriteLine("Evento: " + evento);
                    sw.Close();
                }
            }
        }

    }
}
