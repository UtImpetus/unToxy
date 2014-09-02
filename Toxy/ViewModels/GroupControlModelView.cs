﻿using System;
using System.Windows.Input;
using SharpTox.Core;
using Toxy.MVVM;
using System.Windows.Documents;
using System.IO;
using System.Windows.Markup;
using System.Reflection;
using Toxy.Utils;

namespace Toxy.ViewModels
{
    public class GroupControlModelView : ViewModelBase, IGroupObject
    {
        public Action<IGroupObject, bool> SelectedAction { get; set; }
        public Action<IGroupObject> DeleteAction { get; set; }
        public Action<IGroupObject> RenameAction { get; set; }

        public GroupControlModelView()
        {
            Document = UIHelpers.GetNewFlowDocument();
        }

        private ICommand deleteCommand;

        public ICommand DeleteCommand
        {
            get { return this.deleteCommand ?? (this.deleteCommand = new DelegateCommand(() => this.DeleteAction(this), () => DeleteAction != null)); }
        }

        private ICommand renameCommand;

        public ICommand RenameCommand
        {
            get { return this.renameCommand ?? (this.renameCommand = new DelegateCommand(() => this.RenameAction(this), () => RenameAction != null)); }
        }

        private bool selected;

        public bool Selected
        {
            get { return this.selected; }
            set
            {
                if (!Equals(value, this.Selected))
                {
                    this.selected = value;
                    this.OnPropertyChanged(() => this.Selected);
                }
                if (!value)
                {
                    this.HasNewMessage = false;
                }
                var action = this.SelectedAction;
                if (action != null)
                {
                    action(this, value);
                }
            }
        }

        private int chatNumber;

        public int ChatNumber
        {
            get { return this.chatNumber; }
            set
            {
                if (!Equals(value, this.ChatNumber))
                {
                    this.chatNumber = value;
                    this.OnPropertyChanged(() => this.ChatNumber);
                }
            }
        }

        private string name;

        public string Name
        {
            get { return this.name; }
            set
            {
                if (!Equals(value, this.Name))
                {
                    this.name = value;
                    this.OnPropertyChanged(() => this.Name);
                }
            }
        }

        private ToxUserStatus toxStatus;

        public ToxUserStatus ToxStatus
        {
            get { return this.toxStatus; }
            set
            {
                if (!Equals(value, this.ToxStatus))
                {
                    this.toxStatus = value;
                    this.OnPropertyChanged(() => this.ToxStatus);
                }
            }
        }

        private string statusMessage;

        public string StatusMessage
        {
            get { return this.statusMessage; }
            set
            {
                if (!Equals(value, this.StatusMessage))
                {
                    this.statusMessage = value;
                    this.OnPropertyChanged(() => this.StatusMessage);
                }
            }
        }

        private string additionalInfo;

        public string AdditionalInfo
        {
            get { return this.additionalInfo; }
            set
            {
                if (!Equals(value, this.AdditionalInfo))
                {
                    this.additionalInfo = value;
                    this.OnPropertyChanged(() => this.AdditionalInfo);
                }
            }
        }

        private bool hasNewMessage;

        public bool HasNewMessage
        {
            get { return this.hasNewMessage; }
            set
            {
                if (!Equals(value, this.HasNewMessage))
                {
                    this.hasNewMessage = value;
                    this.OnPropertyChanged(() => this.HasNewMessage);
                }
            }
        }

        private int newMessageCount;

        public int NewMessageCount
        {
            get { return this.newMessageCount; }
            set
            {
                if (!Equals(value, this.NewMessageCount))
                {
                    this.newMessageCount = value;
                    this.OnPropertyChanged(() => this.NewMessageCount);
                }
            }
        }


        public string PublicKey { get; set; }

        private FlowDocument document = new FlowDocument();

        public FlowDocument Document
        {
            get { return document; }
            set { document = value; }
        }

        
    }
}