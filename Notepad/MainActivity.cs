using System;
using System.IO;
using System.Text;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
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
        private Encoding        FileEncoding = Encoding.UTF8;
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
            ReadFile();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == REQUEST_CODE_OPEN_FILE && resultCode == Result.Ok)
            {
                if (data != null)
                {
                    FileUri = data.Data;
                    FileEncoding = Encoding.UTF8;
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
                    var writer = new StreamWriter(stream, FileEncoding);

                    writer.Write(editor.Text);
                    writer.Close();

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
                    FileEncoding = Encoding.UTF8;
                    ReadFile();
                    return true;

                case Resource.Id.encoding_ascii:
                    FileEncoding = Encoding.ASCII;
                    ReadFile();
                    return true;

                case Resource.Id.encoding_sjis:
                    FileEncoding = Encoding.GetEncoding(932); // Shift-JIS
                    ReadFile();
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
                //toolbar.Subtitle += "*";
                toolbar.Title += "・";
            }
        }

        private void ReadFile()
        {
            if (FileUri != null)
            {
                var editor = FindViewById<EditText>(Resource.Id.editor);
                var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
                var content = "";

                if (FileUri.Scheme == "file")
                {
                    var reader = new StreamReader(FileUri.Path, FileEncoding);

                    content = reader.ReadToEnd();
                    reader.Close();
                }
                else
                {
                    var stream = ContentResolver.OpenInputStream(FileUri);
                    var reader = new StreamReader(stream, FileEncoding);

                    content = reader.ReadToEnd();
                    reader.Close();
                }

                editor.Text = content;
                toolbar.Title = "Notepad";
                toolbar.Subtitle = FileUri.Path;
                IsModified = false;
            }
        }
    }
}
