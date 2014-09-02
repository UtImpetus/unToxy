using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using Toxy.Common;

namespace Toxy.Utils
{
    public static class UIHelpers
    {
        public static FlowDocument GetNewFlowDocument()
        {
            Stream doc_stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Toxy.Message.xaml");
            FlowDocument doc = (FlowDocument)XamlReader.Load(doc_stream);
            doc.IsEnabled = true;

            TableRowGroup MessageRows = (TableRowGroup)doc.FindName("CustomCommandsRows");
            TableRow newTableRow = new TableRow();
            var buttonRow = new TableCell();
            var buttonsParagraph = new Paragraph();
            var moreButton = new Button() { Content = "Show more history..." };
            moreButton.Name = Constants.MoreHistoryButtonName;
            moreButton.Visibility = System.Windows.Visibility.Hidden;
            doc.RegisterName(Constants.MoreHistoryButtonName, moreButton);
            moreButton.Click += moreButton_Click;
            buttonsParagraph.Inlines.Add(moreButton);
            buttonRow.Blocks.Add(buttonsParagraph);
            newTableRow.Cells.Add(buttonRow);
            MessageRows.Rows.Insert(0, newTableRow);

            return doc;
        }

        static void moreButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            
        }

        public static void PreloadHistory(SharpTox.Core.Tox tox, System.Windows.Controls.FlowDocumentScrollViewer chatBox, int friendNumber)
        {            
            PreloadHistory(tox, chatBox, tox.GetClientID(friendNumber).GetString());
        }

        public static void PreloadHistory(SharpTox.Core.Tox tox, System.Windows.Controls.FlowDocumentScrollViewer chatBox, string publicKey)
        {
            TableRowGroup MessageRows = (TableRowGroup)chatBox.Document.FindName("MessageRows");
            if (MessageRows != null && MessageRows.Rows.Count > 0) return;
            var task = new Task(() =>
            {
                var historyItems = ChatHistoryHelper.GetRecentHistory(tox, publicKey);
                foreach (var historyItem in historyItems)
                {
                    MainWindow.Current.Dispatcher.Invoke(() =>
                    {
                        chatBox.Document.AddNewMessageRow(tox, new MessageData()
                        {
                            Username = historyItem.From,
                            IsAction = false,
                            IsSelf = false,
                            Message = historyItem.Message,
                            TimeStamp = historyItem.TimeStamp
                        }, false, true);
                    });
                }                
                MainWindow.Current.Dispatcher.Invoke(() =>
                    {
                        if(historyItems.Count>=Constants.CountHistoryItemsPreload)
                        {
                            var button = chatBox.Document.FindName(Constants.MoreHistoryButtonName);
                            if (button != null) ((Button)button).Visibility = System.Windows.Visibility.Visible;
                        }
                        MainWindow.Current.ScrollChatBox(); 
                    });
            }, TaskCreationOptions.AttachedToParent);
            task.Start();
        }
    }
}
