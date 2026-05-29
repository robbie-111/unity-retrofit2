namespace Refit.Core
{
    /// <summary>
    /// multipart/form-data 업로드에서 byte[] 파트를 나타낸다.
    /// 파일 이름과 MIME 타입을 명시할 수 있다.
    ///
    /// <example>
    /// await api.UploadAsync(new ByteArrayPart(fileBytes, "document.pdf", "application/pdf"));
    /// </example>
    /// </summary>
    public class ByteArrayPart
    {
        public byte[] Value { get; }
        public string FileName { get; }
        public string ContentType { get; }

        /// <summary>런타임에 파트 이름을 재정의한다. [AliasAs] 보다 우선순위가 높다.</summary>
        public string Name { get; set; }

        public ByteArrayPart(byte[] value, string fileName, string contentType = "application/octet-stream", string name = null)
        {
            Value = value;
            FileName = fileName;
            ContentType = contentType;
            Name = name;
        }
    }
}
