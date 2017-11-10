using NCE.ModulesCommonData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NCE.FileManage
{
    /// <summary>
    /// Менеджер по работе с файлами
    /// </summary>
    [KnownType("GetKnownTypes")]
    public class FileManager<T> : IRawDataInputModule, IDisposable where T : class
    {
        static Type[] GetKnownTypes()
        {

            return new Type[] { typeof(ConfigFile<T>) };

        }

        private readonly Version _fileVersion = new Version(1, 0, 0, 0);
        /// <summary>
        /// Модуль сохранения файла контроля
        /// </summary>
        private FileSave _fileSaver;
        /// <summary>
        /// Модуль загрузки файла контроля
        /// </summary>
        private FileLoader _fileLoader;
        /// <summary>
        /// Настойки файла контроля
        /// </summary>
        private ConfigFile<T> _configFile;
        /// <summary>
        /// Путь к папке с сохраненными файлами
        /// </summary>
        private const string _saveToFolder = "Result";
        /// <summary>
        /// Путь к папке с временными файлами
        /// </summary>
        private const string _tempFolder = "Data";
        /// <summary>
        /// Флаг сохранения файла
        /// </summary>
        private bool _fileSaved = false;
        /// <summary>
        /// Файл с данными
        /// </summary>
        private FileInfo _dataFile;
        /// <summary>
        /// Конфиг файл
        /// </summary>
        private FileInfo _dataConfigFile;
        /// <summary>
        /// Флаг сохранения файла
        private bool _configSaved = false;

        ///// <summary>
        ///// Модуль сохранения файла контроля
        ///// </summary>
        //public FileSave FileSaver
        //{
        //    get
        //    {
        //        return _fileSaver;
        //    }
        //}
        /// <summary>
        /// True если файл уже записан
        /// </summary>
        public bool DataFileReady
        { get { return _fileSaver.StreamClosed; } }
        public long DataLenght
        { get { return _fileSaver.DataLenght; } }
        public Task Completion => ((IRawSplitterTarget)_fileSaver).Completion;
        public string ModuleName
        { get { return _fileSaver.ModuleName; } }

        /// <summary>
        /// Инициализация и создание директорий
        /// </summary>
        public FileManager()
        {
            //TODO clear all temp files
            //if (!Directory.Exists(_tempFolder))
            //    Directory.CreateDirectory(_tempFolder);
            if (!Directory.Exists(_saveToFolder))
                Directory.CreateDirectory(_saveToFolder);
        }
        /// <summary>
        /// Сохраняет настройки файла контроля и подготавливает модуль сохранения файла контроля 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="config"></param>
        /// <param name="infoData"></param>
        public void StartNewFile(string fileName, bool append)
        {
            _fileSaved = false;
            _configSaved = false;
            if (!_fileSaved)
            {
                //TODO remove not saved file
            }

            //string file;
            //do
            //{
            //    file = System.IO.Path.GetRandomFileName() + ".dat";
            //}
            //while (File.Exists(Path.Combine(_tempFolder, file)));

            _dataFile = new FileInfo(Path.Combine(_saveToFolder, fileName + ".dat"));
            _dataConfigFile = new FileInfo(_dataFile.FullName + ".cfg");
            

            _fileSaver = new FileSave(_dataFile, append);
            _fileSaver.Completion.ContinueWith((x) => { _fileSaved = true; });
        }
        /// <summary>
        /// Загрузка файла контроля
        /// </summary>
        /// <param name="file">Путь к файлу контроля</param>
        /// <param name="frameSize">Размер фрейма</param>
        /// <param name="target">Модуль который будет получатьзагруженные данные</param>
        public void LoadFile(Stream file, int frameSize, ITargetBlock<byte[]> target, long startOffset, long lenght)
        {
            _fileLoader = new FileLoader(file, frameSize, startOffset, lenght);
            //PropagateCompletion - обязательный, автоматическа передача окончания загрузке файла по цепочке модулей
            _fileLoader.LinkTo(target, new DataflowLinkOptions() { PropagateCompletion = true });
            _fileLoader.StartLoadingSingleTrack();
        }
        /// <summary>
        /// Загрузка файла контроля
        /// </summary>
        /// <param name="file">Путь к файлу контроля</param>
        /// <param name="frameSize">Размер фрейма</param>
        /// <param name="target">Модуль который будет получатьзагруженные данные</param>
        public void LoadFile(FileInfo file, int frameSize, ITargetBlock<byte[]> target, long startOffset, long lenght)
        {
            _fileLoader = new FileLoader(file, frameSize, startOffset, lenght);
            //PropagateCompletion - обязательный, автоматическа передача окончания загрузке файла по цепочке модулей
            _fileLoader.LinkTo(target, new DataflowLinkOptions() { PropagateCompletion = true });
            _fileLoader.StartLoadingSingleTrack();
        }
        /// <summary>
        /// Загрузка конфиг файла контроля
        /// </summary>
        /// <param name="file">Путь к файлу</param>
        /// <returns>Десериализованый конфиг</returns>
        public ConfigFile<T> LoadConfig()
        {
            //if (Path.GetExtension(file).ToLowerInvariant() == ".dat")
            //    file += ".config";
            //else if (Path.GetExtension(file).ToLowerInvariant() != ".dat.config")
            //{
            //    return null;
            //}

            if (!_dataConfigFile.Exists)
                return null;

            using (FileStream fS = _dataConfigFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                DataContractSerializer ser = new DataContractSerializer(typeof(ConfigFile<T>));
                return ser.ReadObject(fS) as ConfigFile<T>;
            }
        }
        /// <summary>
        /// Загрузка конфиг файла контроля
        /// </summary>
        /// <param name="file">Путь к файлу</param>
        /// <returns>Десериализованый конфиг</returns>
        public ConfigFile<T> LoadConfig(Stream stream)
        {
            if (stream != null)
            {
                stream.Position = 0;
                DataContractSerializer ser = new DataContractSerializer(typeof(ConfigFile<T>));
                return ser.ReadObject(stream) as ConfigFile<T>;
            }
            return null;
        }
        /// <summary>
        /// Запись сериализованого конфиг файла
        /// </summary>
        /// <param name="configFile">Конфиг класс</param>
        /// <param name="file">Путь куда сохранняем файл</param>
        public void WriteConfigFile(T settings, double barrierCoord, long[][] binnaryByteLenght)
        {
            _configFile = new ConfigFile<T>(settings, _fileVersion, barrierCoord, binnaryByteLenght);
            FileStream writer = _dataConfigFile.Open(FileMode.Create);
            DataContractSerializer ser = new DataContractSerializer(typeof(ConfigFile<T>));
            ser.WriteObject(writer, _configFile);
            writer.Flush();
            writer.Close();
            _configSaved = true;
        }

        /// <summary>
        /// Возвращает путь к сохраненным файлам
        /// </summary>
        /// <param name="dataFile">Путь к данным</param>
        /// <param name="configFile">Путь к конфигу</param>
        /// <returns>True если оба файлы сохраннены</returns>
        public bool GetSavedFilesPath(out FileInfo dataFile, out FileInfo configFile)
        {
            bool res = _fileSaved && _configSaved;
            if (res)
            {
                //dataFile = _dataFile.FullName;
                //configFile = _dataConfigFile.FullName;
                dataFile = _dataFile;
                configFile = _dataConfigFile;
            }
            else
            {
                dataFile = null;
                configFile = null;
            }
            return res;
        }

        public void WaitFileReady()
        {
            Task.WaitAll(new Task[] {_fileSaver.Completion });
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (_fileSaver != null)
                        _fileSaver.Dispose();
                    if (_fileLoader != null)
                        _fileLoader.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FileManager() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        public void PostData(byte[] raw)
        {
            ((IRawSplitterTarget)_fileSaver).PostData(raw);
        }

        public void Complete()
        {
            ((IRawSplitterTarget)_fileSaver).Complete();
        }

        public void Fault(Exception exception)
        {
            ((IRawSplitterTarget)_fileSaver).Fault(exception);
        }
        #endregion
    }

    /// <summary>
    /// Класс для конфига на диск
    /// </summary>
    [DataContract]
    public class ConfigFile<T> where T : class
    {
        [DataMember]
        private T _settings;

        [DataMember]
        private Version _fileVersion;

        [DataMember]
        private double _deadZoneEnd;

        [DataMember]
        private BinaryFileParams _binaryDataParams;

        //[DataMember]
        //private ConfigurationType _config;
        //[DataMember]
        //private InfoData _infoData;

        public double LightBarrierCoord
        {
            get
            {
                return _deadZoneEnd;
            }
        }
        public T Settings
        {
            get
            {
                return _settings;
            }
        }
        public BinaryFileParams BinaryOffsets
        {
            get { return _binaryDataParams; }
        }

        public ConfigFile(T settings, Version fileVersion, double deadZoneEnd, long[][] binaryLenght)//, ConfigurationType config, InfoData infoData)
        {
            _settings = settings;
            _fileVersion = fileVersion;
            _deadZoneEnd = deadZoneEnd;
            _binaryDataParams = new BinaryFileParams(binaryLenght);
            //_config = config;
            //_infoData = infoData;
        }
    }

    [DataContract]
    public class BinaryFileParams
    {
        /// <summary>
        /// Офсеты разных частей бинарного файла, [конфигурация][трек]
        /// </summary>
        [DataMember]
        private BinaryDataOffset[][] _binaryOffset;

        public BinaryDataOffset[][] BinaryOffsets
        { get { return _binaryOffset; } }

        public BinaryFileParams(long[][] lenght)
        {
            _binaryOffset = ModulesCommonData.ArrayExtensions.CreateJaggedArray<BinaryDataOffset[][]>(lenght.Length, lenght[0].Length);

            long prevOffset = 0;
            for (int configuration = 0; configuration < lenght.Length; configuration++)
            {
                for (int track = 0; track < lenght[configuration].Length; track++)
                {
                    _binaryOffset[configuration][track] = new BinaryDataOffset();
                    _binaryOffset[configuration][track].Lenght = lenght[configuration][track];
                    _binaryOffset[configuration][track].Offset = prevOffset;
                    prevOffset += lenght[configuration][track];
                }
            }
        }
    }

    /// <summary>
    /// Класс для описания офсетов
    /// </summary>
    [DataContract]
    public class BinaryDataOffset
    {
        [DataMember]
        private long _offset = 0;
        [DataMember]
        private long _lenght = 0;
        [DataMember]
        private bool _set = false;

        public long Offset { get { return _offset; } set { _offset = value; } }
        public long Lenght { get { return _lenght; } set { _lenght = value; _set = true; } }
        public bool Set { get { return _set; } }
    }
}
