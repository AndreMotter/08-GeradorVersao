using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeradorVersao.DTOs
{
    public class ConfigDTO
    {
        public string Destino { get; set; }
        public bool Logs { get; set; }
        public bool LimparDestinoCadaVersao { get; set; }
        public List<Sistema> Sistemas { get; set; }
    }

    public class Sistema
    {
        public string Nome { get; set; }
        public string CaminhoWeb { get; set; }
        public string CaminhoApi { get; set; }
        public string CaminhoPublish { get; set; }
        public bool RodarNpm { get; set; }
    }
}
