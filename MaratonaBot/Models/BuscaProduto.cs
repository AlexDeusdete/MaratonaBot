using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MaratonaBot.Models
{
    public class BuscaProduto
    {
        public async Task<IList<Attachment>> CarregaProdutos(string prod, int QuantidadeProdutos)
        {
            var endpoint = $"http://busca-produto.azurewebsites.net/api/produtos/{FormataUrl(prod).ToLower()}/{QuantidadeProdutos}";
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(endpoint).Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                else
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var resultado = JsonConvert.DeserializeObject<Models.Produto[]>(json);
                    return GeraAnexo(resultado);
                }
            }
        }

        private string FormataUrl(string value)
        {
            var campo = Regex.Replace(value, "(?i)[^0-9a-záéíóúàèìòùâêîôûãõç\\s]", "");
            campo = RemoverAcentos(campo).Trim().Replace(" ", "-");
            return campo;
        }

        private IList<Attachment> GeraAnexo(Produto[] Produtos)
        {
            var attachments = new List<Attachment>();

            foreach (var prod in Produtos)
            {
                // Construir Card
                var heroCard = new HeroCard
                {
                    Title = prod.Descricao,
                    Subtitle = prod.Preco,
                    Images = new List<CardImage>()
                };

                // Adiciona imagens
                var img = new CardImage { Url = prod.LinkFoto };
                heroCard.Images.Add(img);

                heroCard.Buttons = new List<CardAction>();
                var buyButton = new CardAction
                {
                    // Comprar
                    Title = "Comprar!",
                    Type = "openUrl",
                    Value = prod.LinkProduto
                };

                heroCard.Buttons.Add(buyButton);

                attachments.Add(heroCard.ToAttachment());
            }

            return attachments;
        }

        private string RemoverAcentos(string texto)
        {
            string s = texto.Normalize(NormalizationForm.FormD);

            StringBuilder sb = new StringBuilder();

            for (int k = 0; k < s.Length; k++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(s[k]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(s[k]);
                }
            }
            return sb.ToString();
        }
    }
}