using System.IO;

namespace Refit.Core
{
    /// <summary>
    /// multipart/form-data 업로드에서 Stream 파트를 나타낸다.
    /// 파일 이름과 MIME 타입을 명시할 수 있다.
    ///
    /// <example>
    /// [Multipart][Post("/upload")]
    /// Task UploadAsync([AliasAs("photo")] StreamPart photo);
    ///
    /// await api.UploadAsync(new StreamPart(myStream, "photo.jpg", "image/jpeg"));
    /// </example>
    /// </summary>
    public class StreamPart
    {
        public Stream Value { get; }
        public string FileName { get; }
        public string ContentType { get; }

        /// <summary>런타임에 파트 이름을 재정의한다. [AliasAs] 보다 우선순위가 높다.</summary>
        public string Name { get; set; }

        public StreamPart(Stream value, string fileName, string contentType = "application/octet-stream", string name = null)
        {
            Value = value;
            FileName = fileName;
            ContentType = contentType;
            Name = name;
        }
    }
}
