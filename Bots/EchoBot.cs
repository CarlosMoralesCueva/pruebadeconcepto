using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class EchoBot : ActivityHandler
    {
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            //var replyText = turnContext.Activity.Text;

            //replicar un mensaje
            //await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);

            //contestar con datos adjuntos en línea
            //var reply = MessageFactory.Text("Datos adjuntos en línea.");
            //reply.Attachments = new List<Attachment>() { GetInlineAttachment() };
            //await turnContext.SendActivityAsync(reply, cancellationToken);

            //contestar con un datos adjuntos cargados
            //var reply = MessageFactory.Text("Datos adjuntos cargados.");
            //reply.Attachments = new List<Attachment>() { await GetUploadedAttachmentAsync(turnContext, turnContext.Activity.ServiceUrl,
            //    turnContext.Activity.Conversation.Id, cancellationToken) };
            //await turnContext.SendActivityAsync(reply, cancellationToken);

            //contestar con archivos adjuntos de internet
            //var reply = MessageFactory.Text("Archivos adjuntos de internet.");
            //reply.Attachments = new List<Attachment>() { GetInternetAttachment() };
            //await turnContext.SendActivityAsync(reply, cancellationToken);

            //contestar con opciones
            var reply = await ProcessInput(turnContext, cancellationToken);

            if (reply.Text.Contains("ver opciones"))
            {
                // Respond to the user.
                //dar delay a un mensaje
                await turnContext.SendActivitiesAsync(
                    new Activity[] {
                new Activity { Type = ActivityTypes.Typing },
                new Activity { Type = "delay", Value = 3000 },
                MessageFactory.Text("",""),
                    },
                    cancellationToken);

                //await turnContext.SendActivityAsync(reply, cancellationToken);
                await DisplayOptionsAsync(turnContext, cancellationToken);
            }
            else
            {
                // Respond to the user.
                //dar delay a un mensaje
                await turnContext.SendActivitiesAsync(
                    new Activity[] {
                new Activity { Type = ActivityTypes.Typing },
                new Activity { Type = "delay", Value = 3000 },
                MessageFactory.Text(reply.Text, reply.Text),
                    },
                    cancellationToken);
            }
            
                       
            

        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            string welcomeText = Saludo();

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }

        private string Saludo()
        {
            var hour = DateTime.Now.Hour;

            if (hour >= 0 && hour <= 11)
                return "Buenos días, ¿en qué te puedo ayudar?";
            else if (hour >= 12 && hour <= 17)
                return "Buenas tardes, ¿en qué te puedo ayudar?";
            else
                return "Buenas noches, ¿en qué te puedo ayudar?";
        }

        private static Attachment GetInlineAttachment()
        {
            var imagePath = Path.Combine(Environment.CurrentDirectory, @"architecture-resize.png");
            var imageData = Convert.ToBase64String(File.ReadAllBytes(imagePath));

            return new Attachment
            {
                Name = @"architecture-resize.png",
                ContentType = "image/png",
                ContentUrl = $"data:image/png;base64,{imageData}",
            };
        }

        private static async Task<Attachment> GetUploadedAttachmentAsync(ITurnContext turnContext, string serviceUrl, string conversationId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(serviceUrl))
            {
                throw new ArgumentNullException(nameof(serviceUrl));
            }

            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }

            var imagePath = Path.Combine(Environment.CurrentDirectory, @"test.png");

            var connector = turnContext.TurnState.Get<IConnectorClient>() as ConnectorClient;
            var attachments = new Attachments(connector);
            var response = await attachments.Client.Conversations.UploadAttachmentAsync(
                conversationId,
                new AttachmentData
                {
                    Name = @"test.png",
                    OriginalBase64 = File.ReadAllBytes(imagePath),
                    Type = "image/png",
                },
                cancellationToken);

            var attachmentUri = attachments.GetAttachmentUri(response.Id);

            return new Attachment
            {
                Name = @"test.png",
                ContentType = "image/png",
                ContentUrl = attachmentUri,
            };
        }

        // Creates an <see cref="Attachment"/> to be sent from the bot to the user from a HTTP URL.
        private static Attachment GetInternetAttachment()
        {
            // ContentUrl must be HTTPS.
            return new Attachment
            {
                Name = @"Resources\architecture-resize.png",
                ContentType = "image/png",
                ContentUrl = "https://docs.microsoft.com/en-us/bot-framework/media/how-it-works/architecture-resize.png",
            };
        }


        // Given the input from the message, create the response.
        private static async Task<IMessageActivity> ProcessInput(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;
            IMessageActivity reply = null;

            if (activity.Attachments != null && activity.Attachments.Any())
            {
                // We know the user is sending an attachment as there is at least one item
                // in the Attachments list.
                reply = HandleIncomingAttachment(activity);
            }
            else
            {
                // Send at attachment to the user.
                reply = await HandleOutgoingAttachment(turnContext, activity, cancellationToken);
            }

            return reply;
        }

        private static async Task DisplayOptionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Create a HeroCard with options for the user to interact with the bot.
            var card = new HeroCard
            {
                Text = "Puedes subir un archivo o seleccionar una de las siguientes opciones:",
                Buttons = new List<CardAction>
        {
            // Note that some channels require different values to be used in order to get buttons to display text.
            // In this code the emulator is accounted for with the 'title' parameter, but in other channels you may
            // need to provide a value for other parameters like 'text' or 'displayText'.
            new CardAction(ActionTypes.ImBack, title: "1. Inline Attachment", value: "1"),
            new CardAction(ActionTypes.ImBack, title: "2. Internet Attachment", value: "2"),
            new CardAction(ActionTypes.ImBack, title: "3. Uploaded Attachment", value: "3"),
        },
            };

            var reply = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        // Handle attachments uploaded by users. The bot receives an <see cref="Attachment"/> in an <see cref="Activity"/>.
        // The activity has a "IList{T}" of attachments.    
        // Not all channels allow users to upload files. Some channels have restrictions
        // on file type, size, and other attributes. Consult the documentation for the channel for
        // more information. For example Skype's limits are here
        // <see ref="https://support.skype.com/en/faq/FA34644/skype-file-sharing-file-types-size-and-time-limits"/>.
        private static IMessageActivity HandleIncomingAttachment(IMessageActivity activity)
        {
            var replyText = string.Empty;
            foreach (var file in activity.Attachments)
            {
                // Determine where the file is hosted.
                var remoteFileUrl = file.ContentUrl;

                // Save the attachment to the system temp directory.
                var localFileName = Path.Combine(Path.GetTempPath(), file.Name);

                // Download the actual attachment
                using (var webClient = new WebClient())
                {
                    webClient.DownloadFile(remoteFileUrl, localFileName);
                }

                replyText += $"Attachment \"{file.Name}\"" +
                             $" has been received and saved to \"{localFileName}\"\r\n";
            }

            return MessageFactory.Text(replyText);
        }

        // Returns a reply with the requested Attachment
        private static async Task<IMessageActivity> HandleOutgoingAttachment(ITurnContext turnContext, IMessageActivity activity, CancellationToken cancellationToken)
        {
            // Look at the user input, and figure out what kind of attachment to send.
            IMessageActivity reply = null;
            if (activity.Text.StartsWith("1"))
            {
                reply = MessageFactory.Text("Dato adjunto en línea.");
                reply.Attachments = new List<Attachment>() { GetInlineAttachment() };
            }
            else if (activity.Text.StartsWith("2"))
            {
                reply = MessageFactory.Text("Archivo adjunto de un URL.");
                reply.Attachments = new List<Attachment>() { GetInternetAttachment() };
            }
            else if (activity.Text.StartsWith("3"))
            {
                reply = MessageFactory.Text("Dato adjunto cargado.");

                // Get the uploaded attachment.
                var uploadedAttachment = await GetUploadedAttachmentAsync(turnContext, activity.ServiceUrl, activity.Conversation.Id, cancellationToken);
                reply.Attachments = new List<Attachment>() { uploadedAttachment };
            }
            else
            {
                // The user did not enter input that this bot was built to handle.
                var replyText = turnContext.Activity.Text;
                reply = MessageFactory.Text(replyText, replyText);
            }

            return reply;
        }

    }
}