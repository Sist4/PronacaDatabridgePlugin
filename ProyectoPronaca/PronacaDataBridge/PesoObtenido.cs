using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PronacaPlugin
{
    public class PesoObtenido
    {
        public int Bascula { get; set; }
        public string Peso { get; set; }

        public PesoObtenido(int bascula, string peso)
        {
            Bascula = bascula;
            Peso = peso;
        }
    }
}
