using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Threading;
using System.Threading.Tasks;

using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using Microsoft.Win32;

using SharpTox.Core;
using SharpTox.Av;

using Toxy.Common;
using Toxy.ToxHelpers;
using Toxy.ViewModels;

using Path = System.IO.Path;
using Brushes = System.Windows.Media.Brushes;

using NAudio.Wave;
using Toxy.Utils;
using System.Windows.Threading;
using System.Windows.Data;
using System.ComponentModel;

namespace Toxy
{
    public partial class MainWindow : MetroWindow
    {
        private Tox tox;
        private ToxAv toxav;
        private ToxCall call;

        private Dictionary<int, FlowDocument> convdic = new Dictionary<int, FlowDocument>();
        private List<FileTransfer> transfers = new List<FileTransfer>();
        private List<string> keysOfConnectedGroupChats = new List<string>();

        private bool resizing = false;
        private bool focusTextbox = false;
        private bool typing = false;
        public bool userPressedSave = false;

        private Accent oldAccent;
        private AppTheme oldAppTheme;

        private string toxDataFilename = "data";

        private DateTime emptyLastOnline = new DateTime(1970, 1, 1, 0, 0, 0);
        System.Windows.Forms.NotifyIcon nIcon = new System.Windows.Forms.NotifyIcon();
        Stream newMessageIconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Toxy;component/Resources/Icons/icon2.ico")).Stream;
        Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Toxy;component/Resources/Icons/icon.ico")).Stream;
        private Icon notifyIcon;
        private Icon newMessageNotifyIcon;

        public static MainWindow Current { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Current = this;

            this.DataContext = new MainWindowViewModel();

            ToxOptions options;
            if (this.ViewModel.Configuraion.ProxyEnabled)
                options = new ToxOptions(this.ViewModel.Configuraion.Ipv6Enabled, this.ViewModel.Configuraion.ProxyAddress, this.ViewModel.Configuraion.ProxyPort);
            else
                options = new ToxOptions(this.ViewModel.Configuraion.Ipv6Enabled, this.ViewModel.Configuraion.UdpDisabled);

            applyConfig();

            ChatHistoryHelper.InitLogDatabase();

            tox = new Tox(options);
            tox.Invoker = Dispatcher.BeginInvoke;
            tox.OnNameChange += tox_OnNameChange;
            tox.OnFriendMessage += tox_OnFriendMessage;
            tox.OnFriendAction += tox_OnFriendAction;
            tox.OnFriendRequest += tox_OnFriendRequest;
            tox.OnUserStatus += tox_OnUserStatus;
            tox.OnStatusMessage += tox_OnStatusMessage;
            tox.OnTypingChange += tox_OnTypingChange;
            tox.OnConnectionStatusChanged += tox_OnConnectionStatusChanged;
            tox.OnFileSendRequest += tox_OnFileSendRequest;
            tox.OnFileData += tox_OnFileData;
            tox.OnFileControl += tox_OnFileControl;
            tox.OnReadReceipt += tox_OnReadReceipt;

            tox.OnGroupInvite += tox_OnGroupInvite;
            tox.OnGroupMessage += tox_OnGroupMessage;
            tox.OnGroupAction += tox_OnGroupAction;
            tox.OnGroupNamelistChange += tox_OnGroupNamelistChange;

            toxav = new ToxAv(tox.GetHandle(), ToxAv.DefaultCodecSettings, 1);
            toxav.Invoker = Dispatcher.BeginInvoke;
            toxav.OnInvite += toxav_OnInvite;
            toxav.OnStart += toxav_OnStart;
            toxav.OnStarting += toxav_OnStart;
            toxav.OnEnd += toxav_OnEnd;
            toxav.OnEnding += toxav_OnEnd;
            toxav.OnPeerTimeout += toxav_OnEnd;
            toxav.OnRequestTimeout += toxav_OnEnd;
            toxav.OnReject += toxav_OnEnd;
            toxav.OnCancel += toxav_OnEnd;
            toxav.OnReceivedAudio += toxav_OnReceivedAudio;
            toxav.OnReceivedVideo += toxav_OnReceivedVideo;
            toxav.OnMediaChange += toxav_OnMediaChange;

            bool bootstrap_success = false;
            foreach (ToxConfigNode node in this.ViewModel.Configuraion.Nodes)
            {
                if (tox.BootstrapFromNode(new ToxNode(node.Address, node.Port, new ToxKey(ToxKeyType.Public, node.ClientId))))
                    bootstrap_success = true;
            }

            if (!bootstrap_success)
                Console.WriteLine("Could not bootstrap from any node!");

            if (File.Exists(toxDataFilename))
            {
                if (!tox.Load(toxDataFilename))
                {
                    MessageBox.Show("Could not load tox data, this program will now exit.", "Error");
                    Close();
                }
            }

            tox.Start();

            if (string.IsNullOrEmpty(tox.GetSelfName()))
                tox.SetName("Toxy User");

            this.ViewModel.MainToxyUser.Name = tox.GetSelfName();
            this.ViewModel.MainToxyUser.StatusMessage = tox.GetSelfStatusMessage();

            InitializeNotifyIcon();

            SetStatus(null);
            InitFriends();

            if (tox.GetFriendlistCount() > 0)
                this.ViewModel.SelectedChatObject = this.ViewModel.ChatCollection.OfType<IFriendObject>().FirstOrDefault();

            if (this.ViewModel.Configuraion.GroupChats != null)
            {
                foreach (var chat in this.ViewModel.Configuraion.GroupChats)
                {
                    AddInactiveGroupToView(chat);
                }
            }

            ChatsListBox.Loaded += ChatsListBox_Loaded;
            
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();
        }

        void ChatsListBox_Loaded(object sender, RoutedEventArgs e)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ChatsListBox.ItemsSource);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("GroupName");
            view.GroupDescriptions.Add(groupDescription);
            view.SortDescriptions.Add(new SortDescription("GroupName", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }
        
        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (this.ViewModel.Configuraion.GroupChats != null)
                {
                    var inactiveChats = this.ViewModel.Configuraion.GroupChats.Where(v => keysOfConnectedGroupChats.All(k => k != v.PublicKey)).ToList();
                    foreach (var chat in inactiveChats)
                    {
                        var groupNumber = tox.JoinGroup(chat.FriendNumber, chat.PublicKey);
                        if (groupNumber != -1)
                        {
                            chat.GroupNumber = groupNumber;
                            keysOfConnectedGroupChats.Add(chat.PublicKey);
                            UpdateGroupToOnlineStatus(chat);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                MessageBox.Show(ex.Message);
            }
        }

        private void applyConfig()
        {
            var accent = ThemeManager.GetAccent(this.ViewModel.Configuraion.AccentColor);
            var theme = ThemeManager.GetAppTheme(this.ViewModel.Configuraion.Theme);

            if (accent != null && theme != null)
                ThemeManager.ChangeAppStyle(System.Windows.Application.Current, accent, theme);

            ExecuteActionsOnNotifyIcon();
        }

        private void tox_OnReadReceipt(int friendnumber, uint receipt)
        {
            //a flowdocument should already be created, but hey, just in case
            if (!convdic.ContainsKey(friendnumber))
                return;

            Paragraph para = (Paragraph)convdic[friendnumber].FindChildren<TableRow>().Where(r => r.Tag != null && r.Tag.GetType() != typeof(FileTransfer) && ((MessageData)(r.Tag)).Id == receipt).First().FindChildren<TableCell>().ToArray()[1].Blocks.FirstBlock;

            if (para == null)
                return; //row or cell doesn't exist? odd, just return

            para.Foreground = Brushes.Black;
        }

        private void toxav_OnMediaChange(int call_index, IntPtr args)
        {
            //TODO: change the call type accordingly
        }

        private void InitializeNotifyIcon()
        {
            Stream newMessageIconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Toxy;component/Resources/Icons/icon2.ico")).Stream;
            Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Toxy;component/Resources/Icons/icon.ico")).Stream;

            notifyIcon = new Icon(iconStream);
            newMessageNotifyIcon = new Icon(newMessageIconStream);

            this.nIcon.Icon = notifyIcon;
            nIcon.Click += nIcon_Click;

            var trayIconContextMenu = new System.Windows.Forms.ContextMenu();
            var closeMenuItem = new System.Windows.Forms.MenuItem("Exit", closeMenuItem_Click);
            var openMenuItem = new System.Windows.Forms.MenuItem("Open", openMenuItem_Click);

            var statusMenuItem = new System.Windows.Forms.MenuItem("Status");
            var setOnlineMenuItem = new System.Windows.Forms.MenuItem("Online", setStatusMenuItem_Click);
            var setAwayMenuItem = new System.Windows.Forms.MenuItem("Away", setStatusMenuItem_Click);
            var setBusyMenuItem = new System.Windows.Forms.MenuItem("Busy", setStatusMenuItem_Click);

            setOnlineMenuItem.Tag = 0; // Online
            setAwayMenuItem.Tag = 1; // Away
            setBusyMenuItem.Tag = 2; // Busy

            statusMenuItem.MenuItems.Add(setOnlineMenuItem);
            statusMenuItem.MenuItems.Add(setAwayMenuItem);
            statusMenuItem.MenuItems.Add(setBusyMenuItem);

            trayIconContextMenu.MenuItems.Add(openMenuItem);
            trayIconContextMenu.MenuItems.Add(statusMenuItem);
            trayIconContextMenu.MenuItems.Add(closeMenuItem);
            nIcon.ContextMenu = trayIconContextMenu;
        }

        private void setStatusMenuItem_Click(object sender, EventArgs eventArgs)
        {
            SetStatus((ToxUserStatus)((System.Windows.Forms.MenuItem)sender).Tag);
        }

        void openMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }

        private void closeMenuItem_Click(object sender, EventArgs eventArgs)
        {
            ChatHistoryHelper.Close();
            this.ViewModel.Configuraion.HideInTray = false;
            ViewModel.SaveConfiguraion();
            this.Close();
        }

        private void nIcon_Click(object sender, EventArgs e)
        {
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void toxav_OnReceivedAudio(IntPtr toxav, int call_index, short[] frame, int frame_size, IntPtr userdata)
        {
            if (call == null)
                return;

            call.ProcessAudioFrame(frame, frame_size);
        }

        private void toxav_OnReceivedVideo(IntPtr toxav, int call_index, IntPtr frame, IntPtr userdata)
        {
            try
            {
                if (call == null)
                    return;

                call.ProcessVideoFrame(frame);
            }
            catch(Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public MainWindowViewModel ViewModel
        {
            get { return this.DataContext as MainWindowViewModel; }
        }

        private void toxav_OnEnd(int call_index, IntPtr args)
        {
            EndCall();
            CallButton.Visibility = Visibility.Visible;
            HangupButton.Visibility = Visibility.Collapsed;
        }

        private void toxav_OnStart(int call_index, IntPtr args)
        {
            if (call != null)
                call.Start(this.ViewModel.Configuraion.InputDevice, this.ViewModel.Configuraion.OutputDevice);

            int friendnumber = toxav.GetPeerID(call_index, 0);
            var callingFriend = this.ViewModel.GetFriendObjectByNumber(friendnumber);
            if (callingFriend != null)
            {
                callingFriend.IsCalling = false;
                callingFriend.IsCallingToFriend = false;
                CallButton.Visibility = Visibility.Collapsed;
                if (callingFriend.Selected)
                {
                    HangupButton.Visibility = Visibility.Visible;
                }
                this.ViewModel.CallingFriend = callingFriend;
            }
        }

        private void toxav_OnInvite(int call_index, IntPtr args)
        {
            //TODO: notify the user of another incoming call
            if (call != null)
                return;

            int friendnumber = toxav.GetPeerID(call_index, 0);

            /*ToxAvCodecSettings settings = toxav.GetPeerCodecSettings(call_index, 0);
            if (settings.CallType == ToxAvCallType.Video)
            {
                //we don't support video calls, just reject this and return.
                toxav.Reject(call_index, "Toxy does not support video calls.");
                return;
            }*/

            var friend = this.ViewModel.GetFriendObjectByNumber(friendnumber);
            if (friend != null)
            {
                friend.CallIndex = call_index;
                friend.IsCalling = true;
            }
        }

        private void tox_OnGroupNamelistChange(int groupnumber, int peernumber, ToxChatChange change)
        {
            var group = this.ViewModel.GetGroupObjectByNumber(groupnumber);
            if (group != null)
            {
                if (change == ToxChatChange.PeerAdd || change == ToxChatChange.PeerDel)
                {
                    var status = string.Format("Peers online: {0}", tox.GetGroupMemberCount(group.ChatNumber));
                    group.StatusMessage = status;
                }
                if (group.Selected)
                {
                    group.AdditionalInfo = string.Join(", ", tox.GetGroupNames(group.ChatNumber));
                }
            }
        }

        private void tox_OnGroupAction(int groupnumber, int friendgroupnumber, string action)
        {
            MessageData data = new MessageData() { Username = "*  ", Message = string.Format("{0} {1}", tox.GetGroupMemberName(groupnumber, friendgroupnumber), action), IsAction = true };

            var group = this.ViewModel.GetGroupObjectByNumber(groupnumber);
            if (group != null)
            {
                group.Document.AddNewMessageRow(tox, data, false);
                if (!group.Selected)
                {
                    group.HasNewMessage = true;
                    group.NewMessageCount++;
                }
                else
                {
                    ScrollChatBox();
                }
            }
            if (this.ViewModel.MainToxyUser.ToxStatus != ToxUserStatus.Busy)
                this.Flash();
        }

        private void tox_OnGroupMessage(int groupnumber, int friendgroupnumber, string message)
        {
            MessageData data = new MessageData() { Username = tox.GetGroupMemberName(groupnumber, friendgroupnumber), Message = message };

            var group = this.ViewModel.GetGroupObjectByNumber(groupnumber);
            if (group != null)
            {
                var run = GetLastMessageRun(group.Document);

                if (run != null)
                {
                    if (((MessageData)run.Tag).Username == data.Username)
                        group.Document.AddNewMessageRow(tox, data, true);
                    else
                        group.Document.AddNewMessageRow(tox, data, false);
                }
                else
                {
                    group.Document.AddNewMessageRow(tox, data, false);
                }
            }
            else
            {
                AddGroupToView(groupnumber).Document.AddNewMessageRow(tox, data, false);
            }

            if (group != null)
            {
                if (!group.Selected)
                {
                    group.HasNewMessage = true;
                    group.NewMessageCount++;
                }
                else
                {
                    ScrollChatBox();
                }
            }
            if (this.ViewModel.MainToxyUser.ToxStatus != ToxUserStatus.Busy)
                this.Flash();

            this.nIcon.Icon = newMessageNotifyIcon;
            this.ViewModel.HasNewMessage = true;
            ChatHistoryHelper.AddLineToHistory(group.PublicKey, data.Username, data.Message);
        }

        private void tox_OnGroupInvite(int groupnumber, string group_public_key)
        {
            if (this.ViewModel.Configuraion.GroupChats != null && this.ViewModel.Configuraion.GroupChats.Any(v => v.PublicKey == group_public_key))
            {
                var group = this.ViewModel.GetGroupObjectByNumber(group_public_key);
                if (group != null)
                {
                    SelectGroupControl(group);
                }
            }
            else
            {
                GroupChatHelpers.AddNewGoupToConfig(group_public_key, groupnumber, this.ViewModel.Configuraion);
                AddInactiveGroupToView(this.ViewModel.Configuraion.GroupChats.Last());
            }
        }

        private void tox_OnFriendRequest(string id, string message)
        {
            try
            {
                AddFriendRequestToView(id, message);
                if (this.ViewModel.MainToxyUser.ToxStatus != ToxUserStatus.Busy)
                    this.Flash();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            this.nIcon.Icon = newMessageNotifyIcon;
            this.ViewModel.HasNewMessage = true;
        }

        private void tox_OnFileControl(int friendnumber, int receive_send, int filenumber, int control_type, byte[] data)
        {
            switch ((ToxFileControl)control_type)
            {
                case ToxFileControl.Finished:
                    {
                        FileTransfer ft = GetFileTransfer(friendnumber, filenumber);

                        if (ft == null)
                            return;

                        ft.Stream.Close();
                        ft.Stream = null;

                        ft.Control.TransferFinished();
                        ft.Control.SetStatus("Finished!");
                        ft.Finished = true;

                        transfers.Remove(ft);

                        tox.FileSendControl(ft.FriendNumber, 1, ft.FileNumber, ToxFileControl.Finished, new byte[0]);
                        break;
                    }

                case ToxFileControl.Accept:
                    {
                        FileTransfer ft = GetFileTransfer(friendnumber, filenumber);
                        ft.Control.SetStatus("Transferring....");
                        ft.Stream = new FileStream(ft.FileName, FileMode.Open);
                        ft.Thread = new Thread(transferFile);
                        ft.Thread.Start(ft);

                        break;
                    }

                case ToxFileControl.Kill:
                    {
                        FileTransfer transfer = GetFileTransfer(friendnumber, filenumber);
                        transfer.Finished = true;
                        if (transfer != null)
                        {
                            if (transfer.Stream != null)
                                transfer.Stream.Close();

                            if (transfer.Thread != null)
                            {
                                transfer.Thread.Abort();
                                transfer.Thread.Join();
                            }

                            transfer.Control.HideAllButtons();
                            transfer.Control.SetStatus("Transfer killed!");
                        }

                        break;
                    }
            }
        }

        private void transferFile(object ft)
        {
            FileTransfer transfer = (FileTransfer)ft;

            ToxHandle handle = tox.GetHandle();
            int chunk_size = tox.FileDataSize(transfer.FriendNumber);
            byte[] buffer = new byte[chunk_size];

            while (true)
            {
                ulong remaining = tox.FileDataRemaining(transfer.FriendNumber, transfer.FileNumber, 0);
                if (remaining > (ulong)chunk_size)
                {
                    if (transfer.Stream.Read(buffer, 0, chunk_size) == 0)
                        break;

                    while (!tox.FileSendData(transfer.FriendNumber, transfer.FileNumber, buffer))
                    {
                        int time = (int)ToxFunctions.DoInterval(handle);

                        Console.WriteLine("Could not send data, sleeping for {0}ms", time);
                        Thread.Sleep(time);
                    }

                    Console.WriteLine("Data sent: {0} bytes", buffer.Length);
                }
                else
                {
                    buffer = new byte[remaining];

                    if (transfer.Stream.Read(buffer, 0, (int)remaining) == 0)
                        break;

                    tox.FileSendData(transfer.FriendNumber, transfer.FileNumber, buffer);

                    Console.WriteLine("Sent the last chunk of data: {0} bytes", buffer.Length);
                }

                double value = (double)remaining / (double)transfer.FileSize;
                transfer.Control.SetProgress(100 - (int)(value * 100));
            }

            transfer.Stream.Close();
            tox.FileSendControl(transfer.FriendNumber, 0, transfer.FileNumber, ToxFileControl.Finished, new byte[0]);

            transfer.Control.HideAllButtons();
            transfer.Control.SetStatus("Finished!");
            transfer.Finished = true;
        }

        private FileTransfer GetFileTransfer(int friendnumber, int filenumber)
        {
            foreach (FileTransfer ft in transfers)
                if (ft.FileNumber == filenumber && ft.FriendNumber == friendnumber && !ft.Finished)
                    return ft;

            return null;
        }

        private void tox_OnFileData(int friendnumber, int filenumber, byte[] data)
        {
            try
            {
                FileTransfer ft = GetFileTransfer(friendnumber, filenumber);

                if (ft == null)
                    return;

                if (ft.Stream == null)
                    throw new NullReferenceException("Unexpectedly received data");

                ulong remaining = tox.FileDataRemaining(friendnumber, filenumber, 1);
                double value = (double)remaining / (double)ft.FileSize;

                ft.Control.SetProgress(100 - (int)(value * 100));
                ft.Control.SetStatus(string.Format("{0}/{1}", ft.FileSize - remaining, ft.FileSize));

                if (ft.Stream.CanWrite)
                    ft.Stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private void tox_OnFileSendRequest(int friendnumber, int filenumber, ulong filesize, string filename)
        {
            if (!convdic.ContainsKey(friendnumber))
                convdic.Add(friendnumber, UIHelpers.GetNewFlowDocument());

            FileTransfer transfer = convdic[friendnumber].AddNewFileTransfer(tox, friendnumber, filenumber, filename, filesize, false);

            var friend = this.ViewModel.GetFriendObjectByNumber(friendnumber);
            if (friend != null && !friend.Selected)
            {
                friend.HasNewMessage = true;
                friend.NewMessageCount++;
            }

            if (this.ViewModel.Configuraion.AutoDownloadPictures && this.ViewModel.Configuraion.IsAllowedFile(filename))
            {
                if (transfer.Stream != null)
                    return;

                var newFileName = Path.Combine(ViewModel.Configuraion.DownloadsFolder, filename);
                newFileName = FileHelpers.GetUniqueFilename(newFileName);
                transfer.Stream = new FileStream(newFileName, FileMode.Create);
                transfer.FileName = newFileName;
                transfer.Control.FilePath = newFileName;
                tox.FileSendControl(friendnumber, 1, filenumber, ToxFileControl.Accept, new byte[0]);
            }
            else
            {
                transfer.Control.OnAccept += delegate(int friendnum, int filenum)
                {
                    if (transfer.Stream != null)
                        return;

                    SaveFileDialog dialog = new SaveFileDialog();
                    dialog.FileName = filename;

                    if (dialog.ShowDialog() == true) //guess what, this bool is nullable
                    {
                        transfer.Stream = new FileStream(dialog.FileName, FileMode.Create);
                        transfer.FileName = dialog.FileName;
                        tox.FileSendControl(friendnumber, 1, filenumber, ToxFileControl.Accept, new byte[0]);
                    }
                };


                transfer.Control.OnDecline += delegate(int friendnum, int filenum)
                {
                    if (!transfer.IsSender)
                        tox.FileSendControl(friendnumber, 1, filenumber, ToxFileControl.Kill, new byte[0]);
                    else
                        tox.FileSendControl(friendnumber, 0, filenumber, ToxFileControl.Kill, new byte[0]);

                    if (transfer.Thread != null)
                    {
                        transfer.Thread.Abort();
                        transfer.Thread.Join();
                    }

                    if (transfer.Stream != null)
                        transfer.Stream.Close();

                };
            }

            transfer.Control.OnFileOpen += delegate()
            {
                try { Process.Start(transfer.FileName); }
                catch { /*want to open a "choose program dialog" here*/ }
            };

            transfer.Control.OnFolderOpen += delegate()
            {
                Process.Start("explorer.exe", @"/select, " + transfer.FileName);
            };

            transfers.Add(transfer);
            if (this.ViewModel.MainToxyUser.ToxStatus != ToxUserStatus.Busy)
                this.Flash();
        }

        private void tox_OnConnectionStatusChanged(int friendnumber, int status)
        {
            var friend = this.ViewModel.GetFriendObjectByNumber(friendnumber);
            if (friend == null)
                return;

            if (status == 0)
            {
                DateTime lastOnline = tox.GetLastOnline(friendnumber);
                if (lastOnline == emptyLastOnline)
                {
                    lastOnline = DateTime.Now;
                }
                friend.StatusMessage = string.Format("Last seen: {0} {1}", lastOnline.ToShortDateString(), lastOnline.ToLongTimeString());
                friend.ToxStatus = ToxUserStatus.Invalid; //not the proper way to do it, I know...

                if (friend.Selected)
                {
                    CallButton.Visibility = Visibility.Collapsed;
                    FileButton.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                friend.StatusMessage = tox.GetStatusMessage(friend.ChatNumber);

                if (friend.Selected)
                {
                    CallButton.Visibility = Visibility.Visible;
                    FileButton.Visibility = Visibility.Visible;
                }
            }
        }

        private void tox_OnTypingChange(int friendnumber, bool is_typing)
        {
            var friend = this.ViewModel.GetFriendObjectByNumber(friendnumber);
            if (friend == null)
                return;

            if (friend.Selected)
            {
                if (is_typing)
                    TypingStatusLabel.Content = tox.GetName(friendnumber) + " is typing...";
                else
                    TypingStatusLabel.Content = "";
            }
        }

        private void tox_OnFriendAction(int friendnumber, string action)
        {
            MessageData data = new MessageData() { Username = "*  ", Message = string.Format("{0} {1}", tox.GetName(friendnumber), action), IsAction = true };

            if (convdic.ContainsKey(friendnumber))
            {
                convdic[friendnumber].AddNewMessageRow(tox, data, false);
            }
            else
            {
                FlowDocument document = UIHelpers.GetNewFlowDocument();
                convdic.Add(friendnumber, document);
                convdic[friendnumber].AddNewMessageRow(tox, data, false);
            }

            var friend = this.ViewModel.GetFriendObjectByNumber(friendnumber);
            if (friend != null)
            {
                if (!friend.Selected)
                {
                    friend.HasNewMessage = true;
                    friend.NewMessageCount++;
                }
                else
                {
                    ScrollChatBox();
                }
            }
            if (this.ViewModel.MainToxyUser.ToxStatus != ToxUserStatus.Busy)
                this.Flash();

            this.nIcon.Icon = newMessageNotifyIcon;
            this.ViewModel.HasNewMessage = true;
        }

        private void tox_OnFriendMessage(int friendnumber, string message)
        {
            var friendName = tox.GetName(friendnumber);
            MessageData data = new MessageData() { Username = friendName, Message = message };

            if (convdic.ContainsKey(friendnumber))
            {
                var run = GetLastMessageRun(convdic[friendnumber]);

                if (run != null)
                {
                    if (((MessageData)run.Tag).Username == tox.GetName(friendnumber))
                        convdic[friendnumber].AddNewMessageRow(tox, data, true);
                    else
                        convdic[friendnumber].AddNewMessageRow(tox, data, false);
                }
                else
                {
                    convdic[friendnumber].AddNewMessageRow(tox, data, false);
                }
            }
            else
            {
                FlowDocument document = UIHelpers.GetNewFlowDocument();
                convdic.Add(friendnumber, document);
                convdic[friendnumber].AddNewMessageRow(tox, data, false);
            }

            var friend = this.ViewModel.GetFriendObjectByNumber(friendnumber);
            if (friend != null)
            {
                if (!friend.Selected)
                {
                    friend.HasNewMessage = true;
                    friend.NewMessageCount++;
                }
                else
                {
                    ScrollChatBox();
                }
            }
            if (this.ViewModel.MainToxyUser.ToxStatus != ToxUserStatus.Busy)
                this.Flash();

            this.nIcon.Icon = newMessageNotifyIcon;
            this.ViewModel.HasNewMessage = true;
            ChatHistoryHelper.AddLineToHistory(tox.GetClientID(friendnumber).GetString(), friendName, message);
        }

        internal void ScrollChatBox()
        {
            ScrollViewer viewer = FindScrollViewer(ChatBox);

            if (viewer != null)
                if (viewer.ScrollableHeight >= viewer.VerticalOffset)
                    viewer.ScrollToBottom();
        }

        internal void ScrollChatBox(double delta)
        {
            ScrollViewer viewer = FindScrollViewer(ChatBox);

            if (viewer != null)
                viewer.ScrollToVerticalOffset(viewer.VerticalOffset + delta);
        }

        private static ScrollViewer FindScrollViewer(FlowDocumentScrollViewer viewer)
        {
            if (VisualTreeHelper.GetChildrenCount(viewer) == 0)
                return null;

            DependencyObject first = VisualTreeHelper.GetChild(viewer, 0);
            if (first == null)
                return null;

            Decorator border = (Decorator)VisualTreeHelper.GetChild(first, 0);
            if (border == null)
                return null;

            return (ScrollViewer)border.Child;
        }

        private TableRow GetLastMessageRun(FlowDocument doc)
        {
            try
            {
                return doc.FindChildren<TableRow>().LastOrDefault(t => t.Tag != null && t.Tag.GetType() != typeof(FileTransfer));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return null;
            }
        }

        private void NewGroupButton_Click(object sender, RoutedEventArgs e)
        {
            int groupnumber = tox.NewGroup();
            if (groupnumber != -1)
            {
                AddGroupToView(groupnumber);
            }
        }

        private void tox_OnUserStatus(int friendnumber, ToxUserStatus status)
        {
            var friend = this.ViewModel.GetFriendObjectByNumber(friendnumber);
            if (friend != null)
            {
                friend.ToxStatus = status;
            }
        }

        private void tox_OnStatusMessage(int friendnumber, string newstatus)
        {
            var friend = this.ViewModel.GetFriendObjectByNumber(friendnumber);
            if (friend != null)
            {
                friend.StatusMessage = newstatus;
            }
        }

        private void tox_OnNameChange(int friendnumber, string newname)
        {
            var friend = this.ViewModel.GetFriendObjectByNumber(friendnumber);
            if (friend != null)
            {
                friend.Name = newname;
            }
        }

        private void InitFriends()
        {
            //Creates a new FriendControl for every friend
            foreach (var friendNumber in tox.GetFriendlist())
            {
                AddFriendToView(friendNumber);
            }
            
        }

        private GroupControlModelView AddGroupToView(int groupnumber)
        {
            string groupname = string.Format("Groupchat #{0}", groupnumber);

            var groupMV = new GroupControlModelView();
            groupMV.ChatNumber = groupnumber;
            groupMV.Name = groupname;
            groupMV.GroupName = Constants.GeneralGroupName;
            groupMV.StatusMessage = string.Format("Peers online: {0}", tox.GetGroupMemberCount(groupnumber));
            groupMV.SelectedAction = GroupSelectedAction;
            groupMV.DeleteAction = GroupDeleteAction;
            groupMV.RenameAction = GroupRenameAction;
            this.ViewModel.ChatCollection.Add(groupMV);
            return groupMV;
        }

        private void UpdateGroupToOnlineStatus(GroupChat groupChat)
        {
            var groupMV = this.ViewModel.ChatCollection.FirstOrDefault(v => v.PublicKey == groupChat.PublicKey);
            if (groupMV != null)
            {
                groupMV.ChatNumber = groupChat.GroupNumber;
                groupMV.StatusMessage = string.Format("Peers online: {0}", tox.GetGroupMemberCount(groupChat.GroupNumber));
            }
        }

        private void AddInactiveGroupToView(GroupChat groupChat)
        {
            var groupMV = new GroupControlModelView();
            groupMV.ChatNumber = -1;
            groupMV.Name = groupChat.Name;
            groupMV.PublicKey = groupChat.PublicKey;
            groupMV.GroupName = ViewModel.Configuraion.GetContactsGroupName(groupChat.PublicKey);
            groupMV.StatusMessage = "Waiting...";
            groupMV.SelectedAction = GroupSelectedAction;
            groupMV.DeleteAction = GroupDeleteAction;
            groupMV.RenameAction = GroupRenameAction;
            groupMV.MoveToContactGroupAction = MoveToContactGroupAction;
            this.ViewModel.ChatCollection.Add(groupMV);
        }

        private void GroupDeleteAction(IGroupObject groupObject)
        {
            this.ViewModel.ChatCollection.Remove(groupObject);
            int groupNumber = groupObject.ChatNumber;
            if (groupObject.Selected) ChatBox.Document = null;
            tox.DeleteGroupChat(groupNumber);
            GroupChatHelpers.RemoveGroupFromConfig(this.ViewModel.Configuraion, groupObject.PublicKey);
            groupObject.SelectedAction = null;
            groupObject.DeleteAction = null;
            groupObject.RenameAction = null;
        }

        private void GroupRenameAction(IGroupObject groupObject)
        {
            var dialog = new UserPrompt();
            dialog.ResponseText = groupObject.Name;
            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.ResponseText))
            {
                GroupChatHelpers.RenameGroup(this.ViewModel.Configuraion, groupObject.PublicKey, dialog.ResponseText);
                groupObject.Name = dialog.ResponseText;
            }
        }

        private void GroupSelectedAction(IGroupObject groupObject, bool isSelected)
        {
            groupObject.HasNewMessage = false;
            groupObject.NewMessageCount = 0;

            TypingStatusLabel.Content = "";

            if (isSelected)
            {
                SelectGroupControl(groupObject);
                ScrollChatBox();
                TextToSend.Focus();
            }
        }

        private void AddFriendToView(int friendNumber)
        {
            string friendStatus;
            if (tox.GetFriendConnectionStatus(friendNumber) != 0)
            {
                friendStatus = tox.GetStatusMessage(friendNumber);
            }
            else
            {
                DateTime lastOnline = tox.GetLastOnline(friendNumber);
                if (lastOnline == emptyLastOnline)
                {
                    lastOnline = DateTime.Now;
                }
                friendStatus = string.Format("Last seen: {0} {1}", lastOnline.ToShortDateString(), lastOnline.ToLongTimeString());
            }

            string friendName = tox.GetName(friendNumber);
            if (string.IsNullOrEmpty(friendName))
            {
                friendName = tox.GetClientID(friendNumber).GetString();
            }

            var friendMV = new FriendControlModelView(this.ViewModel);
            friendMV.ChatNumber = friendNumber;
            friendMV.Name = friendName;
            friendMV.GroupName = ViewModel.Configuraion.GetContactsGroupName(tox.GetClientID(friendNumber).GetString());
            friendMV.StatusMessage = friendStatus;
            friendMV.ToxStatus = ToxUserStatus.Invalid;
            friendMV.SelectedAction = FriendSelectedAction;
            friendMV.DenyCallAction = FriendDenyCallAction;
            friendMV.AcceptCallAction = FriendAcceptCallAction;
            friendMV.CopyIDAction = FriendCopyIdAction;
            friendMV.DeleteAction = FriendDeleteAction;
            friendMV.GroupInviteAction = GroupInviteAction;
            friendMV.HangupAction = FriendHangupAction;
            friendMV.MoveToContactGroupAction = MoveToContactGroupAction;
            friendMV.PublicKey = tox.GetClientID(friendNumber).GetString();

            this.ViewModel.ChatCollection.Add(friendMV);
        }

        private void MoveToContactGroupAction(IFriendObject friendObject, string groupName)
        {
            if (this.ViewModel.Configuraion.ContactGroups.Any(v => v.GroupName == groupName))
            {
                friendObject.GroupName = groupName;
                var friend = this.ViewModel.Configuraion.ContactGroups.FirstOrDefault(v => v.PublicKey == friendObject.PublicKey);
                if(friend!=null)
                {
                    friend.GroupName = groupName;
                }
                else
                {
                    this.ViewModel.Configuraion.ContactGroups.Add(new ContactGroupEntity() { GroupName = groupName, PublicKey = friendObject.PublicKey });                    
                }
            }
            else
            {
                var dialog = new UserPrompt();
                dialog.PromtText.Text = "Enter new group name:";
                if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.ResponseText))
                {
                    this.ViewModel.Configuraion.ContactGroups.Add(new ContactGroupEntity() { GroupName = dialog.ResponseText, PublicKey = friendObject.PublicKey });                    
                    friendObject.GroupName = dialog.ResponseText;
                }
            }
            this.ViewModel.SaveConfiguraion();
            var view = CollectionViewSource.GetDefaultView(ChatsListBox.ItemsSource);
            view.Refresh();
        }

        private void FriendHangupAction(IFriendObject friendObject)
        {
            EndCall(friendObject);
        }

        private void GroupInviteAction(IFriendObject friendObject, IGroupObject groupObject)
        {
            tox.InviteFriend(friendObject.ChatNumber, groupObject.ChatNumber);
        }

        private void FriendDeleteAction(IFriendObject friendObject)
        {
            this.ViewModel.ChatCollection.Remove(friendObject);
            var friendNumber = friendObject.ChatNumber;
            if (convdic.ContainsKey(friendNumber))
            {
                convdic.Remove(friendNumber);
                if (friendObject.Selected)
                {
                    ChatBox.Document = null;
                }
            }
            tox.DeleteFriend(friendNumber);
            friendObject.SelectedAction = null;
            friendObject.DenyCallAction = null;
            friendObject.AcceptCallAction = null;
            friendObject.CopyIDAction = null;
            friendObject.DeleteAction = null;
            friendObject.GroupInviteAction = null;
            friendObject.MainViewModel = null;

            tox.Save(toxDataFilename);
        }

        private void FriendCopyIdAction(IFriendObject friendObject)
        {
            Clipboard.Clear();
            Clipboard.SetText(tox.GetClientID(friendObject.ChatNumber).GetString());
        }

        private void FriendSelectedAction(IFriendObject friendObject, bool isSelected)
        {
            friendObject.HasNewMessage = false;
            friendObject.NewMessageCount = 0;

            if (!tox.GetIsTyping(friendObject.ChatNumber))
                TypingStatusLabel.Content = "";
            else
                TypingStatusLabel.Content = tox.GetName(friendObject.ChatNumber) + " is typing...";

            if (isSelected)
            {
                SelectFriendControl(friendObject);
                ScrollChatBox();
                TextToSend.Focus();
            }
        }

        private void FriendAcceptCallAction(IFriendObject friendObject)
        {
            if (call != null)
                return;

            ToxAvCodecSettings settings = ToxAv.DefaultCodecSettings;
            settings.CallType = ToxAvCallType.Video;

            call = new ToxCall(tox, toxav, friendObject.CallIndex, friendObject.ChatNumber, toxav.GetPeerCodecSettings(friendObject.CallIndex, 0).CallType == ToxAvCallType.Video ? true : false);
            call.Answer(settings);
        }

        private void FriendDenyCallAction(IFriendObject friendObject)
        {
            if (call == null)
            {
                toxav.Reject(friendObject.CallIndex, "I'm busy...");
                friendObject.IsCalling = false;
            }
            else
            {
                call.Stop();
                call = null;
            }
        }

        private void AddFriendRequestToView(string id, string message)
        {
            var friendMV = new FriendControlModelView(this.ViewModel);
            friendMV.IsRequest = true;
            friendMV.Name = id;
            friendMV.ToxStatus = ToxUserStatus.Invalid;
            friendMV.RequestMessageData = new MessageData() { Message = message, Username = "Request Message" };
            friendMV.RequestFlowDocument = UIHelpers.GetNewFlowDocument();
            friendMV.SelectedAction = FriendRequestSelectedAction;
            friendMV.AcceptAction = FriendRequestAcceptAction;
            friendMV.DeclineAction = FriendRequestDeclineAction;

            this.ViewModel.ChatRequestCollection.Add(friendMV);

            if (ListViewTabControl.SelectedIndex != 1)
            {
                RequestsTabItem.Header = "Requests*";
            }
        }

        private void FriendRequestSelectedAction(IFriendObject friendObject, bool isSelected)
        {
            friendObject.RequestFlowDocument.AddNewMessageRow(tox, friendObject.RequestMessageData, false);
        }

        private void FriendRequestAcceptAction(IFriendObject friendObject)
        {
            int friendnumber = tox.AddFriendNoRequest(friendObject.Name);

            AddFriendToView(friendnumber);

            this.ViewModel.ChatRequestCollection.Remove(friendObject);
            friendObject.RequestFlowDocument = null;
            friendObject.SelectedAction = null;
            friendObject.AcceptAction = null;
            friendObject.DeclineAction = null;
            friendObject.MainViewModel = null;

            tox.Save(toxDataFilename);
        }

        private void FriendRequestDeclineAction(IFriendObject friendObject)
        {
            this.ViewModel.ChatRequestCollection.Remove(friendObject);
            friendObject.RequestFlowDocument = null;
            friendObject.SelectedAction = null;
            friendObject.AcceptAction = null;
            friendObject.DeclineAction = null;
        }

        private void SelectGroupControl(IGroupObject group)
        {
            if (group == null)
            {
                group.Selected = false;
                return;
            }

            CallButton.Visibility = Visibility.Collapsed;
            FileButton.Visibility = Visibility.Collapsed;
            group.AdditionalInfo = string.Join(", ", tox.GetGroupNames(group.ChatNumber));
            ChatBox.Document = group.Document;
            UIHelpers.PreloadHistory(tox, ChatBox, group.PublicKey);
        }

        private void EndCall()
        {
            if (call != null)
            {
                var friendnumber = toxav.GetPeerID(call.CallIndex, 0);
                var friend = this.ViewModel.GetFriendObjectByNumber(friendnumber);

                this.EndCall(friend);
            }
            else
            {
                this.EndCall(null);
            }
        }

        private void EndCall(IFriendObject friend)
        {
            if (friend != null)
            {
                toxav.Cancel(friend.CallIndex, friend.ChatNumber, "I'm busy...");

                friend.IsCalling = false;
                friend.IsCallingToFriend = false;
            }

            if (call != null)
            {
                call.Stop();
                call = null;
            }

            this.ViewModel.CallingFriend = null;

            HangupButton.Visibility = Visibility.Collapsed;
            CallButton.Visibility = Visibility.Visible;
        }

        private void SelectFriendControl(IFriendObject friend)
        {
            if (friend == null)
            {
                return;
            }
            int friendNumber = friend.ChatNumber;

            if (call != null)
            {
                if (call.FriendNumber != friendNumber)
                    HangupButton.Visibility = Visibility.Collapsed;
                else
                    HangupButton.Visibility = Visibility.Visible;
            }
            else
            {
                if (tox.GetFriendConnectionStatus(friendNumber) != 1)
                {
                    CallButton.Visibility = Visibility.Collapsed;
                    FileButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    CallButton.Visibility = Visibility.Visible;
                    FileButton.Visibility = Visibility.Visible;
                }
            }

            if (convdic.ContainsKey(friend.ChatNumber))
            {
                ChatBox.Document = convdic[friend.ChatNumber];
            }
            else
            {
                FlowDocument document = UIHelpers.GetNewFlowDocument();
                convdic.Add(friend.ChatNumber, document);
                ChatBox.Document = convdic[friend.ChatNumber];
            }

            UIHelpers.PreloadHistory(tox, ChatBox, friendNumber);
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.ViewModel.Configuraion.HideInTray)
            {
                e.Cancel = true;
                this.ShowInTaskbar = false;
                this.WindowState = WindowState.Minimized;
            }
            else
            {
                if (call != null)
                    call.Stop();

                foreach (FileTransfer transfer in transfers)
                {
                    if (transfer.Thread != null)
                    {
                        //TODO: show a message warning the users that there are still file transfers in progress
                        transfer.Thread.Abort();
                        transfer.Thread.Join();
                    }
                }

                tox.Save(toxDataFilename);

                toxav.Dispose();
                tox.Dispose();
            }
        }

        private void OpenAddFriend_Click(object sender, RoutedEventArgs e)
        {
            FriendFlyout.IsOpen = !FriendFlyout.IsOpen;
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            userPressedSave = false;
            if (!SettingsFlyout.IsOpen)
            {
                SettingsUsername.Text = tox.GetSelfName();
                SettingsStatus.Text = tox.GetSelfStatusMessage();
                SettingsNospam.Text = tox.GetNospam().ToString();

                Tuple<AppTheme, Accent> style = ThemeManager.DetectAppStyle(System.Windows.Application.Current);
                Accent accent = ThemeManager.GetAccent(style.Item2.Name);
                oldAccent = accent;
                if (accent != null)
                    AccentComboBox.SelectedItem = AccentComboBox.Items.Cast<AccentColorMenuData>().Single(a => a.Name == style.Item2.Name);

                AppTheme theme = ThemeManager.GetAppTheme(style.Item1.Name);
                oldAppTheme = theme;
                if (theme != null)
                    AppThemeComboBox.SelectedItem = AppThemeComboBox.Items.Cast<AppThemeMenuData>().Single(a => a.Name == style.Item1.Name);

                if (InputDevicesComboBox.Items.Count - 1 >= this.ViewModel.Configuraion.InputDevice)
                    InputDevicesComboBox.SelectedIndex = this.ViewModel.Configuraion.InputDevice;

                if (OutputDevicesComboBox.Items.Count - 1 >= this.ViewModel.Configuraion.OutputDevice)
                    OutputDevicesComboBox.SelectedIndex = this.ViewModel.Configuraion.OutputDevice;

                HideInTrayCheckBox.IsChecked = this.ViewModel.Configuraion.HideInTray;
            }

            SettingsFlyout.IsOpen = !SettingsFlyout.IsOpen;
        }

        private void AddFriend_Click(object sender, RoutedEventArgs e)
        {
            TextRange message = new TextRange(AddFriendMessage.Document.ContentStart, AddFriendMessage.Document.ContentEnd);

            if (!(!string.IsNullOrWhiteSpace(AddFriendID.Text) && message.Text != null))
                return;

            string friendID = AddFriendID.Text.Trim();

            if (friendID.Contains("@"))
            {
                try
                {
                    string id = DnsTools.DiscoverToxID(friendID, this.ViewModel.Configuraion.NameServices);

                    if (string.IsNullOrEmpty(id))
                        throw new Exception("The server returned an empty result");

                    AddFriendID.Text = id;
                }
                catch (Exception ex)
                {
                    this.ShowMessageAsync("Could not find a tox id", ex.Message.ToString());
                }

                return;
            }

            int friendnumber;
            try
            {
                friendnumber = tox.AddFriend(friendID, message.Text);
                FriendFlyout.IsOpen = false;
                AddFriendToView(friendnumber);
            }
            catch (ToxAFException ex)
            {
                if (ex.Error != ToxAFError.SetNewNospam)
                    this.ShowMessageAsync("An error occurred", Tools.GetAFError(ex.Error));

                return;
            }
            catch
            {
                this.ShowMessageAsync("An error occurred", "The ID you entered is not valid.");
                return;
            }

            AddFriendID.Text = string.Empty;
            AddFriendMessage.Document.Blocks.Clear();
            AddFriendMessage.Document.Blocks.Add(new Paragraph(new Run("Hello, I'd like to add you to my friends list.")));

            tox.Save(toxDataFilename);
            FriendFlyout.IsOpen = false;
        }

        private void SaveSettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            userPressedSave = true;

            tox.SetName(SettingsUsername.Text);
            tox.SetStatusMessage(SettingsStatus.Text);

            uint nospam;
            if (uint.TryParse(SettingsNospam.Text, out nospam))
                tox.SetNospam(nospam);

            this.ViewModel.MainToxyUser.Name = SettingsUsername.Text;
            this.ViewModel.MainToxyUser.StatusMessage = SettingsStatus.Text;

            this.ViewModel.Configuraion.HideInTray = HideInTrayCheckBox.IsChecked ?? false;

            SettingsFlyout.IsOpen = false;

            if (AccentComboBox.SelectedItem != null)
            {
                string accentName = ((AccentColorMenuData)AccentComboBox.SelectedItem).Name;
                var theme = ThemeManager.DetectAppStyle(System.Windows.Application.Current);
                var accent = ThemeManager.GetAccent(accentName);
                ThemeManager.ChangeAppStyle(System.Windows.Application.Current, accent, theme.Item1);

                this.ViewModel.Configuraion.AccentColor = accentName;
            }

            if (AppThemeComboBox.SelectedItem != null)
            {
                string themeName = ((AppThemeMenuData)AppThemeComboBox.SelectedItem).Name;
                var theme = ThemeManager.DetectAppStyle(System.Windows.Application.Current);
                var appTheme = ThemeManager.GetAppTheme(themeName);
                ThemeManager.ChangeAppStyle(System.Windows.Application.Current, theme.Item2, appTheme);

                this.ViewModel.Configuraion.Theme = themeName;
            }

            if (call != null)
            {
                if (InputDevicesComboBox.SelectedIndex != this.ViewModel.Configuraion.InputDevice)
                    call.SwitchInputDevice(InputDevicesComboBox.SelectedIndex);

                if (OutputDevicesComboBox.SelectedIndex != this.ViewModel.Configuraion.OutputDevice)
                    call.SwitchOutputDevice(OutputDevicesComboBox.SelectedIndex);
            }

            int index = InputDevicesComboBox.SelectedIndex + 1;
            if (index != 0 && WaveIn.DeviceCount > 0 && WaveIn.DeviceCount >= index)
                this.ViewModel.Configuraion.InputDevice = index - 1;

            index = OutputDevicesComboBox.SelectedIndex + 1;
            if (index != 0 && WaveOut.DeviceCount > 0 && WaveOut.DeviceCount >= index)
                this.ViewModel.Configuraion.OutputDevice = index - 1;

            ExecuteActionsOnNotifyIcon();

            this.ViewModel.SaveConfiguraion();
            tox.Save(toxDataFilename);
        }

        private void TextToSend_KeyDown(object sender, KeyEventArgs e)
        {
            string text = TextToSend.Text;

            if (e.Key == Key.Enter)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    TextToSend.Text += Environment.NewLine;
                    TextToSend.CaretIndex = TextToSend.Text.Length;
                    return;
                }

                if (e.IsRepeat)
                    return;

                if (string.IsNullOrEmpty(text))
                    return;

                var selectedChatNumber = this.ViewModel.SelectedChatNumber;
                if (tox.GetFriendConnectionStatus(selectedChatNumber) == 0 && this.ViewModel.IsFriendSelected)
                    return;

                if (text.StartsWith("/me "))
                {
                    //action
                    string action = text.Substring(4);
                    int messageid = -1;

                    if (this.ViewModel.IsFriendSelected)
                        messageid = tox.SendAction(selectedChatNumber, action);
                    else if (this.ViewModel.IsGroupSelected)
                        tox.SendGroupAction(selectedChatNumber, action);

                    MessageData data = new MessageData() { Username = "*  ", Message = string.Format("{0} {1}", tox.GetSelfName(), action), IsAction = true, Id = messageid, IsSelf = this.ViewModel.IsFriendSelected };

                    if (this.ViewModel.IsFriendSelected)
                    {
                        if (convdic.ContainsKey(selectedChatNumber))
                        {
                            convdic[selectedChatNumber].AddNewMessageRow(tox, data, false);
                        }
                        else
                        {
                            FlowDocument document = UIHelpers.GetNewFlowDocument();
                            convdic.Add(selectedChatNumber, document);
                            convdic[selectedChatNumber].AddNewMessageRow(tox, data, false);
                        }
                    }
                }
                else
                {
                    //regular message
                    foreach (string message in text.WordWrap(ToxConstants.MaxMessageLength))
                    {
                        int messageid = -1;

                        if (this.ViewModel.IsFriendSelected)
                        {
                            messageid = tox.SendMessage(selectedChatNumber, message);
                            ChatHistoryHelper.AddLineToHistory(tox.GetClientID(selectedChatNumber).GetString(), tox.GetSelfName(), message);
                        }
                        else if (this.ViewModel.IsGroupSelected)
                        {
                            tox.SendGroupMessage(selectedChatNumber, message);
                            var group = this.ViewModel.GetGroupObjectByNumber(selectedChatNumber);
                            if (group != null)
                            {
                                ChatHistoryHelper.AddLineToHistory(group.PublicKey, tox.GetSelfName(), message);
                            }
                        }

                        MessageData data = new MessageData() { Username = tox.GetSelfName(), Message = message, Id = messageid, IsSelf = this.ViewModel.IsFriendSelected };

                        if (this.ViewModel.IsFriendSelected)
                        {
                            if (convdic.ContainsKey(selectedChatNumber))
                            {
                                var run = GetLastMessageRun(convdic[selectedChatNumber]);
                                if (run != null)
                                {
                                    if (((MessageData)run.Tag).Username == data.Username)
                                        convdic[selectedChatNumber].AddNewMessageRow(tox, data, true);
                                    else
                                        convdic[selectedChatNumber].AddNewMessageRow(tox, data, false);
                                }
                                else
                                    convdic[selectedChatNumber].AddNewMessageRow(tox, data, false);
                            }
                            else
                            {
                                FlowDocument document = UIHelpers.GetNewFlowDocument();
                                convdic.Add(selectedChatNumber, document);
                                convdic[selectedChatNumber].AddNewMessageRow(tox, data, false);
                            }
                        }
                    }
                }

                ScrollChatBox();

                TextToSend.Text = "";
                e.Handled = true;
            }
            else if (e.Key == Key.Tab && this.ViewModel.IsGroupSelected)
            {
                string[] names = tox.GetGroupNames(this.ViewModel.SelectedChatNumber);

                foreach (string name in names)
                {
                    if (!name.ToLower().StartsWith(text.ToLower()))
                        continue;

                    TextToSend.Text = string.Format("{0}, ", name);
                    TextToSend.SelectionStart = TextToSend.Text.Length;
                }

                e.Handled = true;
            }
        }

        private void GithubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/UtImpetus/unToxy");
        }

        private void TextToSend_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.ViewModel.IsFriendSelected)
                return;

            string text = TextToSend.Text;

            if (string.IsNullOrEmpty(text))
            {
                if (typing)
                {
                    typing = false;
                    tox.SetUserIsTyping(this.ViewModel.SelectedChatNumber, typing);
                }
            }
            else
            {
                if (!typing)
                {
                    typing = true;
                    tox.SetUserIsTyping(this.ViewModel.SelectedChatNumber, typing);
                }
            }
        }

        private void CopyIDButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(tox.GetAddress());
        }

        private void MetroWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!resizing && focusTextbox)
                TextToSend.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
        }

        private void TextToSend_OnGotFocus(object sender, RoutedEventArgs e)
        {
            focusTextbox = true;
        }

        private void TextToSend_OnLostFocus(object sender, RoutedEventArgs e)
        {
            focusTextbox = false;
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (resizing)
            {
                resizing = false;
                if (focusTextbox)
                {
                    TextToSend.Focus();
                    focusTextbox = false;
                }
            }
        }

        private void OnlineThumbButton_Click(object sender, EventArgs e)
        {
            SetStatus(ToxUserStatus.None);
        }

        private void AwayThumbButton_Click(object sender, EventArgs e)
        {
            SetStatus(ToxUserStatus.Away);
        }

        private void BusyThumbButton_Click(object sender, EventArgs e)
        {
            SetStatus(ToxUserStatus.Busy);
        }

        private void ListViewTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RequestsTabItem.IsSelected)
                RequestsTabItem.Header = "Requests";
        }

        private void StatusRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StatusContextMenu.PlacementTarget = this;
            StatusContextMenu.IsOpen = true;
        }

        private void MenuItem_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            SetStatus((ToxUserStatus)int.Parse(menuItem.Tag.ToString()));
        }

        private void SetStatus(ToxUserStatus? newStatus)
        {
            if (newStatus == null)
                newStatus = tox.GetSelfUserStatus();
            else
                tox.SetUserStatus(newStatus.GetValueOrDefault());

            this.ViewModel.MainToxyUser.ToxStatus = newStatus.GetValueOrDefault();
        }

        private void CallButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!this.ViewModel.IsFriendSelected)
                return;

            if (call != null)
                return;

            var selectedChatNumber = this.ViewModel.SelectedChatNumber;
            if (tox.GetFriendConnectionStatus(selectedChatNumber) != 1)
                return;

            ToxAvCodecSettings settings = ToxAv.DefaultCodecSettings;
            settings.CallType = ToxAvCallType.Video;

            int call_index;
            ToxAvError error = toxav.Call(selectedChatNumber, settings, 30, out call_index);
            if (error != ToxAvError.None)
                return;

            int friendnumber = toxav.GetPeerID(call_index, 0);
            call = new ToxCall(tox, toxav, call_index, friendnumber, true);

            CallButton.Visibility = Visibility.Collapsed;
            HangupButton.Visibility = Visibility.Visible;
            var callingFriend = this.ViewModel.GetFriendObjectByNumber(friendnumber);
            if (callingFriend != null)
            {
                this.ViewModel.CallingFriend = callingFriend;
                callingFriend.IsCallingToFriend = true;
            }
        }

        private void MainHangupButton_OnClick(object sender, RoutedEventArgs e)
        {
            EndCall();
        }

        private void FileButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!this.ViewModel.IsFriendSelected)
                return;

            var selectedChatNumber = this.ViewModel.SelectedChatNumber;
            if (tox.GetFriendConnectionStatus(selectedChatNumber) != 1)
                return;

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = Environment.CurrentDirectory;
            dialog.Multiselect = false;

            if (dialog.ShowDialog() != true)
                return;

            string filename = dialog.FileName;
            FileInfo info = new FileInfo(filename);
            int filenumber = tox.NewFileSender(selectedChatNumber, (ulong)info.Length, filename.Split('\\').Last<string>());

            if (filenumber == -1)
                return;

            FileTransfer ft = convdic[selectedChatNumber].AddNewFileTransfer(tox, selectedChatNumber, filenumber, filename, (ulong)info.Length, true);
            ft.Control.SetStatus(string.Format("Waiting for {0} to accept...", tox.GetName(selectedChatNumber)));
            ft.Control.AcceptButton.Visibility = Visibility.Collapsed;
            ft.Control.DeclineButton.Visibility = Visibility.Visible;

            ft.Control.OnDecline += delegate(int friendnum, int filenum)
            {
                if (ft.Thread != null)
                {
                    ft.Thread.Abort();
                    ft.Thread.Join();
                }

                if (ft.Stream != null)
                    ft.Stream.Close();

                if (!ft.IsSender)
                    tox.FileSendControl(ft.FriendNumber, 1, filenumber, ToxFileControl.Kill, new byte[0]);
                else
                    tox.FileSendControl(ft.FriendNumber, 0, filenumber, ToxFileControl.Kill, new byte[0]);
            };

            transfers.Add(ft);
        }

        private void ExecuteActionsOnNotifyIcon()
        {
            nIcon.Visible = this.ViewModel.Configuraion.HideInTray;
        }

        private void mv_Activated(object sender, EventArgs e)
        {
            this.nIcon.Icon = notifyIcon;
            this.ViewModel.HasNewMessage = false;
        }

        private void AccentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var theme = ThemeManager.DetectAppStyle(System.Windows.Application.Current);
            var accent = ThemeManager.GetAccent(((AccentColorMenuData)AccentComboBox.SelectedItem).Name);
            ThemeManager.ChangeAppStyle(System.Windows.Application.Current, accent, theme.Item1);
        }

        private void SettingsFlyout_IsOpenChanged(object sender, EventArgs e)
        {
            if (!SettingsFlyout.IsOpen)
            {
                ThemeManager.ChangeAppStyle(System.Windows.Application.Current, oldAccent, oldAppTheme);
            }
        }

        private void AppThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var theme = ThemeManager.DetectAppStyle(System.Windows.Application.Current);
            var appTheme = ThemeManager.GetAppTheme(((AppThemeMenuData)AppThemeComboBox.SelectedItem).Name);
            ThemeManager.ChangeAppStyle(System.Windows.Application.Current, theme.Item2, appTheme);
        }

        private void SerchTextBoxChanged(object sender, KeyEventArgs e)
        {
            var senderTextbox = (TextBox)sender;

            foreach (var contact in this.ViewModel.ChatCollection.Where(v => v.Name.ToLower().Contains(senderTextbox.Text.ToLower())))
            {
                contact.Visible = true;
            }
            foreach (var contact in this.ViewModel.ChatCollection.Where(v => !v.Name.ToLower().Contains(senderTextbox.Text.ToLower())))
            {
                contact.Visible = false;
            }
        }
    }
}
