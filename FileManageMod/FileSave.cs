using NCE.ModulesCommonData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NCE.FileManage
{
    /// <summary>
    /// Модуль сохраннения файла контроля
    /// </summary>
    public class FileSave : IReceivableSourceBlock<byte[]>, ITargetBlock<byte[]>, IDisposable, IRawDataInputModule
    {
        /// <summary>
        /// Внутренний буфер
        /// </summary>
        private BufferBlock<byte[]> _innerBuffer = new BufferBlock<byte[]>();
        /// <summary>
        /// Функция сохраннения данных в файл
        /// </summary>
        private ActionBlock<byte[]> _saveBlock;
        /// <summary>
        /// Стрим файла
        /// </summary>
        private FileStream _dataFile;
        /// <summary>
        /// Количесво сохраненных байт
        /// </summary>
        private long _dataLenght = 0;
        /// <summary>
        /// Дозапись в конец файла
        /// </summary>
        private bool _append = false;

        public bool StreamClosed { get; set; }
        public string ModuleName
        {
            get
            {
                return "FileSave";
            }
        }
        public long DataLenght
        {
            get { return _dataLenght; }
        }

        public Task Completion => _saveBlock.Completion;

        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="file">Путь к файлу</param>
        public FileSave(FileInfo file, bool append)
        {
            _saveBlock = new ActionBlock<byte[]>(new Action<byte[]>(SaveRawData), new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 });
            if(!_append)
                _dataFile = file.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            else
                _dataFile = file.Open(FileMode.Append, FileAccess.ReadWrite, FileShare.Read);
            //PropagateCompletion - обязательный, автоматическая передача заверешения контроля для закрытия файлов
            _innerBuffer.LinkTo(_saveBlock, new DataflowLinkOptions() { PropagateCompletion = true });


            //Закрытие стримов при завершении контроля
            _saveBlock.Completion.ContinueWith(t =>
            {
                _dataFile.Flush();
                _dataLenght = _dataFile.Position;
                _dataFile.Close();
                _dataFile.Dispose();
                StreamClosed = true;                
            }
            );
        }

        #region Data flow block
        public void Complete()
        {
            _innerBuffer.Complete();
        }

        public byte[] ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<byte[]> target, out bool messageConsumed)
        {
            return ((IReceivableSourceBlock<byte[]>)_innerBuffer).ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        public void Fault(Exception exception)
        {
            ((IReceivableSourceBlock<byte[]>)_innerBuffer).Fault(exception);
        }

        public IDisposable LinkTo(ITargetBlock<byte[]> target, DataflowLinkOptions linkOptions)
        {
            return _innerBuffer.LinkTo(target, linkOptions);
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, byte[] messageValue, ISourceBlock<byte[]> source, bool consumeToAccept)
        {
            return ((ITargetBlock<byte[]>)_innerBuffer).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<byte[]> target)
        {
            ((IReceivableSourceBlock<byte[]>)_innerBuffer).ReleaseReservation(messageHeader, target);
        }

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<byte[]> target)
        {
            return ((IReceivableSourceBlock<byte[]>)_innerBuffer).ReserveMessage(messageHeader, target);
        }

        public bool TryReceive(Predicate<byte[]> filter, out byte[] item)
        {
            return _innerBuffer.TryReceive(filter, out item);
        }

        public bool TryReceiveAll(out IList<byte[]> items)
        {
            return _innerBuffer.TryReceiveAll(out items);
        }
        #endregion

        /// <summary>
        /// Добавлениеданных на сохранение без DataFlow модулей (либо сплитером)
        /// </summary>
        /// <param name="raw">Сырые данные, структура данных DataType</param>
        public void PostData(byte[] raw)
        {
            _innerBuffer.Post(raw);
        }
        /// <summary>
        /// Запись данных на диск
        /// </summary>
        /// <param name="raw">Сырые данные, структура данных DataType</param>
        private void SaveRawData(byte[] raw)
        {
            _dataFile.Write(raw, 0, raw.Length);
            _dataFile.Flush();
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
                    if (_dataFile != null)
                    {
                        _dataFile.Close();
                        _dataFile.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FileSave() {
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
        #endregion
    }
}
