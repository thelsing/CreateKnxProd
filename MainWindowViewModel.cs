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

            var filepath = _dialogService.ChooseFile(".xml", "XML Datei|*.xml");
            if (filepath == null)
                return;

            XmlSerializer serializer = new XmlSerializer(typeof(KNX));
            TextWriter textWriter = new StreamWriter(filepath);
            serializer.Serialize(textWriter, _model);
            textWriter.Close();

            _dialogService.ShowMessage("Speichern erfolgreich!");
        }

        private Hardware_t _hardware;
        private Hardware_tProduct _product;
        private CatalogSection_tCatalogItem _catalogItem;
        private ApplicationProgram_t _applicationProgram;

        private void CreateNew(object param)
        {
            if (_model != null)
                _dialogService.ShowMessage("Es darf keine Datei geöffnet sein.");

            _model = new KNX();


            var manufacturerData = new ManufacturerData_tManufacturer();
            _applicationProgram = new ApplicationProgram_t();
            _hardware = new Hardware_t();
            var catalogSection = new CatalogSection_t();
            _product = new Hardware_tProduct();
            var hardware2Program = new Hardware2Program_t();
            var appProgRef = new ApplicationProgramRef_t();
            _catalogItem = new CatalogSection_tCatalogItem();

            _model.ManufacturerData.Add(manufacturerData);
            manufacturerData.Catalog.Add(catalogSection);
            manufacturerData.ApplicationPrograms.Add(_applicationProgram);
            manufacturerData.Hardware.Add(_hardware);
            _hardware.Products.Add(_product);
            _hardware.Hardware2Programs.Add(hardware2Program);
            hardware2Program.ApplicationProgramRef.Add(appProgRef);
            catalogSection.CatalogItem.Add(_catalogItem);


            _model.CreatedBy = _toolName;
            _model.ToolVersion = _toolVersion;

            _applicationProgram.ApplicationNumber = 0;
            manufacturerData.RefId = "M-00FA";

            _applicationProgram.ApplicationVersion = 0;
            _applicationProgram.ProgramType = ApplicationProgramType_t.ApplicationProgram;
            _applicationProgram.MaskVersion = "MV-57B0";
            _applicationProgram.LoadProcedureStyle = LoadProcedureStyle_t.MergedProcedure;
            _applicationProgram.PeiType = 0;
            _applicationProgram.DefaultLanguage = "de_DE";
            _applicationProgram.DynamicTableManagement = true;
            _applicationProgram.Linkable = false;
            _applicationProgram.MinEtsVersion = "4.0";
            _applicationProgram.Id = string.Format("{0}_A-{1:0000}-{2:00}-0000", manufacturerData.RefId,
                    _applicationProgram.ApplicationNumber, _applicationProgram.ApplicationVersion);

            appProgRef.RefId = _applicationProgram.Id;

            _hardware.SerialNumber = "0";
            _hardware.VersionNumber = 0;
            _hardware.HasIndividualAddress = true;
            _hardware.HasApplicationProgram = true;
            _hardware.IsIPEnabled = true;
            _hardware.Id = string.Format("{0}_H-{1}-{2}", manufacturerData.RefId, _hardware.SerialNumber,
                _hardware.VersionNumber);

            _product.IsRailMounted = false;
            _product.DefaultLanguage = "de_DE";
            _product.OrderNumber = "0";
            _product.Id = string.Format("{0}_P-{1}", _hardware.Id, _product.OrderNumber);

            hardware2Program.MediumTypes.Add("MT-5");
            hardware2Program.Id = string.Format("{0}_HP-{1:0000}-{2:00}-0000", _hardware.Id,
                _applicationProgram.ApplicationNumber, _applicationProgram.ApplicationVersion);

            catalogSection.Name = "Geräte";
            catalogSection.Number = "1";
            catalogSection.DefaultLanguage = "de_DE";
            catalogSection.Id = string.Format("{0}_CS-{1}", manufacturerData.RefId, catalogSection.Number);

            _catalogItem.Name = _product.Text;
            _catalogItem.Number = 1;
            _catalogItem.ProductRefId = _product.Id;
            _catalogItem.Hardware2ProgramRefId = hardware2Program.Id;
            _catalogItem.DefaultLanguage = "de_DE";
            _catalogItem.Id = string.Format("{0}_CI-{1}-{2}", hardware2Program.Id, _product.OrderNumber, _catalogItem.Number);


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
    }
}
