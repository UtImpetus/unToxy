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
        static Dictionary<string, DateTime> lastHistoryMessageTimeStampForPublicKey = new Dictionary<string, DateTime>();

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
            if (string.IsNullOrEmpty(publicKey)) return;
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

        internal static IList<ChatLogItem> GetRecentHistory(Tox tox, string publicKey)
        {
            var result = new List<ChatLogItem>();
            if (lastHistoryMessageTimeStampForPublicKey.Any(v=>v.Key==publicKey)) return result;
            lock (sync)
            {
                try
                {
                    IDbCommand cmd = db.CreateCommand();
                    cmd.CommandText = "SELECT * FROM ChatLog WHERE PublicKey = @PublicKey ORDER BY TimeStamp LIMIT @limit";
                    cmd.Parameters.Add(new SqliteParameter { ParameterName = "@PublicKey", Value = publicKey });
                    cmd.Parameters.Add(new SqliteParameter { ParameterName = "@limit", Value = Constants.CountHistoryItemsPreload });
                    IDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var logitem = new ChatLogItem();
                        logitem.From = reader.GetString(reader.GetOrdinal("FromUser"));
                        logitem.PublicKey = reader.GetString(reader.GetOrdinal("PublicKey"));
                        logitem.Message = reader.GetString(reader.GetOrdinal("Message"));
                        logitem.TimeStamp = reader.GetDateTime(reader.GetOrdinal("TimeStamp"));
                        result.Insert(0, logitem);
                    }

                    var lastItem = result.FirstOrDefault();
                    if(lastItem!=null)
                    {
                        lastHistoryMessageTimeStampForPublicKey.Add(publicKey, lastItem.TimeStamp);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogException(e);
                }
            }
            return result;
        }

        public static void Close()
        {
            db.Close();
            db.Dispose();
        }
    }
}
