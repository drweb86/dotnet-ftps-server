using System.Runtime.InteropServices;
using System.Text;

namespace FtpsServerConsole;

// Questions use the same shape as Copilot CLI and other modern prompts:
// a cyan "?", a dim default, and a cyan "❯" on the highlighted choice.
internal static class CliPrompt
{
    private const string Reset = "\u001b[0m";
    private const string Dim = "\u001b[38;5;245m";
    private const string Cyan = "\u001b[36m";
    private const string Green = "\u001b[32m";
    private const string Red = "\u001b[31m";
    private const string Bold = "\u001b[1m";
    private const string Inverse = "\u001b[7m";

    private static readonly bool Fancy = DetectFancy();

    internal static void Begin()
    {
        if (Fancy)
            Console.Write("\u001b[?25l");
    }

    internal static void End()
    {
        if (Fancy)
            Console.Write("\u001b[?25h" + Reset);
    }

    internal static void Intro(string title, string? hint)
    {
        Console.WriteLine();
        if (!Fancy)
        {
            Console.WriteLine(title);
            if (!string.IsNullOrEmpty(hint))
                Console.WriteLine(hint);
            return;
        }

        Console.WriteLine($"{Cyan}┌{Reset} {Bold}{title}{Reset}");
        if (!string.IsNullOrEmpty(hint))
            Console.WriteLine($"{Dim}│{Reset} {Dim}{hint}{Reset}");
    }

    internal static void Section(string title, string? hint)
    {
        Console.WriteLine();
        if (!Fancy)
        {
            Console.WriteLine(title);
            if (!string.IsNullOrEmpty(hint))
                Console.WriteLine(hint);
            return;
        }

        Console.WriteLine($"{Cyan}●{Reset} {Bold}{title}{Reset}");
        if (!string.IsNullOrEmpty(hint))
            Console.WriteLine($"{Dim}│{Reset} {Dim}{hint}{Reset}");
    }

    internal static void Success(string message) => WriteStatus(Green, "✔", message);

    internal static void Error(string message) => WriteStatus(Red, "✖", message);

    internal static void Note(string message)
    {
        Console.WriteLine(Fancy ? $"{Dim}{message}{Reset}" : message);
    }

    internal static void Outro(string message)
    {
        if (!Fancy)
        {
            Console.WriteLine(message);
            return;
        }

        Console.WriteLine($"{Green}└{Reset} {message}");
    }

    internal static string AskText(
        string label,
        string? hint,
        string? defaultValue,
        bool allowEmpty,
        bool secret,
        Func<string, string?>? validate)
    {
        if (!Fancy)
            return AskTextPlain(label, hint, defaultValue, allowEmpty, secret, validate);

        WriteQuestion(label);
        var buffer = new StringBuilder();
        var cursor = 0;
        var edited = false;
        string? error = null;

        while (true)
        {
            RenderInput(buffer.ToString(), cursor, edited, defaultValue, hint, secret);
            if (error != null)
                Console.Write($"\n{Red}✖{Reset}  {Red}{Fit(error, Width() - 4)}{Reset}");

            var key = Console.ReadKey(intercept: true);
            if (error != null)
            {
                Console.Write("\r\u001b[2K\u001b[1A");
                error = null;
            }

            if (IsCancel(key))
                throw new OperationCanceledException();

            if (key.Key == ConsoleKey.Enter)
            {
                var value = edited || string.IsNullOrEmpty(defaultValue) ? buffer.ToString() : defaultValue!;
                if (!edited && allowEmpty && string.IsNullOrEmpty(defaultValue))
                    value = "";
                error = validate?.Invoke(value);
                if (error == null)
                {
                    var shown = secret ? Mask(value) : value;
                    if (string.IsNullOrEmpty(shown))
                        shown = "—";
                    FinishBlock(1, Summary(label, shown));
                    return value;
                }

                continue;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                edited = true;
                if (cursor > 0)
                {
                    buffer.Remove(cursor - 1, 1);
                    cursor--;
                }

                continue;
            }

            if (key.Key == ConsoleKey.Delete)
            {
                edited = true;
                if (cursor < buffer.Length)
                    buffer.Remove(cursor, 1);
                continue;
            }

            if (key.Key == ConsoleKey.LeftArrow)
            {
                if (cursor > 0)
                    cursor--;
                continue;
            }

            if (key.Key == ConsoleKey.RightArrow)
            {
                if (cursor < buffer.Length)
                    cursor++;
                continue;
            }

            if (key.Key == ConsoleKey.Home)
            {
                cursor = 0;
                continue;
            }

            if (key.Key == ConsoleKey.End)
            {
                cursor = buffer.Length;
                continue;
            }

            if (key.KeyChar < ' ' || char.IsControl(key.KeyChar) || buffer.Length >= 4096)
                continue;

            edited = true;
            buffer.Insert(cursor, key.KeyChar);
            cursor++;
        }
    }

    internal static T AskSelect<T>(string label, IReadOnlyList<(T Value, string Label)> options, int selectedIndex)
    {
        if (options.Count == 0)
            throw new ArgumentException("A choice list needs at least one option.", nameof(options));

        if (!Fancy)
            return AskSelectPlain(label, options, selectedIndex);

        selectedIndex = Math.Clamp(selectedIndex, 0, options.Count - 1);
        WriteQuestion(label);
        while (true)
        {
            for (var i = 0; i < options.Count; i++)
            {
                var selected = i == selectedIndex;
                var mark = selected ? $"{Cyan}❯{Reset}" : " ";
                var text = selected ? $"{Cyan}{options[i].Label}{Reset}" : $"{Dim}{options[i].Label}{Reset}";
                var end = i == options.Count - 1 ? "" : "\n";
                Console.Write($"\r\u001b[2K{Dim}│{Reset}  {mark} {text}{end}");
            }

            var key = Console.ReadKey(intercept: true);
            if (IsCancel(key))
                throw new OperationCanceledException();

            if (key.Key is ConsoleKey.UpArrow or ConsoleKey.LeftArrow)
            {
                selectedIndex = (selectedIndex - 1 + options.Count) % options.Count;
                Rewind(options.Count);
                continue;
            }

            if (key.Key is ConsoleKey.DownArrow or ConsoleKey.RightArrow)
            {
                selectedIndex = (selectedIndex + 1) % options.Count;
                Rewind(options.Count);
                continue;
            }

            var matchedLetter = TryMatchChoice(key, options, out var matched);
            if (matchedLetter)
                selectedIndex = matched;

            if (key.Key == ConsoleKey.Enter || matchedLetter)
            {
                FinishBlock(options.Count, Summary(label, options[selectedIndex].Label));
                return options[selectedIndex].Value;
            }

            Rewind(options.Count);
        }
    }

    internal static bool AskConfirm(string label, bool defaultYes)
    {
        var yes = Resources.Strings.ConsoleYes;
        var no = Resources.Strings.ConsoleNo;
        var choice = AskSelect(label, [(true, yes), (false, no)], defaultYes ? 0 : 1);
        return choice;
    }

    private static string AskTextPlain(
        string label,
        string? hint,
        string? defaultValue,
        bool allowEmpty,
        bool secret,
        Func<string, string?>? validate)
    {
        while (true)
        {
            if (!string.IsNullOrEmpty(hint))
                Console.WriteLine(hint);
            Console.Write(string.IsNullOrEmpty(defaultValue) ? $"{label}: " : $"{label} [{defaultValue}]: ");
            var entered = secret && !Console.IsInputRedirected ? ReadSecretPlain() : Console.ReadLine() ?? "";
            var edited = entered.Length > 0;
            var value = !edited && !string.IsNullOrEmpty(defaultValue) ? defaultValue : entered;
            if (!edited && allowEmpty && string.IsNullOrEmpty(defaultValue))
                value = "";
            var error = validate?.Invoke(value);
            if (error == null)
                return value;
            Error(error);
        }
    }

    private static T AskSelectPlain<T>(string label, IReadOnlyList<(T Value, string Label)> options, int selectedIndex)
    {
        selectedIndex = Math.Clamp(selectedIndex, 0, options.Count - 1);
        while (true)
        {
            Console.WriteLine(label);
            for (var i = 0; i < options.Count; i++)
                Console.WriteLine($"  {i + 1}. {options[i].Label}{(i == selectedIndex ? " *" : "")}");
            Console.Write("> ");
            var entered = Console.ReadLine()?.Trim() ?? "";
            if (entered.Length == 0)
                return options[selectedIndex].Value;
            if (int.TryParse(entered, out var number) && number >= 1 && number <= options.Count)
                return options[number - 1].Value;

            for (var i = 0; i < options.Count; i++)
            {
                if (options[i].Label.StartsWith(entered, StringComparison.CurrentCultureIgnoreCase))
                    return options[i].Value;
            }
        }
    }

    private static string ReadSecretPlain()
    {
        var buffer = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return buffer.ToString();
            }

            if (IsCancel(key))
                throw new OperationCanceledException();
            if (key.Key == ConsoleKey.Backspace && buffer.Length > 0)
            {
                buffer.Length--;
                continue;
            }

            if (key.KeyChar < ' ' || char.IsControl(key.KeyChar) || buffer.Length >= 4096)
                continue;
            buffer.Append(key.KeyChar);
        }
    }

    private static void WriteQuestion(string label)
    {
        Console.WriteLine(Fancy ? $"{Cyan}?{Reset} {Bold}{label}{Reset}" : label);
    }

    private static void RenderInput(string buffer, int cursor, bool edited, string? defaultValue, string? hint, bool secret)
    {
        var available = Math.Max(8, Width() - 6);
        string body;
        if (!edited && buffer.Length == 0 && !string.IsNullOrEmpty(defaultValue))
            body = RenderGhost(defaultValue, available);
        else if (buffer.Length == 0)
            body = RenderGhost(string.IsNullOrEmpty(hint) ? "" : hint, available);
        else
            body = RenderBuffer(secret ? Mask(buffer) : buffer, cursor, available);

        Console.Write($"\r\u001b[2K{Dim}│{Reset} {body}");
    }

    // The cursor sits on the first character, so a default is not pushed one column right.
    private static string RenderGhost(string text, int available)
    {
        var shown = Fit(text, available);
        if (shown.Length == 0)
            return Inverse + " " + Reset;
        return Inverse + shown[0] + Reset + Dim + shown[1..] + Reset;
    }

    private static string RenderBuffer(string shown, int cursor, int width)
    {
        var start = 0;
        if (shown.Length > width)
        {
            start = Math.Min(cursor, shown.Length - width);
            start = Math.Max(0, start);
        }

        var sliceLength = Math.Min(width, shown.Length - start);
        var slice = shown.Substring(start, sliceLength);
        var local = cursor - start;
        if (local >= slice.Length)
            return slice + Inverse + " " + Reset;

        return slice[..local] + Inverse + slice[local] + Reset + slice[(local + 1)..];
    }

    private static void FinishBlock(int bodyLines, string summary)
    {
        // The cursor sits on the last answer line. Replace the question above it,
        // then delete the answer lines so the next question starts directly underneath.
        Console.Write($"\u001b[{bodyLines}A\r\u001b[2K{summary}\u001b[1E");
        for (var i = 0; i < bodyLines; i++)
            Console.Write("\u001b[1M");
        Console.Write('\r');
    }

    private static string Summary(string label, string value)
    {
        var room = Math.Max(4, Width() - label.Length - 8);
        return $"{Green}✔{Reset}  {label} {Dim}·{Reset} {Fit(value, room)}";
    }

    private static void Rewind(int lines)
    {
        if (lines > 1)
            Console.Write($"\u001b[{lines - 1}A");
    }

    private static bool TryMatchChoice<T>(ConsoleKeyInfo key, IReadOnlyList<(T Value, string Label)> options, out int index)
    {
        index = -1;
        if (key.KeyChar < ' ' || char.IsControl(key.KeyChar))
            return false;

        var found = -1;
        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i].Label;
            if (option.Length == 0 || !char.ToUpperInvariant(option[0]).Equals(char.ToUpperInvariant(key.KeyChar)))
                continue;
            if (found >= 0)
                return false;
            found = i;
        }

        if (found < 0)
            return false;
        index = found;
        return true;
    }

    private static void WriteStatus(string color, string mark, string message)
    {
        Console.WriteLine(Fancy ? $"{color}{mark}{Reset}  {message}" : message);
    }

    private static string Mask(string value) => new('\u2022', value.Length);

    private static string Fit(string text, int width)
    {
        if (text.Length <= width)
            return text;
        if (width <= 1)
            return "…";
        return text[..(width - 1)] + "…";
    }

    private static int Width()
    {
        try
        {
            return Console.WindowWidth > 0 ? Console.WindowWidth : 80;
        }
        catch (IOException)
        {
            return 80;
        }
    }

    private static bool IsCancel(ConsoleKeyInfo key) =>
        key.Key == ConsoleKey.Escape || key.KeyChar == '\u0003' ||
        (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.C);

    private static bool DetectFancy()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR")))
            return false;
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
            return false;
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
            return false;
        }

        return !OperatingSystem.IsWindows() || EnableVirtualTerminal();
    }

    private static bool EnableVirtualTerminal()
    {
        var handle = GetStdHandle(-11);
        if (handle == IntPtr.Zero || handle == new IntPtr(-1))
            return false;
        if (!GetConsoleMode(handle, out var mode))
            return false;
        return SetConsoleMode(handle, mode | 0x0004);
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
}
