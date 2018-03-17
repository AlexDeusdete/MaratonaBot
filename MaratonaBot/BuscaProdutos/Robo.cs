using System.Collections.Specialized;
using System.Linq;
using System.Text;
using HtmlAgilityPack;

namespace Scraping
{
    /// <summary>
    /// Classe para utilização do Web Request.
    /// </summary>
    public class Robo
    {
        public RoboWebClient RoboWebClient { get; set; }

        /// <summary>
        /// Métodos para efetuar chamadas via GET.
        /// </summary>
        /// <param name="url">Url a ser pesquisada.</param>
        /// <returns>HtmlDocumento - utilizado para facilitar o parse do Html.</returns>
        public HtmlDocument HttpGet(string url)
        {
            lock (this)
            {
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(RoboWebClient.DownloadString(url));

                return htmlDocument;
            }
        }
    }

}