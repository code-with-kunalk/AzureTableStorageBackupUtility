using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureStorageBackupUtility
{
    public class TableGenericEntity : TableServiceEntity
    {
        public TableGenericEntity()
        {

        }

        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTime TimeStamp { get; set; }

        Dictionary<string, Property> _properties = new Dictionary<string, Property>();
        public Dictionary<string, Property> Properties
        {
            get
            {
                return _properties;
            }
            set
            {
                _properties = value;
            }
        }


        internal Property this[string key]
        {
            get
            {
                return this._properties[key];
            }

            set
            {
                this._properties[key] = value;
            }
        }
    }

    public class Property
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public string Type { get; set; }

        public Property(string name, string type, object value)
        {
            this.Name = name;
            this.Type = StandardTypeName(type);
            this.Value = value;
        }

        public string StandardTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return "string";

            switch (typeName.ToLower().Replace("edm.", String.Empty))
            {
                default:
                    return "string";
                case "string":
                case "edm.string":
                    return "string";
                case "byte":
                    return "byte";
                case "sbyte":
                    return "sbyte";
                case "int":
                case "int32":
                    return "int";
                case "int16":
                    return "int16";
                case "int64":
                    return "int64";
                case "double":
                    return "double";
                case "single":
                    return "single";
                case "bool":
                case "boolean":
                    return "bool";
                case "decimal":
                    return "decimal";
                case "datetime":
                    return "datetime";
                case "binary":
                    return "binary";
                case "guid":
                    return "guid";
            }
        }
    }
}
