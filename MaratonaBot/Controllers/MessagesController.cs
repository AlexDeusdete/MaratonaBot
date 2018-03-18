using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;
using MaratonaBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using SimilarProducts.Services;
using SpeechToText.Services;

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
                    var audioAttachment = activity.Attachments?.FirstOrDefault(a => a.ContentType.Contains("audio/wav") || a.ContentType.Contains("audio/ogg") || a.ContentType.Contains("application/octet-stream"));

                    if (image != null)
                    {
                        using (var stream = await GetStream(connector, image))
                        {
                            Activity message = activity.CreateReply("Só um minuto, já te mando o que encontrei!");
                            await connector.Conversations.ReplyToActivityAsync(message);

                            var entity = GetNameImage(stream).Result;

                            var reply = activity.CreateReply(entity);
                            reply.Type = ActivityTypes.Message;
                            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            var produto = new BuscaProduto();
                            reply.Attachments = await produto.CarregaProdutos(entity, 5);
                            await connector.Conversations.ReplyToActivityAsync(reply);
                        }
                    }else if(audioAttachment != null)
                    {
                        var stream = await GetStream(connector, audioAttachment);
                        var text = await GetTextFromAudioAsync(stream);
                        activity.Text = text;
                        await Conversation.SendAsync(activity, () => new Dialogs.Conversation(service));
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
                case ActivityTypes.ContactRelationUpdate:
                    if (activity.Action == "add")
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
        public async Task<string> GetTextFromAudioAsync(Stream audiostream)
        {
            var requestUri = @"https://speech.platform.bing.com/recognize?scenarios=smd&appid=D4D52672-91D7-4C74-8AD8-42B1D98141A5&locale=pt-br&device.os=bot&form=BCSSTT&version=3.0&format=json&instanceid=565D69FF-E928-4B7E-87DA-9A750B96D9E3&requestid=" + Guid.NewGuid();

            using (var client = new HttpClient())
            {
                var token = Authentication.Instance.GetAccessToken();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

                using (var binaryContent = new ByteArrayContent(StreamToBytes(audiostream)))
                {
                    binaryContent.Headers.TryAddWithoutValidation("content-type", "audio/wav; codec=\"audio/pcm\"; samplerate=16000");

                    var response = await client.PostAsync(requestUri, binaryContent);
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new HttpException((int)response.StatusCode, $"({response.StatusCode}) {response.ReasonPhrase}");
                    }

                    var responseString = await response.Content.ReadAsStringAsync();
                    try
                    {
                        dynamic data = JsonConvert.DeserializeObject(responseString);
                        return data.header.name;
                    }
                    catch (JsonReaderException ex)
                    {
                        throw new Exception(responseString, ex);
                    }
                }
            }
        }
        private static byte[] StreamToBytes(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
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

        private static async Task<Stream> GetStream(ConnectorClient connector, Attachment imageAttachment)
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
            /*if (connector.Credentials is MicrosoftAppCredentials credentials)
            {
                return await credentials.GetTokenAsync();
            }*/

            return await (connector.Credentials as MicrosoftAppCredentials).GetTokenAsync();
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