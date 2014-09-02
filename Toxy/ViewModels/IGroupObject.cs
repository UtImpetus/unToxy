using System;
using System.Windows.Documents;

namespace Toxy.ViewModels
{
    public interface IGroupObject : IChatObject
    {
        Action<IGroupObject, bool> SelectedAction { get; set; }
        Action<IGroupObject> DeleteAction { get; set; }
        Action<IGroupObject> RenameAction { get; set; }
        FlowDocument Document { get; set; }
    }
}