using DataBridge.Attended.Plugins;
using DataBridge.Core.Business;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PronacaPlugin
{
    class Aplicacion : ApplicationProcessing
    {
        string codeBase;
        UriBuilder uri;
        string path;
        Configuration cfg;
        public Aplicacion()
        {
            codeBase = Assembly.GetExecutingAssembly().CodeBase;
            uri = new UriBuilder(codeBase);
            path = Uri.UnescapeDataString(uri.Path);
            cfg = ConfigurationManager.OpenExeConfiguration(path);
        }
        public override void LoggedIn(UserModel myLoggedInUser)
        {
            string operador = myLoggedInUser.Name;
            DateTime fecha = DateTime.Now;
            Log.operadorActual(operador);
            registrarOperador(operador,fecha);

        }
        public void registrarOperador(string operador,DateTime fecha)
        {
            string Conexion_Bd = cfg.AppSettings.Settings["Conexion_Local"].Value;
            using (var Conn = new SqlConnection(Conexion_Bd))
            {
                Conn.Open();
                using (var command = new SqlCommand("INSERT INTO [DBVehiculos].[dbo].[Login] VALUES(@operador,@fecha)", Conn))
                {
                    command.Parameters.Add(new SqlParameter("@operador", operador));
                    command.Parameters.Add(new SqlParameter("@fecha", fecha));
                    int rowsAdded = command.ExecuteNonQuery();
                }
            }
        }


    }
}
