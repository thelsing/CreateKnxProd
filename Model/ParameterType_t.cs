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
    public partial class ParameterType_T : object, System.ComponentModel.INotifyPropertyChanged
    {
        [XmlIgnore]
        public ParameterTypeType Type
        {
            get
            {
                if (TypeFloat != null)
                {
                    return ParameterTypeType.Float;
                }

                if (TypeNumber != null)
                {
                    return ParameterTypeType.Number;
                }

                if (TypeRestriction != null)
                {
                    return ParameterTypeType.Restriction;
                }

                if (TypeText != null)
                {
                    return ParameterTypeType.Text;
                }

                throw new InvalidOperationException("Item has an invalid value");
            }
            set
            {
                PropertyChangedEventHandler eh = (s, e) =>
                {
                    if (e.PropertyName == nameof(SizeInByte))
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SizeInByte)));
                };

                TypeNumber = null;
                TypeFloat = null;
                TypeRestriction = null;
                TypeText = null;

                switch (value)
                {
                    case ParameterTypeType.Number:
                        TypeNumber = new ParameterType_TTypeNumber() { MinInclusive = 0, MaxInclusive = 50 , SizeInBit=32 };
                        TypeNumber.PropertyChanged += eh;
                        break;
                    case ParameterTypeType.Float:
                        TypeFloat = new ParameterType_TTypeFloat() { MinInclusive = 0, MaxInclusive = 50, Encoding=ParameterType_TTypeFloatEncoding.IEEE_754_Single };
                        TypeFloat.PropertyChanged += eh;
                        break;
                    case ParameterTypeType.Restriction:
                        TypeRestriction = new ParameterType_TTypeRestriction() { Base = ParameterType_TTypeRestrictionBase.Value, SizeInBit = 8 };
                        TypeRestriction.PropertyChanged += eh;
                        break;
                    case ParameterTypeType.Text:
                        TypeText = new ParameterType_TTypeText() { SizeInByte = 50 };
                        TypeText.PropertyChanged += eh;
                        break;
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SizeInByte)));
            }
        }

        [XmlIgnore]
        public uint SizeInByte
        {
            get
            {
                if (TypeNumber != null)
                {
                    return TypeNumber.SizeInByte;
                }

                if (TypeFloat != null)
                {
                    return TypeFloat.SizeInByte;
                }

                if (TypeRestriction != null)
                {
                    return TypeRestriction.SizeInByte;
                }

                if (TypeText != null)
                {
                    return TypeText.SizeInByte;
                }

                return 0;
            }
        }
    }
}
