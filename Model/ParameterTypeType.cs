using CreateKnxProd.Converter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateKnxProd.Model
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ParameterTypeType
    {
        [Description("Zahl")]
        Number,
        [Description("Fließkommazahl")]
        Float,
        [Description("Aufzählung")]
        Restriction,
        [Description("Text")]
        Text
    }

    public static class ParameterTypeTypeExtension
    {
        public static IEnumerable<ParameterTypeType> GetEnumTypes => Enum.GetValues(typeof(ParameterTypeType)).Cast<ParameterTypeType>();
    }
}
