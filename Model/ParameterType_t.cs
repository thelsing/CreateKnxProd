using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CreateKnxProd.Model
{
    public partial class ParameterType_t : object, System.ComponentModel.INotifyPropertyChanged
    {
        [XmlIgnore]
        public ParameterTypeType Type
        {
            get
            {
                if (Item is ParameterType_tTypeFloat)
                {
                    return ParameterTypeType.Float;
                }

                if (Item is ParameterType_tTypeNumber)
                {
                    return ParameterTypeType.Number;
                }

                if (Item is ParameterType_tTypeRestriction)
                {
                    return ParameterTypeType.Restriction;
                }

                if (Item is ParameterType_tTypeText)
                {
                    return ParameterTypeType.Text;
                }

                throw new InvalidOperationException("Item has an invalid value");
            }
            set
            {
                switch (value)
                {
                    case ParameterTypeType.Number:
                        Item = new ParameterType_tTypeNumber() { minInclusive = 0, maxInclusive = 50 , SizeInBit=32 };
                        break;
                    case ParameterTypeType.Float:
                        Item = new ParameterType_tTypeFloat() { minInclusive = 0, maxInclusive = 50, Encoding=ParameterType_tTypeFloatEncoding.IEEE754Single };
                        break;
                    case ParameterTypeType.Restriction:
                        Item = new ParameterType_tTypeRestriction() { Base = ParameterType_tTypeRestrictionBase.Value, SizeInBit = 8 };
                        break;
                    case ParameterTypeType.Text:
                        Item = new ParameterType_tTypeText() { SizeInByte = 50 };
                        break;
                }
                RaisePropertyChanged(nameof(SizeInByte));
            }
        }

        [XmlIgnore]
        public uint SizeInByte
        {
            get
            {
                if (Item is IGetByteSize igetsize)
                {
                    return igetsize.SizeInByte;
                }

                return 0;
            }
        }

        partial void OnCreated()
        {
            PropertyChangedEventHandler eh = (s, e) =>
            {
                if (e.PropertyName == "SizeInByte")
                    RaisePropertyChanged(e.PropertyName);
            };
            this.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName != "Item")
                    return;

                if (Item != null)
                    ((INotifyPropertyChanged)Item).PropertyChanged += eh;
            };
            this.PropertyChanging += (sender, e) =>
            {
                if (Item != null)
                    ((INotifyPropertyChanged)Item).PropertyChanged -= eh;
            };
        }
    }
}
