using System;
using System.IO;
using System.Text;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace Notepad
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    [IntentFilter(new[] { Intent.ActionView, Intent.ActionEdit },
                   Categories = new[] { Intent.CategoryDefault },
                   DataMimeType = "*/*")]
    public class MainActivity : AppCompatActivity
    {
        private static readonly int REQUEST_CODE_OPEN_FILE  = 42;
        private static readonly int REQUEST_CODE_WRITE_FILE = 43;

        private Android.Net.Uri FileUri      = null;
        private Utils.Encoding  FileEncoding = Utils.Encoding.UTF8;
        private bool            IsModified   = false;
        private int             CurrentLines = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            var counter = FindViewById<TextView>(Resource.Id.counter);
            var editor = FindViewById<EditText>(Resource.Id.editor);
            var watcher = new EditorWatcher();
            var builder = new Typeface.Builder(Assets, "GenShinMonoM.ttf");
            var typeface = builder.Build();

            editor.Typeface = typeface;
            counter.Typeface = typeface;
            counter.Text = Utils.GenCounterString(editor.Text);

            watcher.OnChanged += OnEditorTextChanged;
            editor.AddTextChangedListener(watcher);
        }

        protected override void OnResume()
        {
            base.OnResume();
            
            if (Intent.Action != Intent.ActionMain)
                FileUri = Intent.Data;

            if (FileUri != null)
                SetContent(ReadFile());
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == REQUEST_CODE_OPEN_FILE && resultCode == Result.Ok)
            {
                if (data != null)
                {
                    FileUri = data.Data;
                    FileEncoding = Utils.Encoding.UTF8;
                    IsModified = false;
                }
            }
            else if (requestCode == REQUEST_CODE_WRITE_FILE && resultCode == Result.Ok)
            {
                if (data != null)
                {
                    var editor = FindViewById<EditText>(Resource.Id.editor);
                    var uri = data.Data;
                    var stream = ContentResolver.OpenOutputStream(uri);
                    //var writer = new StreamWriter(stream, FileEncoding);
                    StreamWriter writer = null;

                    switch (FileEncoding)
                    {
                        case Utils.Encoding.UTF8:
                            writer = new StreamWriter(stream, Encoding.UTF8);
                            writer.Write(editor.Text);
                            break;

                        case Utils.Encoding.ASCII:
                            writer = new StreamWriter(stream, Encoding.ASCII);
                            writer.Write(editor.Text);
                            break;

                        case Utils.Encoding.SJIS:
                            writer = new StreamWriter(stream, Encoding.GetEncoding(932));
                            writer.Write(editor.Text);
                            break;

                        case Utils.Encoding.HEX:
                            var content = editor.Text;
                            content.Replace("\n", "");

                            var strBytes = content.Split(' ');
                            var bytes = new byte[strBytes.Length];

                            for (int i = 0; i < strBytes.Length - 1; i++)
                            {
                                try
                                {
                                    bytes[i] = byte.Parse(strBytes[i], System.Globalization.NumberStyles.HexNumber);
                                }
                                catch (FormatException)
                                {
                                    Snackbar.Make(editor, string.Format("Cannot save invalid hex data\nbyte[{0}]: {1}", i, bytes[i]), Snackbar.LengthShort).Show();
                                }
                            }

                            var bw = new BinaryWriter(stream, Encoding.UTF8);
                            bw.Write(bytes);

                            break;
                    }

                    writer?.Close();

                    FileUri = uri;
                }
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            if (Intent.Action == Intent.ActionMain)
            {
                MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            }
            else
            {
                MenuInflater.Inflate(Resource.Menu.menu_no_open_file, menu);
            }

            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_save:
                    var cIntent = new Intent(Intent.ActionCreateDocument);

                    cIntent.AddCategory(Intent.CategoryOpenable);
                    cIntent.SetType("*/*");
                    StartActivityForResult(cIntent, REQUEST_CODE_WRITE_FILE);
                    return true;

                case Resource.Id.action_open_file:
                    var oIntent = new Intent(Intent.ActionOpenDocument);

                    oIntent.AddCategory(Intent.CategoryOpenable);
                    oIntent.SetType("*/*");
                    StartActivityForResult(oIntent, REQUEST_CODE_OPEN_FILE);
                    return true;

                case Resource.Id.encoding_utf8:
                    FileEncoding = Utils.Encoding.UTF8;
                    
                    if (FileUri != null)
                        SetContent(ReadFile());

                    return true;

                case Resource.Id.encoding_ascii:
                    FileEncoding = Utils.Encoding.ASCII;

                    if (FileUri != null)
                        SetContent(ReadFile());

                    return true;

                case Resource.Id.encoding_sjis:
                    FileEncoding = Utils.Encoding.SJIS;

                    if (FileUri != null)
                        SetContent(ReadFile());

                    return true;

                case Resource.Id.encoding_hex:
                    FileEncoding = Utils.Encoding.HEX;

                    if (FileUri != null)
                        SetContent(ReadFile());

                    return true;

                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        private void OnEditorTextChanged(ICharSequence s, int start, int before, int count)
        {
            if (CurrentLines != Utils.CountLines(s.ToString()))
            {
                var counter = FindViewById<TextView>(Resource.Id.counter);
                var counterStr = Utils.GenCounterString(s.ToString());
                counter.Text = counterStr;
                CurrentLines = Utils.CountLines(s.ToString());
            }

            if (!IsModified)
            {
                IsModified = true;

                var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
                toolbar.Title += "・";
            }
        }

        private string ReadFile()
        {
            var content = "";

            if (FileUri.Scheme == "file")
            {
                StreamReader reader = null;

                switch (FileEncoding)
                {
                    case Utils.Encoding.UTF8:
                        reader = new StreamReader(FileUri.Path, Encoding.UTF8);
                        content = reader.ReadToEnd();
                        break;

                    case Utils.Encoding.ASCII:
                        reader = new StreamReader(FileUri.Path, Encoding.ASCII);
                        content = reader.ReadToEnd();
                        break;

                    case Utils.Encoding.SJIS:
                        reader = new StreamReader(FileUri.Path, Encoding.GetEncoding(932)); //Shift-JIS
                        content = reader.ReadToEnd();
                        break;

                    case Utils.Encoding.HEX:
                        var stream = new FileStream(FileUri.Path, FileMode.Open);
                        var ms = new MemoryStream();

                        stream.CopyTo(ms);

                        var bytes = ms.ToArray();
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            if (0 < i && i % 5 == 0)
                                content += "\n";

                            content += string.Format("{0} ", bytes[i].ToString("X2"));
                        }

                        ms.Close();
                        stream.Close();

                        break;
                }
                
                reader?.Close();
            }
            else
            {
                var stream = ContentResolver.OpenInputStream(FileUri);
                StreamReader reader = null;

                switch (FileEncoding)
                {
                    case Utils.Encoding.UTF8:
                        reader = new StreamReader(stream, Encoding.UTF8);
                        content = reader.ReadToEnd();
                        break;

                    case Utils.Encoding.ASCII:
                        reader = new StreamReader(stream, Encoding.ASCII);
                        content = reader.ReadToEnd();
                        break;

                    case Utils.Encoding.SJIS:
                        reader = new StreamReader(stream, Encoding.GetEncoding(932));
                        content = reader.ReadToEnd();
                        break;

                    case Utils.Encoding.HEX:
                        var ms = new MemoryStream();
                        stream.CopyTo(ms);

                        var bytes = ms.ToArray();
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            if (0 < i && i % 5 == 0)
                                content += "\n";

                            content += string.Format("{0} ", bytes[i].ToString("X2"));
                        }

                        ms.Close();

                        break;
                }
                
                reader?.Close();
                stream.Close();
            }

            return content;
        }

        private void SetContent(string content)
        {
            var editor = FindViewById<EditText>(Resource.Id.editor);
            var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            var appName = Resources.GetString(Resource.String.app_name);

            editor.Text = content;
            toolbar.Title = appName;
            toolbar.Subtitle = FileUri.Path;
        }
    }
}
