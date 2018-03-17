using System;
using System.Net;

namespace Scraping
{
    public class RoboWebClient : WebClient
    {
        //Criei estas propriedades apenas para controle.
        //Caso preciso expor essas propriedades para alteração basta tirar o readonly e alterar para public.
        //Lembre-se os sites podem variar e muito, então expor uma variável pode deixar seu código mais flexivel.
        public CookieContainer _cookie = new CookieContainer();
        public bool _allowAutoRedirect;

        /// <summary>
        /// fiz um override do método para contemplar as variveis expostas.
        /// neste caso esta privadas, mas para o caso de expor, deve seguir a forma abaixo.
        /// </summary>
        /// <param name="address">Endereço de busca.</param>
        /// <returns>Retorno da requisição a ser tratada.</returns>
        protected override WebRequest GetWebRequest(Uri address)
        {

            WebRequest request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
            {
                (request as HttpWebRequest).ServicePoint.Expect100Continue = false;
                (request as HttpWebRequest).CookieContainer = _cookie;
                (request as HttpWebRequest).KeepAlive = false;
                (request as HttpWebRequest).AllowAutoRedirect = _allowAutoRedirect;
            }

            return request;
        }
    }
}