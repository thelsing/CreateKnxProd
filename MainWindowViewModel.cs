using CreateKnxProd.Model;
using CreateKnxProd.Properties;
using CreateKnxProd.Signing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Input;
using System.Xml.Serialization;
using System.IO.Compression;

namespace CreateKnxProd
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private RelayCommand _exportCommand;
        private RelayCommand _createNewCommand;
        private RelayCommand _openCommand;
        private RelayCommand _saveCommand;
        private RelayCommand _saveAsCommand;
        private RelayCommand _closeCommand;

        private IDialogService _dialogService;

        private const string _toolName = "KNX MT";
        private const string _toolVersion = "5.1.255.16695";

        private string _openFile = null;
        private KNX _model = null;

        private const string _fileExtension = ".xml";
        private readonly string _fileFilter = Ressources.XMLFiles;

        private ManufacturerData_TManufacturer _manufacturerData;
        private Hardware_T _hardware;
        private Hardware_TProductsProduct _product;
        private CatalogSection_T _catalogSection;
        private CatalogSection_TCatalogItem _catalogItem;
        private ApplicationProgram_T _applicationProgram;
        private Hardware2Program_T _hardware2Program;
        private ApplicationProgramRef_T _appProgRef;
        private ApplicationProgramStatic_TCodeRelativeSegment _codeSegment;

        public MainWindowViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            _exportCommand = new RelayCommand(o => _model != null, Export);
            _saveCommand = new RelayCommand(o => _model != null, Save);
            _saveAsCommand = new RelayCommand(o => _model != null, SaveAs);
            _openCommand = new RelayCommand(o => true, Open);
            _createNewCommand = new RelayCommand(o => true, CreateNew);
            _closeCommand = new RelayCommand(o => _model != null, Close);

            new Thread(DownloadKnxMasterXml).Start();
        }

        private void DownloadKnxMasterXml()
        {
            try
            {
                if (!File.Exists("knx_master.xml"))
                {
                    var client = new WebClient();
                    client.DownloadFile("https://update.knx.org/data/XML/project-20/knx_master.xml", "knx_master.xml");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(ex.ToString());
            }
        }

        private void CreateDefaultParameterBlock()
        {
            var parameterBlock = new ComObjectParameterBlock_T();
            parameterBlock.Name = "ParameterPage";
            parameterBlock.Text = Ressources.CommonParameters;
            ParameterBlock.Add(parameterBlock);
        }

        private void Open(object param)
        {
            try
            {
                if (_model != null)
                {
                    var cancel = AskSaveCancel();
                    if (cancel)
                        return;
                }

                var filePath = _dialogService.ChooseFileToOpen(_fileExtension, _fileFilter);
                if (filePath == null)
                    return;

                _openFile = filePath;

                XmlSerializer serializer = new XmlSerializer(typeof(KNX));
                using (var reader = new StreamReader(_openFile))
                {
                    _model = (KNX)serializer.Deserialize(reader);
                }

                if(File.Exists("knx_master.xml"))
                {
                    using (var reader = new StreamReader("knx_master.xml"))
                    {
                        var masterData = (KNX)serializer.Deserialize(reader);
                        _model.MasterData = masterData.MasterData;
                    }
                }

                _manufacturerData = _model.ManufacturerData.First();
                _hardware = _manufacturerData.Hardware.First();
                _product = _hardware.Products.First();
                _catalogSection = _manufacturerData.Catalog.First();
                _catalogItem = _catalogSection.CatalogItem.First();
                _hardware2Program = _hardware.Hardware2Programs.First();
                _applicationProgram = _manufacturerData.ApplicationPrograms.First();
                _appProgRef = _hardware2Program.ApplicationProgramRef.First();
                _codeSegment = _applicationProgram.Static.Code.RelativeSegment.First();

                Parameters.Clear();
                foreach (var item in _applicationProgram.Static.Parameters.Parameter)
                {
                    item.AllTypes = ParameterTypes;
                    item.Type = ParameterTypes.First(t => t.Id == item.ParameterType);
                    item.AllBlocks = ParameterBlock;
                    item.Block = ParameterBlock.FirstOrDefault(t => t.ParameterRefRef.FirstOrDefault(o => _applicationProgram.Static.ParameterRefs.FirstOrDefault(p => o.RefId == p.Id)?.RefId == item.Id) != null);
                    if (item.Block == null)
                    {
                        if (ParameterBlock.Count == 0)
                        {
                            CreateDefaultParameterBlock();
                        }
                        item.Block = ParameterBlock.First();
                    }
                    Parameters.Add(item);
                }

                RaiseChanged();
            }
            catch (Exception ex)
            {
                ClearData();
                _dialogService.ShowMessage(ex.ToString());
            }
        }

        private void SaveAs(object param)
        {
            try
            {
                if (_model == null)
                    return;

                var filepath = _dialogService.ChooseSaveFile(_fileExtension, _fileFilter);
                if (filepath == null)
                    return;

                _openFile = filepath;
                Save(param);
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(ex.ToString());
            }
        }

        private void Save(object param)
        {
            try
            {
                if (_model == null)
                    return;

                if (_openFile == null)
                    SaveAs(param);

                if (string.IsNullOrWhiteSpace(_applicationProgram.ReplacesVersions))
                    _applicationProgram.ReplacesVersions = null;

                _product.RegistrationInfo = new RegistrationInfo_T() { RegistrationStatus = RegistrationStatus_T.Registered };
                _hardware2Program.RegistrationInfo = new RegistrationInfo_T()
                {
                    RegistrationStatus = RegistrationStatus_T.Registered,
                    RegistrationNumber = "0001/" + _hardware.VersionNumber.ToString() + _applicationProgram.ApplicationVersion
                };

                UpdateMediumInfo();
                HandleParameters();
                RegenerateDynamic();
                CreateLoadProcedures();
                HandleComObjects();
                CorrectIds();

                // remove masterdata temporaly to avoid writing it to file
                var masterData = _model.MasterData;
                _model.MasterData = null;

                XmlSerializer serializer = new XmlSerializer(typeof(KNX));
                using (var xmlWriter = new StreamWriter(_openFile, false, Encoding.UTF8))
                {
                    serializer.Serialize(xmlWriter, _model);
                }
                _model.MasterData = masterData;

                _dialogService.ShowMessage(Ressources.SaveSuccess);
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(ex.ToString());
            }
        }

        private void UpdateMediumInfo()
        {
            if (_hardware2Program.MediumTypes[0] == "MT-5")
            {
                _applicationProgram.MaskVersion = "MV-57B0";
                _hardware.IsIPEnabled = true;
                _hardware.BusCurrent = null;
            }
            else if (_hardware2Program.MediumTypes[0] == "MT-2")
            {
                _applicationProgram.MaskVersion = "MV-27B0";
                _hardware.IsIPEnabled = false;
                _hardware.BusCurrent = null;
            }
            else
            {
                _applicationProgram.MaskVersion = "MV-07B0";
                _hardware.IsIPEnabled = false;
                _hardware.BusCurrent = 10;
            }
        }

        private void HandleComObjects()
        {
            var appStatic = _applicationProgram.Static;
            appStatic.ComObjectRefs.Clear();

            uint i = 1;
            foreach (var item in appStatic.ComObjectTable.ComObject)
            {
                item.Number = i++;
                if (item.Text.Length <= 50)
                    item.Name = item.Text;
                else
                    item.Name = item.Text.Substring(0, 50);
                item.ReadOnInitFlag = Enable_T.Disabled;
                appStatic.ComObjectRefs.Add(new ComObjectRef_T() { ComObject = item });

            }
        }

        private void CreateLoadProcedures()
        {
            var ldProc1 = new LoadProcedures_TLoadProcedure();
            ldProc1.MergeId = 2;

            var ldCtrlCreate = new LdCtrlRelSegment_T();
            ldCtrlCreate.LsmIdx = 4;
            ldCtrlCreate.Mode = 1;
            ldCtrlCreate.Fill = 0;
            ldCtrlCreate.AppliesTo = LdCtrlProcType_T.Full;
            ldCtrlCreate.Size = _codeSegment.Size;
            ldProc1.LdCtrlRelSegment.Add(ldCtrlCreate);

            var ldCtrlUpdate = new LdCtrlRelSegment_T();
            ldCtrlUpdate.LsmIdx = 4;
            ldCtrlUpdate.Mode = 0;
            ldCtrlUpdate.Fill = 0;
            ldCtrlUpdate.AppliesTo = LdCtrlProcType_T.Par;
            ldCtrlUpdate.Size = _codeSegment.Size;
            ldProc1.LdCtrlRelSegment.Add(ldCtrlUpdate);

            var ldProc2 = new LoadProcedures_TLoadProcedure();
            ldProc2.MergeId = 4;

            var ldCtrlWrite = new LdCtrlWriteRelMem_T();
            ldCtrlWrite.ObjIdx = 4;
            ldCtrlWrite.Offset = 0;
            ldCtrlWrite.Verify = true;
            ldCtrlWrite.AppliesTo = LdCtrlProcType_T.FullCommaPar;
            ldCtrlWrite.Size = _codeSegment.Size;
            ldProc2.LdCtrlWriteRelMem.Add(ldCtrlWrite);

            var ldProc3 = new LoadProcedures_TLoadProcedure();
            ldProc3.MergeId = 7;

            var ldCtrlImageProp = new LdCtrlLoadImageProp_T();
            ldCtrlImageProp.ObjIdx = 4;
            ldCtrlImageProp.PropId = 27;
            ldProc3.LdCtrlLoadImageProp.Add(ldCtrlImageProp);

            var appStatic = _applicationProgram.Static;
            appStatic.LoadProcedures.Clear();
            appStatic.LoadProcedures.Add(ldProc1);
            appStatic.LoadProcedures.Add(ldProc2);
            appStatic.LoadProcedures.Add(ldProc3);
        }

        private void HandleParameters()
        {
            var appStatic = _applicationProgram.Static;
            appStatic.Parameters.Parameter.Clear();
            appStatic.ParameterRefs.Clear();

            uint offset = 0;
            uint size = 0;
            foreach (var item in Parameters)
            {
                appStatic.Parameters.Parameter.Add(item);
                item.Memory = new MemoryParameter_T() { Offset = offset, BitOffset = 0 };
                offset += item.Type.SizeInByte;
                size += item.Type.SizeInByte;

                appStatic.ParameterRefs.Add(new ParameterRef_T() { Parameter = item });
            }
            _codeSegment.Size = size;
        }

        private void RegenerateDynamic()
        {
            if (_applicationProgram.Dynamic == null)
                _applicationProgram.Dynamic = new ApplicationProgramDynamic_T();

            var appDynamic = _applicationProgram.Dynamic;

            var appStatic = _applicationProgram.Static;
            appDynamic.Choose?.Clear();
            appDynamic.Channel?.Clear();

            if (ParameterBlock.Count == 0)
            {
                CreateDefaultParameterBlock();
            }
            int i = 1;
            foreach (var parameterBlock in ParameterBlock)
            {
                parameterBlock.ParameterRefRef.Clear();
                foreach (var paramRef in appStatic.ParameterRefs)
                {
                    if ((i == 1 && paramRef.Parameter.Block == null) || paramRef.Parameter.Block == parameterBlock)
                        parameterBlock.ParameterRefRef.Add(new ParameterRefRef_T() { ParameterRef = paramRef });
                }

                parameterBlock.ComObjectRefRef.Clear();
                if (i == 1)     // Not supported yet, ComObjs are affected to the first ParameterBlock
                    foreach (var comObjRef in appStatic.ComObjectRefs)
                    {
                        parameterBlock.ComObjectRefRef.Add(new ComObjectRefRef_T() { ComObjectRef = comObjRef });
                    }
                ++i;
            }
        }

        private bool AskSaveCancel()
        {
            var result = _dialogService.Ask(Ressources.AskSave);
            if (result == null)
                return true;

            if (result.Value)
                Save(null);

            return false;
        }

        private void ClearData()
        {
            _model = null;
            _openFile = null;
            _manufacturerData = null;
            _hardware = null;
            _product = null;
            _catalogItem = null;
            _catalogSection = null;
            _applicationProgram = null;
            _hardware2Program = null;
            _appProgRef = null;
            _codeSegment = null;
            Parameters.Clear();
            RaiseChanged();
        }

        private void Close(object param)
        {
            try
            {
                if (_model == null)
                    return;

                var cancel = AskSaveCancel();
                if (cancel)
                    return;

                ClearData();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(ex.ToString());
            }
        }

        private void CreateNew(object param)
        {
            try
            {
                if (_model != null)
                {
                    var cancel = AskSaveCancel();
                    if (cancel)
                        return;
                }

                _model = new KNX();

                string lang = Thread.CurrentThread.CurrentCulture.Name;

                _manufacturerData = new ManufacturerData_TManufacturer();
                _applicationProgram = new ApplicationProgram_T();
                _hardware = new Hardware_T();
                _catalogSection = new CatalogSection_T();
                _product = new Hardware_TProductsProduct();
                _hardware2Program = new Hardware2Program_T();
                _appProgRef = new ApplicationProgramRef_T();
                _catalogItem = new CatalogSection_TCatalogItem();

                _model.ManufacturerData.Add(_manufacturerData);
                _manufacturerData.Catalog.Add(_catalogSection);
                _manufacturerData.ApplicationPrograms.Add(_applicationProgram);
                _manufacturerData.Hardware.Add(_hardware);
                _hardware.Products.Add(_product);
                _hardware.Hardware2Programs.Add(_hardware2Program);
                _hardware2Program.ApplicationProgramRef.Add(_appProgRef);
                _catalogSection.CatalogItem.Add(_catalogItem);


                _model.CreatedBy = _toolName;
                _model.ToolVersion = _toolVersion;

                ApplicationNumber = 0;
                ApplicationVersion = 0;
                _applicationProgram.ProgramType = ApplicationProgramType_T.ApplicationProgram;
                _applicationProgram.MaskVersion = "MV-57B0";
                _applicationProgram.LoadProcedureStyle = LoadProcedureStyle_T.MergedProcedure;
                _applicationProgram.PeiType = 0;
                _applicationProgram.DefaultLanguage = lang;
                _applicationProgram.DynamicTableManagement = false;
                _applicationProgram.Linkable = false;
                _applicationProgram.MinEtsVersion = "5.0";
                _applicationProgram.ReplacesVersions = null;
                _applicationProgram.IsSecureEnabled = false;
                _applicationProgram.MaxSecurityIndividualAddressEntries = 32;
                _applicationProgram.MaxSecurityGroupKeyTableEntries = 50;

                var appStatic = new ApplicationProgramStatic_T();
                _applicationProgram.Static = appStatic;

                var code = new ApplicationProgramStatic_TCode();
                appStatic.Code = code;

                _codeSegment = new ApplicationProgramStatic_TCodeRelativeSegment();
                code.RelativeSegment.Add(_codeSegment);
                _codeSegment.Name = "Parameters";
                _codeSegment.Offset = 0;
                _codeSegment.LoadStateMachine = 4;
                _codeSegment.Size = 0;

                appStatic.Parameters = new ApplicationProgramStatic_TParameters();
                _applicationProgram.Dynamic = new ApplicationProgramDynamic_T();
                _applicationProgram.Dynamic.ChannelIndependentBlock.Add(new ChannelIndependentBlock_T());
                CreateDefaultParameterBlock();
                appStatic.AddressTable = new ApplicationProgramStatic_TAddressTable();
                appStatic.AddressTable.MaxEntries = ushort.MaxValue;

                appStatic.AssociationTable = new ApplicationProgramStatic_TAssociationTable();
                appStatic.AssociationTable.MaxEntries = ushort.MaxValue;
                appStatic.ComObjectTable = new ApplicationProgramStatic_TComObjectTable();
                appStatic.Options = new ApplicationProgramStatic_TOptions();

                HardwareSerial = "0";
                HardwareVersion = 0;
                _hardware.HasIndividualAddress = true;
                _hardware.HasApplicationProgram = true;
                _hardware.IsIPEnabled = true;
                _hardware.BusCurrent = 10;

                _product.IsRailMounted = false;
                _product.DefaultLanguage = lang;
                OrderNumber = "0";

                _hardware2Program.MediumTypes.Add("MT-5");
                _hardware2Program.MediumTypes.Add("MT-2");

                _catalogSection.Name = Ressources.Devices;
                _catalogSection.Number = "1";
                _catalogSection.DefaultLanguage = lang;

                _catalogItem.Name = _product.Text;
                _catalogItem.Number = 1;
                _catalogItem.ProductRefId = _product.Id;
                _catalogItem.Hardware2ProgramRefId = _hardware2Program.Id;
                _catalogItem.DefaultLanguage = lang;

                RaiseChanged();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(ex.ToString());
                ClearData();
            }
        }

        private void CorrectIds()
        {
            _manufacturerData.RefId = "M-00FA";
            _applicationProgram.Id = string.Format("{0}_A-{1:X04}-{2:X02}-0000", _manufacturerData.RefId,
                    _applicationProgram.ApplicationNumber, _applicationProgram.ApplicationVersion);
            _appProgRef.RefId = _applicationProgram.Id;
            _codeSegment.Id = string.Format("{0}_RS-{1:00}-{2:00000}", _applicationProgram.Id, _codeSegment.LoadStateMachine,
                _codeSegment.Offset);

            _hardware.Id = string.Format("{0}_H-{1}-{2}", _manufacturerData.RefId, _hardware.SerialNumber.Replace("-", ".2D"),
                                        _hardware.VersionNumber);
            _product.Id = string.Format("{0}_P-{1}", _hardware.Id, _product.OrderNumber.Replace("-", ".2D"));
            _hardware2Program.Id = string.Format("{0}_HP-{1:X04}-{2:X02}-0000", _hardware.Id,
                        _applicationProgram.ApplicationNumber, _applicationProgram.ApplicationVersion);

            _catalogSection.Id = string.Format("{0}_CS-{1}", _manufacturerData.RefId, _catalogSection.Number);

            _catalogItem.Id = string.Format("{0}_CI-{1}-{2}", _hardware2Program.Id, _product.OrderNumber.Replace("-", ".2D"), _catalogItem.Number);
            _catalogItem.ProductRefId = _product.Id;
            _catalogItem.Hardware2ProgramRefId = _hardware2Program.Id;

            var appStatic = _applicationProgram.Static;

            foreach (var paraType in appStatic.ParameterTypes)
            {
                paraType.Id = string.Format("{0}_PT-{1}", _applicationProgram.Id, paraType.Name);

                var paraTypeRestriction = paraType.TypeRestriction;
                if (paraTypeRestriction == null)
                    continue;

                foreach (var paraEnum in paraTypeRestriction.Enumeration)
                    paraEnum.Id = string.Format("{0}_EN-{1}", paraType.Id, paraEnum.Value);
            }

            int i = 1;
            foreach (var parameter in appStatic.Parameters.Parameter)
            {
                parameter.ParameterType = parameter.Type.Id;
                parameter.Id = string.Format("{0}_P-{1}", _applicationProgram.Id, i++);
                var memory = parameter.Memory;
                if (memory != null)
                {
                    memory.CodeSegment = _codeSegment.Id;
                }
            }

            i = 1;
            foreach (var parameterRef in appStatic.ParameterRefs)
            {
                parameterRef.Id = string.Format("{0}_R-{1}", parameterRef.Parameter.Id, i++);
                parameterRef.RefId = parameterRef.Parameter.Id;
            }

            foreach (var comObj in appStatic.ComObjectTable.ComObject)
            {
                comObj.Id = $"{_applicationProgram.Id}_O-{comObj.Number}";
            }

            i = 1;
            foreach (var comObjRef in appStatic.ComObjectRefs)
            {
                comObjRef.Id = $"{comObjRef.ComObject.Id}_R-{i++}";
                comObjRef.RefId = comObjRef.ComObject.Id;
            }

            i = 1;
            foreach (var parameterBlock in _applicationProgram.Dynamic.ChannelIndependentBlock.First().ParameterBlock)
            {
                parameterBlock.Id = string.Format("{0}_PB-{1}", _applicationProgram.Id, i++);
                foreach (var item in parameterBlock.ParameterRefRef)
                    item.RefId = item.ParameterRef.Id;

                foreach (var item in parameterBlock.ComObjectRefRef)
                    item.RefId = item.ComObjectRef.Id;
            }
        }

        private void Export(object param)
        {
            try
            {
                if (_model == null)
                    return;

                var cancel = AskSaveCancel();
                if (cancel)
                    return;

                // Remove all data secure related attributes
                if (_model.ManufacturerData.First().ApplicationPrograms.First().IsSecureEnabled == false)
                {
                    _model.ManufacturerData.First().ApplicationPrograms.First().MaxSecurityIndividualAddressEntries = 0;
                    _model.ManufacturerData.First().ApplicationPrograms.First().MaxSecurityGroupKeyTableEntries = 0;
                }

                var files = new string[] { _openFile };

                var outputFile = _dialogService.ChooseSaveFile(".knxprod", "KNXProd|*.knxprod");
                if (outputFile == null)
                    return;

                string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectory);

                File.Copy("knx_master.xml", Path.Combine(tempDirectory, "knx_master.xml"));

                string mfid = _model.ManufacturerData.First().RefId;
                Directory.CreateDirectory(Path.Combine(tempDirectory, mfid));

                KNX document = new KNX();
                ManufacturerData_TManufacturer manufacturerData = new ManufacturerData_TManufacturer();

                document.CreatedBy = _model.CreatedBy;
                document.ToolVersion = _model.ToolVersion;
                document.ManufacturerData.Add(manufacturerData);
                document.ManufacturerData.First().RefId = _model.ManufacturerData.First().RefId;

                manufacturerData.Catalog.Add(_model.ManufacturerData.First().Catalog.First());

                XmlSerializer serializer = new XmlSerializer(typeof(KNX));

                using (var xmlWriter = new StreamWriter(Path.Combine(tempDirectory, mfid, "Catalog.xml"), false, Encoding.UTF8))
                {
                    serializer.Serialize(xmlWriter, document);
                }

                manufacturerData.Catalog.Clear();
                manufacturerData.ApplicationPrograms.Add(_model.ManufacturerData.First().ApplicationPrograms.First());
                string applicationProgramFilename = _model.ManufacturerData.First().ApplicationPrograms.First().Id + ".xml";

                using (var xmlWriter = new StreamWriter(Path.Combine(tempDirectory, mfid, applicationProgramFilename), false, Encoding.UTF8))
                {
                    serializer.Serialize(xmlWriter, document);
                }

                manufacturerData.ApplicationPrograms.Clear();
                manufacturerData.Hardware.Add(_model.ManufacturerData.First().Hardware.First());

                using (var xmlWriter = new StreamWriter(Path.Combine(tempDirectory, mfid, "Hardware.xml"), false, Encoding.UTF8))
                {
                    serializer.Serialize(xmlWriter, document);
                }

                // Sign ApplicationProgram XML file and patch RefIds in ApplicationProgram XML file
                IDictionary<string, string> applProgIdMappings = new Dictionary<string, string>();
                IDictionary<string, string> applProgHashes = new Dictionary<string, string>();
                IDictionary<string, string> mapBaggageIdToFileIntegrity = new Dictionary<string, string>(50);

                FileInfo applProgFileInfo = new FileInfo(Path.Combine(tempDirectory, mfid, applicationProgramFilename));
                FileInfo hwFileInfo = new FileInfo(Path.Combine(tempDirectory, mfid, "Hardware.xml"));
                FileInfo catalogFileInfo = new FileInfo(Path.Combine(tempDirectory, mfid, "Catalog.xml"));

                ApplicationProgramHasher aph = new ApplicationProgramHasher(applProgFileInfo, mapBaggageIdToFileIntegrity, true);
                aph.HashFile();

                string oldApplProgId = aph.OldApplProgId;
                string newApplProgId = aph.NewApplProgId;
                string genHashString = aph.GeneratedHashString;

                /*
                Console.WriteLine("OldApplProgId: " + oldApplProgId);
                Console.WriteLine("NewApplProgId: " + newApplProgId);
                Console.WriteLine("GeneratedHashString: " + genHashString);
                Console.WriteLine("XML filename: " + applicationProgramFilename);
                */

                applProgIdMappings.Add(oldApplProgId, newApplProgId);
                if (!applProgHashes.ContainsKey(newApplProgId))
                    applProgHashes.Add(newApplProgId, genHashString);

                // Sign Hardware.xml and patch RefIds in Hardware.xml
                HardwareSigner hws = new HardwareSigner(hwFileInfo, applProgIdMappings, applProgHashes, true);
                hws.SignFile();
                IDictionary<string, string> hardware2ProgramIdMapping = hws.OldNewIdMappings;

                // Patch RefIds in Catalog.xml
                CatalogIdPatcher cip = new CatalogIdPatcher(catalogFileInfo, hardware2ProgramIdMapping);
                cip.Patch();

                // Signing directory
                XmlSigning.SignDirectory(Path.Combine(tempDirectory, mfid));
                
                //Check if ZIP already exists and delete
                if (File.Exists(outputFile))
                    File.Delete(outputFile);

                // Create ZIP file (aka knxprod) archive
                ZipFile.CreateFromDirectory(tempDirectory, outputFile);

                // Remove temporary working directory recursively
                Directory.Delete(tempDirectory, true);

                _dialogService.ShowMessage(Ressources.ExportSuccess);
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(ex.ToString());
            }
        }

        private void RaiseChanged()
        {
            RaisePropertyChanged(nameof(EditEnabled));
            RaisePropertyChanged(nameof(HardwareName));
            RaisePropertyChanged(nameof(HardwareSerial));
            RaisePropertyChanged(nameof(HardwareVersion));
            RaisePropertyChanged(nameof(ProductName));
            RaisePropertyChanged(nameof(OrderNumber));
            RaisePropertyChanged(nameof(ApplicationName));
            RaisePropertyChanged(nameof(ApplicationNumber));
            RaisePropertyChanged(nameof(ApplicationVersion));
            RaisePropertyChanged(nameof(ParameterBlock));
            RaisePropertyChanged(nameof(ParameterTypes));
            RaisePropertyChanged(nameof(Parameters));
            RaisePropertyChanged(nameof(ComObjects));
            RaisePropertyChanged(nameof(MediumType));
            RaisePropertyChanged(nameof(ReplacesVersions));
            RaisePropertyChanged(nameof(IsSecureEnabled));
            RaisePropertyChanged(nameof(MaxSecurityIndividualAddressEntries));
            RaisePropertyChanged(nameof(MaxSecurityGroupKeyTableEntries));
            RaisePropertyChanged(nameof(IsRailMounted));
            RaisePropertyChanged(nameof(BusCurrent));
        }

        #region Properties
        public bool EditEnabled
        {
            get => _model != null;
        }

        public string HardwareName
        {
            get
            {
                return _hardware?.Name;
            }
            set
            {
                _hardware.Name = value;
                RaisePropertyChanged(nameof(HardwareName));
            }
        }

        public string HardwareSerial
        {
            get
            {
                return _hardware?.SerialNumber;
            }
            set
            {
                _hardware.SerialNumber = value;
                RaisePropertyChanged(nameof(HardwareSerial));
            }
        }

        public ushort? HardwareVersion
        {
            get => _hardware?.VersionNumber;
            set
            {
                _hardware.VersionNumber = value ?? 0;
                RaisePropertyChanged(nameof(HardwareVersion));
            }
        }

        public string ProductName
        {
            get
            {
                return _product?.Text;
            }
            set
            {
                _product.Text = value;
                _catalogItem.Name = value;
                RaisePropertyChanged(nameof(ProductName));
            }
        }

        public string OrderNumber
        {
            get
            {
                return _product?.OrderNumber;
            }
            set
            {
                _product.OrderNumber = value;
                RaisePropertyChanged(nameof(OrderNumber));
            }
        }

        public string ApplicationName
        {
            get
            {
                return _applicationProgram?.Name;
            }
            set
            {
                _applicationProgram.Name = value;
                RaisePropertyChanged(nameof(ApplicationName));
            }
        }

        public ushort? ApplicationNumber
        {
            get
            {
                return _applicationProgram?.ApplicationNumber;
            }
            set
            {
                _applicationProgram.ApplicationNumber = value ?? 0;
                RaisePropertyChanged(nameof(ApplicationNumber));
            }
        }

        public byte? ApplicationVersion
        {
            get
            {
                return _applicationProgram?.ApplicationVersion;
            }
            set
            {
                _applicationProgram.ApplicationVersion = value ?? 0;
                RaisePropertyChanged(nameof(ApplicationVersion));
            }
        }

        public string ReplacesVersions
        {
            get
            {
                if (_applicationProgram == null)
                    return "";

                return _applicationProgram.ReplacesVersions;
                //return string.Join(" ", _applicationProgram.ReplacesVersions.Select(b => b.ToString()));
            }
            set
            {
                _applicationProgram.ReplacesVersions = value;
                if (string.IsNullOrWhiteSpace(value))
                    _applicationProgram.ReplacesVersions = null;
                //try
                //{
                //    _applicationProgram.ReplacesVersions = value.Split(' ').Select(s => byte.Parse(s)).ToArray();
                //}
                //catch(Exception ex)
                //{
                //    _applicationProgram.ReplacesVersions = null;
                //}

                RaisePropertyChanged(nameof(ReplacesVersions));
            }
        }

        public bool IsSecureEnabled
        {
            get
            {
                if (_applicationProgram == null)
                    return false;

                return _applicationProgram.IsSecureEnabled;
            }
            set
            {
                _applicationProgram.IsSecureEnabled = value;

                RaisePropertyChanged(nameof(IsSecureEnabled));
            }
        }

        public ushort MaxSecurityIndividualAddressEntries
        {
            get
            {
                if (_applicationProgram == null)
                    return 0;

                return _applicationProgram.MaxSecurityIndividualAddressEntries;
            }
            set
            {
                _applicationProgram.MaxSecurityIndividualAddressEntries = value;

                RaisePropertyChanged(nameof(MaxSecurityIndividualAddressEntries));
            }
        }

        public ushort MaxSecurityGroupKeyTableEntries
        {
            get
            {
                if (_applicationProgram == null)
                    return 0;

                return _applicationProgram.MaxSecurityGroupKeyTableEntries;
            }
            set
            {
                _applicationProgram.MaxSecurityIndividualAddressEntries = value;

                RaisePropertyChanged(nameof(MaxSecurityGroupKeyTableEntries));
            }
        }

        public ObservableCollection<ComObjectParameterBlock_T> ParameterBlock
        {
            get => _applicationProgram?.Dynamic?.ChannelIndependentBlock?.First()?.ParameterBlock;
        }

        public bool? IsRailMounted
        {
            get
            {
                return _product?.IsRailMounted;
            }
            set
            {
                _product.IsRailMounted = value ?? false;
                RaisePropertyChanged(nameof(IsRailMounted));
            }
        }

        public float? BusCurrent
        {
            get
            {
                return _hardware?.BusCurrent;
            }
            set
            {
                _hardware.BusCurrent = value ?? 10;
                RaisePropertyChanged(nameof(BusCurrent));
            }
        }

        public ObservableCollection<ParameterType_T> ParameterTypes
        {
            get => _applicationProgram?.Static?.ParameterTypes;
        }

        public ObservableCollection<ApplicationProgramStatic_TParametersParameter> Parameters { get; private set; } = new ObservableCollection<ApplicationProgramStatic_TParametersParameter>();

        public ObservableCollection<ComObject_T> ComObjects
        {
            get => _applicationProgram?.Static?.ComObjectTable?.ComObject;
        }

        public string MediumType
        {
            get => _hardware2Program?.MediumTypes[0];
            set
            {
                _hardware2Program.MediumTypes[0] = value;
                RaisePropertyChanged(nameof(MediumType));
            }
        }

        #endregion

        #region Reflection
        private object InvokeMethod(Type type, string methodName, object[] args)
        {

            var mi = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            return mi.Invoke(null, args);
        }

        private void SetProperty(Type type, string propertyName, object value)
        {
            PropertyInfo prop = type.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Static);
            prop.SetValue(null, value, null);
        }
        #endregion

        #region Commands

        public ICommand ExportCommand
        {
            get => _exportCommand;
        }

        public ICommand CreateNewCommand
        {
            get => _createNewCommand;
        }

        public ICommand OpenCommand
        {
            get => _openCommand;
        }

        public ICommand CloseCommand
        {
            get => _closeCommand;
        }

        public ICommand SaveCommand
        {
            get => _saveCommand;
        }

        public ICommand SaveAsCommand
        {
            get => _saveAsCommand;
        }

        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
        #endregion
    }
}
