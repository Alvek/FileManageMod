using NCE.CommonData;
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
    public class AscanViewer : IDisposable
    {
        /// <summary>
        /// Поток файла
        /// </summary>
        private Stream _stream;
        /// <summary>
        /// To detect redundant calls
        /// </summary>
        private bool disposedValue = false;
        /// <summary>
        /// Manager
        /// </summary>
        private DataTypeManager _manager;
        /// <summary>
        /// Ascan count
        /// </summary>
        private long _count;
        ///// <summary>
        ///// Ascan size
        ///// </summary>
        //private int _ascanSize;

        public AscanViewer(FileInfo file, DataTypeManager manager)
        {
            _manager = manager;
            _stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            _count = _stream.Length / manager.FrameSize;
            //_ascanSize = AscanSize(_stream, manager);
        }

        public long GetAscanCount()
        {
            return _count;
        }

        public byte[] GetFrameByAscanIdx(int idx)
        {
            byte[] frame = new byte[_manager.FrameSize];
            _stream.Position = idx * _manager.FrameSize;
            _stream.Read(frame, 0, frame.Length);
            return frame;
        }

        //private int AscanSize(Stream s, DataTypeManager manager)
        //{
        //    byte[] value = new byte[2];
        //    _stream.Position = 0;
        //    _stream.Seek(_manager.AscanPointCountOffset, SeekOrigin.Current);
        //    value[0] = (byte)s.ReadByte();
        //    value[1] = (byte)s.ReadByte();
        //    return BitConverter.ToInt16(value, 0);
        //}
        
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        
        #region IDisposable Support

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
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        /* TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AscanViewer() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }*/

        #endregion
    }
}
