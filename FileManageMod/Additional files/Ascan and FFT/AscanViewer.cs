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
        ///// <summary>
        ///// Ascan count
        ///// </summary>
        //private long _count;
        ///// <summary>
        ///// Ascan size
        ///// </summary>
        //private int _ascanSize;
        private Dictionary<int, List<long>> _ascanOffsetsByChannell;// = new Dictionary<int, long>();

        public AscanViewer(FileInfo file, DataTypeManager manager)
        {
            _manager = manager;
            _stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            long count = _stream.Length / manager.FrameSize;
            _ascanOffsetsByChannell = count * 2 < int.MaxValue ?  new Dictionary<int, List<long>>((int)count * 2) : new Dictionary<int, List<long>>(int.MaxValue);

            CreateAscanOffsetsByChannel(_stream, manager.FrameSize);

            //_ascanSize = AscanSize(_stream, manager);
        }

        public int GetAscanCount(int channelId)
        {
            if (_ascanOffsetsByChannell.ContainsKey(channelId))
                return _ascanOffsetsByChannell[channelId].Count;
            else
                return 0;
        }

        public bool GetFrameByAscanIdx(int channelId, int ascanIdx, out byte[] frame)
        {
            bool res = false;
            frame = null;
            if (_ascanOffsetsByChannell.ContainsKey(channelId) && ascanIdx < _ascanOffsetsByChannell[channelId].Count)
            {
                try
                {
                    frame = new byte[_manager.FrameSize];
                    _stream.Position = _ascanOffsetsByChannell[channelId][ascanIdx];
                    _stream.Read(frame, 0, frame.Length);
                    res = true;
                }
                catch { frame = null; }
            }
            return res;
        }

        private void CreateAscanOffsetsByChannel(Stream file, int frameSize)
        {
            int offsetToNext = frameSize - 1;
            file.Position = 0;
            int id = 0;
            while (_stream.Position < file.Length)
            {
                id = file.ReadByte();
                if (!_ascanOffsetsByChannell.ContainsKey(id))
                {
                    _ascanOffsetsByChannell[id] = new List<long>();
                }
                _ascanOffsetsByChannell[id].Add(file.Position - 1);
                file.Seek(offsetToNext, SeekOrigin.Current);
            }
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
