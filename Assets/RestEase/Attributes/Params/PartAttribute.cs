using System;

namespace RestEase.Attributes.Params
{
    /// <summary>
    /// multipart/form-data 요청의 파일 파트. [Multipart] 메서드에서만 사용 가능.
    /// 파라미터 타입은 byte[] 또는 FilePart 이어야 한다.
    /// <example>
    /// [Multipart][Post("upload")]
    /// Task UploadAsync([Part("file")] FilePart file);
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class PartAttribute : Attribute
    {
        /// <summary>멀티파트 폼 필드 이름. null 이면 파라미터 이름을 사용.</summary>
        public string Name { get; }
        public PartAttribute(string name = null) { Name = name; }
    }
}
