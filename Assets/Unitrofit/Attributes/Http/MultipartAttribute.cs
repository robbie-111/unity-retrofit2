using System;

namespace Unitrofit.Attributes.Http
{
    /// <summary>
    /// multipart/form-data 요청임을 선언한다.
    /// [Part] 파라미터와 함께 사용해야 한다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class MultipartAttribute : Attribute { }
}
