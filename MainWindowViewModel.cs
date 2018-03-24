using CreateKnxProd.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;

namespace CreateKnxProd
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private RelayCommand _exportCommand;
        private RelayCommand _createNewCommand;
        private RelayCommand _openCommand;
        private RelayCommand _saveCommand;
        private RelayCommand _closeCommand;

        private RelayCommand _addParameterCommand;
        private RelayCommand _delParameterCommand;

        private RelayCommand _addCommObjCommand;
        private RelayCommand _delCommObjCommand;

        private IDialogService _dialogService;

        private const string _toolName = "KNX MT";
        private const string _toolVersion = "5.6.407.26745";

        private KNX _model = null;

        public MainWindowViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            _exportCommand = new RelayCommand(o => true, Export);
            _saveCommand = new RelayCommand(o => true, Save);
            _openCommand = new RelayCommand(o => true, Dummy);
            _addParameterCommand = new RelayCommand(o => true, Dummy);
            _delParameterCommand = new RelayCommand(o => true, Dummy);
            _createNewCommand = new RelayCommand(o => true, CreateNew);
            _addCommObjCommand = new RelayCommand(o => true, Dummy);
            _delCommObjCommand = new RelayCommand(o => true, Dummy);
            _closeCommand = new RelayCommand(o => true, Dummy);
        }

        private void Dummy(object param)
        {
            _dialogService.ShowMessage("Dummy");
        }

        private void Save(object param)
        {
            if (_model == null)
                return;

            CorrectIds();

            var filepath = _dialogService.ChooseFile(".xml", "XML Datei|*.xml");
            if (filepath == null)
                return;

            XmlSerializer serializer = new XmlSerializer(typeof(KNX));
            var xmlWriter = new StreamWriter(filepath, false, Encoding.Default);
            serializer.Serialize(xmlWriter, _model);
            xmlWriter.Close();

            _dialogService.ShowMessage("Speichern erfolgreich!");
        }

        private ManufacturerData_tManufacturer _manufacturerData = new ManufacturerData_tManufacturer();
        private Hardware_t _hardware = new Hardware_t();
        private Hardware_tProduct _product = new Hardware_tProduct();
        private CatalogSection_tCatalogItem _catalogItem = new CatalogSection_tCatalogItem();
        private CatalogSection_t _catalogSection = new CatalogSection_t();
        private ApplicationProgram_t _applicationProgram = new ApplicationProgram_t();
        private Hardware2Program_t _hardware2Program = new Hardware2Program_t();
        private ApplicationProgramRef_t _appProgRef = new ApplicationProgramRef_t();

        private void CreateNew(object param)
        {
            if (_model != null)
                _dialogService.ShowMessage("Es darf keine Datei geöffnet sein.");

            _model = new KNX();


            _manufacturerData = new ManufacturerData_tManufacturer();
            _applicationProgram = new ApplicationProgram_t();
            _hardware = new Hardware_t();
            _catalogSection = new CatalogSection_t();
            _product = new Hardware_tProduct();
            _hardware2Program = new Hardware2Program_t();
            _appProgRef = new ApplicationProgramRef_t();
            _catalogItem = new CatalogSection_tCatalogItem();

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
            _applicationProgram.ProgramType = ApplicationProgramType_t.ApplicationProgram;
            _applicationProgram.MaskVersion = "MV-57B0";
            _applicationProgram.LoadProcedureStyle = LoadProcedureStyle_t.MergedProcedure;
            _applicationProgram.PeiType = 0;
            _applicationProgram.DefaultLanguage = "de_DE";
            _applicationProgram.DynamicTableManagement = true;
            _applicationProgram.Linkable = false;
            _applicationProgram.MinEtsVersion = "4.0";

            var appStatic = new ApplicationProgramStatic_t();
            _applicationProgram.Static = appStatic;

            HardwareSerial = "0";
            HardwareVersion = 0;
            _hardware.HasIndividualAddress = true;
            _hardware.HasApplicationProgram = true;
            _hardware.IsIPEnabled = true;

            _product.IsRailMounted = false;
            _product.DefaultLanguage = "de_DE";
            OrderNumber = "0";
            
            _hardware2Program.MediumTypes.Add("MT-5");

            _catalogSection.Name = "Geräte";
            _catalogSection.Number = "1";
            _catalogSection.DefaultLanguage = "de_DE";
            
            _catalogItem.Name = _product.Text;
            _catalogItem.Number = 1;
            _catalogItem.ProductRefId = _product.Id;
            _catalogItem.Hardware2ProgramRefId = _hardware2Program.Id;
            _catalogItem.DefaultLanguage = "de_DE";

            CorrectIds();

        }

        private void CorrectIds()
        {
            _manufacturerData.RefId = "M-00FA";
            _applicationProgram.Id = string.Format("{0}_A-{1:0000}-{2:00}-0000", _manufacturerData.RefId,
                    _applicationProgram.ApplicationNumber, _applicationProgram.ApplicationVersion);
            _appProgRef.RefId = _applicationProgram.Id;
            _hardware.Id = string.Format("{0}_H-{1}-{2}", _manufacturerData.RefId, _hardware.SerialNumber,
                                        _hardware.VersionNumber);
            _product.Id = string.Format("{0}_P-{1}", _hardware.Id, _product.OrderNumber);
            _hardware2Program.Id = string.Format("{0}_HP-{1:0000}-{2:00}-0000", _hardware.Id,
                        _applicationProgram.ApplicationNumber, _applicationProgram.ApplicationVersion);

            _catalogSection.Id = string.Format("{0}_CS-{1}", _manufacturerData.RefId, _catalogSection.Number);

            _catalogItem.Id = string.Format("{0}_CI-{1}-{2}", _hardware2Program.Id, _product.OrderNumber, _catalogItem.Number);
            _catalogItem.ProductRefId = _product.Id;
            _catalogItem.Hardware2ProgramRefId = _hardware2Program.Id;
        }

        private void Export(object param)
        {
            try
            {
                var files = new string[] { "Catalog.mtxml", "Hardware.mtxml", "ApplicationProgram.mtxml" };
                var outputFile = "TestRef.knxprod";
                var toolName = "MT";
                var toolVersion = "5.6.407.26745";

                var asm = Assembly.LoadFrom("Knx.Ets.Converter.ConverterEngine.dll");
                var eng = asm.GetType("Knx.Ets.Converter.ConverterEngine.ConverterEngine");
                var bas = asm.GetType("Knx.Ets.Converter.ConverterEngine.ConvertBase");

                //ConvertBase.Uninitialize();
                InvokeMethod(bas, "Uninitialize", null);
                
                //documentSet = ConverterEngine.BuildUpRawDocumentSet(fileList);
                var dset = InvokeMethod(eng, "BuildUpRawDocumentSet", new object[] { files });

                //ConverterEngine.CheckOutputFileName(outputFileName, ".knxprod");
                InvokeMethod(eng, "CheckOutputFileName", new object[] { outputFile, ".knxprod" });

                //ConvertBase.CleanUnregistered = true;
                SetProperty(bas, "CleanUnregistered", false);
                
                //DocumentSet documentSet = ConverterEngine.ReOrganizeDocumentSet(docSet);
                dset = InvokeMethod(eng, "ReOrganizeDocumentSet", new object[] { dset });

                //CheckSignaturesForKnxprod
                //ConverterEngine.UpdateSignaturesForKnxprodFromTrustedSource(documentSet);
                InvokeMethod(eng, "CheckSignaturesForKnxprod", new object[] { dset });

                //ConverterEngine.PersistDocumentSetAsXmlOutput(documentSet, outputFileName, externalFiles, string.Empty, true, createdBy, toolVersion);
                InvokeMethod(eng, "PersistDocumentSetAsXmlOutput", new object[] { dset, outputFile, null,
                            "", true, toolName, toolVersion });
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage(ex.ToString());
            }
        }

        #region Properties
        public string HardwareName
        {
            get
            {
                return _hardware.Name;
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
                return _hardware.SerialNumber;
            }
            set
            {
                _hardware.SerialNumber = value;
                RaisePropertyChanged(nameof(HardwareSerial));
            }
        }

        public ushort HardwareVersion
        {
            get => _hardware.VersionNumber;
            set
            {
                _hardware.VersionNumber = value;
                RaisePropertyChanged(nameof(HardwareVersion));
            }
        }

        public string ProductName
        {
            get
            {
                return _product.Text;
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
                return _product.OrderNumber;
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
                return _applicationProgram.Name;
            }
            set
            {
                _applicationProgram.Name = value;
                RaisePropertyChanged(nameof(ApplicationName));
            }
        }

        public ushort ApplicationNumber
        {
            get
            {
                return _applicationProgram.ApplicationNumber;
            }
            set
            {
                _applicationProgram.ApplicationNumber = value;
                RaisePropertyChanged(nameof(ApplicationNumber));
            }
        }

        public byte ApplicationVersion
        {
            get
            {
                return _applicationProgram.ApplicationVersion;
            }
            set
            {
                _applicationProgram.ApplicationVersion = value;
                RaisePropertyChanged(nameof(ApplicationVersion));
            }
        }
        #endregion

        #region Reflection
        private object InvokeMethod(Type type, string methodName, object[]  args)
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
            get
            {
                return _exportCommand;
            }
        }

        public ICommand CreateNewCommand
        {
            get
            {
                return _createNewCommand;
            }
        }

        public ICommand OpenCommand
        {
            get
            {
                return _openCommand;
            }
        }

        public ICommand CloseCommand
        {
            get
            {
                return _closeCommand;
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand;
            }
        }

        public ICommand AddParameterCommand
        {
            get
            {
                return _addParameterCommand;
            }
        }

        public ICommand DelParameterCommand
        {
            get
            {
                return _delParameterCommand;
            }
        }

        public ICommand AddCommObjCommand
        {
            get
            {
                return _addCommObjCommand;
            }
        }

        public ICommand DelCommObjCommand
        {
            get
            {
                return _delCommObjCommand;
            }
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

        class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }
    }
}
