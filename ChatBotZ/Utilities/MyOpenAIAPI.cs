using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static ChatBotZ.Utilities.MyOpenAIAPI;

/*
* The models listed are:
* 
* 1. "ada" and "ada:2020-05-03" are models that are designed for text generation and text completion tasks.
* 
* 2. "ada-code-search-code" and "ada-code-search-text" are models that are designed for code search tasks.
*
* 3. "ada-search-document" and "ada-search-query" are models that are designed for document search tasks.
*
* 4. "ada-similarity" is a model that is designed for text similarity tasks.
* 
* 5. "audio-transcribe-001" is a model that is designed for audio transcription tasks.
*
* 6. "babbage", "babbage:2020-05-03" are models that are designed for text generation and text completion tasks.
*
* 7. "babbage-code-search-code" and "babbage-code-search-text" are models that are designed for code search tasks.
*
* 8. "babbage-search-document" and "babbage-search-query" are models that are designed for document search tasks.
*
* 9. "babbage-similarity" is a model that is designed for text similarity tasks.
*
* 10. "code-cushman-001" and "cushman:2020-05-03" are models that are designed for code generation and code completion
*
* 11. "code-davinci-002" and "code-davinci-edit-001" are models that are designed for code generation and code completion tasks.
*
* 12. "code-search-ada-code-001", "code-search-ada-text-001", "code-search-babbage-code-001", and "code-search-babbage-text-001" are models that 
*       are designed for code search tasks.
*
* 13. "curie", "curie:2020-05-03" are models that are designed for text generation and text completion tasks.
*
* 14. "curie-instruct-beta", "curie-search-document", "curie-search-query", and "curie-similarity" are models that are designed for text generation,
*       text completion, document search and text similarity tasks.
*
* 15. "davinci", "davinci:2020-05-03" are models that are designed for text generation and text completion tasks.
*
* 16. "davinci-if:3.0.0", "davinci-instruct-beta", "davinci-instruct-beta:2.0.0", "davinci-search-document", "davinci-search-query", and 
*       "davinci-similarity" are models that are designed for text generation, text completion, document search and text similarity tasks.
*
* 17. "if-curie-v2" and "if-davinci-v2" are models that are designed for text completion tasks.
*
* 18. "if-davinci:3.0.0" is a model that is designed for text completion tasks.
*
* 19. "text-ada-001", "text-ada:001", "text-babbage-001", "text-babbage:001", "text-curie-001", "text-curie:001", "text-davinci-001", "text-davinci-002",
*       "text-davinci-003", "text-davinci-edit-001", "text-davinci-insert-001", "text-davinci-insert-002", and "text-davinci:001" are models that are designed
*       for text generation, text completion and text editing tasks.
*
* 20. "text-babbage-001", "text-babbage:001", "text-curie-001", "text-curie:001", "text-davinci-001", "text-davinci-002", "text-davinci-003", 
*       "text-davinci-edit-001", "text-davinci-insert-001", "text-davinci-insert-002", and "text-davinci:001" are models that are designed for text 
*       generation, text completion and text editing tasks.
*
* 21. "text-embedding-ada-002", "text-search-ada-doc-001", "text-search-ada-query-001", "text-search-babbage-doc-001", "text-search-babbage-query-001", 
*       "text-search-curie-doc-001", "text-search-curie-query-001", "text-search-davinci-doc-001", "text-search-davinci-query-001", "text-similarity-ada-001", 
*       "text-similarity-babbage-001", "text-similarity-curie-001", "text-similarity-davinci-001" are models that are designed for text search, text similarity
*       and text embedding tasks.
*/
namespace ChatBotZ.Utilities
{
    public class MyOpenAIAPI
    {
        private static readonly string completionEndpoint = "https://api.openai.com/v1/completions";
        private static readonly string chatCompletionEndpoint = "https://api.openai.com/v1/chat/completions";
        private static readonly string editsEndpoint = "https://api.openai.com/v1/edits";
        private static readonly string modelEndpoint = "https://api.openai.com/v1/models";
        private static readonly string transcriptionEndpoint = "https://api.openai.com/v1/audio/transcriptions";
        private static readonly string translationEndpoint = "https://api.openai.com/v1/audio/translations";
        private static readonly string imageGenerationEndpoint = "https://api.openai.com/v1/images/generations";
        private static readonly string imageVariationsEndpoint = "https://api.openai.com/v1/images/variations";
        private static readonly string ModerationsEndpoint = "https://api.openai.com/v1/moderations";
        private static readonly string imageEditsEndpoint = "https://api.openai.com/v1/images/edits";
        private static readonly string fileUploadEndpoint = "https://api.openai.com/v1/files";

        // gpt-3 codeblock = "function:" "}\n"

        public MyOpenAIAPI()
        {
        }

        public static double CalculateTokenCost(string input)
        {
            int wordCount = input.Split(' ').Length;
            int averageWordLength = 4;
            double costPerWord = 1.0 / wordCount;
            double costPerCharacter = costPerWord / averageWordLength;
            double cost = input.Length * costPerCharacter;
            return cost;
        }

        public static async Task<bool> CheckTextForViolations(string apiKey, string prompt)
        {
            try
            {
                bool Violates = false;
                TextViolationCheckRequest payload = new TextViolationCheckRequest()
                {
                    Input = prompt,
                    Model = "text-moderation-latest"
                };
                // create an HTTP client
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                string json = JsonConvert.SerializeObject(payload);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(ModerationsEndpoint, content);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API request failed with status code: {(int)response.StatusCode}");
                }
                string responseJson = await response.Content.ReadAsStringAsync();
                TextModerationResponse responseData = JsonConvert.DeserializeObject<TextModerationResponse>(responseJson);

                foreach (TextModerationResponse.ModResult modResults in responseData.Results)
                {
                    if (modResults.Flagged)
                    {
                        Violates = true;
                    }
                }

                return Violates;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        public static async Task<string> GenerateTextAsyncHttp(string apiKey, string prompt, string model = "text-davinci-003", int maxTokens = 2000)
        {
            try
            {
                prompt = prompt.Trim();
                if (await CheckTextForViolations(apiKey, prompt))
                {
                    throw new Exception("This input violates OpenAI moderation checks.");
                }
                //string stopSequence = @"\?\?\?";
                string returnString = string.Empty;
                switch (model)
                {
                    case "text-devinci-003":
                        maxTokens = 4000;
                        break;
                    case "code-devinci-002":
                        maxTokens = 8000;
                        break;
                    default:
                        break;
                }
                int promptCost = (int)CalculateTokenCost(prompt);
                int remainingTokens = maxTokens - 150 - promptCost;
                if (remainingTokens <= 10)
                {
                    throw new Exception("API request too long.\nOpenAI API would fail to respond.\n Please shorten and try again.");
                }

                CompletionRequest payload = new CompletionRequest
                {
                    Prompt = prompt.Trim(),
                    Model = model,
                    Temperature = 0.1,
                    MaxTokens = maxTokens,
                    TopP = 1.0,
                    Frequency_Penalty = 0.6,
                    Presence_Penalty = 0.0,
                    User = apiKey
                };
                // create an HTTP client
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                string json = JsonConvert.SerializeObject(payload);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(completionEndpoint, content);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("API request failed with status code: " + (int)response.StatusCode);
                }
                string responseJson = await response.Content.ReadAsStringAsync();
                CompletionResult responseData = JsonConvert.DeserializeObject<CompletionResult>(responseJson);
                if (await CheckTextForViolations(apiKey, responseData.Choices.First().Text))
                {
                    throw new Exception("This response Violates OpenAI Moderation checks.");
                }
                returnString += $"Using Model: {responseData.Model}\n";
                returnString += $"Took: {responseData.Usage.Total_Tokens} Total Tokens and {responseData.Usage.Completion_Tokens} Tokens were used for the completion.\n";
                returnString += $"Stop Reason: {responseData.Choices.First().Finish_Reason}\n";
                returnString += $"{responseData.Choices.First().Text}\n\n";
                return returnString;
            }
            catch (HttpRequestException ex)
            {
                throw ex;
            }
        }

        public static async Task<Message> GenerateChatCompletionAsync(string apiKey, List<Message> messages)
        {
            try
            {
                ChatCompletionRequest payload = new ChatCompletionRequest
                {
                    Model = "gpt-3.5-turbo",
                    Messages = messages
                };

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
                string json = JsonConvert.SerializeObject(payload);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await client.PostAsync(chatCompletionEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseJson = await response.Content.ReadAsStringAsync();
                    ChatCompletionResult result = JsonConvert.DeserializeObject<ChatCompletionResult>(responseJson);
                    return result.Choices[0].Message;
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static async Task<string> EditTextAsyncHttp(string apiKey, string model, string instruction, string input = null, int maxTokens = 2000)
        {
            try
            {
                input = input.Trim();
                string returnString = string.Empty;
                if (await CheckTextForViolations(apiKey, input))
                {
                    throw new Exception("This input violates OpenAI moderation checks.");
                }
                int inputCost = (int)CalculateTokenCost(input);
                int remainingTokens = maxTokens - 110 - inputCost;
                if (remainingTokens < (maxTokens / 2))
                {
                    throw new Exception("API request too long.\nOpenAI API would fail to respond.\n Please shorten and try again.");
                }
                TextEditRequest payload = new TextEditRequest
                {
                    Model = model,
                    Input = input,
                    Instruction = instruction
                };
                // create an HTTP client
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                string json = JsonConvert.SerializeObject(payload);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(editsEndpoint, content);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("API request failed with status code: " + (int)response.StatusCode);
                }
                string responseJson = await response.Content.ReadAsStringAsync();
                TextEditResponse responseData = JsonConvert.DeserializeObject<TextEditResponse>(responseJson);
                double tokenPricePer1k = 0.02;
                double totalCost = responseData.Usages.Total_Tokens * (tokenPricePer1k / 1000);
                returnString += $"Took: {responseData.Usages.Total_Tokens} Total Tokens and {responseData.Usages.Completion_Tokens} Tokens were used for the completion.\n";
                returnString += $"Total Translation Cost: ${totalCost} at ${tokenPricePer1k} per 1000 tokens\n";
                returnString += responseData.Choices.First().Text;
                return returnString;
            }
            catch (HttpRequestException ex)
            {
                throw ex;
            }
        }

        public static async Task<string> GenerateImageAsyncHttp(string apiKey, string prompt, string size = "1024x1024")
        {
            try
            {
                prompt = prompt.Trim();
                if (await CheckTextForViolations(apiKey, prompt))
                {
                    throw new Exception("This input violates OpenAI moderation checks.");
                }
                // set up the request body
                ImageGenerationRequest payload = new ImageGenerationRequest
                {
                    Prompt = prompt,
                    Size = size,
                    User = apiKey
                };

                // create an HTTP client
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                string json = JsonConvert.SerializeObject(payload);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(imageGenerationEndpoint, content);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API request failed with status code: {(int)response.StatusCode}");
                }
                string responseJson = await response.Content.ReadAsStringAsync();
                ImageUrlResponse responseData = JsonConvert.DeserializeObject<ImageUrlResponse>(responseJson);
                return responseData.Data.First().Url;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        public static async Task<string> ImageVariationsAsyncHttp(string apiKey, byte[] image, string size = "1024x1024")
        {
            try
            {
                using MultipartFormDataContent content = new MultipartFormDataContent();
                ByteArrayContent imageContent = new ByteArrayContent(image);
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                content.Add(imageContent, "image", "otter.png");
                content.Add(new StringContent(size), "size");
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                HttpResponseMessage response = await client.PostAsync(imageVariationsEndpoint, content);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API request failed with status code: {(int)response.StatusCode}");
                }
                string responseJson = await response.Content.ReadAsStringAsync();
                ImageUrlResponse responseData = JsonConvert.DeserializeObject<ImageUrlResponse>(responseJson);
                return responseData.Data.First().Url;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        public static async Task<string> EditImageAsyncHttp(string apiKey, string prompt, byte[] image, byte[] mask, string size = "1024x1024")
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    using (MultipartFormDataContent content = new MultipartFormDataContent())
                    {
                        ByteArrayContent imageContent = new ByteArrayContent(image);
                        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                        content.Add(imageContent, "image", "image.png");

                        if (mask != null)
                        {
                            ByteArrayContent maskContent = new ByteArrayContent(mask);
                            maskContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                            content.Add(maskContent, "mask", "mask.png");
                        }

                        content.Add(new StringContent(prompt), "prompt");
                        content.Add(new StringContent(1.ToString()), "n");
                        content.Add(new StringContent(size), "size");
                        HttpResponseMessage response = await client.PostAsync(imageEditsEndpoint, content);
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"Failed to generate image edits: {response.ReasonPhrase}");
                        }
                        string responseJson = await response.Content.ReadAsStringAsync();
                        ImageUrlResponse responseData = JsonConvert.DeserializeObject<ImageUrlResponse>(responseJson);
                        return responseData.Data.First().Url;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        public static async Task<string> EditImageAsyncHttp(string apiKey, string prompt, byte[] image, string size = "1024x1024")
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    using (MultipartFormDataContent content = new MultipartFormDataContent())
                    {
                        ByteArrayContent imageContent = new ByteArrayContent(image);
                        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                        content.Add(imageContent, "image", "image.png");

                        content.Add(new StringContent(prompt), "prompt");
                        content.Add(new StringContent(1.ToString()), "n");
                        content.Add(new StringContent(size), "size");
                        HttpResponseMessage response = await client.PostAsync(imageEditsEndpoint, content);
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"Failed to generate image edits: {response.ReasonPhrase}");
                        }
                        string responseJson = await response.Content.ReadAsStringAsync();
                        ImageUrlResponse responseData = JsonConvert.DeserializeObject<ImageUrlResponse>(responseJson);
                        return responseData.Data.First().Url;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        public static async Task<string> TranslateAudioAsyncHttp(string apiKey, byte[] audio, string filename, string model = "whisper-1")
        {
            try
            {
                string audioB64 = Convert.ToBase64String(audio);
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    using (MultipartFormDataContent content = new MultipartFormDataContent())
                    {
                        ByteArrayContent audioContent = new ByteArrayContent(audio);
                        audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                        content.Add(audioContent, "file", filename);

                        content.Add(new StringContent(model), "model");

                        HttpResponseMessage response = await client.PostAsync(translationEndpoint, content);
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"Failed to transcribe audio: {response.ReasonPhrase}");
                        }

                        string responseJson = await response.Content.ReadAsStringAsync();
                        TranscriptionResponse responseData = JsonConvert.DeserializeObject<TranscriptionResponse>(responseJson);
                        return responseData.Text;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        public static async Task<string> TranscribeAudioAsyncHttp(string apiKey, byte[] audio, string filename, string model = "whisper-1")
        {
            try
            {
                string audioB64 = Convert.ToBase64String(audio);
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    using (MultipartFormDataContent content = new MultipartFormDataContent())
                    {
                        ByteArrayContent audioContent = new ByteArrayContent(audio);
                        audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/mpeg");
                        content.Add(audioContent, "file", filename);

                        content.Add(new StringContent(model), "model");

                        HttpResponseMessage response = await client.PostAsync(transcriptionEndpoint, content);
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"Failed to transcribe audio: {response.ReasonPhrase}");
                        }

                        string responseJson = await response.Content.ReadAsStringAsync();
                        TranscriptionResponse responseData = JsonConvert.DeserializeObject<TranscriptionResponse>(responseJson);
                        return responseData.Text;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }
        
        public static async Task<byte[]> GenerateSpeechAsyncHttp(string apiKey, string prompt, string model = "text-davinci-003", int maxTokens = 100)
        {
            try
            {
                SpeechGenerationRequest payload = new SpeechGenerationRequest
                {
                    Prompt = prompt,
                    Model = model,
                    Max_Tokens = maxTokens,
                    Format = "json"

                };
                // create an HTTP client
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                string json = JsonConvert.SerializeObject(payload);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/engines/whisper/completions", content);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("API request failed with status code: " + (int)response.StatusCode);
                }
                byte[] responseData = await response.Content.ReadAsByteArrayAsync();
                return responseData;
            }
            catch (HttpRequestException ex)
            {
                throw ex;
            }
        }

        public static async Task<string> UploadFileToOpenAIAsync(string apiKey, byte[] file, string purpose)
        {
            try
            {
                string jsonLines = Convert.ToBase64String(file);
                FileUploadRequest payload = new FileUploadRequest
                {
                    Purpose = purpose,
                    File = jsonLines
                };
                /*
                 *  public class FileUploadRequest
                 *  {
                 *      [JsonProperty("purpose")]
                 *      public string Purpose { get; set; }
                 *      [JsonProperty("file")]
                 *      public string File { get; set; }
                 *  } 
                */
                // create an HTTP client
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                string json = JsonConvert.SerializeObject(payload);
                StringContent content = new StringContent(json, Encoding.UTF8);
                HttpResponseMessage response = await client.PostAsync(fileUploadEndpoint, content); // fileUploadEndpoint = "https://api.openai.com/v1/files";
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API request failed with status code: {(int)response.StatusCode}");
                }
                string responseJson = await response.Content.ReadAsStringAsync();
                FileInformation responseData = JsonConvert.DeserializeObject<FileInformation>(responseJson);
                return $"{fileUploadEndpoint}/{responseData.Filename}";
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        public static async Task<byte[]> RetrieveFileDetailsAsyncHttp(string apiKey, string fileId)
        {
            try
            {
                // create an HTTP client
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                HttpResponseMessage response = await client.GetAsync($"{fileUploadEndpoint}/{fileId}");
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API request failed with status code: {(int)response.StatusCode}");
                }
                byte[] file = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<byte[]>();
                return file;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        public static async Task<byte[]> RetrieveFileContentsAsyncHttp(string apiKey, string fileId)
        {
            try
            {
                // create an HTTP client
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                HttpResponseMessage response = await client.GetAsync($"{fileUploadEndpoint}/{fileId}/content");
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API request failed with status code: {(int)response.StatusCode}");
                }
                byte[] file = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<byte[]>();
                return file;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        public static async Task DeleteFileAsync(string apiKey, string fileUrl)
        {
            try
            {
                // create an HTTP client
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                string deleteEndpoint = $"{fileUrl}";
                HttpResponseMessage response = await client.DeleteAsync(deleteEndpoint);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API request failed with status code: {(int)response.StatusCode}");
                }
                Console.WriteLine("File deleted successfully");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        public static async Task<Model> RetrieveModelDetailsAsync(string apiKey, string modelId)
        {
            try
            {
                // create an HTTP client
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                HttpResponseMessage response = await client.GetAsync($"{modelEndpoint}/{modelId}");
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API request failed with status code: {(int)response.StatusCode}");
                }
                string responseJson = await response.Content.ReadAsStringAsync();
                Model responseData = JsonConvert.DeserializeObject<Model>(responseJson);
                return responseData;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        public static async Task<List<Model>> GetModelsAsync(string apiKey)
        {
            try
            {
                List<Model> retList = new List<Model>();
                // create an HTTP client
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                HttpResponseMessage response = await client.GetAsync(modelEndpoint);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API request failed with status code: {(int)response.StatusCode}");
                }
                string responseJson = await response.Content.ReadAsStringAsync();
                ModelListResponse responseData = JsonConvert.DeserializeObject<ModelListResponse>(responseJson);
                retList = responseData.Data.ToList<Model>();
                return retList;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        public static async Task<List<Model>> GetBotList(string apiKey)
        {
            try
            {
                List<Model> models = await GetModelsAsync(apiKey);
                ConcurrentBag<Model> resultList = new ConcurrentBag<Model>();
                if (models != null)
                {
                    IEnumerable<Task<Model>> tasks = models.Select(model => RetrieveModelDetailsAsync(apiKey, model.Id));
                    Model[] details = await Task.WhenAll(tasks);

                    foreach (Model modelDetails in details)
                    {
                        if (modelDetails.Permissions.Any(p => p.AllowView))
                        {
                            resultList.Add(modelDetails);
                        }
                    }
                }

                return resultList.ToList();
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur while trying to retrieve the models
                throw new Exception(ex.Message);
            }
        }

        public static async Task<List<string>> GetBotNameList(string apiKey)
        {
            List<string> models = new List<string>();
            try
            {
                // Get the list of available models
                List<Model> result = await GetBotList(apiKey);
                if (result != null)
                {
                    // Start the async operations in parallel using Parallel.ForEach
                    await Task.WhenAll(result.Select(async model =>
                    {
                        if (!models.Contains(model.Id))
                        {
                            models.Add(model.Id);
                            await Task.Delay(0);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur while trying to retrieve the models
                throw new Exception(ex.Message);
            }
            return models ?? new List<string>();
        }

        public class CompletionRequest
        {
            [JsonProperty("model")]
            public string Model { get; set; }
            [JsonProperty("prompt")]
            public string Prompt { get; set; }
            [JsonProperty("suffix")]
            public string Suffix { get; set; } = null;
            [JsonProperty("max_tokens")]
            public int MaxTokens { get; set; }
            [JsonProperty("temperature")]
            public double Temperature { get; set; } = 0;
            [JsonProperty("top_p")]
            public double TopP { get; set; } = 1;
            [JsonProperty("n")]
            public int N { get; set; } = 1;
            [JsonProperty("stream")]
            public bool Stream { get; set; } = false;
            [JsonProperty("logprobs")]
            public object Logprobs { get; set; } = null;
            [JsonProperty("echo")]
            public bool Echo { get; set; } = false;
            [JsonProperty("stop")]
            public object Stop { get; set; } = $"`?`?";
            [JsonProperty("presence_penalty")]
            public double Presence_Penalty { get; set; } = 0;
            [JsonProperty("frequency_penalty")]
            public double Frequency_Penalty { get; set; } = 0;
            [JsonProperty("best_of")]
            public int Best_Of { get; set; } = 1;
            //[JsonProperty("logit_bias")]
            //public object Logit_Bias { get; set; } = null;
            [JsonProperty("user")]
            public string User { get; set; }
            public CompletionRequest()
            {
            }
        }

        public class CompletionResult
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("object")]
            public object Object { get; set; }
            [JsonProperty("created")]
            public long Created { get; set; }
            [JsonProperty("model")]
            public string Model { get; set; }
            [JsonProperty("choices")]
            public List<Choice> Choices { get; set; }
            [JsonProperty("usage")]
            public Usage Usage { get; set; }
            public CompletionResult()
            {
            }
        }

        public class Message
        {
            [JsonProperty("role")]
            public string Role { get; set; }
            [JsonProperty("content")]
            public string Content { get; set; }
        }

        public class ChatCompletionRequest
        {
            [JsonProperty("model")]
            public string Model { get; set; }
            [JsonProperty("messages")]
            public List<Message> Messages { get; set; }
        }

        public class ChatCompletionChoice
        {
            [JsonProperty("index")]
            public int Index { get; set; }
            [JsonProperty("message")]
            public Message Message { get; set; }
            [JsonProperty("finish_reason")]
            public string FinishReason { get; set; }
        }

        public class ChatCompletionResult
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("object")]
            public string Object { get; set; }
            [JsonProperty("created")]
            public long Created { get; set; }
            [JsonProperty("choices")]
            public List<ChatCompletionChoice> Choices { get; set; }
            [JsonProperty("usage")]
            public Usage Usage { get; set; }
        }

        public class TextEditRequest
        {
            [JsonProperty("model")]
            public string Model { get; set; }
            [JsonProperty("input")]
            public string Input { get; set; }
            [JsonProperty("instruction")]
            public string Instruction { get; set; }
            [JsonProperty("n")]
            public int N { get; set; } = 1;
            [JsonProperty("temperature")]
            public double Temperature { get; set; } = 0;
            [JsonProperty("top_p")]
            public double TopP { get; set; } = 1;
        }

        public class TextEditResponse
        {
            [JsonProperty("object")]
            public object Object { get; set; }
            [JsonProperty("created")]
            public long Created { get; set; }
            [JsonProperty("choices")]
            public List<TextEditChoice> Choices { get; set; }
            [JsonProperty("usage")]
            public Usage Usages { get; set; }
        }

        public class TextEditChoice
        {
            [JsonProperty("text")]
            public string Text { get; set; }
            [JsonProperty("index")]
            public int Index { get; set; }
        }

        public class Usage
        {
            [JsonProperty("prompt_tokens")]
            public int Prompt_Tokens { get; set; }
            [JsonProperty("completion_tokens")]
            public int Completion_Tokens { get; set; }
            [JsonProperty("total_tokens")]
            public int Total_Tokens { get; set; }
        }

        public class Choice
        {
            [JsonProperty("text")]
            public string Text { get; set; }
            [JsonProperty("index")]
            public int Index { get; set; }
            [JsonProperty("logprobs")]
            public object Logprobs { get; set; }
            [JsonProperty("finish_reason")]
            public string Finish_Reason { get; set; }
        }

        public class ObjectsInImg
        {
            [JsonProperty("bbox")]
            public List<int> Bbox { get; set; }
            [JsonProperty("object_id")]
            public object ObjectId { get; set; }
        }

        public class TextViolationCheckRequest
        {
            [JsonProperty("input")]
            public string Input { get; set; }
            [JsonProperty("model")]
            public string Model { get; set; }
            public TextViolationCheckRequest() { }
        }

        public class TextModerationResponse
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("model")]
            public string Model { get; set; }
            [JsonProperty("results")]
            public List<ModResult> Results { get; set; }

            public class ModResult
            {
                [JsonProperty("categories")]
                public Categories CategoriesBool { get; set; }
                [JsonProperty("category_scores")]
                public CategoryScores Category_Scores { get; set; }
                [JsonProperty("flagged")]
                public bool Flagged { get; set; }

                public class Categories
                {
                    [JsonProperty("hate")]
                    public bool Hate { get; set; }
                    [JsonProperty("hate/threatening")]
                    public bool Hate_Threatening { get; set; }
                    [JsonProperty("self-harm")]
                    public bool Self_Harm { get; set; }
                    [JsonProperty("sexual")]
                    public bool Sexual { get; set; }
                    [JsonProperty("sexual/minors")]
                    public bool Sexual_Minors { get; set; }
                    [JsonProperty("violence")]
                    public bool Violence { get; set; }
                    [JsonProperty("violence/graphic")]
                    public bool Violence_Graphic { get; set; }
                }

                public class CategoryScores
                {
                    [JsonProperty("hate")]
                    public double Hate { get; set; }
                    [JsonProperty("hate/threatening")]
                    public double Hate_Threatening { get; set; }
                    [JsonProperty("self_harm")]
                    public double Self_Harm { get; set; }
                    [JsonProperty("sexual")]
                    public double Sexual { get; set; }
                    [JsonProperty("sexual_minors")]
                    public double Sexual_Minors { get; set; }
                    [JsonProperty("violence")]
                    public double Violence { get; set; }
                    [JsonProperty("violence_graphic")]
                    public double Violence_Graphic { get; set; }
                }
            }
        }

        public class ImageGenerationRequest
        {
            [JsonProperty("prompt")]
            public string Prompt { get; set; }
            [JsonProperty("n")]
            public int N { get; set; } = 1;
            [JsonProperty("size")]
            public string Size { get; set; } = "1024x1024";
            [JsonProperty("response_format")]
            public string Response_Format { get; set; } = "url"; // "b64_json"
            [JsonProperty("user")]
            public string User { get; set; }
            public ImageGenerationRequest() { }
            public ImageGenerationRequest(string prompt, int n, string size)
            {
                Prompt = prompt;
                N = n;
                Size = size;
            }
        }

        public class ImageEditRequest
        {
            [JsonProperty("image_url")]
            public string Image { get; set; }
            [JsonProperty("mask")]
            public string Mask { get; set; }
            [JsonProperty("prompt")]
            public string Prompt { get; set; }
            [JsonProperty("n")]
            public int N { get; set; } = 1;
            [JsonProperty("size")]
            public string Size { get; set; } = "1024x1024";
            [JsonProperty("response_format")]
            public string Response_Format { get; set; } = "url"; // "b64_json"
            [JsonProperty("user")]
            public string User { get; set; }
        }

        public class ImageVariationRequest
        {
            [JsonProperty("image")]
            public string Image { get; set; }
            [JsonProperty("n")]
            public int N { get; set; } = 1;
            [JsonProperty("size")]
            public string Size { get; set; } = "1024x1024";//
            public ImageVariationRequest()
            {
            }
            public ImageVariationRequest(byte[] image)
            {
                Image = image.ToString();
            }
        }

        public class ImageUrlResponse
        {
            [JsonProperty("created")]
            public long Created { get; set; }
            [JsonProperty("data")]
            public List<ImageUrl> Data { get; set; }
        }

        public class ImageJsonResponse
        {
            [JsonProperty("created")]
            public long Created { get; set; }
            [JsonProperty("data")]
            public List<B64Json> Data { get; set; }
        }

        public class ImageUrl
        {
            public string Url { get; set; }
        }

        public class B64Json
        {
            [JsonProperty("b64_json")]
            public string B64_Json { get; set; }
        }

        public class ModelListResponse
        {
            [JsonProperty("list")]
            public string List { get; set; }
            [JsonProperty("data")]
            public List<Model> Data { get; set; }
        }

        public class Model
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("object")]
            public object Object { get; set; }
            [JsonProperty("owned_by")]
            public string OwnedBy { get; set; }
            [JsonProperty("permission")]
            public List<Permission> Permissions { get; set; }
        }

        public class Permission
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("object")]
            public object Object { get; set; }
            [JsonProperty("created")]
            public int Created { get; set; }
            [JsonProperty("allow_create_engine")]
            public bool AllowCreateEngine { get; set; }
            [JsonProperty("allow_sampling")]
            public bool AllowSampling { get; set; }
            [JsonProperty("allow_logprobs")]
            public bool AllowLogprobs { get; set; }
            [JsonProperty("allow_search_indices")]
            public bool AllowSearchIndices { get; set; }
            [JsonProperty("allow_view")]
            public bool AllowView { get; set; }
            [JsonProperty("allow_fine_tuning")]
            public bool AllowFineTuning { get; set; }
            [JsonProperty("organization")]
            public string Organization { get; set; }
            [JsonProperty("group")]
            public object Group { get; set; }
            [JsonProperty("is_blocking")]
            public bool IsBlocking { get; set; }
        }

        public class SpeechGenerationRequest
        {
            [JsonProperty("prompt")]
            public string Prompt { get; set; }
            [JsonProperty("model")]
            public string Model { get; set; }
            [JsonProperty("format")]
            public string Format { get; set; }
            [JsonProperty("max_tokens")]
            public int Max_Tokens { get; set; }
            [JsonProperty("stop")]
            public string Stop { get; set; }

            public SpeechGenerationRequest()
            {
                Stop = "\"\"\"";
            }
        }

        public class FileUploadRequest
        {
            [JsonProperty("purpose")]
            public string Purpose { get; set; }
            [JsonProperty("file")]
            public string File { get; set; }
        }

        public class FileInformation
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("object")]
            public object Object { get; set; }
            [JsonProperty("bytes")]
            public int Bytes { get; set; }
            [JsonProperty("created_at")]
            public int CreatedAt { get; set; }
            [JsonProperty("filename")]
            public string Filename { get; set; }
            [JsonProperty("purpose")]
            public string Purpose { get; set; }
        }

        public class FileDeleteResponse
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("object")]
            public string File { get; set; }
            [JsonProperty("deleted")]
            public bool Deleted { get; set; }
        }

        public class FileObject
        {
            [JsonProperty("file")]
            public byte[] File { get; set; }
            public FileObject(byte[] file)
            {
                File = file;
            }
        }

        public class TranscriptionRequest
        {
            [JsonProperty("file")]
            public string File { get; set; }
            [JsonProperty("model")]
            public string Model { get; set; }
            public TranscriptionRequest()
            {
            }
        }

        public class TranscriptionResponse
        {
            [JsonProperty("text")]
            public string Text { get; set; }
            public TranscriptionResponse()
            {
            }
        }


    }
}

/* Usage
var openAI = new OpenAIAPI("YOUR_API_KEY");
var text = await openAI.GenerateText("What is the capital of France?");
Console.WriteLine(text);
*/



