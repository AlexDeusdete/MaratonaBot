using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using BuscaProdutos;
using Microsoft.Bot.Connector;

namespace MaratonaBot.Dialogs
{
    [Serializable]
    public class Conversation : LuisDialog<object>
    {
        private const int QtdProdutos = 5;
        public Conversation(ILuisService service) : base(service) { }
        /// <summary>
        /// intenção de algo sem sentido para o bot
        /// </summary>
        [LuisIntent("None")]
        public async Task NoneAsync(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Desculpe, eu não entendi...\n" +
                                    "Sou um Bot e tenho um pouco de dificuldade para entender algumas frases.");
            context.Done<string>(null);
        }
        /// <summary>
        /// Quando não houve intenção reconhecida.
        /// </summary>
        [LuisIntent("")]
        public async Task IntencaoNaoReconhecida(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Desculpe, eu não entendi...\n" +
                                    "Sou um Bot e tenho um pouco de dificuldade para entender algumas frases.");
            context.Done<string>(null);
        }
        /// <summary>
        /// Quando a intenção for uma saudação
        /// </summary>
        [LuisIntent("Saudacao")]
        public async Task ItencaoSaudacao(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Olá, é um prazer poder te atender!");
            context.Done<string>(null);
        }
        /// <summary>
        /// Quando a intenção for uma busca de produtos
        /// </summary>
        [LuisIntent("Procura-Produto")]
        public async Task ItencaoProcurandoProduto(IDialogContext context, LuisResult result)
        {
            var entity = result.Entities.FirstOrDefault(c => c.Type == "Produto")?.Entity;

            if (string.IsNullOrEmpty(entity))
            {
                await context.PostAsync("Poderia melhorar um pouco sua frase? Não consegui entender.");
            }
            else
            {
                await context.PostAsync("Só um minuto, já te mando o que encontrei!");
                var produto = new Produtos();
                var reply = context.MakeMessage();
                reply.Type = ActivityTypes.Message;
                reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                reply.Attachments = produto.CarregaProdutos(entity, QtdProdutos);
                await context.PostAsync(reply);
            }


            context.Done<string>(null);
        }
        /// <summary>
        /// Quando a intenção for uma pergunta 
        /// solicitando a descrição das habilidades do Bot
        /// </summary>
        [LuisIntent("Habilidades-manual-de-uso")]
        public async Task ItencaoHabilidades(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Olha eu posso fazer as seguites coisas\n"+
                                    "Procurar produtos por foto, imagem, descrição\n"+
                                    " ou até mesmo você pode me enviar um audio contando "+
                                    "como esse produto é que eu procuro para você!");
            context.Done<string>(null);
        }
        /// <summary>
        /// Quando a intenção for um agradecimento ou reclamação do bot
        /// </summary>
        [LuisIntent("FeedBack")]
        public async Task ItencaoFeedBack(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("foi um prazer poder te ajudar!");
            context.Done<string>(null);
        }
        /// <summary>
        /// Quando a intenção for um pedido de ajuda
        /// </summary>
        [LuisIntent("Ajuda")]
        public async Task ItencaoAjuda(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Me fala como posso te ajudar\n"+
                                    "vou fazer o que puder para te ajudar");
            context.Done<string>(null);
        }
    }
}