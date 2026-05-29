using System.IO;
using UnityEngine;

namespace Unitrofit.Core
{
    /// <summary>
    /// multipart/form-data 업로드용 바디.
    /// FileInfo 또는 byte[] 로 생성할 수 있다.
    /// </summary>
    public class MultipartBody
    {
        public static readonly string DefaultField    = "file";
        public static readonly string DefaultMimeType = "application/octet-stream";

        public string   Field    { get; }
        public string   FileName { get; }
        public string   MimeType { get; }

        private readonly byte[]    _rawData;
        private readonly FileInfo  _fileInfo;

        // ── 생성자 ──────────────────────────────────────────────────────

        public MultipartBody(byte[] data, string fileName,
            string field    = null,
            string mimeType = null)
        {
            _rawData = data;
            FileName = fileName;
            Field    = field    ?? DefaultField;
            MimeType = mimeType ?? DefaultMimeType;
        }

        public MultipartBody(FileInfo fileInfo,
            string fileName = null,
            string field    = null,
            string mimeType = null)
        {
            _fileInfo = fileInfo;
            FileName  = fileName ?? fileInfo.Name;
            Field     = field    ?? DefaultField;
            MimeType  = mimeType ?? DefaultMimeType;
        }

        // ── 데이터 접근 ─────────────────────────────────────────────────

        public byte[] GetData()
        {
            if (_rawData != null) return _rawData;

            if (_fileInfo != null)
            {
                if (_fileInfo.Length > 5_242_880L)
                    Debug.LogWarning("[Unitrofit] 파일 크기가 5MB를 초과합니다. 청크 업로드를 권장합니다.");
                return File.ReadAllBytes(_fileInfo.FullName);
            }

            return null;
        }
    }
}
