using System;
using System.Reflection;
using System.IO;

namespace CreateKnxProd.Signing
{
    class XmlSigning
    {
        public static void SignDirectory(
            string path,
            bool useCasingOfBaggagesXml = false,
            string[] excludeFileEndings = null)
        {
            Assembly asm = Assembly.LoadFrom("C:\\Program Files (x86)\\ETS5\\Knx.Ets.XmlSigning.dll");

            Type ds = asm.GetType("Knx.Ets.XmlSigning.XmlSigning");

            ds.GetMethod("SignDirectory", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { path, useCasingOfBaggagesXml, excludeFileEndings });
        }
    }
}
