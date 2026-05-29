namespace Refit.Core
{
    /// <summary>
    /// multipart/form-data 업로드에서 파일(바이너리) 파트를 나타낸다.
    /// <see cref="StreamPart"/>, <see cref="ByteArrayPart"/> 와 함께 Refit 의 3가지 파트 래퍼 중 하나.
    /// </summary>
    public class FilePart
    {
        public byte[] Data { get; }
        public string FileName { get; }
        public string ContentType { get; }

        /// <summary>런타임에 파트 이름을 재정의한다. [AliasAs] 보다 우선순위가 높다.</summary>
        public string Name { get; set; }

        public FilePart(byte[] data, string fileName, string contentType = "application/octet-stream", string name = null)
        {
            Data = data;
            FileName = fileName;
            ContentType = contentType;
            Name = name;
        }
    }
}
