using Newtonsoft.Json;

namespace MaratonaBot.Models
{
    public class Produto
    {
        [JsonProperty("descricao")]
        public string Descricao { get; set; }
        [JsonProperty("preco")]
        public string Preco { get; set; }
        [JsonProperty("linkFoto")]
        public string LinkFoto { get; set; }
        [JsonProperty("linkProduto")]
        public string LinkProduto { get; set; }
    }

}