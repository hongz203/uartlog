using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using UartLogTerminal.Models;

namespace UartLogTerminal;

public static class RichTextLogRenderer
{
    public static readonly DependencyProperty LinesSourceProperty =
        DependencyProperty.RegisterAttached(
            "LinesSource",
            typeof(IEnumerable),
            typeof(RichTextLogRenderer),
            new PropertyMetadata(null, OnLinesSourceChanged));

    private static readonly DependencyProperty CollectionHandlerProperty =
        DependencyProperty.RegisterAttached(
            "CollectionHandler",
            typeof(NotifyCollectionChangedEventHandler),
            typeof(RichTextLogRenderer),
            new PropertyMetadata(null));

    public static IEnumerable? GetLinesSource(DependencyObject obj)
    {
        return (IEnumerable?)obj.GetValue(LinesSourceProperty);
    }

    public static void SetLinesSource(DependencyObject obj, IEnumerable? value)
    {
        obj.SetValue(LinesSourceProperty, value);
    }

    private static void OnLinesSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBox richTextBox)
        {
            return;
        }

        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            NotifyCollectionChangedEventHandler? oldHandler =
                (NotifyCollectionChangedEventHandler?)richTextBox.GetValue(CollectionHandlerProperty);
            if (oldHandler is not null)
            {
                oldCollection.CollectionChanged -= oldHandler;
            }
        }

        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            NotifyCollectionChangedEventHandler handler = (_, _) => RenderLines(richTextBox);
            richTextBox.SetValue(CollectionHandlerProperty, handler);
            newCollection.CollectionChanged += handler;
        }
        else
        {
            richTextBox.ClearValue(CollectionHandlerProperty);
        }

        RenderLines(richTextBox);
    }

    private static void RenderLines(RichTextBox richTextBox)
    {
        FlowDocument document = richTextBox.Document;
        document.Blocks.Clear();

        IEnumerable? source = GetLinesSource(richTextBox);
        if (source is null)
        {
            return;
        }

        foreach (object? item in source)
        {
            if (item is not ColoredLogLine line)
            {
                continue;
            }

            Paragraph paragraph = new()
            {
                Margin = new Thickness(0)
            };

            foreach (LineSegment segment in line.Segments)
            {
                Run run = new(segment.Text)
                {
                    Foreground = segment.ForegroundBrush,
                    Background = segment.BackgroundBrush
                };
                paragraph.Inlines.Add(run);
            }

            document.Blocks.Add(paragraph);
        }
    }
}
