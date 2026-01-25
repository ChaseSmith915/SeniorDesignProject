using Android.Text;
using Java.Lang;

public class AllowedCharacterFilter : Java.Lang.Object, IInputFilter
{
    public ICharSequence FilterFormatted(
        ICharSequence source,
        int start,
        int end,
        ISpanned dest,
        int dstart,
        int dend)
    {
        for (int i = start; i < end; i++)
        {
            char c = source.CharAt(i);

            // Allow letters, digits, and spaces
            if (!char.IsLetterOrDigit(c) && c != ' ')
            {
                return new Java.Lang.String(""); // block invalid characters
            }
        }

        return null; // accept input
    }
}
