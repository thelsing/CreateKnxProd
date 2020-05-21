using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace CreateKnxProd.Signing
{
    class HardwareSigner
    {
        public HardwareSigner(
             FileInfo hardwareFile,
             IDictionary<string, string> applProgIdMappings,
             IDictionary<string, string> applProgHashes,
             bool patchIds)
        {
            Assembly asm1 = Assembly.LoadFrom("C:\\Program Files (x86)\\ETS5\\Knx.Ets.XmlSigning.dll");
            Assembly asm2 = Assembly.LoadFrom("C:\\Program Files (x86)\\ETS5\\Knx.Ets.Xml.ObjectModel.dll");

            Type RegistrationKeyEnum = asm2.GetType("Knx.Ets.Xml.ObjectModel.RegistrationKey");
            object registrationKey = Enum.Parse(RegistrationKeyEnum, "knxconv");

            // registrationKey= Knx.Ets.Xml.ObjectModel.RegistrationKey.knxconv (is an enum)
            _instance = Activator.CreateInstance(asm1.GetType("Knx.Ets.XmlSigning.HardwareSigner"), hardwareFile, applProgIdMappings, applProgHashes, patchIds, registrationKey);
            _type = asm1.GetType("Knx.Ets.XmlSigning.HardwareSigner");
        }

        public void SignFile()
        {
            _type.GetMethod("SignFile", BindingFlags.Instance | BindingFlags.Public).Invoke(_instance, null);
        }

        private readonly object _instance;
        private readonly Type _type;

        public IDictionary<string, string> OldNewIdMappings
        {
            get
            {
                return (IDictionary<string, string>)_type.GetProperty("OldNewIdMappings", BindingFlags.Public | BindingFlags.Instance).GetValue(_instance);
            }
        }
    }
}
