using HtmlAgilityPack;
using Microsoft.Bot.Connector;
using Scraping;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace BuscaProdutos
{
    public class Produtos : Robo
    {
        /// <summary>
        /// Construtor para instânciar o Client
        /// </summary>
        public Produtos()
        {
            RoboWebClient = new RoboWebClient();
        }

        /// <summary>
        /// Método onde é feito o crawler para carregar os produtos.
        /// </summary>
        /// <returns></returns>
        public IList<Attachment> CarregaProdutos(string Url, int QuantidadeProdutos)
        {
            NameValueCollection parametros = new NameValueCollection();
            HtmlDocument doc = new HtmlDocument();
            //Carrega a pagina do buscador 
            //Estou atribuindo o resultado ao HtmlAgilityPack para fazer o parse do HTML.
            this.RoboWebClient._allowAutoRedirect = false;
            var ret = this.HttpGet("https://www.buscape.com.br/search/"+Url);

            //Capturando apenas as tags que estão definidas como article e ordenando pelo ID de cada Tag.
            var produtos = ret.DocumentNode.SelectNodes("//div[@class='bui-card bui-product']").ToList();
            List<Produto> listprod = new List<Produto>();


            var qtd = 0;// variavel que vai controlar o limite de produtos
            //Percorrendo os artigos que ja foram selecionados.
            foreach (var item in produtos)
            {
                Produto prd = new Produto();

                //Carregando o Html de cada artigo.
                doc.LoadHtml(item.InnerHtml);

                //Estou utilizando o HtmlAgilityPack.HtmlEntity.DeEntitize para fazer o HtmlDecode dos textos capturados de cada artigo.
                // Utilizo também o UTF8 para limpar o restante dos Encodes que estiverem na página.
                prd.Descricao = HtmlEntity.DeEntitize(ConvertUTF(doc.DocumentNode.DescendantsAndSelf().FirstOrDefault(d => d.Attributes["class"] != null && d.Attributes["class"].Value == "bui-product__name").InnerText));
                prd.Preco = float.Parse(HtmlEntity.DeEntitize(doc.DocumentNode.DescendantsAndSelf().FirstOrDefault(d => d.Name == "input" && d.Attributes["name"].Value == "priceProduct").Attributes["value"].Value));
                prd.LinkFoto = HtmlEntity.DeEntitize(ConvertUTF(doc.DocumentNode.DescendantsAndSelf().FirstOrDefault(d => d.Name == "img").Attributes["src"].Value));
                prd.LinkProduto = HtmlEntity.DeEntitize(ConvertUTF(doc.DocumentNode.DescendantsAndSelf().FirstOrDefault(d => d.Name == "a" && d.Attributes["data-gacategory"].Value == "Ir a loja").Attributes["href"].Value));
                listprod.Add(prd);
                qtd++;
                if (qtd >= QuantidadeProdutos)//quando for igual a quantidade solicitada sai
                    break;
            }

            return GeraAnexo(listprod.OrderBy(d => d.Preco).ToList());
        }

        private IList<Attachment> GeraAnexo(List<Produto> Produtos)
        {
            var attachments = new List<Attachment>();

            foreach (var prod in Produtos)
            {
                // Construir Card
                var heroCard = new HeroCard
                {
                    Title = prod.Descricao,
                    Subtitle = string.Format("{0:C}", prod.Preco),
                    Images = new List<CardImage>()
                };

                // Adiciona imagens
                var img = new CardImage { Url = prod.LinkFoto };
                heroCard.Images.Add(img);

                heroCard.Buttons = new List<CardAction>();
                var buyButton = new CardAction();

                // Comprar
                buyButton.Title = "Comprar!";
                buyButton.Type = "openUrl";
                buyButton.Value = prod.LinkProduto;

                heroCard.Buttons.Add(buyButton);

                attachments.Add(heroCard.ToAttachment());
            }

            return attachments;
        }

        private string ConvertUTF(string texto)
        {
            // Convertendo o texto para o Enconding default e Array de bytes.
            byte[] data = Encoding.Default.GetBytes(texto);

            //Convertendo o texto limpo para UTF8.
            string ret = Encoding.UTF8.GetString(data);

            return ret;
        }

    }

    /// <summary>
    /// Classe espelho do produto.
    /// </summary>
    public class Produto
    {
        public string Descricao { get; set; }
        public float Preco { get; set; }
        public string LinkFoto { get; set; }
        public string LinkProduto { get; set; }
    }
}