using System.Xml.Serialization;

namespace ReswareConnectorWeb.Models
{
    [XmlRoot(ElementName = "FileCustomFields", Namespace = "http://schemas.datacontract.org/2004/07/Adeptive.Reset.API.CustomFields")]
    public class FileCustomFields
    {
        [XmlArray(ElementName = "CustomFields")]
        [XmlArrayItem(ElementName = "CustomField", Namespace = "")]
        public List<CustomField> CustomFields { get; set; }

        [XmlElement(ElementName = "DocumentID", IsNullable = true)]
        public string DocumentID { get; set; }
    }

    public class CustomField
    {
        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "Value")]
        public string Value { get; set; }
    }
}
