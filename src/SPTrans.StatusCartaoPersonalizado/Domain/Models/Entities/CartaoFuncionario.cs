using System;
using System.Collections.Generic;
using System.Text;

namespace SPTrans.StatusCartaoPersonalizado.Domain.Models.Entities
{
    public class CartaoFuncionario
    {
        public string NumCpf { get; set; }
        public string NumCartao { get; set; }
        public string StatusCartao { get; set; }
        public string Motivo { get; set; }
        public DateTime DataHoraProcessamento { get; set; }
    }
}
