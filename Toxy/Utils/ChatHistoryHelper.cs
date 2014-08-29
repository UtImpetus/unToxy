using Community.CsharpSqlite.SQLiteClient;
using SharpTox.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toxy.Common;

namespace Toxy.Utils
{
    public static class ChatHistoryHelper
    {
        static Object sync = new object();
        static string dbFileName = "base.sqlite";
        static SqliteConnection db = null;

        public static void InitLogDatabase()
        {
            try
            {
                if (!File.Exists(dbFileName))
                {
                    db = new SqliteConnection(string.Format("Version=3,uri=file:{0}", dbFileName));
                    db.Open();
                    IDbCommand cmd = db.CreateCommand();
                    cmd.CommandText = "CREATE TABLE ChatLog(PublicKey TEXT,FromUser TEXT,Message TEXT, TimeStamp DATETIME )";
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    db = new SqliteConnection(string.Format("Version=3,uri=file:{0}", dbFileName));
                    db.Open();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public static void AddLineToHistory(string publicKey, string from, string message)
        {
            try
            {
                IDbCommand cmd = db.CreateCommand();
                cmd.CommandText = "INSERT INTO ChatLog  ( PublicKey,FromUser,Message, TimeStamp) VALUES (@PublicKey,@From,@Message,@TimeStamp)";
                cmd.Parameters.Add(new SqliteParameter { ParameterName = "@TimeStamp", Value = DateTime.Now });
                cmd.Parameters.Add(new SqliteParameter { ParameterName = "@PublicKey", Value = publicKey });
                cmd.Parameters.Add(new SqliteParameter { ParameterName = "@From", Value = from });
                cmd.Parameters.Add(new SqliteParameter { ParameterName = "@Message", Value = message });
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        internal static void FillRecentHistory(Tox tox, System.Windows.Documents.FlowDocument flowDocument, string publicKey)
        {
            if (flowDocument.Tag is string) return;
            lock (sync)
            {
                try
                {
                    IDbCommand cmd = db.CreateCommand();
                    cmd.CommandText = "SELECT * FROM ChatLog WHERE PublicKey = @PublicKey ORDER BY TimeStamp DESC LIMIT 20";
                    cmd.Parameters.Add(new SqliteParameter { ParameterName = "@PublicKey", Value = publicKey });
                    IDataReader reader = cmd.ExecuteReader();
                    var logItems = new List<ChatLogItem>();
                    while (reader.Read())
                    {
                        var logitem = new ChatLogItem();
                        logitem.From = reader.GetString(reader.GetOrdinal("FromUser"));
                        logitem.PublicKey = reader.GetString(reader.GetOrdinal("PublicKey"));
                        logitem.Message = reader.GetString(reader.GetOrdinal("Message"));
                        logItems.Insert(0,logitem);
                    }
                                        
                    foreach (var item in logItems)
                    {
                        flowDocument.AddNewMessageRow(tox, new MessageData() { Username = item.From, IsAction = false, IsSelf = false, Message = item.Message }, false);
                    }
                    flowDocument.Tag = "histoy updated";
                }
                catch (Exception e)
                {
                    Logger.LogException(e);
                }
            }
        }

        public static void Close()
        {
            db.Close();
            db.Dispose();
        }
    }
}
