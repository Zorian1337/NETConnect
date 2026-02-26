using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.CustomConsole;

// This will be the class responsible for outputting everything to console
public class ConsoleDebugging
{
    public static CancellationTokenSource Token { get; private set; } = new CancellationTokenSource();

    public enum ConsoleBufferReturnPosition { Default, Original, NewPosition, NewLine }
    public record ConsoleBufferItem(int ConsoleTop, int ConsoleLeft, string Text, ConsoleBufferReturnPosition ReturnPosition = ConsoleBufferReturnPosition.NewLine, Action? Func = null); // Text needs to be a command 

    //public static Dictionary<int, ConsoleBufferItem> ConsoleBuffer = new Dictionary<int, ConsoleBufferItem>();

    
    public static ConcurrentQueue<ConsoleBufferItem> ConsoleBufferQueue = new ConcurrentQueue<ConsoleBufferItem>();
    //public static List<ConsoleBufferItem> PendingConsoleBuffer = new List<ConsoleBufferItem>();


    // Console Queue (either to run a command next or a display a text)


    public static void Print(string Message) => ConsoleBufferQueue.Enqueue(new ConsoleBufferItem(Console.CursorTop, Console.CursorLeft, Message));
    public static void Print(ConsoleBufferItem Item) => ConsoleBufferQueue.Enqueue(Item);

    public static void WriteConsoleLine(int CursorTop, int CursorLeft, string Text, ConsoleBufferReturnPosition ReturnPosition = ConsoleBufferReturnPosition.NewLine, ConsoleColor forecolor = ConsoleColor.Gray, ConsoleColor backcolor = ConsoleColor.Black)
    {
        // Reset area to default
        ClearConsoleLine(CursorTop, CursorLeft, Text.Length);

        ConsoleColor Forecolor = Console.ForegroundColor;
        ConsoleColor Backcolor = Console.BackgroundColor;

        int currentLineCursor = Console.CursorTop;
        
        Console.SetCursorPosition(CursorLeft, CursorTop);

        Console.ForegroundColor = forecolor;
        Console.BackgroundColor = backcolor;

        Console.WriteLine("ran");
        Console.Write(Text);

        Console.ForegroundColor = forecolor;
        Console.BackgroundColor = backcolor;

        // Reset cursor to start of the line
        if (ReturnPosition == ConsoleBufferReturnPosition.Original) Console.SetCursorPosition(0, currentLineCursor);
        else if (ReturnPosition == ConsoleBufferReturnPosition.NewLine) Console.SetCursorPosition(0, Console.CursorTop + 1);
        else if (ReturnPosition == ConsoleBufferReturnPosition.Default) Console.SetCursorPosition(0, 0);
    }

    public static void ClearConsoleLine(int CursorLine, bool IsResetColor = true) => ClearConsoleLine(CursorLine, 0, Console.WindowWidth);
    public static void ClearConsoleLine(int CursorTop, int CursorLeft, int Length, bool IsResetColor = true)
    {
        int currentLineCursor = Console.CursorTop;

        // Move to the line you want to clear
        Console.SetCursorPosition(CursorLeft, CursorTop);

        if (IsResetColor)
        {
            ConsoleColor forecolor = Console.ForegroundColor;
            ConsoleColor backcolor = Console.BackgroundColor;

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;

            // Overwrite the line with spaces
            Console.Write(new string(' ', Length));

            Console.ForegroundColor = forecolor;
            Console.BackgroundColor = backcolor;
        }
        // Overwrite the line with spaces
        else Console.Write(new string(' ', Length));

        // Reset cursor to start of the line
        Console.SetCursorPosition(0, currentLineCursor);
    }


    public static void StartWriter()
    {
        // This is responible for clearing and updating our screen with the information we want to display
        Console.Clear();

        while (true)
        {
            Console.Clear();

            if (ConsoleBufferQueue.TryDequeue(out ConsoleBufferItem Item))
            {
                WriteConsoleLine(Item.ConsoleTop, Item.ConsoleLeft, Item.Text);

                Item.Func?.Invoke();
            }

            //Console.WriteLine("looping console");

            //var Temp = ConsoleBuffer.ToArray();
            //foreach (var item in Temp)
            //{
            //    Console.CursorTop = item.Key;

            //    if (item.Value.ConsoleLeft != 0) { Console.CursorLeft = item.Value.ConsoleLeft; }
            //    Console.WriteLine(item.Value.Text);
            //}

            Thread.Sleep(100);

            



            
        }
    }
}
