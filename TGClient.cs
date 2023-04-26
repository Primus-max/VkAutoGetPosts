using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TdLib;
using static TdLib.TdApi;
using static TdLib.TdApi.MessageContent;
using File = System.IO.File;

public class TGClient : IDisposable
{
    private readonly TdClient _client;
    private static readonly HashSet<string> StopWords = new HashSet<string> { "реклама", "подписывайся", "по ссылке", "сво", "хохлы", "свиньи", "война", "путин", "зелебоб", "война", "спецоперация", "укропы", "рашисты", "шойгу", "подоляк", "байден", "суровикин", "калибр", "ракета", "взрывы", "беспилотник", "шольц", "дуда", "зеленский" };
    private int _MessageCount = 0;
    public int MessageCount
    {
        get { return _MessageCount; }
        set { _MessageCount = value; }
    }

    public TGClient()
    {
        _client = new TdClient();
    }

    // Получаю список чатов
    public async Task<IEnumerable<TdApi.Chat>> GetChatsAsync()
    {
        var chats = await _client.ExecuteAsync(new TdApi.GetChats() { ChatList = null, Limit = int.MaxValue });
        return ((TdApi.Chats)chats).ChatIds.Select(id => new TdApi.Chat { Id = id });
    }

    // Получаю все сообщения из чата
    public async Task<List<Message>> GetAllChatMessagesAsync(long chatId)
    {
        List<Message> allMessages = new List<Message>();
        int offset = 0;
        int pageSize = 100;
        long? fromMessageId = await GetLastMessageIdAsync(chatId);

        while (true)
        {
            var history = await _client.GetChatHistoryAsync(chatId, (long)fromMessageId, offset, pageSize, false);

            if (history is null || !history.Messages_.Any())
            {
                break;
            }

            //allMessages.AddRange(history.Messages_);

            foreach (var message in history.Messages_)
            {
                if (!await FilterMessageAsync(message)) continue;

                await ExtractContentFromMessagesAsync(message, chatId);
            }

            fromMessageId = (long)(history.Messages_.LastOrDefault()?.Id);
        }

        return allMessages;
    }

    // Получаю последнее сообщение из чата, если есть чат
    private async Task<long> GetLastMessageIdAsync(long chatId)
    {
        // Получаем путь к файлу с сохраненными данными
        string contentFilePath = await LoadContentFilePathAsync(chatId);

        // Если файл существует, пробуем загрузить его содержимое
        if (File.Exists(contentFilePath))
        {
            try
            {
                // Загружаем список содержимого
                List<Content> contents = await LoadContentListAsync(contentFilePath);

                // Получаем ID последнего сообщения из сохраненных данных
                return contents.LastOrDefault()?.MessageId ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке файла: {ex.Message}");
            }
        }

        // Если файла нет или произошла ошибка при загрузке, возвращаем 0
        return 0;
    }

    // Метод для других методов получения текста и загрузки фото из чатов
    public async Task ExtractContentFromMessagesAsync(Message message, long chatId)
    {
        try
        {
            string contentFilePath = await LoadContentFilePathAsync(chatId);
            List<Content> contents = await LoadContentListAsync(contentFilePath);

            // Извелекаю фото из сообщения
            string localImagePath = await DownloadImageAsync(message);

            // Пропускаю сообщение если в сообщении нет фото
            if (String.IsNullOrEmpty(localImagePath))
            {
                return;
            }

            string messageText = await ExtractMessageTextAsync(message);
            Content newContent = await CreateNewContentAsync(message, messageText, localImagePath, contents);
            contents.Add(newContent);
            await SaveContentListAsync(contentFilePath, contents);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}");
        }
    }

    // Получаю путь к файлу с контентом json 
    private static async Task<string> LoadContentFilePathAsync(long chatId)
    {
        return await Task.Run(() =>
        {
            string dataDir = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "content");
            Directory.CreateDirectory(dataDir);
            string contentFileName = $"{chatId}.json";
            string contentFilePath = System.IO.Path.Combine(dataDir, contentFileName);
            return contentFilePath;
        });
    }

    // Загружаю и десеареализую список сообщений чата
    private static async Task<List<Content>> LoadContentListAsync(string contentFilePath)
    {
        List<Content>? contents = new List<Content>();
        if (File.Exists(contentFilePath))
        {
            try
            {
                string? jsonContent = await System.IO.File.ReadAllTextAsync(contentFilePath);
                contents = JsonConvert.DeserializeObject<List<Content>>(jsonContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке json файла: {ex.Message}");
            }
        }

        return contents;
    }

    // Извлекаю текст сообщения
    private static async Task<string> ExtractMessageTextAsync(Message message)
    {
        return await Task.Run(() =>
        {
            if (message.Content is MessagePhoto messagePhotoContent)
            {
                return messagePhotoContent.Caption?.Text ?? string.Empty;
            }
            return string.Empty;
        });
    }

    // Загружаю фото из сообщения
    private async Task<string> DownloadImageAsync(Message message)
    {
        string localImagePath = string.Empty;
        if (message.Content is MessagePhoto messagePhoto)
        {
            try
            {
                PhotoSize? largestSize = messagePhoto.Photo.Sizes.OrderByDescending(s => s.Width).FirstOrDefault();
                if (largestSize != null)
                {
                    var downloadedFile = await _client.DownloadFileAsync(largestSize.Photo.Id, 1, 0, 0, true);
                    Thread.Sleep(300);
                    if (downloadedFile.Local.IsDownloadingCompleted)
                    {
                        localImagePath = downloadedFile.Local.Path;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при скачивании файла: {ex.Message}");
            }
        }
        return localImagePath;
    }

    // Создаю объект контента
    private static async Task<Content> CreateNewContentAsync(Message message, string messageText, string localImagePath, List<Content> contents)
    {
        return await Task.Run(() =>
        {
            Content newContent = new Content
            {
                MessageId = message.Id,
                Text = messageText,
                ImagePaths = new List<string>(),
                ImageViewed = false,
                MediaAlbumId = message.MediaAlbumId
            };
            if (message.MediaAlbumId == 0)
            {
                newContent.ImagePaths.Add(localImagePath);
            }
            else
            {
                Content? albumContent = contents.FirstOrDefault(c => c.MediaAlbumId == message.MediaAlbumId);
                if (albumContent != null)
                {
                    albumContent?.ImagePaths?.Add(localImagePath);
                }
            }
            return newContent;
        });
    }

    // Сохраняю новый объект Content в Json чата
    private static async Task SaveContentListAsync(string contentFilePath, List<Content> contents)
    {
        try
        {
            string jsonContentString = JsonConvert.SerializeObject(contents, Formatting.Indented);
            await File.WriteAllTextAsync(contentFilePath, jsonContentString);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}");
        }
    }

    // Фильтрую сообщения
    public static async Task<bool> FilterMessageAsync(Message message)
    {
        if (message.ReplyMarkup != null)
        {
            return false;
        }

        if (message.Content is MessageVideo or MessageAnimation or MessageSticker)
        {
            return false;
        }

        if (message.Content is MessageText messageText)
        {
            var text = messageText.Text.Text;

            if (text.Contains("http"))
            {
                return false;
            }

            foreach (var word in StopWords)
            {
                if (text.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
        }

        await Task.Delay(100);

        return true;
    }

    // Соединение
    public async Task ConnectAsync(string phoneNumber, string authenticationCode, string apiHash, int apiId)
    {
        var parameters = new TdApi.SetTdlibParameters
        {
            UseTestDc = false,
            DatabaseDirectory = "tdlib",
            FilesDirectory = "tdlib",
            UseFileDatabase = true,
            UseChatInfoDatabase = true,
            UseMessageDatabase = true,
            UseSecretChats = false,
            ApiId = apiId,
            ApiHash = apiHash,
            SystemLanguageCode = "en",
            DeviceModel = "Desktop",
            SystemVersion = "Windows 10",
            ApplicationVersion = "1.0",
            EnableStorageOptimizer = true,
            IgnoreFileNames = false,
        };

        await _client.SetTdlibParametersAsync(
            parameters.UseTestDc,
            parameters.DatabaseDirectory,
            parameters.FilesDirectory,
            parameters.DatabaseEncryptionKey,
            parameters.UseFileDatabase,
            parameters.UseChatInfoDatabase,
            parameters.UseMessageDatabase,
            parameters.UseSecretChats,
            parameters.ApiId,
            parameters.ApiHash,
            parameters.SystemLanguageCode,
            parameters.DeviceModel,
            parameters.SystemVersion,
            parameters.ApplicationVersion,
            parameters.EnableStorageOptimizer,
            parameters.IgnoreFileNames
        );

        try
        {
            TdApi.AuthorizationState authorizationState = null;

            // Проверяем текущее состояние авторизации
            while (true)
            {
                authorizationState = await _client.ExecuteAsync(new TdApi.GetAuthorizationState());

                if (authorizationState.DataType == "authorizationStateWaitPhoneNumber")
                {
                    // Запрашиваем номер телефона у пользователя
                    if (String.IsNullOrEmpty(phoneNumber))
                        phoneNumber = Microsoft.VisualBasic.Interaction.InputBox("Введите номер телефона начина с +7", "Ввод номера");

                    if (phoneNumber == "")
                    {
                        MessageBox.Show("Вы не ввели номер!", "Ошибка");
                        return;
                    }

                    // Отправляем запрос на авторизацию по номеру телефона
                    await _client.ExecuteAsync(new TdApi.SetAuthenticationPhoneNumber { PhoneNumber = phoneNumber });
                }
                else if (authorizationState.DataType == "authorizationStateWaitCode")
                {
                    // Запрашиваем код авторизации у пользователя
                    authenticationCode = Microsoft.VisualBasic.Interaction.InputBox("Введите код подтверждения который пришел вам в телеграм", "Ввод номера");

                    if (authenticationCode == "")
                    {
                        MessageBox.Show("Вы не ввели номер!", "Ошибка");
                        return;
                    }

                    // Отправляем запрос на авторизацию по коду
                    await _client.ExecuteAsync(new TdApi.CheckAuthenticationCode { Code = authenticationCode });
                }

                else if (authorizationState.DataType == "authorizationStateReady")
                {
                    // Клиент авторизован, можно начинать работу с TdLib
                    break;
                }
                else if (authorizationState.DataType == "authorizationStateClosed")
                {
                    // Авторизация закрыта, необходимо выйти из приложения
                    throw new Exception("Authorization closed");
                }
                else
                {
                    // Неизвестное состояние авторизации, необходимо выйти из приложения
                    throw new Exception("Unknown authorization state");
                }

                await Task.Delay(1000); // Ждем 1 секунду и запрашиваем состояние авторизации снова
            }

        }
        catch (TdException ex)
        {
            MessageBox.Show(ex.Message);
            throw;
        }

    }

    public async Task DisconnectAsync()
    {
        await _client.CloseAsync();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}