using System;

namespace Refit.Attributes.Http
{
    /// <summary>
    /// POST/PUT/PATCH 메서드를 multipart/form-data 로 전송한다.
    /// boundary 를 지정하지 않으면 기본값 "----RefitBoundary" 가 사용된다.
    ///
    /// <example>
    /// [Multipart]
    /// [Post("/upload")]
    /// Task UploadAsync([AliasAs("file")] StreamPart stream);
    ///
    /// [Multipart("myCustomBoundary")]
    /// [Post("/upload")]
    /// Task UploadAsync([AliasAs("file")] StreamPart stream);
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class MultipartAttribute : Attribute
    {
        public const string DefaultBoundary = "----RefitBoundary";
        public string Boundary { get; }
        public MultipartAttribute(string boundary = DefaultBoundary) { Boundary = boundary; }
    }
}
