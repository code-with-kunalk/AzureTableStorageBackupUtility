using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.StorageClient;
using System.Xml;
using System.Data.Services.Client;
using System.Xml.Linq;


namespace AzureStorageBackupUtility
{
    public class TableStorageHelper
    {
        private readonly string _sourceAccountName;
        private readonly string _sourceAccountKey;

        public TableStorageHelper()
        {
            _sourceAccountName = ConfigurationManager.AppSettings["SourceAccountName"];
            _sourceAccountKey = ConfigurationManager.AppSettings["SourceAccountKey"];
            GetStorageAccount();
        }

        public CloudStorageAccount StorageAccount { get; set; }

        public List<string> GetAllTables()
        {
            if (null == StorageAccount) StorageAccount = GetStorageAccount();
            return StorageAccount.CreateCloudTableClient().ListTables().ToList();
        }

        private CloudStorageAccount GetStorageAccount()
        {
            return new CloudStorageAccount(new StorageCredentialsAccountAndKey(_sourceAccountName, _sourceAccountKey), true);
        }

        public IEnumerable<TableGenericEntity> GetTableData(string tableName)
        {
            if (null == StorageAccount) GetStorageAccount();
            var tableClient = StorageAccount.CreateCloudTableClient();
            var tableContext = GetServiceContext(tableClient);
            var query = from entity in tableContext.CreateQuery<TableGenericEntity>(tableName) select entity;
            var allItemsQuery = query.AsTableServiceQuery();
            var entities = allItemsQuery.Execute();
            return entities;
        }

        private TableServiceContext GetServiceContext(CloudTableClient tableClient)
        {
            var tableServiceContext = tableClient.GetDataServiceContext();
            tableServiceContext.ResolveType += ResolveEntityType;
            tableServiceContext.ReadingEntity += new EventHandler<ReadingWritingEntityEventArgs>(OnReadingEntity);
            return tableServiceContext;
        }

        public Type ResolveEntityType(String name)
        {
            return typeof(TableGenericEntity);
        }

        public void OnReadingEntity(object sender, ReadingWritingEntityEventArgs args)
        {
            XNamespace atomNamespace = "http://www.w3.org/2005/Atom";
            XNamespace dataServiceNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices";
            XNamespace metadataNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";

            TableGenericEntity entity = args.Entity as TableGenericEntity;
            if (entity == null)
            {
                return;
            }

            // read each property, type and value in the payload   
            var properties = args.Entity.GetType().GetProperties();
            var q = from p in args.Data.Element(atomNamespace + "content")
                                    .Element(metadataNamespace + "properties")
                                    .Elements()
                    where properties.All(pp => pp.Name != p.Name.LocalName)
                    select new
                    {
                        Name = p.Name.LocalName,
                        IsNull = string.Equals("true", p.Attribute(metadataNamespace + "null") == null ? null : p.Attribute(metadataNamespace + "null").Value, StringComparison.OrdinalIgnoreCase),
                        TypeName = p.Attribute(metadataNamespace + "type") == null ? null : p.Attribute(metadataNamespace + "type").Value,
                        p.Value
                    };

            foreach (var dp in q)
            {
                entity[dp.Name] = new Property(dp.Name, dp.TypeName, GetTypedEdmValue(dp.TypeName, dp.Value, dp.IsNull));
            }
        }

        private static object GetTypedEdmValue(string type, string value, bool isnull)
        {
            if (isnull) return null;

            if (string.IsNullOrEmpty(type)) return value;

            switch (type)
            {
                case "Edm.String": return value;
                case "Edm.Byte": return Convert.ChangeType(value, typeof(byte));
                case "Edm.SByte": return Convert.ChangeType(value, typeof(sbyte));
                case "Edm.Int16": return Convert.ChangeType(value, typeof(short));
                case "Edm.Int32": return Convert.ChangeType(value, typeof(int));
                case "Edm.Int64": return Convert.ChangeType(value, typeof(long));
                case "Edm.Double": return Convert.ChangeType(value, typeof(double));
                case "Edm.Single": return Convert.ChangeType(value, typeof(float));
                case "Edm.Boolean": return Convert.ChangeType(value, typeof(bool));
                case "Edm.Decimal": return Convert.ChangeType(value, typeof(decimal));
                case "Edm.DateTime": return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind);
                case "Edm.Binary": return Convert.FromBase64String(value);
                case "Edm.Guid": return new Guid(value);

                default: throw new NotSupportedException("Not supported type " + type);
            }
        }
        
    }
}
