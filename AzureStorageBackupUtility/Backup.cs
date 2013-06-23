using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AzureStorageBackupUtility
{
    public class Backup
    {
        private TableStorageHelper TableStorageHelper { get; set; }
        private readonly Logging _logging;
        private readonly string _filePath;

        public Backup()
        {
            TableStorageHelper = new TableStorageHelper();
            _logging = new Logging();
            _logging.SourceClassName = "Backup";
            _filePath = ConfigurationManager.AppSettings["backupPath"] + DateTime.UtcNow.Date.ToString("yyyyMMdd");
        }

        private List<string> GetAllTables()
        {
            try
            {
                return TableStorageHelper.GetAllTables();
            }
            catch (Exception ex)
            {
                _logging.LogException("GetAllTables", ex, "");
                return null;
            }
        }

        public bool InitiateBackup()
        {
            var lstTables = GetAllTables();
            if (null == lstTables)
            {
                _logging.Log("InitiateBackup", "Tables could not be retrieved.", "");
                return false;
            }
            foreach (var table in lstTables)
            {
                CreateXml(table);
            }
            return true;
        }

        private void CreateXml(string tableName)
        {
            int count = 0;
            try
            {
                XmlWriter xw = XmlWriter.Create(File.CreateText(_filePath + "\\" + tableName + ".xml"));
                xw.WriteStartDocument(true);
                xw.WriteStartElement(tableName);

                var entites = TableStorageHelper.GetTableData(tableName);

                foreach (var entity in entites)
                {
                    xw.WriteStartElement("entity");

                    xw.WriteStartElement("PartitionKey");
                    xw.WriteValue(entity.PartitionKey);
                    xw.WriteEndElement();

                    xw.WriteStartElement("RowKey");
                    xw.WriteValue(entity.RowKey);
                    xw.WriteEndElement();

                    xw.WriteStartElement("Timestamp");
                    xw.WriteValue(entity.Timestamp);
                    xw.WriteEndElement();

                    foreach (var property in entity.Properties)
                    {
                        xw.WriteStartElement(property.Key);
                        if (property.Value.Type != "string")
                        {
                            xw.WriteStartAttribute("type");
                            xw.WriteValue(property.Value.Type);
                            xw.WriteEndAttribute();
                        }
                        if (property.Value.Value == null)
                        {
                            xw.WriteStartAttribute("null");
                            xw.WriteValue(true);
                            xw.WriteEndAttribute();
                        }
                        else
                        {
                            var value = property.Value.Value;
                            if (property.Value.Type.ToLowerInvariant().Equals("guid"))
                                value = value.ToString();
                            xw.WriteValue(value);
                        }
                        xw.WriteEndElement();
                    }
                    xw.WriteEndElement();
                    count++;
                }
                xw.WriteEndElement();
                xw.WriteEndDocument();
                xw.Close();
                _logging.LogCompletion(tableName, count, true);
            }
            catch (Exception ex)
            {
                _logging.LogException("CreateXml", ex, String.Format("Table Name: {0}", tableName));
                _logging.LogCompletion(tableName, count, false);
            }
        }
    }
}
