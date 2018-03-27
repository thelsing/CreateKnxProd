using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CreateKnxProd.Model
{
    public partial class ParameterType_tTypeText : INotifyPropertyChanged, IGetByteSize
    {
        [XmlIgnore]
        public uint SizeInByte
        {
            get
            {
                uint size = SizeInBit / 8;
                if (SizeInBit % 8 != 0)
                    size += 1;

                return size;
            }

            set
            {
                SizeInBit = value * 8;
                RaisePropertyChanged(nameof(SizeInByte));
            }
        }
    }
}
