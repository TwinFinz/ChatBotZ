using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Text.Method;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using ChatBotZ.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using System.Drawing.Imaging;
using Java.IO;
using System.Runtime.Remoting.Contexts;
using Android.Preferences;

/* Run IF DEBUG
 * 
 * #if DEBUG
 *  // Code to run in Debug mode
 * #elif TRIAL
 *  // Code to run when in Trial mode
 * #elif RELEASE
 *   // Code to run in Release mode
 * #endif
 *  
 */

namespace ChatBotZ
{
    #region Suppressions
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable IDE0044 // Add readonly modifier
    #endregion
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {

        /* -------------- Variables -------------- */
        #region Variables
        private static string apiKey = "";
        private static List<string> botNameList = new List<string>();
        private static List<string> botNameListImageGen = new List<string>();
        private static List<string> botNameListAudio = new List<string>();
        private static List<string> editEndpointModelList = new List<string>();
        private static List<string> insertEndpointModelList = new List<string>();
        private static List<string> embedEndpointModelList = new List<string>();
        //private static List<MyOpenAIAPI.Message> chatCompletionMessages = new List<MyOpenAIAPI.Message>();
        private readonly object _lockBotNameList = new object();
        private readonly object _lockEditModelList = new object();
        private readonly object _lockInsertModelList = new object();
        private readonly object _lockEmbedModelList = new object();
        private readonly object _lockBotNameListImageGen = new object();
        private readonly object _lockBotNameListAudio = new object();
        private readonly object _lockselectedModel = new object();
        private static string selectedModel = string.Empty;
        private static readonly string pathBase = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
        private static bool ttsEnabled = false;
        private static bool settingsShown = false;
        private static bool chatEmulation = false;
        private static EditText entry;
        private static ISharedPreferences sharedPreferences;
        private static int curLayout = int.MinValue;
        private static byte[] selectedAudioFile;
        private static string selectedAudioFileName = "";
        private static byte[] selectedImageFile;
        private static byte[] selectedMaskFile;
        private static readonly string preferenceName = Application.Context.PackageName;
        private const string apiKeyPreference = "ApiKey";
        private const string botListPreference = "BotList";
        private const string selectedModelPreference = "SelectedModel";
        private const string imageBotListPreference = "ImageBotList";
        private const string audioBotListPreference = "AudioBotList";
        private const string imagePreference = "imagePreference";
        private const string maskPreference = "maskPreference";
        private const string editModelPreference = "editModelList";
        private const string insertModelPreference = "insertModelList";
        private const string embedModelPreference = "embedModelList";
        private static string previousResponses = string.Empty;
        private static MyOpenAIAPI.Message GptSystemMsg = new MyOpenAIAPI.Message()
        {
            Role = "System",
            Content = "You are ChatGPT, a large language model trained by OpenAI. Answer as concisely as possible. Knowledge cutoff: {knowledge_cutoff} Current date: {current_date}"
        };
        private static List<MyOpenAIAPI.Message> chatCompletionMessages = new List<MyOpenAIAPI.Message>() { GptSystemMsg };

        private static int backPressedTimes;
#if DEBUG
        private static List<string> settingsList = new List<string>()
        { "Completion Generation", "Chat Completion Generation", "Image Generation", "Image Edits", "Image Variation", "Audio Transcription", "Show All Models", "Reset Stored Lists", "Reset ApiKey", "Credits/Licenses" };
#elif TRIAL
        // This runs in trial Version
        private static List<string> settingsList = new List<string>() 
        { "Completion Generation", "Show All Models", "Reset Stored Lists", "Reset ApiKey", "Credits/Licenses" };
#elif RELEASE
        private static List<string> settingsList = new List<string>()
        { "Completion Generation", "Image Generation", "Image Edits", "Image Variation", "Audio Transcription", "Show All Models", "Reset Stored Lists", "Reset ApiKey", "Credits/Licenses" };
#endif
        #endregion
        /* -------------- End Variables -------------- */
        /* -------------- Startup/Events -------------- */
        #region Startup/Events
        protected override void OnCreate(Bundle savedInstanceState)
        {
            try
            {
                base.OnCreate(savedInstanceState);
                Xamarin.Essentials.Platform.Init(this, savedInstanceState);
                Task.Delay(50);
                RequestedOrientation = Android.Content.PM.ScreenOrientation.Portrait;
                switch (curLayout)
                {
                    case Resource.Layout.GenerateCompletion:
                        SetContentView(curLayout);
                        InitCompletions();
                        break;
                    case Resource.Layout.GenerateImage:
                        SetContentView(curLayout);
                        InitImageGen();
                        break;
                    case Resource.Layout.ImageVariations:
                        SetContentView(curLayout);
                        InitImageVariations();
                        break;
                    case Resource.Layout.ImageEditing:
                        SetContentView(curLayout);
                        InitEditImage();
                        break;
                    case Resource.Layout.AudioTranscription:
                        SetContentView(curLayout);
                        InitTranscribe();
                        break;
                    case Resource.Layout.SettingsPage:
                        SetContentView(curLayout);
                        InitSettings();
                        break;
                    case Resource.Layout.License:
                        SetContentView(curLayout);
                        InitLicense();
                        break;
                    case int.MinValue:
                        SetContentView(Resource.Layout.GenerateCompletion);
                        InitCompletions();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                if (ex.Data.Contains("(Object reference "))
                {
                    OnCreate(savedInstanceState);
                    return;
                }
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);
        }

        protected async override void OnDestroy()
        {
            base.OnDestroy();
            try
            {
                await WriteToSharedPreferences(apiKeyPreference, apiKey);
                lock (_lockBotNameList)
                {
                    WriteToSharedPreferences(botListPreference, botNameList);
                }
                lock (_lockBotNameListImageGen)
                {
                    WriteToSharedPreferences(imageBotListPreference, botNameListImageGen);
                }
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        protected async override void OnPause()
        {
            base.OnPause();
            // Perform actions when the app is closed
            try
            {
                await WriteToSharedPreferences(apiKeyPreference, apiKey);
                lock (_lockBotNameList)
                {
                    WriteToSharedPreferences(botListPreference, botNameList);
                }
                lock (_lockBotNameListImageGen)
                {
                    WriteToSharedPreferences(imageBotListPreference, botNameListImageGen);
                }
                lock (_lockselectedModel)
                {
                    WriteToSharedPreferences(selectedModelPreference, selectedModel);
                }
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        override async public void OnBackPressed()
        {
            try
            {
                settingsShown = false;
                if (backPressedTimes == 1)
                {
                    Java.Lang.JavaSystem.Exit(0);
                    Process.KillProcess(Process.MyPid());
                }
                else if (curLayout == Resource.Layout.License)
                {
                    ((ViewGroup)this.FindViewById(Android.Resource.Id.Content)).RemoveAllViews();
                    this.SetContentView(Resource.Layout.GenerateCompletion);
                    InitCompletions();
                }
                else if (!settingsShown)
                {
                    Toast.MakeText(Application.Context, "Press BACK one more time to exit.", ToastLength.Long).Show();
                    backPressedTimes++;
                }

            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            // Function to run when orientation changes
            HandleOrientationChange();
        }

        public async Task UpdateBotList(List<string> botNameList)
        {
            try
            {
#if TRIAL
                List<string> botNameListUpdate = new List<string>() { "ada", "babbage", "curie", "davinci", "text-ada-001", "text-babbage-001", "text-curie-001", "text-davinci-003", "code-cushman-001", "code-davinci-002" };
#else
                List<string> botNameListUpdate = await MyOpenAIAPI.GetBotNameList(apiKey);
#endif
                botNameListAudio ??= new List<string>();
                botNameListImageGen ??= new List<string>();
                if (botNameListUpdate != null)
                {
                    Parallel.ForEach(botNameListUpdate, botName =>
                    {
                        if (!botName.Contains(":") && !botNameListAudio.Contains(botName) && botName.ToLower().StartsWith("audio-"))
                        {
                            lock (_lockBotNameListAudio)
                            {
                                botNameListAudio.Add(botName.ToLower());
                            }
                        }
                        else if ((botName.ToLower().StartsWith("if-") || botName.ToLower().StartsWith("image-")) && !botName.Contains(":") && !botNameListImageGen.Contains(botName))
                        {
                            lock (_lockBotNameListImageGen)
                            {
                                botNameListImageGen.Add(botName.ToLower());
                            }
                        }
                        else if (!editEndpointModelList.Contains(botName.ToLower()) && !botName.Contains(":") && botName.ToLower().Contains("-edit-"))
                        {
                            lock (_lockEditModelList)
                            {
                                editEndpointModelList.Add(botName.ToLower());
                            }
                        }
                        else if (!insertEndpointModelList.Contains(botName.ToLower()) && !botName.Contains(":") && botName.ToLower().Contains("-insert-"))
                        {
                            lock (_lockInsertModelList)
                            {
                                insertEndpointModelList.Add(botName.ToLower());
                            }
                        }
                        else if (!embedEndpointModelList.Contains(botName.ToLower()) && !botName.Contains(":") && botName.ToLower().Contains("-embedding-"))
                        {
                            lock (_lockEmbedModelList)
                            {
                                embedEndpointModelList.Add(botName.ToLower());
                            }
                        }
                        else if (!botNameList.Contains(botName.ToLower()) && !botName.Contains(":"))
                        {
                            lock (_lockBotNameList)
                            {
                                botNameList.Add(botName.ToLower());
                            }
                        }

                    });
                    Parallel.ForEach(editEndpointModelList, botName =>
                   {
                       if (!botNameList.Contains(botName.ToLower()))
                       {
                           lock (_lockBotNameList)
                           {
                               botNameList.Add(botName);
                           }
                       }
                   });

                    botNameList.Remove("whisper-1");
                    botNameList.Remove("gpt-3.5-turbo-0301");
                    botNameList.Remove("gpt-3.5-turbo");
                    botNameList.Sort();
                }
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }
        #endregion
        /* -------------- End Startup -------------- */
        /* -------------- Layout Inits -------------- */
        #region Layout Inits
        public async void InitCompletions()
        {
            try
            {
                if (this.FindViewById(Android.Resource.Id.Content) is ViewGroup contentView)
                {
                    contentView.RemoveAllViews();
                }
                this.SetContentView(Resource.Layout.GenerateCompletion);
                sharedPreferences = GetSharedPreferences(preferenceName, FileCreationMode.Private);
                Button sendBtn = FindViewById<Button>(Resource.Id.send_button);
                if (sendBtn != null)
                {
                    sendBtn.Click += Execute;
                    sendBtn.LongClick += RecordThenExecute;
                }
                ImageButton settingBtn = FindViewById<ImageButton>(Resource.Id.action_settings);
                if (settingBtn != null)
                {
                    settingBtn.Click += SettingsBtn_Click;
                }
                apiKey = await ReadFromSharedPreferences<string>(apiKeyPreference) ?? string.Empty;
                if (apiKey == string.Empty)
                {
                    apiKey = await PromptUser("Key Request", "API Key: Not Found!\nGet your API Key here: https://beta.MyOpenAIAPI.com/account/api-keys\nThen enter your API key here.", "Saving API key", "API key not valid!\nPlease enter your API key");
                }
                if (apiKey == string.Empty)
                {
                    Java.Lang.JavaSystem.Exit(0);
                    Process.KillProcess(Process.MyPid());
                }
                botNameList = await ReadFromSharedPreferences<List<string>>(botListPreference) ?? new List<string>();
                bool update = botNameList.Count <= 3 || CheckAgeOfPreference(botListPreference, sharedPreferences) >= 7;
                if (update)
                {
                    await UpdateBotList(botNameList);
                }
                lock (_lockBotNameListImageGen)
                {
                    Task<List<string>> result = ReadFromSharedPreferences<List<string>>(imageBotListPreference);
                    botNameListImageGen = result.Result ?? new List<string>();
                }
                lock (_lockBotNameListAudio)
                {
                    Task<List<string>> result = ReadFromSharedPreferences<List<string>>(audioBotListPreference);
                    botNameListAudio = result.Result ?? new List<string>();
                }
                string lastSelectedModel = await ReadFromSharedPreferences<string>(selectedModelPreference);
                Spinner botSelection = FindViewById<Spinner>(Resource.Id.botselector);
                if (botSelection != null)
                {
                    ArrayAdapter adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, botNameList);
                    adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
                    botSelection.Adapter = adapter;
                    botSelection.ItemSelected += UpdateSelectedModel;
                    int index = botNameList.IndexOf(lastSelectedModel);
                    if (index < botNameList.Count && index > 0)
                    {
                        botSelection.SetSelection(index);
                    }
                    else
                    {
                        botSelection.SetSelection(0);
                        index = 0;
                    }
                    selectedModel = botSelection.GetItemAtPosition(index).ToString();
                }
                await WriteToSharedPreferences(apiKeyPreference, apiKey);
                lock (_lockBotNameList)
                {
                    WriteToSharedPreferences(botListPreference, botNameList);
                }
                lock (_lockBotNameListImageGen)
                {
                    WriteToSharedPreferences(imageBotListPreference, botNameListImageGen);
                }
                TextView logView = FindViewById<TextView>(Resource.Id.logView);
                if (logView != null)
                {
                    SpannableStringBuilder ssb = new SpannableStringBuilder(await HighlightCodeSnippet(IntroMsg, true));
                    if (ssb != null)
                    {
                        logView.TextFormatted = (Java.Lang.ICharSequence)ssb;
                    }
                }
                chatEmulation = await ShowYesNoMessage("Emulate ChatGPT?", "Do you want to include previous message when submitting prompts?");
                previousResponses = string.Empty;

            }
            catch (Exception ex)
            {
                if (ex.Data.Contains("(Object reference "))
                {
                    InitCompletions();
                    return;
                }

                await ShowMessage("Failed", ex.Message);
            }
        }

        public async void InitChatCompletions()
        {
            try
            {
                if (this.FindViewById(Android.Resource.Id.Content) is ViewGroup contentView)
                {
                    contentView.RemoveAllViews();
                }
                this.SetContentView(Resource.Layout.GenerateChatCompletion);
                sharedPreferences = GetSharedPreferences(preferenceName, FileCreationMode.Private);
                Button sendBtn = FindViewById<Button>(Resource.Id.sendchat_button);
                if (sendBtn != null)
                {
                    sendBtn.Click += ExecuteChatCompletion;
                }
                ImageButton settingBtn = FindViewById<ImageButton>(Resource.Id.action_settings);
                if (settingBtn != null)
                {
                    settingBtn.Click += SettingsBtn_Click;
                }
                chatCompletionMessages = new List<MyOpenAIAPI.Message>() { GptSystemMsg };
            }
            catch (Exception ex)
            {
                if (ex.Data.Contains("(Object reference "))
                {
                    InitChatCompletions();
                    return;
                }

                await ShowMessage("Failed", ex.Message);
            }
        }

        public async void InitSettings()
        {
            try
            {
                Button CompletionsBtn = FindViewById<Button>(Resource.Id.CompletionsBtn);
                CompletionsBtn.Click += OnCompletionsBtnClick;
                Button ImageGenBtn = FindViewById<Button>(Resource.Id.ImageGenBtn);
                ImageGenBtn.Click += OnImageGenBtnClick;
                Button TranscribeBtn = FindViewById<Button>(Resource.Id.TranscribeBtn);
                TranscribeBtn.Click += OnTranscribeBtnClick;
                Button editImageBtn = FindViewById<Button>(Resource.Id.editImageBtn);
                editImageBtn.Click += OnImageEditsBtnClick;
                Button ResetBotsListBtn = FindViewById<Button>(Resource.Id.ResetBotsListBtn);
                ResetBotsListBtn.Click += OnResetBotsListBtnClick;
                Button ResetAPIBtn = FindViewById<Button>(Resource.Id.ResetAPIBtn);
                ResetAPIBtn.Click += OnResetAPIBtnClick;
                Button ImageVariationBtn = FindViewById<Button>(Resource.Id.ImageVariationBtn);
                ImageVariationBtn.Click += OnImageVariationBtnClick;
                Button LicensesBtn = FindViewById<Button>(Resource.Id.LicensesBtn);
                LicensesBtn.Click += OnLicensesBtnClick;

                curLayout = Resource.Layout.SettingsPage;

            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        public async void InitAudioTranscribe()
        {
            try
            {
                ((ViewGroup)this.FindViewById(Android.Resource.Id.Content)).RemoveAllViews();
                this.SetContentView(Resource.Layout.AudioTranscription);
                ImageButton SettingBtn = FindViewById<ImageButton>(Resource.Id.action_settings);
                SettingBtn.Click += SettingsBtn_Click;
                Button RecordAudioBtn = FindViewById<Button>(Resource.Id.RecordAudioBtn);
                RecordAudioBtn.Click += RecordAudioBtn_Click;
                Button SelectAudioBtn = FindViewById<Button>(Resource.Id.SelectAudioBtn);
                SelectAudioBtn.Click += SelectAudioBtn_Click;
                Button TranscribeAudioBtn = FindViewById<Button>(Resource.Id.TranscribeAudioBtn);
                TranscribeAudioBtn.Click += ExecuteTranscribeAudio;
                TextView transcribedTxtView = FindViewById<TextView>(Resource.Id.transcribedTxtView);
                curLayout = Resource.Layout.AudioTranscription;
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        public async void InitImageGen()
        {
            try
            {
                ((ViewGroup)this.FindViewById(Android.Resource.Id.Content)).RemoveAllViews();
                this.SetContentView(Resource.Layout.GenerateImage);
                sharedPreferences = GetSharedPreferences(preferenceName, FileCreationMode.Private);
                Button SendBtn = FindViewById<Button>(Resource.Id.send_button);
                SendBtn.Click += ExecuteImageGen;
                ImageButton SettingBtn = FindViewById<ImageButton>(Resource.Id.action_settings);
                SettingBtn.Click += SettingsBtn_Click;
                ImageView imageView = FindViewById<ImageView>(Resource.Id.imageGenOutput);
                imageView.LongClick += OnImageClick;
                curLayout = Resource.Layout.GenerateImage;
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        public async void InitImageVariations()
        {
            try
            {
                ((ViewGroup)this.FindViewById(Android.Resource.Id.Content)).RemoveAllViews();
                this.SetContentView(Resource.Layout.ImageVariations);
                ImageButton SettingBtn = FindViewById<ImageButton>(Resource.Id.action_settings);
                SettingBtn.Click += SettingsBtn_Click;
                Button SelectImgBtn = FindViewById<Button>(Resource.Id.SelectImageBtn);
                SelectImgBtn.Click += SelectImgBtn_Click;
                Button ImageVariationBtn = FindViewById<Button>(Resource.Id.ModifyImageBtn);
                ImageVariationBtn.Click += ExecuteImageVariations;
                ImageView imageVarOutput = FindViewById<ImageView>(Resource.Id.imageVarOutput);
                imageVarOutput.LongClick += OnImageClick;

                curLayout = Resource.Layout.ImageVariations;
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        public async void InitEditImage()
        {
            try
            {
                ((ViewGroup)this.FindViewById(Android.Resource.Id.Content)).RemoveAllViews();
                this.SetContentView(Resource.Layout.ImageEditing);
                ImageButton SettingBtn = FindViewById<ImageButton>(Resource.Id.action_settings);
                SettingBtn.Click += SettingsBtn_Click;
                Button SelectImgBtn = FindViewById<Button>(Resource.Id.SelectImageBtn);
                SelectImgBtn.Click += SelectImgBtn_Click;
                Button ImageVariationBtn = FindViewById<Button>(Resource.Id.ModifyImageBtn);
                ImageVariationBtn.Click += ExecuteEditImg;
                ImageView imageVarOutput = FindViewById<ImageView>(Resource.Id.imageVarOutput);
                imageVarOutput.LongClick += OnImageClick;

                curLayout = Resource.Layout.ImageEditing;
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        public async void InitTranscribe()
        {
            try
            {
                ImageButton SettingBtn = FindViewById<ImageButton>(Resource.Id.action_settings);
                SettingBtn.Click += SettingsBtn_Click;
                Button SelectImgBtn = FindViewById<Button>(Resource.Id.SelectAudioBtn);
                SelectImgBtn.Click += SelectAudioBtn_Click;
                Button TranscribeAudioBtn = FindViewById<Button>(Resource.Id.TranscribeAudioBtn);
                TranscribeAudioBtn.Click += ExecuteTranscribeAudio;

                curLayout = Resource.Layout.AudioTranscription;
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        public async void InitSpeechGen()
        {
            try
            {
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        public async void InitLicense()
        {
            try
            {
                ((ViewGroup)this.FindViewById(Android.Resource.Id.Content)).RemoveAllViews();
                this.SetContentView(Resource.Layout.License);
                TextView licenseView = FindViewById<TextView>(Resource.Id.licenseView);
                licenseView.TextFormatted = (Java.Lang.ICharSequence)await HighlightCodeSnippet(LicenseTxt, true);
                curLayout = Resource.Layout.License;
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }
        #endregion
        /* -------------- End Layout Inits -------------- */
        /* -------------- Ui Elements -------------- */
        #region Ui Elements
        public async Task ShowMessage(string title, string msg)
        {
            string response = string.Empty;
            Android.App.AlertDialog.Builder dialog = new Android.App.AlertDialog.Builder(this);
            dialog.SetTitle(title);
            dialog.SetMessage(msg);
            dialog.SetCancelable(false);
            dialog.SetPositiveButton("OK", (senderAlert, args) => { });
            Android.App.AlertDialog alert = dialog.Create();
            alert.Show();
            await Task.CompletedTask;
        }

        public async Task<bool> ShowYesNoMessage(string title, string msg)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            bool response = false;
            Android.App.AlertDialog.Builder dialog = new Android.App.AlertDialog.Builder(this);
            dialog.SetTitle(title);
            dialog.SetMessage(msg);
            dialog.SetCancelable(false);
            dialog.SetPositiveButton("Yes", (senderAlert, args) =>
            {
                response = true;
                tcs.SetResult(response);
            });
            dialog.SetNegativeButton("No", (senderAlert, args) =>
            {
                response = false;
                tcs.SetResult(response);
            });
            Android.App.AlertDialog alert = dialog.Create();
            alert.Show();
            await Task.CompletedTask;
            return await tcs.Task;
        }

        public async Task<string> PromptUser(string title, string msg, string successToast = "Successfully loaded", string failureToast = "Failed to load")
        {
            string response = string.Empty;
            entry = new EditText(Android.App.Application.Context);
            Android.App.AlertDialog.Builder dialog = new Android.App.AlertDialog.Builder(this);
            dialog.SetView(entry);

            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            dialog.SetPositiveButton("OK", (senderAlert, args) =>
            {
                response = entry.Text;
                if (string.IsNullOrEmpty(response))
                {
                    Toast.MakeText(this, failureToast, ToastLength.Long).Show();
                }
                else
                {
                    Toast.MakeText(this, successToast, ToastLength.Long).Show();
                }
                tcs.SetResult(response);
            });
            dialog.SetTitle(title);
            dialog.SetMessage(msg);
            dialog.SetCancelable(false);
            dialog.Create().Show();
            return await tcs.Task;
        }

        private async void SetImageFromUrl(ImageView imageView, string url)
        {
            try
            {
                using HttpClient client = new HttpClient();
                byte[] imageBytes = await client.GetByteArrayAsync(url);
                Bitmap imageBitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                imageView.SetImageBitmap(imageBitmap);
                imageView.Visibility = ViewStates.Visible;
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        private async void OnImageClick(object sender, EventArgs e)
        {
            try
            {
                if (await ShowYesNoMessage("Save it?", "This is a great image, Do you want to save it?"))
                {
                    ImageView imageView = (ImageView)sender;
                    Drawable drawable = imageView.Drawable;
                    Android.Graphics.Bitmap bitmap = ((BitmapDrawable)drawable).Bitmap;
                    string filePath = System.IO.Path.Combine(pathBase, "GeneratedImage.png");
                    int i = 1;
                    while (System.IO.File.Exists(filePath))
                    {
                        filePath = System.IO.Path.Combine(pathBase, $"GeneratedImage({i}).png");
                        i++;
                    }
                    // Convert bitmap to byte array
                    MemoryStream stream = new MemoryStream();
                    if (bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream))
                    {
                        byte[] imageBytes = stream.ToArray();
                        // Share the image
                        System.IO.File.WriteAllBytes(filePath, imageBytes);
                        SendBroadcast(new Intent(Intent.ActionMediaScannerScanFile, Android.Net.Uri.FromFile(new Java.IO.File(filePath))));
                        Toast.MakeText(Application.Context, $"Image saved to: {filePath}", ToastLength.Long).Show();
                    }
                }
                else
                {
                    Toast.MakeText(Application.Context, $"Not Saved.", ToastLength.Short).Show();
                }

            }
            catch (Exception ex)
            {
                Toast.MakeText(Application.Context, $"{ex.Message}", ToastLength.Long).Show();
            }
        }

        private async void SelectAudioBtn_Click(object sender, EventArgs e)
        {
            FileResult audioFile = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Select Audio File" });

            if (audioFile != null)
            {
                Toast.MakeText(Application.Context, $"Selected: {audioFile.FullPath}", ToastLength.Long).Show();
                selectedAudioFileName = audioFile.FileName;
                selectedAudioFile = Utilities.MyFileUtils.ReadBinaryFile(audioFile.FullPath);
                TextView transcribedTxtView = FindViewById<TextView>(Resource.Id.transcribedTxtView);
                transcribedTxtView.Text = $"Selected File: {audioFile.FullPath}\n\n";
            }
            else if (audioFile != null)
            {
                Toast.MakeText(Application.Context, $"File MUST be a valid file.", ToastLength.Long).Show();
                return;
            }
            else
            {
                return;
            }

        }

        private async Task<byte[]> SelectAudioFile()
        {
            FileResult file = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Select Wav File" });
            if (file != null && file.FileName.EndsWith(".wav"))
            {
                Toast.MakeText(Application.Context, $"Selected: {file.FullPath}", ToastLength.Long).Show();
                return Utilities.MyFileUtils.ReadBinaryFile(file.FullPath);
            }
            else if (file != null)
            {
                Toast.MakeText(Application.Context, $"File MUST be a valid WAV file.", ToastLength.Long).Show();
                return null;
            }
            else
            {
                return null;
            }
        }

        private async void SelectImgBtn_Click(object sender, EventArgs e)
        {
            selectedImageFile = await MyImageUtils.SelectImageFile();
            ImageView imageView = FindViewById<ImageView>(Resource.Id.imageVarOutput);
            Bitmap bitmap = BitmapFactory.DecodeByteArray(selectedImageFile, 0, selectedImageFile.Length);
            imageView.SetImageBitmap(bitmap);
            imageView.Visibility = ViewStates.Visible;
            if (curLayout == Resource.Layout.ImageVariations)
            {
                selectedMaskFile = null;
                return;
            }
            else
            {
                selectedMaskFile = await ShowYesNoMessage("Select a mask?", "Do you want to select a mask to use?") ? await MyImageUtils.SelectImageFile() : null;
            }
        }

        private async void HandleOrientationChange()
        {
            await ShowMessage("Detected", "We successfully detected orientation change.");
        }

        private Task<SpannableString> HighlightCodeSnippet(string codeSnippet, bool HighlightModels = false)
        {
            ConcurrentDictionary<string, Color> BotNameKeywords = new ConcurrentDictionary<string, Color>();
            {
                lock (_lockBotNameList)
                {
                    List<string> sortedBotNameList = botNameList.OrderByDescending(x => x.Length).ThenByDescending(x => x).ToList();
                    Task[] modelNameColorize = sortedBotNameList.Select(botName => Task.Factory.StartNew(() =>
                    {
                        BotNameKeywords[botName] = Color.Aqua;
                    })).ToArray();
                    Task.WaitAll(modelNameColorize);
                    BotNameKeywords["OpenAI"] = Color.OliveDrab;
                    BotNameKeywords["ChatBotZ"] = Color.MediumSeaGreen;
                }

            }
            ConcurrentDictionary<string, Color> cSharpKeywords = new ConcurrentDictionary<string, Color>(new Dictionary<string, Color>()
            {
                { "{", Color.DarkGoldenrod },
                { "}", Color.DarkGoldenrod },
                { "[", Color.DarkGoldenrod },
                { "]", Color.DarkGoldenrod },
                { "abstract", Color.Purple },
                { "as", Color.Purple },
                { "base", Color.Purple },
                { "bool", Color.Blue },
                { "break", Color.Purple },
                { "byte", Color.Blue },
                { "case", Color.Purple },
                { "catch", Color.Purple },
                { "char", Color.Blue },
                { "checked", Color.Purple },
                { "class", Color.Green },
                { "const", Color.Purple },
                { "continue", Color.Purple },
                { "decimal", Color.Blue },
                { "default", Color.Purple },
                { "delegate", Color.Green },
                { "do", Color.Purple },
                { "double", Color.Blue },
                { "else", Color.Purple },
                { "enum", Color.Green },
                { "event", Color.Green },
                { "explicit", Color.Purple },
                { "extern", Color.Purple },
                { "false", Color.Blue },
                { "finally", Color.Purple },
                { "fixed", Color.Purple },
                { "float", Color.Blue },
                { "for", Color.Purple },
                { "foreach", Color.Purple },
                { "goto", Color.Purple },
                { "if", Color.Purple },
                { "implicit", Color.Purple },
                { "in", Color.Purple },
                { "int", Color.Blue },
                { "interface", Color.Green },
                { "internal", Color.Purple },
                { "is", Color.Purple },
                { "lock", Color.Purple },
                { "long", Color.Blue },
                { "namespace", Color.Green },
                { "new", Color.Purple },
                { "null", Color.Blue },
                { "object", Color.Blue },
                { "operator", Color.Purple },
                { "out", Color.Purple },
                { "override", Color.Purple },
                { "params", Color.Purple },
                { "private", Color.Purple },
                { "protected", Color.Purple },
                { "public", Color.Purple },
                { "readonly", Color.Purple },
                { "ref", Color.Purple },
                { "return", Color.Purple },
                { "sbyte", Color.Blue },
                { "sealed", Color.Purple },
                { "short", Color.Blue },
                { "sizeof", Color.Purple },
                { "stackalloc", Color.Purple },
                { "static", Color.Purple },
                { "string", Color.Blue },
                { "struct", Color.Green },
                { "switch", Color.Purple },
                { "this", Color.Purple },
                { "throw", Color.Purple },
                { "true", Color.Blue },
                { "try", Color.Purple },
                { "typeof", Color.Purple },
                { "uint", Color.Blue },
                { "ulong", Color.Blue },
                { "unchecked", Color.Purple },
                { "unsafe", Color.Purple },
                { "ushort", Color.Blue },
                { "using", Color.Purple },
                { "virtual", Color.Purple },
                { "void", Color.Blue },
                { "volatile", Color.Purple },
                { "while", Color.Purple }
            });
            ConcurrentDictionary<string, Color> pythonKeywords = new ConcurrentDictionary<string, Color>(new Dictionary<string, Color>()
            {
                { "{", Color.DarkGoldenrod },
                { "}", Color.DarkGoldenrod },
                { "[", Color.DarkGoldenrod },
                { "]", Color.DarkGoldenrod },
                { "and", Color.Purple },
                { "as", Color.Purple },
                { "assert", Color.Purple },
                { "async", Color.Purple },
                { "await", Color.Purple },
                { "break", Color.Purple },
                { "class", Color.Green },
                { "continue", Color.Purple },
                { "def", Color.Green },
                { "del", Color.Purple },
                { "elif", Color.Purple },
                { "else", Color.Purple },
                { "except", Color.Purple },
                { "False", Color.Blue },
                { "finally", Color.Purple },
                { "for", Color.Purple },
                { "from", Color.Purple },
                { "global", Color.Purple },
                { "if", Color.Purple },
                { "import", Color.Purple },
                { "in", Color.Purple },
                { "is", Color.Purple },
                { "lambda", Color.Green },
                { "None", Color.Blue },
                { "nonlocal", Color.Purple },
                { "not", Color.Purple },
                { "or", Color.Purple },
                { "pass", Color.Purple },
                { "raise", Color.Purple },
                { "return", Color.Purple },
                { "True", Color.Blue },
                { "try", Color.Purple },
                { "while", Color.Purple },
                { "with", Color.Purple },
                { "yield", Color.Purple }
            });
            ConcurrentDictionary<string, Color> htmlCssKeywords = new ConcurrentDictionary<string, Color>(new Dictionary<string, Color>()
            {
                { "{", Color.DarkGoldenrod },
                { "}", Color.DarkGoldenrod },
                { "[", Color.DarkGoldenrod },
                { "]", Color.DarkGoldenrod },
                { "<html>", Color.Orange },
                { "<head>", Color.Orange },
                { "<title>", Color.Orange },
                { "<body>", Color.Orange },
                { "<h1>", Color.Orange },
                { "<h2>", Color.Orange },
                { "<h3>", Color.Orange },
                { "<h4>", Color.Orange },
                { "<h5>", Color.Orange },
                { "<h6>", Color.Orange },
                { "<p>", Color.Orange },
                { "<a>", Color.Orange },
                { "<img>", Color.Orange },
                { "<ul>", Color.Orange },
                { "<ol>", Color.Orange },
                { "<li>", Color.Orange },
                { "<div>", Color.Orange },
                { "<span>", Color.Orange },
                { "<table>", Color.Orange },
                { "<tr>", Color.Orange },
                { "<td>", Color.Orange },
                { "<th>", Color.Orange },
                { "background-color", Color.Purple },
                { "color", Color.Purple },
                { "font-family", Color.Purple },
                { "font-size", Color.Purple },
                { "height", Color.Purple },
                { "width", Color.Purple },
                { "margin", Color.Purple },
                { "padding", Color.Purple },
                { "border", Color.Purple },
                { "display", Color.Purple },
                { "float", Color.Purple },
                { "text-align", Color.Purple }
            });
            ConcurrentDictionary<string, Color> cppKeywords = new ConcurrentDictionary<string, Color>(new Dictionary<string, Color>()
            {
                { "{", Color.DarkGoldenrod },
                { "}", Color.DarkGoldenrod },
                { "[", Color.DarkGoldenrod },
                { "]", Color.DarkGoldenrod },
                { "auto", Color.Purple },
                { "break", Color.Purple },
                { "case", Color.Purple },
                { "char", Color.Green },
                { "const", Color.Purple },
                { "continue", Color.Purple },
                { "default", Color.Purple },
                { "do", Color.Purple },
                { "double", Color.Green },
                { "else", Color.Purple },
                { "enum", Color.Green },
                { "extern", Color.Purple },
                { "float", Color.Green },
                { "for", Color.Purple },
                { "goto", Color.Purple },
                { "if", Color.Purple },
                { "int", Color.Green },
                { "long", Color.Green },
                { "register", Color.Purple },
                { "return", Color.Purple },
                { "short", Color.Green },
                { "signed", Color.Green },
                { "sizeof", Color.Purple },
                { "static", Color.Purple },
                { "struct", Color.Green },
                { "switch", Color.Purple },
                { "typedef", Color.Purple },
                { "union", Color.Green },
                { "unsigned", Color.Green },
                { "void", Color.Green },
                { "volatile", Color.Purple },
                { "while", Color.Purple }
            });
            ConcurrentDictionary<string, Color> xmlKeywords = new ConcurrentDictionary<string, Color>(new Dictionary<string, Color>()
            {
                { "{", Color.DarkGoldenrod },
                { "}", Color.DarkGoldenrod },
                { "[", Color.DarkGoldenrod },
                { "]", Color.DarkGoldenrod },
                { "<!--", Color.Purple },
                { "<!DOCTYPE", Color.Purple },
                { "<![CDATA[", Color.Purple },
                { "<?", Color.Purple },
                { "</", Color.Purple },
                { "<", Color.Purple },
                { ">", Color.Purple },
                { "/>", Color.Purple },
                { "=", Color.Purple }
            });
            ConcurrentDictionary<string, Color> javaKeywords = new ConcurrentDictionary<string, Color>(new Dictionary<string, Color>()
            {
                { "{", Color.DarkGoldenrod },
                { "}", Color.DarkGoldenrod },
                { "[", Color.DarkGoldenrod },
                { "]", Color.DarkGoldenrod },
                { "abstract", Color.Purple },
                { "assert", Color.Purple },
                { "boolean", Color.Green },
                { "break", Color.Purple },
                { "byte", Color.Green },
                { "case", Color.Purple },
                { "catch", Color.Purple },
                { "char", Color.Green },
                { "class", Color.Purple },
                { "const", Color.Purple },
                { "continue", Color.Purple },
                { "default", Color.Purple },
                { "do", Color.Purple },
                { "double", Color.Green },
                { "else", Color.Purple },
                { "enum", Color.Green },
                { "extends", Color.Purple },
                { "final", Color.Purple },
                { "finally", Color.Purple },
                { "float", Color.Green },
                { "for", Color.Purple },
                { "goto", Color.Purple },
                { "if", Color.Purple },
                { "implements", Color.Purple },
                { "import", Color.Purple },
                { "instanceof", Color.Purple },
                { "int", Color.Green },
                { "interface", Color.Purple },
                { "long", Color.Green },
                { "native", Color.Purple },
                { "new", Color.Purple },
                { "package", Color.Purple },
                { "private", Color.Purple },
                { "protected", Color.Purple },
                { "public", Color.Purple },
                { "return", Color.Purple },
                { "short", Color.Green },
                { "static", Color.Purple },
                { "strictfp", Color.Purple },
                { "super", Color.Purple },
                { "switch", Color.Purple },
                { "synchronized", Color.Purple },
                { "this", Color.Purple },
                { "throw", Color.Purple },
                { "throws", Color.Purple },
                { "transient", Color.Purple },
                { "try", Color.Purple },
                { "void", Color.Green },
                { "volatile", Color.Purple },
                { "while", Color.Purple }
            });
            ConcurrentDictionary<string, Color> keywords = null;
            int start = 0;
            if (codeSnippet.Contains("```") && (codeSnippet.Contains("c#") || codeSnippet.Contains("csharp")))
            {
                keywords = cSharpKeywords;
            }
            else if (codeSnippet.Contains("```") && codeSnippet.Contains("python"))
            {
                keywords = pythonKeywords;
            }
            else if (codeSnippet.Contains("```") && (codeSnippet.Contains("html") || codeSnippet.Contains("css")))
            {
                keywords = htmlCssKeywords;
            }
            else if (codeSnippet.Contains("```") && (codeSnippet.Contains("cpp") || codeSnippet.Contains("c++")))
            {
                keywords = cppKeywords;
            }
            else if (codeSnippet.Contains("```") && codeSnippet.Contains("xml"))
            {
                keywords = xmlKeywords;
            }
            else if (codeSnippet.Contains("```") && codeSnippet.Contains("java"))
            {
                keywords = javaKeywords;
            }
            else if (HighlightModels && botNameList.Any(name => codeSnippet.Contains(name)))
            {
                keywords = BotNameKeywords;
            }
            if (keywords == null)
            {
                return Task.FromResult(new SpannableString(codeSnippet));
            }
            object _spannableLock = new object();
            SpannableString spannableString = new SpannableString(codeSnippet[start..]);
            IEnumerable<KeyValuePair<string, Color>> sortedKeywords = keywords.OrderByDescending(k => k.Key.Length).ThenByDescending(k => k.Key);
            Parallel.ForEach(sortedKeywords, keyword =>
            {
                string pattern = @"\b" + Regex.Escape(keyword.Key) + @"\b";
                MatchCollection matches = Regex.Matches(spannableString.ToString(), pattern, RegexOptions.IgnoreCase);

                Parallel.ForEach((IEnumerable<Match>)matches, match =>
                {
                    int startIndex = match.Index;
                    int endIndex = match.Index + match.Length;
                    lock (_spannableLock)
                    {
                        spannableString.SetSpan(new ForegroundColorSpan(keyword.Value), startIndex, endIndex, SpanTypes.ExclusiveExclusive);
                    }
                });
            });
            return Task.FromResult(spannableString);
        }
        #endregion
        /* -------------- Ui Elements -------------- */
        /* -------------- Registered Functions ------------- */
        #region Registered Functions
        private async void UpdateSelectedModel(object sender, AdapterView.ItemSelectedEventArgs e) // When user changes model to using dropdown menu 
        {
            try
            {
                Spinner botSelection = FindViewById<Spinner>(Resource.Id.botselector);
                selectedModel = botSelection.SelectedItem.ToString();
                lock (_lockselectedModel)
                {
                    WriteToSharedPreferences(selectedModelPreference, selectedModel);
                }
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            return id == Resource.Id.action_settings || base.OnOptionsItemSelected(item);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        #endregion
        /* -------------- End Registered Functions ------------- */
        /* -------------- Buttons -------------- */
        #region Buttons
        private async void Execute(object sender, EventArgs e)
        {
#if DEBUG
            try
            {
                Window.AddFlags(WindowManagerFlags.KeepScreenOn);
                EditText inputbox = FindViewById<EditText>(Resource.Id.message_input);
                TextView logBox = FindViewById<TextView>(Resource.Id.logView);
                TextView codeView = FindViewById<TextView>(Resource.Id.codeView);
                string aiResponse = string.Empty;
                string inputText = string.Empty;
                logBox.Text = string.Empty;
                inputText = inputbox.Text.Trim() ?? string.Empty;
                previousResponses += $"{inputText}\n\n";
                if (chatEmulation)
                {
                    inputText = previousResponses.Trim();
                }
                string waitMsg = $"Please Wait... \nSending:\n" + inputText;
                logBox.Text = waitMsg;
                // Send text to model and retrieve response
                Toast.MakeText(Application.Context, $"Generating...", ToastLength.Short).Show();
                if (inputText != string.Empty)
                {
                    if (editEndpointModelList.Contains(selectedModel) || insertEndpointModelList.Contains(selectedModel))
                    {
                        aiResponse = await MyOpenAIAPI.EditTextAsyncHttp(apiKey, selectedModel, inputText) ?? string.Empty;
                    }
                    else
                    {
                        aiResponse = await MyOpenAIAPI.GenerateTextAsyncHttp(apiKey, inputText, selectedModel) ?? string.Empty;
                    }
                }
                else
                {
                    await ShowMessage("Warning", "You must enter text or else this will not work.");
                }
                string[] parts = aiResponse.Split("\n");
                string justResponse = string.Join("\n", parts.Skip(5));
                if (justResponse != null && justResponse != string.Empty)
                {
                    if (previousResponses.Length > 900)
                    {
                        string[] responses = previousResponses.Split("\n\n");
                        string lastResponse = string.Join("\n\n", responses.Skip(responses.Length - 4));
                        previousResponses = $"{lastResponse}\n\n{justResponse}";
                    }
                    else
                    {
                        previousResponses = $"{inputText}\n\n{justResponse}";
                    }                    
                    if (ttsEnabled)
                    {
                        await MyEssentialUtils.SpeakText(justResponse);
                    }
                }
                if (chatEmulation)
                {
                    logBox.Text = $"{string.Join("\n", parts.Take(5))}{previousResponses}";
                }
                else
                {
                    logBox.Text = aiResponse;
                }
                codeView.Text = string.Empty;
                inputbox.Text = string.Empty;
                Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
            }

#else
            try
            {
                Window.AddFlags(WindowManagerFlags.KeepScreenOn);
                EditText inputbox = FindViewById<EditText>(Resource.Id.message_input);
                TextView logBox = FindViewById<TextView>(Resource.Id.logView);
                string aiResponse = string.Empty;
                string inputText = string.Empty;
                logBox.Text = string.Empty;
                inputText = inputbox.Text.Trim() ?? string.Empty;
                previousResponses += $"{inputText}\n\n";
                if (chatEmulation)
                {
                    inputText = previousResponses.Trim();
                }
                string waitMsg = $"Please Wait... \nSending:\n" + inputText;
                logBox.Text = waitMsg;
                // Send text to model and retrieve response
                Toast.MakeText(Application.Context, $"Generating...", ToastLength.Short).Show();
                if (inputText != string.Empty)
                {
                    aiResponse = await MyOpenAIAPI.GenerateTextAsyncHttp(apiKey, inputText, selectedModel) ?? string.Empty;
                }
                else
                {
                    await ShowMessage("Warning", "You must enter text or else this will not work.");
                }
                string[] parts = aiResponse.Split("\n");
                string justResponse = string.Join("\n", parts.Skip(5));
                if (justResponse != null && justResponse != string.Empty)
                {
                    if (previousResponses.Length > 900)
                    {
                        string[] responses = previousResponses.Split("\n\n");
                        string lastResponse = string.Join("\n\n", responses.Skip(responses.Length - 4));
                        previousResponses = $"{lastResponse}\n\n{justResponse}";
                    }
                    else
                    {
                        previousResponses = $"{inputText}\n\n{justResponse}";
                    }
                    if (ttsEnabled)
                    {
                        await MyEssentialUtils.SpeakText(justResponse);
                    }
                }
                if (chatEmulation)
                {
                    logBox.Text = $"{string.Join("\n", parts.Take(5))}{previousResponses}";
                }
                else
                {
                    logBox.Text = aiResponse;
                }
                inputbox.Text = string.Empty;
                Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
            }
#endif
            catch (Exception ex)
            {
                if (ex.Message.Contains("400"))
                {
                    await ShowMessage("Error!", "HTTP Error: 400\nThe Server will respond with this if the prompt contains prohibited words or Characters. \nPossibly the prompt is too long?\nTry rewording or shortening your prompt and try again.");
                }
                else if (ex.Message.Contains("404"))
                {
                    await ShowMessage("Error!", "HTTP Error: 404\nThe Server will respond with this if the server refuses to process. \nPossibly the API Key has insufficient permissions? \nTry changing model or API Key and try again.");
                }
                else
                {
                    await ShowMessage("Error!", $"Error: {ex.Message}");
                }
            }
        }

        private async void RecordThenExecute(object sender, EventArgs e)
        {

#if DEBUG
            try
            {
            }
#else
            try
            {
                TextView logBox = FindViewById<TextView>(Resource.Id.logView);
                if (await ShowYesNoMessage("TTS Responses?", "Would you like to use TTS feature?"))
                {
                    Toast.MakeText(Application.Context, $"We have Enabled the TTS functionality.", ToastLength.Long).Show();
                    ttsEnabled = true;
                }
                else
                {
                    Toast.MakeText(Application.Context, $"We have Disabled the TTS functionality.", ToastLength.Long).Show();
                    ttsEnabled = false;
                }

            }
#endif
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        private async void ExecuteChatCompletion(object sender, EventArgs e)
        {
            try
            {
                Window.AddFlags(WindowManagerFlags.KeepScreenOn);
                EditText inputbox = FindViewById<EditText>(Resource.Id.message_input);
                TextView logBox = FindViewById<TextView>(Resource.Id.logView);
                string aiResponse = string.Empty;
                string inputText = string.Empty;
                logBox.Text = string.Empty;
                inputText = inputbox.Text.Trim() ?? string.Empty;
                MyOpenAIAPI.Message inputMsg = new MyOpenAIAPI.Message
                {
                    Role = "user",
                    Content = inputText
                };
                chatCompletionMessages.Add(inputMsg);
                string waitMsg = $"Please Wait... \nSending:\n" + inputText;
                logBox.Text = waitMsg;
                // Send text to model and retrieve response
                Toast.MakeText(Application.Context, $"Generating...", ToastLength.Short).Show();
                if (inputText != string.Empty)
                {
                    MyOpenAIAPI.Message response = await MyOpenAIAPI.GenerateChatCompletionAsync(apiKey, chatCompletionMessages);
                    aiResponse = response.Content;
                    chatCompletionMessages.Add(response);
                    if (chatCompletionMessages.Count > 6)
                    {
                        chatCompletionMessages.RemoveRange(1,3);
                    }
                }
                else
                {
                    await ShowMessage("Warning", "You must enter text or else this will not work.");
                }
                foreach (var message in chatCompletionMessages)
                {
                    aiResponse += message.Role + ": " + message.Content + "\n";
                }
                logBox.Text = aiResponse;                
                inputbox.Text = string.Empty;
                Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("400"))
                {
                    await ShowMessage("Error!", "HTTP Error: 400\nThe Server will respond with this if the prompt contains prohibited words or Characters. \nPossibly the prompt is too long?\nTry rewording or shortening your prompt and try again.");
                }
                else if (ex.Message.Contains("404"))
                {
                    await ShowMessage("Error!", "HTTP Error: 404\nThe Server will respond with this if the server refuses to process. \nPossibly the API Key has insufficient permissions? \nTry changing model or API Key and try again.");
                }
                else
                {
                    await ShowMessage("Error!", $"Error: {ex.Message}");
                }
            }
        }

        private void RecordAudioBtn_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException(); // record audio and set to selectedAudioFile and save name to selectedAudioFileName
        }

        private void SettingsBtn_Click(object sender, System.EventArgs e)
        {
            backPressedTimes = 0;
            settingsShown = true;
            Android.App.AlertDialog.Builder builder = new Android.App.AlertDialog.Builder(this, Android.Resource.Style.ThemeDeviceDefaultDialogAlert);
            builder.SetTitle("Settings");
            builder.SetItems(settingsList.ToArray(), async (s, args) =>
            {
                switch (settingsList[args.Which])
                {
                    case "Credits/Licenses": // Credits
                        settingsShown = false;
                        OnLicensesBtnClick(this, e);
                        break;
                    case "Completion Generation": // CompletionGeneration
                        settingsShown = false;
                        OnCompletionsBtnClick(this, e);
                        break;
                    case "Chat Completion Generation": // ChatCompletionGeneration
                        settingsShown = false;
                        OnChatCompletionsBtnClick(this, e);
                        break;
                    case "Audio Transcription": // ResetApiKey
                        settingsShown = false;
                        OnAudioTranscribeBtnClick(this, e);
                        break;
                    case "Image Generation": // ImageGeneration
                        settingsShown = false;
                        OnImageGenBtnClick(this, e);
                        break;
                    case "Image Edits": // ImageEditGeneration
                        settingsShown = false;
                        OnImageEditsBtnClick(this, e);
                        break;
                    case "Image Variation": // ImageVariationGeneration
                        settingsShown = false;
                        OnImageVariationBtnClick(this, e);
                        break;
                    case "Reset Stored Lists": // ResetLists
                        settingsShown = false;
                        OnResetBotsListBtnClick(this, e);
                        break;
                    case "Reset ApiKey": // ResetApiKey
                        settingsShown = false;
                        OnResetAPIBtnClick(this, e);
                        break;
                    case "Show All Models":
                        List<string> botNames = await MyOpenAIAPI.GetBotNameList(apiKey);
                        botNames.Sort();
                        string modelsString = string.Empty;
                        object _lockModelsString = new object();
                        if (botNames != null)
                        {
                            Parallel.ForEach(botNames, curName =>
                            {
                                if (botNames.Contains(curName.ToLower()))
                                {
                                    lock (_lockModelsString)
                                    {
                                        modelsString += $"{curName.ToLower()}\n-----------------------------\n";
                                    }
                                }
                            });
                            await ShowMessage("Available Models", $"Models:\n-----------------------------\n{modelsString}");
                        }
                        break;
                }
            });
            builder.Create().Show();
        }

        private async void OnCompletionsBtnClick(object sender, EventArgs e)
        {
            try
            {
                InitCompletions();
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        private async void OnChatCompletionsBtnClick(object sender, EventArgs e)
        {
            try
            {
                InitChatCompletions();
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        private async void OnAudioTranscribeBtnClick(object sender, EventArgs e)
        {
            try
            {
                InitAudioTranscribe();
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        private async void OnImageGenBtnClick(object sender, EventArgs e)
        {
            try
            {
                InitImageGen();
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        private async void OnImageEditsBtnClick(object sender, EventArgs e)
        {
            try
            {
                InitEditImage();
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        private async void OnTranscribeBtnClick(object sender, EventArgs e)
        {
            try
            {

#if DEBUG
                ((ViewGroup)this.FindViewById(Android.Resource.Id.Content)).RemoveAllViews();
                this.SetContentView(Resource.Layout.AudioTranscription);
                InitTranscribe();
#else
                await ShowMessage("Not Implemented", "We are sorry, we have not yet implemented that specific feature");
#endif

            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        private async void OnImageVariationBtnClick(object sender, EventArgs e)
        {
            try
            {
                InitImageVariations();
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        private void OnLicensesBtnClick(object sender, EventArgs e)
        {
            InitLicense();
        }

        private async void OnResetBotsListBtnClick(object sender, EventArgs e)
        {
            try
            {
                if (await ShowYesNoMessage("Resetting Models List", "We will reset the models list and close."))
                {
                    Toast.MakeText(this, "Models list has been reset.", ToastLength.Long).Show();
                    botNameList = new List<string>();
                    await WriteToSharedPreferences(botListPreference, botNameList);
                    botNameListImageGen = new List<string>();
                    await WriteToSharedPreferences(imageBotListPreference, botNameListImageGen);
                    botNameListAudio = new List<string>();
                    await WriteToSharedPreferences(audioBotListPreference, botNameListAudio);
                    selectedModel = string.Empty;
                    await WriteToSharedPreferences(selectedModelPreference, selectedModel);
                    Java.Lang.JavaSystem.Exit(0);
                    Process.KillProcess(Process.MyPid());
                }
                else
                {
                    Toast.MakeText(this, "Canceled", ToastLength.Long).Show();
                }
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        private async void OnResetAPIBtnClick(object sender, EventArgs e)
        {
            try
            {
                if (await ShowYesNoMessage("Resetting API Key", "We will reset the API key and close."))
                {
                    Toast.MakeText(this, "API key has been reset.", ToastLength.Long).Show();
                    apiKey = string.Empty;
                    await WriteToSharedPreferences(apiKeyPreference, apiKey);
                    botNameList = new List<string>();
                    await WriteToSharedPreferences(botListPreference, botNameList);
                    botNameListImageGen = new List<string>();
                    await WriteToSharedPreferences(imageBotListPreference, botNameListImageGen);
                    botNameListAudio = new List<string>();
                    await WriteToSharedPreferences(audioBotListPreference, botNameListAudio);
                    selectedModel = string.Empty;
                    await WriteToSharedPreferences(selectedModelPreference, selectedModel);
                    Java.Lang.JavaSystem.Exit(0);
                    Process.KillProcess(Process.MyPid());
                }
                else
                {
                    Toast.MakeText(this, "Canceled", ToastLength.Long).Show();
                }
            }
            catch (Exception ex)
            {
                await ShowMessage("Failed", ex.Message);
            }
        }

        private async void ExecuteImageGen(object sender, EventArgs e)
        {
            try
            {
                Window.AddFlags(WindowManagerFlags.KeepScreenOn);
                EditText inputbox = FindViewById<EditText>(Resource.Id.message_input);
                ImageView imageView = FindViewById<ImageView>(Resource.Id.imageGenOutput);
                using HttpClient client = new HttpClient();
                string inputText = inputbox.Text ?? string.Empty;
                inputbox.Text = string.Empty;
                // Send text to model and retrieve response
                Toast.MakeText(Application.Context, $"Generating...", ToastLength.Short).Show();
                string imageUrl = await MyOpenAIAPI.GenerateImageAsyncHttp(apiKey, inputText);
                SetImageFromUrl(imageView, imageUrl);
                Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
            }
            catch (Exception ex)
            {

                if (ex.Message.Contains("400"))
                {
                    await ShowMessage("Error!", "HTTP Error: 400 Occurred.\nThe Server will respond with this if the prompt is too long. Try to rephrase prompt and try again.");
                }
                else
                {
                    await ShowMessage("Error!", $@"Error: {ex.Message}");
                }
            }
        }

        private async void ExecuteImageVariations(object sender, EventArgs e)
        {
            byte[] image = selectedImageFile;
            try
            {
                Window.AddFlags(WindowManagerFlags.KeepScreenOn);
                if (image != null)
                {
                    byte[] imageResized = await MyImageUtils.CropAndResizeImage(image);
                    if (imageResized != null)
                    {
                        ImageView imageVarOutput = FindViewById<ImageView>(Resource.Id.imageVarOutput);
                        Toast.MakeText(Application.Context, $"Generating...", ToastLength.Short).Show();
                        string imageUrl = await MyOpenAIAPI.ImageVariationsAsyncHttp(apiKey, imageResized);
                        SetImageFromUrl(imageVarOutput, imageUrl);
                    }
                }
                else
                {
                    FileResult imageInput = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Select an image to continue" });
                    selectedImageFile = await System.IO.File.ReadAllBytesAsync(imageInput.FullPath);
                    ExecuteImageVariations(this, e);
                }
                Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("415"))
                {
                    await ShowMessage("Error!", "Error: 415 Unsupported Media Type. \nThe image MUST be valid format under 1024x1024, Square and be under 4MB.");
                }
                else if (ex.Message.Contains("400"))
                {

                    await ShowMessage("Error!", "Error: The server had an issue with this image.\nDoes it have transparent area to generate in?\nThe server could be down too. Try a different PNG image.");
                }
                else
                {
                    await ShowMessage("Error!", $"Error: {ex.Message}");
                }
            }
        }

        private async void ExecuteEditImg(object sender, EventArgs e)
        {

#if DEBUG
            try
            {
                Window.AddFlags(WindowManagerFlags.KeepScreenOn);
                byte[] image = selectedImageFile;
                byte[] mask = selectedMaskFile ?? MyImageUtils.CreateBlankPNG(1024, 1024);
                string imageUrl = string.Empty;
                ImageView imageVarOutput = FindViewById<ImageView>(Resource.Id.imageVarOutput);
                EditText imageEditInput = FindViewById<EditText>(Resource.Id.imageEditInput);
                string prompt = string.Empty;
                if (imageEditInput.Text != string.Empty)
                {
                    prompt = imageEditInput.Text ?? null;
                }
                if (image != null)
                {
                    byte[] imageResized = await MyImageUtils.CropAndResizeImage(image);
                    if (imageResized != null)
                    {
                        Toast.MakeText(Application.Context, $"Starting...", ToastLength.Short).Show();
                        imageUrl = await MyOpenAIAPI.EditImageAsyncHttp(apiKey, prompt, imageResized, mask);
                        SetImageFromUrl(imageVarOutput, imageUrl);
                        //Bitmap imageBitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                        imageEditInput.Text = string.Empty;
                    }
                }
                else
                {
                    SelectImgBtn_Click(sender, e);
                    ExecuteEditImg(sender, e);
                }
            }
#else
            try
            {
                Window.AddFlags(WindowManagerFlags.KeepScreenOn);
                byte[] image = selectedImageFile;
                byte[] mask = selectedMaskFile ?? null;
                string imageUrl = string.Empty;
                ImageView imageVarOutput = FindViewById<ImageView>(Resource.Id.imageVarOutput);
                EditText imageEditInput = FindViewById<EditText>(Resource.Id.imageEditInput);
                string prompt = string.Empty;
                if (imageEditInput.Text != string.Empty)
                {
                    prompt = imageEditInput.Text ?? null;
                }
                if (image != null)
                {
                    byte[] imageResized = await MyImageUtils.CropAndResizeImage(image);
                    if (imageResized != null)
                    {
                        Toast.MakeText(Application.Context, $"Generating...", ToastLength.Short).Show();
                        imageUrl = await MyOpenAIAPI.EditImageAsyncHttp(apiKey, prompt, imageResized, mask);
                        SetImageFromUrl(imageVarOutput, imageUrl);
                        imageEditInput.Text = string.Empty;
                    }
                }
                else
                {
                    SelectImgBtn_Click(sender, e);
                    ExecuteEditImg(sender, e);
                }


                Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
            }
#endif
            catch (Exception ex)
            {
                if (ex.Message.Contains("415"))
                {
                    await ShowMessage("Error!", "Error: 415 Unsupported Media Type. \nThe image MUST be valid format under 1024x1024, Square and be under 4MB.");
                }
                else if (ex.Message.Contains("400") || ex.Message.Contains("BAD REQUEST"))
                {
                    await ShowMessage("Error!", "Error: The server had an issue with this image.\nIt has issues detecting transparency in some images.\nPlease select a mask and try again.");
                }
                else
                {
                    await ShowMessage("Error!", $"Error: {ex.Message}");
                }
            }
        }

        private async void ExecuteTranscribeAudio(object sender, EventArgs e)
        {
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            TextView transcribedTxtView = FindViewById<TextView>(Resource.Id.transcribedTxtView);
            Toast.MakeText(Application.Context, $"Generating...", ToastLength.Short).Show();
            if (selectedAudioFile != null)
            {
                string TranscribedText;
                bool Translate = await ShowYesNoMessage("Translate It?", "Do you want to translate this audio to english?");
                if (Translate)
                {
                    TranscribedText = await MyOpenAIAPI.TranslateAudioAsyncHttp(apiKey, selectedAudioFile, selectedAudioFileName);
                }
                else
                {
                    TranscribedText = await MyOpenAIAPI.TranscribeAudioAsyncHttp(apiKey, selectedAudioFile, selectedAudioFileName);
                }
                if (TranscribedText != "")
                {
                    transcribedTxtView.Text = TranscribedText;
                }
            }
            else
            {
                await ShowMessage("No File Selected", "You need to select a file first.");
            }
            Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
        }

        #endregion
        /* -------------- End Buttons -------------- */
        /* -------------- Preferences -------------- */
        #region Preferences
        public static Task WriteToPreferences(string name, object obj)
        {
            if (obj == null)
            {
                //throw new ArgumentNullException("obj can't be null");
                return Task.CompletedTask;
            }
            if (string.IsNullOrEmpty(name))
            {
                //throw new ArgumentNullException("filePath can't be null or empty");
                return Task.CompletedTask;
            }

            try
            {
                string json = JsonConvert.SerializeObject(obj); // here
                //string encryptedJson = MyEncryptionUtils.RSAEncrypt(json, MyEncryptionUtils.RSAkey);
                Preferences.Set(name, json);
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        public static Task WriteToSharedPreferences(string name, object obj)
        {
            ISharedPreferences prefs = Application.Context.GetSharedPreferences(preferenceName, FileCreationMode.Private); // This makes it NOT shared.
            ISharedPreferencesEditor prefEditor = prefs.Edit();
            if (obj == null)
            {
                //throw new ArgumentNullException("obj can't be null");
                return Task.CompletedTask;
            }
            if (string.IsNullOrEmpty(name))
            {
                //throw new ArgumentNullException("filePath can't be null or empty");
                return Task.CompletedTask;
            }
            try
            {
                string json = JsonConvert.SerializeObject(obj); // here
                //string encryptedJson = MyEncryptionUtils.RSAEncrypt(json, MyEncryptionUtils.RSAkey);
                prefEditor.PutString(name, json);
                prefEditor.Commit();
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        public static Task<T> ReadFromPreferences<T>(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                //throw new ArgumentNullException("filePath can't be null or empty");
                return default;
            }
            try
            {
                string json = Preferences.Get(name, string.Empty) ?? "";
                //string decryptedJson = MyEncryptionUtils.RSADecrypt(json, encryptionKey);
                return Task.FromResult(JsonConvert.DeserializeObject<T>(json) ?? default);
            }
            catch (Exception e)
            {
                return (Task<T>)Task.FromException(e);
            }
        }

        public static Task<T> ReadFromSharedPreferences<T>(string name)
        {
            ISharedPreferences prefs = Application.Context.GetSharedPreferences(preferenceName, FileCreationMode.Private); // This makes it NOT shared.
            if (string.IsNullOrEmpty(name))
            {
                //throw new ArgumentNullException("filePath can't be null or empty");
                return default;
            }
            try
            {
                string json = prefs.GetString(name, string.Empty) ?? "";
                //string decryptedJson = MyEncryptionUtils.RSADecrypt(json, encryptionKey);
                return Task.FromResult(JsonConvert.DeserializeObject<T>(json) ?? default);
            }
            catch (Exception e)
            {
                return (Task<T>)Task.FromException(e);
            }
        }

        public double CheckAgeOfPreference(string preference, ISharedPreferences preferences)
        {
            if (long.TryParse(preferences.GetString(preference, "0"), out long lastModifiedTimestamp))
            {
                if (lastModifiedTimestamp == 0)
                {
                    return double.NaN;
                }
                else
                {
                    TimeSpan timeSpan = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(lastModifiedTimestamp);
                    return timeSpan.TotalDays;
                }
            }
            else
            {
                return double.NaN;
            }
        }

        private byte[] ConvertImageToPngFormat(byte[] imageBytes)
        {
            using (MemoryStream inputStream = new MemoryStream(imageBytes))
            {
                using (Bitmap bitmap = BitmapFactory.DecodeStream(inputStream))
                {
                    using (MemoryStream outputStream = new MemoryStream())
                    {
                        bitmap.Compress(Bitmap.CompressFormat.Png, 0, outputStream);
                        return outputStream.ToArray();
                    }
                }
            }
        }
        #endregion
        /* -------------- End Preferences -------------- */
        #region Suppressions Enables
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CS0618 // Type or member is 
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore IDE0079 // Remove unnecessary suppression
        #endregion

        #region License
        private static readonly string LicenseTxt = "The software that you are using includes several open-source libraries which are licensed under various open-source licenses.\n" +
                    "The libraries and their corresponding licenses are:\n\n AndroidX.AppCompat.App licensed under the Apache 2.0 license.\n\n Newtonsoft.Json and AndroidX.AppCompat.App " +
                    "licensed under the MIT license.\n\n OpenAI-API-dotnet library is in the public domain.\n\nPlease refer to the original license text for each library for the specific" +
                    " terms and conditions.\nYou must comply with the terms and conditions of these licenses, including any attribution or notice requirements.\nFailure to comply with the" +
                    " terms of these licenses could result in legal action being taken against you.\n\n------------------------------------------------------------------------\n" +
                    "** Apache License **\nCopyright [respective year] [respective copyright holder]\n\nLicensed under the Apache License, Version 2.0 (the \"License\");\nyou may not use this file except in" +
                    " compliance with the License.\nYou may obtain a copy of the License at\n\nhttp://www.apache.org/licenses/LICENSE-2.0\n\nUnless required by applicable law or agreed" +
                    " to in writing, software\ndistributed under the License is distributed on an \"AS IS\" BASIS,\nWITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.\n" +
                    "See the License for the specific language governing permissions and\nlimitations under the License.\n------------------------------------------------------------------------\n" +
                    "** Mit License **\nCopyright (c) [respective year] [respective copyright holder]\n\nPermission is hereby granted, free of charge, to any person obtaining a copy\nof this software and associated" +
                    " documentation files (the \"Software\"), to deal\nin the Software without restriction, including without limitation the rights\nto use, copy, modify, merge, publish, distribute, " +
                    "sublicense, and/or sell\ncopies of the Software, and to permit persons to whom the Software is\nfurnished to do so, subject to the following conditions:\n\nThe above copyright" +
                    " notice and this permission notice shall be included in all\ncopies or substantial portions of the Software.\n\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND," +
                    " EXPRESS OR\nIMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\nFITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE\nAUTHORS OR COPYRIGHT" +
                    " HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER\nLIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,\nOUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE" +
                    " OR OTHER DEALINGS IN THE\nSOFTWARE.\n\n------------------------------------------------------------------------\n";

        private static readonly string IntroMsg = "************ Welcome to ChatBotZ ************\nHere is some info to let you know a bit more about the models\n\n" +
            "ada: OpenAI's language model for generating human-like text. It was trained on a diverse range of internet text and has a generative capacity of several hundreds of words.\n\n" +
            "babbage: OpenAI's language model for generating computer code. It was trained on a large corpus of code from GitHub and can generate code snippets that are syntactically and semantically correct.\n\n" +
            "curie: OpenAI's language model that specializes in answering questions. It was fine-tuned on a large dataset of question-answer pairs and is capable of providing accurate and relevant answers to a wide range of questions.\n\n" +
            "davinci: OpenAI's most advanced language model to date, capable of understanding and generating human-like text across a broad range of topics and styles. It was trained on a diverse range of text from the internet and has a generative capacity of several thousand words.\n\n" +
            "ada-code-search-code: A language model designed specifically for code search in 'ada' architecture. \n\n" +
            "ada-code-search-text: A language model for code search in natural language in the 'ada' architecture. \n\n" +
            "ada-similarity: A language model designed for text similarity tasks, using the 'ada' architecture. \n\n" +
            "babbage-code-search-code: A language model designed specifically for code search. \n\n" +
            "babbage-code-search-text: A language model for code search in natural language. \n\n" +
            "babbage-similarity: A language model designed for text similarity tasks, using the 'babbage' architecture. \n\n" +
            "code-cushman-001: A language model for code-related tasks. \n\n" +
            "code-davinci-002: V2 of the language model for code-related tasks, using the 'davinci' architecture. \n\n" +
            "code-davinci-edit-001: A language model for code editing tasks. \n\n" +
            "code-search-ada-text-001: A language model for code search using natural language in the 'ada' architecture. \n\n" +
            "code-search-babbage-code-001: A language model for code search in code. \n\n" +
            "code-search-babbage-text-001: A language model for code search using natural language. \n\n" +
            "curie-instruct-beta: A language model for instructional generation. \n\n" +
            "curie-search-query: A language model designed specifically for search queries. \n\n" +
            "davinci-instruct-beta: An improved version of the 'curie-instruct-beta' language model. \n\n" +
            "davinci-search-document: A language model for document search. \n\n" +
            "text-ada-001: A language model for text-based tasks, using the 'ada' architecture. \n\n" +
            "text-curie-001: A language model for text-based tasks, using the 'curie' architecture. \n\n" +
            "text-davinci-001: A language model for text-based tasks, using the 'davinci' architecture. \n\n" +
            "text-davinci-002: V2 of the language model for text-based tasks, using the 'davinci' architecture. \n\n" +
            "text-davinci-003: V3 of the language model for text-based tasks, using the 'davinci' architecture. \n\n" +
            "text-davinci-edit-001: A language model for text editing tasks. \n\n" +
            "text-davinci-insert-002: A language model for inserting text into existing text. \n\n" +
            "text-search-ada-doc-001: A language model for document search in the 'ada' architecture. \n\n" +
            "text-search-ada-query-001: A language model designed for search queries in the 'ada' architecture. \n\n" +
            "An example of code translation looks as follows\n\n************************\n" +
            "********Prompt********\n************************\nTranslate this code from Python to Csharp\n\n" +
            "### Python\n//Python code goes here.\n\n### Csharp\n// Leave this empty.";

        /* App Description
"ChatBotZ - The ultimate AI-powered chatbot experience on android! With cutting-edge OpenAI technology,
this app gives you access to the latest and most advanced text generation models. Unleash the full potential 
of machine learning and AI to complete your sentences, generate paragraphs, and even create entire articles
with just a few taps. Be ahead of the curve and elevate your communication to the next level with ChatBotZ.
Please note that an API key from the OpenAI website is required to access the full capabilities of the app."
*/

        /* License Text:

        The software that you are using includes several open-source libraries which are licensed under various open-source licenses.
        The libraries and their corresponding licenses are:

        AndroidX.AppCompat.App licensed under the Apache 2.0 license.

        Newtonsoft.Json AndroidX.AppCompat.App licensed under the MIT license.

        OpenAI-API-dotnet library is in the public domain.

        Please refer to the original license text for each library for the specific terms and conditions.
        You must comply with the terms and conditions of these licenses, including any attribution or notice requirements.
        Failure to comply with the terms of these licenses could result in legal action being taken against you.

        ------------------------------------------------------------------------
        ** Apache License **
        Copyright [year] [copyright holder]

        Licensed under the Apache License, Version 2.0 (the "License");
        you may not use this file except in compliance with the License.
        You may obtain a copy of the License at

        http://www.apache.org/licenses/LICENSE-2.0

        Unless required by applicable law or agreed to in writing, software
        distributed under the License is distributed on an "AS IS" BASIS,
        WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
        See the License for the specific language governing permissions and
        limitations under the License.
        ------------------------------------------------------------------------
        ** Mit License **
        Copyright (c) [year] [copyright holder]

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        SOFTWARE.

        ------------------------------------------------------------------------

        */
        #endregion
    }
}




