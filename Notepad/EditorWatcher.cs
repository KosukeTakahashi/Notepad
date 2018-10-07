using System;
using Android.Text;
using Java.Lang;

namespace Notepad
{
    class EditorWatcher : Java.Lang.Object, ITextWatcher
    {
        public event Action<IEditable> AfterChanged = null;
        public event Action<ICharSequence, int, int, int> BeforeChanged = null;
        public event Action<ICharSequence, int, int, int> OnChanged = null;

        public void AfterTextChanged(IEditable s) => AfterChanged?.Invoke(s);

        public void BeforeTextChanged(ICharSequence s, int start, int count, int after) => BeforeChanged?.Invoke(s, start, count, after);

        public void OnTextChanged(ICharSequence s, int start, int before, int count) => OnChanged?.Invoke(s, start, before, count);
    }
}