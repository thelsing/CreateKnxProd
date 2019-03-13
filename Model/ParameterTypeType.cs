using CreateKnxProd.Converter;
using CreateKnxProd.Properties;
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
        [Description("Number")]
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
        public static IEnumerable<Enable_T> GetEnumEnable_T =>
            Enum.GetValues(typeof(Enable_T)).Cast<Enable_T>();
        public static IEnumerable<ComObjectSize_T> GetEnumComObjectSize_T =>
            Enum.GetValues(typeof(ComObjectSize_T)).Cast<ComObjectSize_T>();
        public static IEnumerable<ComObjectPriority_T> GetEnumComObjectPriority_T =>
            Enum.GetValues(typeof(ComObjectPriority_T)).Cast<ComObjectPriority_T>();
    }
}
