namespace RestEase.Core
{
    /// <summary>
    /// multipart/form-data 업로드 시 파일 정보를 담는 데이터 클래스.
    /// </summary>
    public class FilePart
    {
        /// <summary>폼 필드 이름 (서버에서 파일을 식별하는 key)</summary>
        public string FieldName { get; set; }

        /// <summary>업로드할 파일의 원본 이름</summary>
        public string FileName { get; set; }

        /// <summary>MIME 타입 (예: "image/jpeg", "application/octet-stream")</summary>
        public string MimeType { get; set; }

        /// <summary>파일 바이너리 데이터</summary>
        public byte[] Data { get; set; }

        public FilePart(string fieldName, string fileName, byte[] data, string mimeType = "application/octet-stream")
        {
            FieldName = fieldName;
            FileName = fileName;
            Data = data;
            MimeType = mimeType;
        }
    }
}
