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

    public static class EnumExtension
    {
        public static IEnumerable<ParameterTypeType> GetEnumParameterTypTypes => 
            Enum.GetValues(typeof(ParameterTypeType)).Cast<ParameterTypeType>();
        public static IEnumerable<Enable_t> GetEnumEnable_t =>
            Enum.GetValues(typeof(Enable_t)).Cast<Enable_t>();
        public static IEnumerable<ComObjectSize_t> GetEnumComObjectSize_t =>
            Enum.GetValues(typeof(ComObjectSize_t)).Cast<ComObjectSize_t>();
        public static IEnumerable<ComObjectPriority_t> GetEnumComObjectPriority_t =>
            Enum.GetValues(typeof(ComObjectPriority_t)).Cast<ComObjectPriority_t>();
    }
}
