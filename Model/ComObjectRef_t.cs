using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CreateKnxProd.Model
{
    public partial class ComObjectRef_T : INotifyPropertyChanged
    {
        [XmlIgnore]
        public ComObject_T ComObject { get; set; }
    }
}
