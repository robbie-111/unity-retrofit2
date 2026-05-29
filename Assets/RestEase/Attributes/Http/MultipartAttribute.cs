using System;

namespace RestEase.Attributes.Http
{
    /// <summary>
    /// POST/PUT/PATCH 메서드를 multipart/form-data 로 전송한다.
    /// [Part] 파라미터와 함께 사용해야 한다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class MultipartAttribute : Attribute { }
}
