using System;

namespace Refit.Attributes.Params
{
    /// <summary>
    /// [Multipart] 메서드에서 일반 값(string, int 등)을 폼 파트로 추가한다.
    /// 파일 업로드에는 <c>StreamPart</c>, <c>ByteArrayPart</c>, <c>FilePart</c> 를 사용한다.
    ///
    /// <example>
    /// [Multipart]
    /// [Post("/upload")]
    /// Task UploadAsync([AliasAs("file")] StreamPart file, [Part] string description);
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class PartAttribute : Attribute { }
}
