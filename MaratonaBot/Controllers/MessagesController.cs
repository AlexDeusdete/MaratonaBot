using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Http;
using MaratonaBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using SimilarProducts.Services;

namespace MaratonaBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            var connector = new ConnectorClient(new Uri(activity.ServiceUrl));

            var attributes = new LuisModelAttribute(
                ConfigurationManager.AppSettings["LuisId"],
                ConfigurationManager.AppSettings["LuisSubscriptionKey"]);
            var service = new LuisService(attributes);

            switch (activity.Type)
            {
                case ActivityTypes.Message:
                    var image = activity.Attachments?.FirstOrDefault(a => a.ContentType.Contains("image"));
                    if (image != null)
                    {
                        using (var stream = await GetImageStream(connector, image))
                        {
                            var entity = GetNameImage(stream).Result;

                            //Activity message = activity.CreateReply("Só um minuto, já te mando o que encontrei!");
                            //await connector.Conversations.ReplyToActivityAsync(message);

                            var reply = activity.CreateReply(entity);
                            reply.Type = ActivityTypes.Message;
                            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            var produto = new BuscaProduto();
                            reply.Attachments = await produto.CarregaProdutos(entity, 5);
                            await connector.Conversations.ReplyToActivityAsync(reply);
                        }
                    }
                    else
                    {
                        await Conversation.SendAsync(activity, () => new Dialogs.Conversation(service));
                    }
                    break;
                case ActivityTypes.ConversationUpdate:
                    if (activity.MembersAdded.Any(o => o.Id == activity.Recipient.Id))
                    {
                        var reply = activity.CreateReply();
                        reply.Text = "Olá, Eu sou um Bot que vai te ajudar na busca de produtos!";

                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    break;
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Gets a list of visually similar images from an image stream.
        /// </summary>
        /// <param name="stream">The stream to an image.</param>
        /// <returns>List of visually similar images.</returns>
        public async Task<string> GetNameImage(Stream stream)
        {
            using (var httpClient = new HttpClient())
            {
                var ApiKey = WebConfigurationManager.AppSettings["BingSearchApiKey"];
                const string BingApiUrl = "https://api.cognitive.microsoft.com/bing/v7.0/images/details?modules=All&mkt=pt-br";

                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ApiKey);

                var strContent = new StreamContent(stream);
                strContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { FileName = "Any-Name-Works" };

                var content = new MultipartFormDataContent
                {
                    strContent
                };

                var postResponse = httpClient.PostAsync(BingApiUrl, content).Result;
                var text = await postResponse.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<BingImageResponse>(text);
                var TT = response.VisuallySimilarImages.Value.First().Name;
                return response.VisuallySimilarImages.Value.First().Name;
            }
        }

        private static async Task<Stream> GetImageStream(ConnectorClient connector, Attachment imageAttachment)
        {
            using (var httpClient = new HttpClient())
            {
                // The Skype attachment URLs are secured by JwtToken,
                // you should set the JwtToken of your bot as the authorization header for the GET request your bot initiates to fetch the image.
                // https://github.com/Microsoft/BotBuilder/issues/662
                var uri = new Uri(imageAttachment.ContentUrl);
                if (uri.Host.EndsWith("skype.com") && uri.Scheme == "https")
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(connector));
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                }

                return await httpClient.GetStreamAsync(uri);
            }
        }
        /// <summary>
        /// Gets the JwT token of the bot. 
        /// </summary>
        /// <param name="connector"></param>
        /// <returns>JwT token of the bot</returns>
        private static async Task<string> GetTokenAsync(ConnectorClient connector)
        {
            if (connector.Credentials is MicrosoftAppCredentials credentials)
            {
                return await credentials.GetTokenAsync();
            }

            return null;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}