using NCE.ModulesCommonData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NCE.UTscanner.FileManageMod.Additional_files.Ascan_and_FFT
{
    public class FileSaveZedGraphPointsXY : IReceivableSourceBlock<double[]>, ITargetBlock<double[]>, IModule, IDisposable
    {
        /// <summary>
        /// Внутренний буфер
        /// </summary>
        private BufferBlock<double[]> _innerBuffer = new BufferBlock<double[]>();
        /// <summary>
        /// Функция сохраннения данных в файл
        /// </summary>
        private ActionBlock<double[]> _saveBlock;
        /// <summary>
        /// Стрим файла
        /// </summary>
        private FileStream _dataFile;
        private bool _streamClosed;
        /// <summary>
        /// Количесво сохраненных байт
        /// </summary>
        private long _dataLenght = 0;
        private int _blockSize = 0;
        private BinaryWriter _bWriter;

        public bool StreamClosed { get => _streamClosed; private set => _streamClosed = value; }

        public long DataLenght
        {
            get { return _dataLenght; }
        }


        public FileSaveZedGraphPointsXY(FileInfo file, int blockSize)
        {
            _blockSize = blockSize;
            _saveBlock = new ActionBlock<double[]>(new Action<double[]>(SavePoints), new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 });

            _dataFile = file.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            _bWriter = new BinaryWriter(_dataFile);
            //PropagateCompletion - обязательный, автоматическая передача заверешения контроля для закрытия файлов
            _innerBuffer.LinkTo(_saveBlock, new DataflowLinkOptions() { PropagateCompletion = true });//Закрытие стримов при завершении контроля
            _saveBlock.Completion.ContinueWith(t =>
            {
                _bWriter.Flush();
                _dataFile.Flush();
                _dataLenght = _dataFile.Position;
                _bWriter.Close();
                _bWriter.Dispose();
                _dataFile.Close();
                _dataFile.Dispose();
                _streamClosed = true;
            }
            );
        }

        public Task Completion => _saveBlock.Completion;

        public string ModuleName => throw new NotImplementedException();
   
        public void Complete()
        {
            _innerBuffer.Complete();
        }

        #region Dataflow
        public double[] ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<double[]> target, out bool messageConsumed)
        {
            return ((IReceivableSourceBlock<double[]>)_innerBuffer).ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        public void Fault(Exception exception)
        {
            ((IReceivableSourceBlock<double[]>)_innerBuffer).Fault(exception);
        }

        public IDisposable LinkTo(ITargetBlock<double[]> target, DataflowLinkOptions linkOptions)
        {
            return _innerBuffer.LinkTo(target, linkOptions);
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, double[] messageValue, ISourceBlock<double[]> source, bool consumeToAccept)
        {
            return ((ITargetBlock<double[]>)_innerBuffer).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<double[]> target)
        {
            ((IReceivableSourceBlock<double[]>)_innerBuffer).ReleaseReservation(messageHeader, target);
        }

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<double[]> target)
        {
            return ((IReceivableSourceBlock<double[]>)_innerBuffer).ReserveMessage(messageHeader, target);
        }

        public bool TryReceive(Predicate<double[]> filter, out double[] item)
        {
            return _innerBuffer.TryReceive(filter, out item);
        }

        public bool TryReceiveAll(out IList<double[]> items)
        {
            return _innerBuffer.TryReceiveAll(out items);
        }
        #endregion

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
                    if (_bWriter != null)
                    {
                        _bWriter.Close();
                        _bWriter.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        /* TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FileSaveZedGraphPointsXY() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }*/

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        /// <summary>
        /// Save points to disk (array lenght must be multiple of blockSize)
        /// </summary>
        /// <param name="pointsBlocks"></param>
        public void PostData(double[] pointsBlocks)
        {
            _innerBuffer.Post(pointsBlocks);
        }

        private void SavePoints(double[] points)
        {
            if (points.Length % _blockSize != 0)
            {
                throw new ArgumentOutOfRangeException("points", "Input array is not multipole of block size!");
            }
            for (int i = 0; i < points.Length; i ++)
            {
                _bWriter.Write(points[i]);
            }
            _bWriter.Flush();
        }
    }
}
