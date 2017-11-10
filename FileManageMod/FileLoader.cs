using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NCE.UTscanner.ModulesCommonData;

namespace NCE.FileManage
{
    /// <summary>
    /// Модуль чтения из файла
    /// </summary>
    public class FileLoader : ISourceBlock<byte[]>, IDisposable, IModule
    {
        /// <summary>
        /// Внутренний буфер
        /// </summary>
        private BufferBlock<byte[]> _innerBlock;
        /// <summary>
        /// Поток файла
        /// </summary>
        private Stream _stream;
        /// <summary>
        /// Размер блока при чтении из файла без учета размера фрейма
        /// </summary>
        private const int _blockToRead = 4096;
        /// <summary>
        /// Количество байт при чтении из файла с учетом размера фрейма
        /// </summary>
        private int _bytesToRead;
        /// <summary>
        /// Фунция чтения из файла
        /// </summary>
        private Task _loadFile;
        private long _startOffset;
        private long _lenght;

        public string ModuleName
        {
            get
            {
                return "FileLoader";
            }
        }


        /// <summary>
        /// Инициализация модуля
        /// </summary>
        /// <param name="file">Путь к файлу</param>
        /// <param name="frameSize">Размер одного фрейма в байтах</param>
        public FileLoader(FileInfo file, int frameSize, long offset, long lenght)
        {
            _innerBlock = new BufferBlock<byte[]>();
            _startOffset = offset;
            _lenght = lenght;
            _stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            _stream.Position = offset;
            _bytesToRead = _blockToRead - _blockToRead % frameSize;
            _loadFile = new Task(LoadSingleTrackAct);
        }
        /// <summary>
        /// Инициализация модуля
        /// </summary>
        /// <param name="stream">Поток с данными</param>
        /// <param name="frameSize">Размер одного фрейма в байтах</param>
        public FileLoader(Stream stream, int frameSize, long offset, long lenght)
        {
            _innerBlock = new BufferBlock<byte[]>();

            _startOffset = offset;
            _lenght = lenght;
            _stream = stream;
            _stream.Position = offset;
            _bytesToRead = _blockToRead - _blockToRead % frameSize;
            _loadFile = new Task(LoadSingleTrackAct);
        }

        #region Dataflow
        public Task Completion => _innerBlock.Completion;

        public void Complete()
        {
            _innerBlock.Complete();
        }

        public byte[] ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<byte[]> target, out bool messageConsumed)
        {
            return ((ISourceBlock<byte[]>)_innerBlock).ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        public void Fault(Exception exception)
        {
            ((ISourceBlock<byte[]>)_innerBlock).Fault(exception);
        }

        public IDisposable LinkTo(ITargetBlock<byte[]> target, DataflowLinkOptions linkOptions)
        {
            return _innerBlock.LinkTo(target, linkOptions);
        }

        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<byte[]> target)
        {
            ((ISourceBlock<byte[]>)_innerBlock).ReleaseReservation(messageHeader, target);
        }

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<byte[]> target)
        {
            return ((ISourceBlock<byte[]>)_innerBlock).ReserveMessage(messageHeader, target);
        }
        #endregion
        /// <summary>
        /// Старт чтения файла
        /// </summary>
        public void StartLoadingSingleTrack()
        {
            _loadFile.Start();
        }
        /// <summary>
        /// Функция чтения из файла
        /// </summary>
        private void LoadSingleTrackAct()
        {
            byte[] res = new byte[_bytesToRead];
            int offset = 0;
            int bytesRead = 0;
            int count = 0;
            //Читаем из файла количество фреймов которое поместилось в _blockToRead
            while ((bytesRead = _stream.Read(res, offset, _bytesToRead)) != 0)
            {
                if (bytesRead == _bytesToRead)
                {
                    byte[] temp = new byte[bytesRead];
                    Array.Copy(res, temp, bytesRead);
                    _innerBlock.Post(temp);
                }
                else//Конец файла\трека
                {
                    long trackData = _stream.Position - (_startOffset + _lenght);
                    byte[] temp = new byte[bytesRead - trackData];
                    Array.Copy(res, temp, bytesRead - trackData);
                    _innerBlock.Post(temp);
                }
                count++;
            }
            _innerBlock.Complete();
            _stream.Close();
            _stream.Dispose();
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
                    if (_stream != null)
                    {
                        _stream.Close();
                        _stream.Dispose();
                    }
                    if (_loadFile != null)
                        _loadFile.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FileLoader() {
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
